using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Goals.CreateGoal;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Goals.CreateGoal;

public sealed class CreateGoalHandlerTests
{
    // Fixed clock: 2026-03-01 → max deadline = 2036-03-01
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

    private static async Task<(Wallet wallet, WalletBalance balance)> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, string name = "Savings", decimal balance = 0m)
    {
        var wallet = new Wallet { Name = name, OwnerId = ownerId, Currency = "EUR" };
        var walletBalance = new WalletBalance { WalletId = wallet.Id, Balance = balance };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(walletBalance);
        await db.SaveChangesAsync();
        return (wallet, walletBalance);
    }

    [Fact]
    public async Task ValidRequest_NoDeadline_ReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id, "Vacation Fund");
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Europe Vacation", 5000m, null, wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Europe Vacation", result.Value.Name);
        Assert.Equal(5000m, result.Value.TargetAmount);
        Assert.Null(result.Value.Deadline);
        Assert.Equal(wallet.Id, result.Value.WalletId);
        Assert.Equal("Vacation Fund", result.Value.WalletName);
        Assert.Equal(0m, result.Value.CurrentProgress);
        Assert.Equal(0, result.Value.LastMilestone);
    }

    [Fact]
    public async Task ValidRequest_WithDeadline_ReturnsFormattedDeadline()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "New Car", 20000m, "2027-06-30", wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("2027-06-30", result.Value.Deadline);
    }

    [Fact]
    public async Task CurrentProgress_InitializedFromWalletBalance()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        // Wallet already has 1200 in balance
        var (wallet, _) = await CreateWalletAsync(db, user.Id, "Emergency Fund", 1200m);
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Emergency Fund Goal", 5000m, null, wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(1200m, result.Value.CurrentProgress);

        // Verify in database
        var inDb = await db.Goals.FirstOrDefaultAsync(g => g.Id == result.Value.Id, ct);
        Assert.NotNull(inDb);
        Assert.Equal(1200m, inDb.CurrentProgress);
    }

    [Fact]
    public async Task InvalidWallet_OtherUsersWallet_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (walletA, _) = await CreateWalletAsync(db, userA.Id);
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Goal", 1000m, null, walletA.Id);

        var result = await handler.HandleAsync(request, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task InvalidDeadlineFormat_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Goal", 1000m, "not-a-date", wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task DeadlineMoreThan10Years_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var handler = new CreateGoalHandler(db, TestClock);

        // Clock is 2026-03-01 → max is 2036-03-01 → 2036-03-02 is invalid
        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Goal", 1000m, "2036-03-02", wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task SharedWalletMember_CanCreateGoal()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, userA.Id, "Shared Wallet");
        // Add userB as member
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Shared Goal", 3000m, null, wallet.Id);

        var result = await handler.HandleAsync(request, userB.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Shared Wallet", result.Value.WalletName);
    }

    [Fact]
    public async Task LastMilestone_InitializedToZero()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var handler = new CreateGoalHandler(db, TestClock);

        var request = new CreateGoalEndpoint.CreateGoalRequest(
            "Goal", 1000m, null, wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.LastMilestone);
    }
}
