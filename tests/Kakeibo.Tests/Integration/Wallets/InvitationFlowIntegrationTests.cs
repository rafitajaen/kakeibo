using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.AcceptInvitation;
using Kakeibo.Api.Features.Wallets.CreateWallet;
using Kakeibo.Api.Features.Wallets.GetWalletMembers;
using Kakeibo.Api.Features.Wallets.InviteToWallet;
using Kakeibo.Api.Features.Wallets.RemoveMember;
using Kakeibo.Api.Features.Wallets.RevokeInvitation;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kakeibo.Tests.Integration.Wallets;

/// <summary>
/// Integration tests for the shared wallet invitation lifecycle.
/// Chains multiple handlers to verify Phase 2b acceptance criteria:
/// create shared wallet → invite → accept → list members → remove member → revoke invitation.
/// </summary>
public sealed class InvitationFlowIntegrationTests
{
    private static async Task<User> CreateUserAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        string email)
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

    [Fact]
    public async Task SharedWallet_FullInvitationFlow_InviteAcceptListRemove()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var logger = NullLogger<InviteToWalletHandler>.Instance;

        var owner = await CreateUserAsync(db, "owner@example.com");
        var invitee = await CreateUserAsync(db, "invitee@example.com");

        // --- CREATE shared wallet (owner is auto-added as WalletMember) ---
        var createResult = await new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance).HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Apartment Expenses", "Shared"),
            owner.Id, ct);

        Assert.True(createResult.IsSuccess);
        var walletId = createResult.Value.Id;

        // --- INVITE ---
        var inviteHandler = new InviteToWalletHandler(db, eventBus, emailService, clock, logger);
        var inviteResult = await inviteHandler.HandleAsync(
            new InviteToWalletEndpoint.InviteToWalletRequest(invitee.Email),
            walletId, owner.Id, ct);

        Assert.True(inviteResult.IsSuccess);
        var invitationCode = inviteResult.Value.Code;
        Assert.NotEmpty(invitationCode);

        // Email was attempted
        _ = emailService.Received(1).SendWalletInvitationEmailAsync(
            Arg.Any<Guid>(),
            invitee.Email,
            "Apartment Expenses",
            Arg.Any<string>(),
            invitationCode,
            Arg.Any<CancellationToken>());

        // --- ACCEPT ---
        var acceptHandler = new AcceptInvitationHandler(db, eventBus, clock);
        var acceptResult = await acceptHandler.HandleAsync(invitationCode, invitee.Id, ct);

        Assert.True(acceptResult.IsSuccess);
        Assert.Equal(walletId, acceptResult.Value.WalletId);
        Assert.Equal("Apartment Expenses", acceptResult.Value.WalletName);

        // --- LIST MEMBERS (owner + invitee) ---
        var membersResult = await new GetWalletMembersHandler(db).HandleAsync(walletId, owner.Id, ct);

        Assert.True(membersResult.IsSuccess);
        // Owner is listed first, then the new member
        Assert.Equal(2, membersResult.Value.Count);
        Assert.Contains(membersResult.Value, m => m.UserId == owner.Id && m.Role == "Owner");
        Assert.Contains(membersResult.Value, m => m.UserId == invitee.Id && m.Role == "Guest");

        // Invitee can also see members (equal rights)
        var membersAsInvitee = await new GetWalletMembersHandler(db).HandleAsync(walletId, invitee.Id, ct);
        Assert.True(membersAsInvitee.IsSuccess);
        Assert.Equal(2, membersAsInvitee.Value.Count);

        // --- REMOVE MEMBER (owner removes invitee) ---
        // First add another member so wallet doesn't drop below 2 (RULE-001)
        var extra = await CreateUserAsync(db, "extra@example.com");
        db.WalletMembers.Add(new WalletMember { WalletId = walletId, UserId = extra.Id });
        await db.SaveChangesAsync(ct);

        var removeResult = await new RemoveMemberHandler(db, eventBus).HandleAsync(
            walletId, invitee.Id, owner.Id, ct);

        Assert.True(removeResult.IsSuccess);

        var membersAfterRemove = await new GetWalletMembersHandler(db).HandleAsync(walletId, owner.Id, ct);
        Assert.Equal(2, membersAfterRemove.Value.Count); // owner + extra
        Assert.DoesNotContain(membersAfterRemove.Value, m => m.UserId == invitee.Id);
    }

    [Fact]
    public async Task InviteToWallet_AcceptTwice_SecondAcceptReturnsConflict()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var logger = NullLogger<InviteToWalletHandler>.Instance;

        var owner = await CreateUserAsync(db, "owner2@example.com");
        var invitee = await CreateUserAsync(db, "invitee2@example.com");
        var stranger = await CreateUserAsync(db, "stranger@example.com");

        var wallet = await new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance).HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Trip Fund", "Shared"), owner.Id, ct);

        var invite = await new InviteToWalletHandler(db, eventBus, emailService, clock, logger)
            .HandleAsync(new InviteToWalletEndpoint.InviteToWalletRequest(invitee.Email),
                wallet.Value.Id, owner.Id, ct);

        // First accept — success
        var first = await new AcceptInvitationHandler(db, eventBus, clock)
            .HandleAsync(invite.Value.Code, invitee.Id, ct);
        Assert.True(first.IsSuccess);

        // Second accept with same code — invitation is already accepted
        var second = await new AcceptInvitationHandler(db, eventBus, clock)
            .HandleAsync(invite.Value.Code, stranger.Id, ct);
        Assert.True(second.IsFailure);
        Assert.Equal("conflict", second.Error.Code);
    }

    [Fact]
    public async Task RevokeInvitation_RevokedInvitation_CannotBeAccepted()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var logger = NullLogger<InviteToWalletHandler>.Instance;

        var owner = await CreateUserAsync(db, "owner3@example.com");
        var invitee = await CreateUserAsync(db, "invitee3@example.com");

        var wallet = await new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance).HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Revoke Test Wallet", "Shared"), owner.Id, ct);

        var invite = await new InviteToWalletHandler(db, eventBus, emailService, clock, logger)
            .HandleAsync(new InviteToWalletEndpoint.InviteToWalletRequest(invitee.Email),
                wallet.Value.Id, owner.Id, ct);

        // Revoke the invitation
        var revokeResult = await new RevokeInvitationHandler(db, clock)
            .HandleAsync(wallet.Value.Id, invite.Value.Code, owner.Id, ct);
        Assert.True(revokeResult.IsSuccess);

        // Accepting a revoked invitation returns validation error
        var acceptResult = await new AcceptInvitationHandler(db, eventBus, clock)
            .HandleAsync(invite.Value.Code, invitee.Id, ct);
        Assert.True(acceptResult.IsFailure);
        Assert.Equal("validation", acceptResult.Error.Code);
    }

    [Fact]
    public async Task RemoveMember_LastMember_ReturnsValidationError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(Instant.FromUtc(2026, 3, 1, 12, 0));
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var logger = NullLogger<InviteToWalletHandler>.Instance;

        var owner = await CreateUserAsync(db, "owner4@example.com");
        var member = await CreateUserAsync(db, "member4@example.com");

        var wallet = await new CreateWalletHandler(db, eventBus, NodaTime.SystemClock.Instance).HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Last Member Wallet", "Shared"), owner.Id, ct);

        var invite = await new InviteToWalletHandler(db, eventBus, emailService, clock, logger)
            .HandleAsync(new InviteToWalletEndpoint.InviteToWalletRequest(member.Email),
                wallet.Value.Id, owner.Id, ct);

        await new AcceptInvitationHandler(db, eventBus, clock)
            .HandleAsync(invite.Value.Code, member.Id, ct);

        // Wallet now has 2 WalletMembers (owner + member). Removing 1 leaves only 1 → must fail.
        var removeResult = await new RemoveMemberHandler(db, eventBus)
            .HandleAsync(wallet.Value.Id, member.Id, owner.Id, ct);

        Assert.True(removeResult.IsFailure);
        Assert.Equal("validation", removeResult.Error.Code);
    }
}
