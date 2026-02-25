using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.UpdateBudget;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Testing;

namespace Kakeibo.Tests.Features.Budgets.UpdateBudget;

public sealed class UpdateBudgetHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        await db.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task ValidRequest_UpdatesAllMutableFields()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Old Name", HousingId, 500m, "2026-01-01", "2026-06-30", null),
            user.Id, ct);

        var result = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("New Name", FoodId, 800m, "2026-02-01", "2026-12-31", null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value.Name);
        Assert.Equal(FoodId, result.Value.CategoryId);
        Assert.Equal("Food & Dining", result.Value.CategoryName);
        Assert.Equal(800m, result.Value.Limit);
        Assert.Equal("2026-02-01", result.Value.StartDate);
        Assert.Equal("2026-12-31", result.Value.EndDate);
    }

    [Fact]
    public async Task NotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new UpdateBudgetHandler(db, TestClock);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task OtherUsersBudget_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("UserA Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            userA.Id, ct);

        var result = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Hijacked", HousingId, 999m, "2026-01-01", "2026-12-31", null),
            userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task EndDateBeforeStartDate_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        var result = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Budget", HousingId, 500m, "2026-12-31", "2026-01-01", null),
            user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task DoesNotResetCurrentSpending_WhenOnlyNameOrLimitChanges()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        // Manually set CurrentSpending to simulate prior event handler activity
        var budget = await db.Budgets.FindAsync([created.Value.Id], ct);
        budget!.CurrentSpending = 250m;
        await db.SaveChangesAsync(ct);

        // Update only Name and Limit — CurrentSpending should NOT be reset
        var result = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Renamed", HousingId, 600m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(250m, result.Value.CurrentSpending);
    }

    [Fact]
    public async Task CategoryChanged_RecalculatesSpending()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var createHandler = new CreateBudgetHandler(db);
        var updateHandler = new UpdateBudgetHandler(db, TestClock);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        // Manually set CurrentSpending (simulating prior Housing expenses)
        var budget = await db.Budgets.FindAsync([created.Value.Id], ct);
        budget!.CurrentSpending = 200m;
        await db.SaveChangesAsync(ct);

        // Add a Food expense transaction
        var foodTransaction = new Transaction
        {
            Type = TransactionType.Expense,
            Amount = 75m,
            Description = "Food expense",
            Date = new NodaTime.LocalDate(2026, 3, 15),
            CategoryId = FoodId,
            WalletId = wallet.Id,
            UserId = user.Id
        };
        db.Transactions.Add(foodTransaction);
        await db.SaveChangesAsync(ct);

        // Change category to Food — should recalculate from 75m (actual Food expenses in DB)
        var result = await updateHandler.HandleAsync(
            created.Value.Id,
            new UpdateBudgetEndpoint.UpdateBudgetRequest("Budget", FoodId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(75m, result.Value.CurrentSpending);
    }
}
