using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Kakeibo.Api.Features.Goals.ListGoals;

namespace Kakeibo.Tests.Features.Goals.ListGoals;

public sealed class ListGoalsHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

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
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, string name = "Savings")
    {
        var wallet = new Wallet { Name = name, OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task<Guid> CreateGoalAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid walletId,
        string name = "Test Goal",
        decimal target = 1000m)
    {
        var handler = new CreateGoalHandler(db, TestClock);
        var result = await handler.HandleAsync(
            new CreateGoalEndpoint.CreateGoalRequest(name, target, null, walletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task ReturnsAllGoalsForUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new ListGoalsHandler(db);

        await CreateGoalAsync(db, user.Id, wallet.Id, "Goal A");
        await CreateGoalAsync(db, user.Id, wallet.Id, "Goal B");

        var result = await handler.HandleAsync(user.Id, null, ct);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DoesNotReturnOtherUsersGoals()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var walletA = await CreateWalletAsync(db, userA.Id);
        var walletB = await CreateWalletAsync(db, userB.Id);
        var handler = new ListGoalsHandler(db);

        await CreateGoalAsync(db, userA.Id, walletA.Id, "UserA Goal");
        await CreateGoalAsync(db, userB.Id, walletB.Id, "UserB Goal");

        var result = await handler.HandleAsync(userA.Id, null, ct);

        Assert.Single(result);
        Assert.Equal("UserA Goal", result[0].Name);
    }

    [Fact]
    public async Task FiltersGoalsByWalletId()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var walletA = await CreateWalletAsync(db, user.Id, "Wallet A");
        var walletB = await CreateWalletAsync(db, user.Id, "Wallet B");
        var handler = new ListGoalsHandler(db);

        await CreateGoalAsync(db, user.Id, walletA.Id, "Goal A");
        await CreateGoalAsync(db, user.Id, walletB.Id, "Goal B");

        var result = await handler.HandleAsync(user.Id, walletA.Id, ct);

        Assert.Single(result);
        Assert.Equal("Goal A", result[0].Name);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenNoGoals()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new ListGoalsHandler(db);

        var result = await handler.HandleAsync(user.Id, null, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExcludesSoftDeletedGoals()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var handler = new ListGoalsHandler(db);

        var goalId = await CreateGoalAsync(db, user.Id, wallet.Id, "Active Goal");

        // Soft-delete the goal
        var goal = await db.Goals.FindAsync([goalId], ct);
        goal!.DeletedAt = TestClock.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        var result = await handler.HandleAsync(user.Id, null, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task IncludesWalletName()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id, "My Fund");
        var handler = new ListGoalsHandler(db);

        await CreateGoalAsync(db, user.Id, wallet.Id, "Test Goal");

        var result = await handler.HandleAsync(user.Id, null, ct);

        Assert.Single(result);
        Assert.Equal("My Fund", result[0].WalletName);
    }
}
