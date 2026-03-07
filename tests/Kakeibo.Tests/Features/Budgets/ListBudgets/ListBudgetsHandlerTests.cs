using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.ListBudgets;

namespace Kakeibo.Tests.Features.Budgets.ListBudgets;

public sealed class ListBudgetsHandlerTests
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
            Username = $"user_{Guid.NewGuid():N}"[..12],
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
    public async Task ReturnsBudgetsForUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var listHandler = new ListBudgetsHandler(db);

        await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Housing Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        var result = await listHandler.HandleAsync(user.Id, null, null, ct);

        Assert.Single(result);
        Assert.Equal("Housing Budget", result[0].Name);
    }

    [Fact]
    public async Task ExcludesOtherUsersBudgets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var listHandler = new ListBudgetsHandler(db);

        await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("UserA Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            userA.Id, ct);

        var result = await listHandler.HandleAsync(userB.Id, null, null, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExcludesDeletedBudgets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var deleteHandler = new Kakeibo.Api.Features.Budgets.DeleteBudget.DeleteBudgetHandler(db, TestClock);
        var listHandler = new ListBudgetsHandler(db);

        var created = await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        await deleteHandler.HandleAsync(created.Value.Id, user.Id, ct);

        var result = await listHandler.HandleAsync(user.Id, null, null, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterByCategoryId_ReturnsOnlyMatchingBudgets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var createHandler = new CreateBudgetHandler(db);
        var listHandler = new ListBudgetsHandler(db);

        await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Housing", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Food", FoodId, 300m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        var result = await listHandler.HandleAsync(user.Id, HousingId, null, ct);

        Assert.Single(result);
        Assert.Equal("Housing", result[0].Name);
    }

    [Fact]
    public async Task FilterByWalletId_ReturnsOnlyMatchingBudgets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var createHandler = new CreateBudgetHandler(db);
        var listHandler = new ListBudgetsHandler(db);

        await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("All Wallets", HousingId, 500m, "2026-01-01", "2026-12-31", null),
            user.Id, ct);

        await createHandler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Specific Wallet", FoodId, 300m, "2026-01-01", "2026-12-31", wallet.Id),
            user.Id, ct);

        var result = await listHandler.HandleAsync(user.Id, null, wallet.Id, ct);

        Assert.Single(result);
        Assert.Equal("Specific Wallet", result[0].Name);
    }

    [Fact]
    public async Task EmptyList_ReturnsEmpty()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var listHandler = new ListBudgetsHandler(db);

        var result = await listHandler.HandleAsync(user.Id, null, null, ct);

        Assert.Empty(result);
    }
}
