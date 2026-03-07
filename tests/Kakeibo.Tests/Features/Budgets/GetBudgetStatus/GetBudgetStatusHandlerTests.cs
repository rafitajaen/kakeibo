using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.GetBudgetStatus;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Budgets.GetBudgetStatus;

public sealed class GetBudgetStatusHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Guid> CreateBudgetWithSpendingAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        decimal limit,
        decimal currentSpending,
        CancellationToken ct)
    {
        var createHandler = new CreateBudgetHandler(db);
        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Test Budget", HousingId, limit, "2026-01-01", "2026-12-31", null),
            userId, ct);

        // Set CurrentSpending directly to simulate event handler activity
        var budget = await db.Budgets.FindAsync([created.Value.Id], ct);
        budget!.CurrentSpending = currentSpending;
        await db.SaveChangesAsync(ct);

        return created.Value.Id;
    }

    [Fact]
    public async Task ReturnsAllComputedFields()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetBudgetStatusHandler(db);

        var budgetId = await CreateBudgetWithSpendingAsync(db, user.Id, 400m, 100m, ct);

        var result = await handler.HandleAsync(budgetId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(budgetId, result.Value.Id);
        Assert.Equal("Test Budget", result.Value.Name);
        Assert.Equal(400m, result.Value.Limit);
        Assert.Equal(100m, result.Value.CurrentSpending);
        Assert.Equal(300m, result.Value.Remaining);
        Assert.Equal(25m, result.Value.PercentageUsed);
        Assert.Equal("OnTrack", result.Value.Status);
        Assert.Equal("2026-01-01", result.Value.StartDate);
        Assert.Equal("2026-12-31", result.Value.EndDate);
    }

    [Fact]
    public async Task Status_OnTrack_WhenBelow75Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetBudgetStatusHandler(db);

        // 74% — just below warning threshold
        var budgetId = await CreateBudgetWithSpendingAsync(db, user.Id, 100m, 74m, ct);

        var result = await handler.HandleAsync(budgetId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("OnTrack", result.Value.Status);
    }

    [Fact]
    public async Task Status_Warning_WhenBetween75And100Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetBudgetStatusHandler(db);

        // 75% — exactly at warning threshold
        var budgetId = await CreateBudgetWithSpendingAsync(db, user.Id, 100m, 75m, ct);

        var result = await handler.HandleAsync(budgetId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Warning", result.Value.Status);
    }

    [Fact]
    public async Task Status_Exceeded_WhenAtOrAbove100Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetBudgetStatusHandler(db);

        // 105% — over budget
        var budgetId = await CreateBudgetWithSpendingAsync(db, user.Id, 100m, 105m, ct);

        var result = await handler.HandleAsync(budgetId, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Exceeded", result.Value.Status);
        Assert.Equal(0m, result.Value.Remaining); // Remaining is capped at 0
        Assert.Equal(105m, result.Value.PercentageUsed);
    }

    [Fact]
    public async Task NotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new GetBudgetStatusHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

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
        var handler = new GetBudgetStatusHandler(db);

        var budgetId = await CreateBudgetWithSpendingAsync(db, userA.Id, 100m, 0m, ct);

        var result = await handler.HandleAsync(budgetId, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }
}
