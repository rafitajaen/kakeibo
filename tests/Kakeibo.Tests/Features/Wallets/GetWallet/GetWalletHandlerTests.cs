using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.GetWallet;
using Kakeibo.Api.Infrastructure.Events;

namespace Kakeibo.Tests.Features.Wallets.GetWallet;

public sealed class GetWalletHandlerTests
{
    private static async Task<User> CreateTestUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "user@example.com")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "irrelevant-hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task HandleAsync_OwnWallet_ReturnsWallet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var user = await CreateTestUserAsync(db);
        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("My Wallet", "Personal"), user.Id, ct);

        var handler = new GetWalletHandler(db);
        var result = await handler.HandleAsync(createResult.Value.Id, user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("My Wallet", result.Value.Name),
            () => Assert.Equal("Personal", result.Value.Type),
            () => Assert.Equal(0m, result.Value.Balance));
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var user = await CreateTestUserAsync(db);
        var handler = new GetWalletHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AnotherUsersWallet_ReturnsForbiddenError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();

        var owner = await CreateTestUserAsync(db, "owner@example.com");
        var other = await CreateTestUserAsync(db, "other@example.com");

        var createHandler = new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance);
        var createResult = await createHandler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Private Wallet", "Personal"), owner.Id, ct);

        var handler = new GetWalletHandler(db);
        var result = await handler.HandleAsync(createResult.Value.Id, other.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }
}
