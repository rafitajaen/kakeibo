using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Features.Wallets.RemoveMember;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Wallets.RemoveMember;

public sealed class RemoveMemberHandlerTests
{
    private static async Task<User> CreateUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email = "user@example.com")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
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
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = ownerId, Role = WalletMemberRole.Owner });
        await db.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task HandleAsync_OwnerRemovesMember_RemovesMemberAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var handler = new RemoveMemberHandler(db, eventBus);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        var result = await handler.HandleAsync(wallet.Id, member.Id, owner.Id, ct);

        var inDb = await db.WalletMembers
            .FirstOrDefaultAsync(m => m.WalletId == wallet.Id && m.UserId == member.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Null(inDb));

        eventBus.Received(1).Publish(Arg.Is<MemberLeftEvent>(e =>
            e.WalletId == wallet.Id &&
            e.UserId == member.Id));
    }

    [Fact]
    public async Task HandleAsync_MemberRemovesSelf_Succeeds()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var handler = new RemoveMemberHandler(db, eventBus);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        // Member removes themselves (requesterId == targetUserId)
        var result = await handler.HandleAsync(wallet.Id, member.Id, member.Id, ct);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_NonOwnerRemovesOtherMember_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new RemoveMemberHandler(db, Substitute.For<IEventBus>());

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member1 = await CreateUserAsync(db, "member1@example.com");
        var member2 = await CreateUserAsync(db, "member2@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member1.Id });
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member2.Id });
        await db.SaveChangesAsync(ct);

        // member1 tries to remove member2 — only owner can remove others
        var result = await handler.HandleAsync(wallet.Id, member2.Id, member1.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_RemoveOwner_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new RemoveMemberHandler(db, Substitute.For<IEventBus>());

        var owner = await CreateUserAsync(db, "owner@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        // Owner cannot be removed via this endpoint (not even by themselves as owner)
        var result = await handler.HandleAsync(wallet.Id, owner.Id, owner.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_MemberNotInWallet_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new RemoveMemberHandler(db, Substitute.For<IEventBus>());

        var owner = await CreateUserAsync(db, "owner@example.com");
        var outsider = await CreateUserAsync(db, "outsider@example.com");
        var wallet = await CreateSharedWalletAsync(db, owner.Id);

        // Owner tries to remove someone not in the wallet
        var result = await handler.HandleAsync(wallet.Id, outsider.Id, owner.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }
}
