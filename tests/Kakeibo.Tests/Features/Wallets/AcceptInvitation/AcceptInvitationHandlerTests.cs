using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.AcceptInvitation;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Wallets.AcceptInvitation;

public sealed class AcceptInvitationHandlerTests
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

    private static async Task<(Wallet Wallet, Invitation Invitation)> CreateWalletWithInvitationAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid ownerId,
        string inviteeEmail = "invitee@example.com",
        Instant? expiresAt = null,
        bool revoked = false,
        bool accepted = false)
    {
        var now = Instant.FromUtc(2026, 3, 1, 12, 0);

        var wallet = new Wallet
        {
            Name = "Shared Wallet",
            Type = WalletType.Shared,
            OwnerId = ownerId,
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var invitation = new Invitation
        {
            WalletId = wallet.Id,
            InviterUserId = ownerId,
            InviteeEmail = inviteeEmail,
            Code = "TESTCODE12345678901234567890AB",
            ExpiresAt = expiresAt ?? now.Plus(Duration.FromDays(7)),
            RevokedAt = revoked ? now : null,
            AcceptedAt = accepted ? now : null
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();

        return (wallet, invitation);
    }

    [Fact]
    public async Task HandleAsync_ValidCode_CreatesWalletMemberAndPublishesEvents()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 11, 0)); // 1 hour before now
        var eventBus = Substitute.For<IEventBus>();
        var handler = new AcceptInvitationHandler(db, eventBus, clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var invitee = await CreateUserAsync(db, "invitee@example.com");
        var (wallet, invitation) = await CreateWalletWithInvitationAsync(db, owner.Id);

        var result = await handler.HandleAsync(invitation.Code, invitee.Id, ct);

        var memberInDb = await db.WalletMembers.FirstOrDefaultAsync(
            m => m.WalletId == wallet.Id && m.UserId == invitee.Id, ct);
        var invitationInDb = await db.Invitations.FirstOrDefaultAsync(i => i.Id == invitation.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.NotNull(memberInDb),
            () => Assert.NotNull(invitationInDb!.AcceptedAt));

        eventBus.Received(1).Publish(Arg.Is<InvitationAcceptedEvent>(e =>
            e.InvitationId == invitation.Id &&
            e.WalletId == wallet.Id &&
            e.UserId == invitee.Id));

        eventBus.Received(1).Publish(Arg.Is<MemberJoinedEvent>(e =>
            e.WalletId == wallet.Id &&
            e.UserId == invitee.Id));
    }

    [Fact]
    public async Task HandleAsync_InvalidCode_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new AcceptInvitationHandler(
            db,
            Substitute.For<IEventBus>(),
            new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0)));

        var user = await CreateUserAsync(db);

        var result = await handler.HandleAsync("NONEXISTENT00000000000000000000", user.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("not_found", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_ExpiredCode_ReturnsValidationError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        // Clock is after the invitation expiry
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 10, 12, 0));
        var handler = new AcceptInvitationHandler(db, Substitute.For<IEventBus>(), clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var invitee = await CreateUserAsync(db, "invitee@example.com");

        // Expires in the past relative to clock
        var (_, invitation) = await CreateWalletWithInvitationAsync(
            db, owner.Id,
            expiresAt: Instant.FromUtc(2026, 3, 5, 12, 0));

        var result = await handler.HandleAsync(invitation.Code, invitee.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("validation", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_RevokedCode_ReturnsValidationError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 11, 0));
        var handler = new AcceptInvitationHandler(db, Substitute.For<IEventBus>(), clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var invitee = await CreateUserAsync(db, "invitee@example.com");

        var (_, invitation) = await CreateWalletWithInvitationAsync(db, owner.Id, revoked: true);

        var result = await handler.HandleAsync(invitation.Code, invitee.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("validation", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_AlreadyAcceptedCode_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 11, 0));
        var handler = new AcceptInvitationHandler(db, Substitute.For<IEventBus>(), clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var invitee = await CreateUserAsync(db, "invitee@example.com");

        var (_, invitation) = await CreateWalletWithInvitationAsync(db, owner.Id, accepted: true);

        var result = await handler.HandleAsync(invitation.Code, invitee.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_UserAlreadyMember_ReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 11, 0));
        var handler = new AcceptInvitationHandler(db, Substitute.For<IEventBus>(), clock);

        var owner = await CreateUserAsync(db, "owner@example.com");
        var member = await CreateUserAsync(db, "member@example.com");
        var (wallet, invitation) = await CreateWalletWithInvitationAsync(db, owner.Id, "member@example.com");

        // Add member directly before accepting
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member.Id });
        await db.SaveChangesAsync(ct);

        var result = await handler.HandleAsync(invitation.Code, member.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }

    [Fact]
    public async Task HandleAsync_ConcurrentAccept_DbConstraintViolationReturnsConflict()
    {
        // Simulates the race condition where two requests pass the application-level
        // alreadyMember check before either commits. The second SaveChangesAsync hits
        // the (WalletId, UserId) unique constraint and must return Conflict, not 500.
        await using var db1 = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 11, 0));

        var owner = await CreateUserAsync(db1, "concurrent-owner@example.com");
        var invitee = await CreateUserAsync(db1, "concurrent-invitee@example.com");
        var (wallet, invitation) = await CreateWalletWithInvitationAsync(db1, owner.Id);

        // db2 shares the same database as db1 — simulates the "first concurrent request" committing
        await using var db2 = TestDbContextFactory.CreateSecondContext(db1);

        // "First request" (db2): bypasses application checks and directly commits member + accepts invitation.
        // This simulates the race condition where db2 wins the commit race.
        db2.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = invitee.Id });
        var inv2 = await db2.Invitations.FindAsync([invitation.Id], ct);
        inv2!.AcceptedAt = clock.GetCurrentInstant();
        await db2.SaveChangesAsync(ct);

        // "Second request" (db1 handler): invitation still looks un-accepted in db1's context,
        // so the handler passes the application-level check and tries to SaveChangesAsync.
        // This must hit the (WalletId, UserId) unique constraint → DbUpdateException → Error.Conflict.
        var handler = new AcceptInvitationHandler(db1, Substitute.For<IEventBus>(), clock);
        var result = await handler.HandleAsync(invitation.Code, invitee.Id, ct);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("conflict", result.Error.Code));
    }
}
