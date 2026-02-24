using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.GetWalletMembers;

namespace Kakeibo.Tests.Features.Wallets.GetWalletMembers;

public sealed class GetWalletMembersHandlerTests
{
    private static async Task<User> CreateUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "user@example.com")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Wallet> CreateSharedWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid ownerId)
    {
        var wallet = new Wallet
        {
            Name = "Shared Wallet",
            Type = WalletType.Shared,
            OwnerId = ownerId,
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task HandleAsync_OwnerRequest_ReturnsOwnerAndMembers()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new GetWalletMembersHandler(db);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        var result = await handler.HandleAsync(wallet.Id, owner.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(2, result.Value.Count),
            () => Assert.Contains(result.Value, m => m.UserId == owner.Id && m.IsOwner),
            () => Assert.Contains(result.Value, m => m.UserId == member.Id && !m.IsOwner));
    }

    [Fact]
    public async Task HandleAsync_MemberRequest_ReturnsOwnerAndMembers()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new GetWalletMembersHandler(db);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        var result = await handler.HandleAsync(wallet.Id, member.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal(2, result.Value.Count));
    }

    [Fact]
    public async Task HandleAsync_NonMemberRequest_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new GetWalletMembersHandler(db);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var outsider = await CreateUserAsync(db, "outsider@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        var result = await handler.HandleAsync(wallet.Id, outsider.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new GetWalletMembersHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
