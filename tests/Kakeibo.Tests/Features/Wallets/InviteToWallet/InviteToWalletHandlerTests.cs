using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Features.Wallets.InviteToWallet;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Wallets.InviteToWallet;

public sealed class InviteToWalletHandlerTests
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

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid ownerId,
        WalletType type = WalletType.Shared,
        string name = "Shared Wallet")
    {
        var wallet = new Wallet
        {
            Name = name,
            Type = type,
            OwnerId = ownerId,
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task HandleAsync_ValidInvitation_CreatesInvitationAndPublishesEvent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<Kakeibo.Api.Infrastructure.Email.IEmailService>();
        var handler = new InviteToWalletHandler(db, eventBus, emailService, clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var wallet = await CreateWalletAsync(db, owner.Id);
        var request = new InviteToWalletEndpoint.InviteToWalletRequest("invitee@example.com");

        var result = await handler.HandleAsync(request, wallet.Id, owner.Id, ct);

        var inDb = result.IsSuccess
            ? await db.Invitations.FirstOrDefaultAsync(i => i.Id == result.Value.InvitationId, ct)
            : null;

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.NotNull(inDb),
            () => Assert.Equal(wallet.Id, inDb!.WalletId),
            () => Assert.Equal(owner.Id, inDb!.InviterUserId),
            () => Assert.Equal("invitee@example.com", inDb!.InviteeEmail),
            () => Assert.Equal(32, inDb!.Code.Length),
            () => Assert.Equal(clock.GetCurrentInstant().Plus(Duration.FromDays(7)), inDb!.ExpiresAt));

        eventBus.Received(1).Publish(Arg.Is<InvitationSentEvent>(e =>
            e.InvitationId == inDb!.Id &&
            e.WalletId == wallet.Id &&
            e.InviterUserId == owner.Id &&
            e.InviteeEmail == "invitee@example.com"));
    }

    [Fact]
    public async Task HandleAsync_PersonalWallet_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new InviteToWalletHandler(
            db,
            Substitute.For<IEventBus>(),
            Substitute.For<Kakeibo.Api.Infrastructure.Email.IEmailService>(),
            new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0)));

        var owner = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, owner.Id, WalletType.Personal);
        var request = new InviteToWalletEndpoint.InviteToWalletRequest("other@example.com");

        var result = await handler.HandleAsync(request, wallet.Id, owner.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_NonMemberRequester_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new InviteToWalletHandler(
            db,
            Substitute.For<IEventBus>(),
            Substitute.For<Kakeibo.Api.Infrastructure.Email.IEmailService>(),
            new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0)));

        var owner = await CreateUserAsync(db, "owner@example.com");
        var outsider = await CreateUserAsync(db, "outsider@example.com");
        var wallet = await CreateWalletAsync(db, owner.Id);
        var request = new InviteToWalletEndpoint.InviteToWalletRequest("someone@example.com");

        var result = await handler.HandleAsync(request, wallet.Id, outsider.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("forbidden", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_DuplicatePendingInvitation_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var handler = new InviteToWalletHandler(
            db,
            Substitute.For<IEventBus>(),
            Substitute.For<Kakeibo.Api.Infrastructure.Email.IEmailService>(),
            clock);

        var owner = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, owner.Id);
        var request = new InviteToWalletEndpoint.InviteToWalletRequest("invitee@example.com");

        await handler.HandleAsync(request, wallet.Id, owner.Id, ct);

        // Second invite to same email for same wallet
        var result = await handler.HandleAsync(request, wallet.Id, owner.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_InviteeAlreadyMember_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var handler = new InviteToWalletHandler(
            db,
            Substitute.For<IEventBus>(),
            Substitute.For<Kakeibo.Api.Infrastructure.Email.IEmailService>(),
            clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var wallet = await CreateWalletAsync(db, owner.Id);

        // Add member directly
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        var request = new InviteToWalletEndpoint.InviteToWalletRequest("member@example.com");

        var result = await handler.HandleAsync(request, wallet.Id, owner.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_WalletMemberCanInvite()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var handler = new InviteToWalletHandler(
            db,
            Substitute.For<IEventBus>(),
            Substitute.For<Kakeibo.Api.Infrastructure.Email.IEmailService>(),
            clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var wallet = await CreateWalletAsync(db, owner.Id);

        // Add member
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        var request = new InviteToWalletEndpoint.InviteToWalletRequest("newperson@example.com");

        var result = await handler.HandleAsync(request, wallet.Id, member.Id, ct);

        Assert.True(result.IsSuccess);
    }
}
