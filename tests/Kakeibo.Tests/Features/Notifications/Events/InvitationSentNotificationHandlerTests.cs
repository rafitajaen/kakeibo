using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class InvitationSentNotificationHandlerTests
{
    private static ILogger<InvitationSentNotificationHandler> CreateFakeLogger() =>
        Substitute.For<ILogger<InvitationSentNotificationHandler>>();

    private static async Task<(User inviter, Wallet wallet, Invitation invitation)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db)
    {
        var inviter = new User
        {
            Email = $"inviter-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(inviter);

        var wallet = new Wallet { Name = "Shared Apartment", OwnerId = inviter.Id, Currency = "EUR", Type = WalletType.Shared };
        db.Wallets.Add(wallet);

        var invitation = new Invitation
        {
            WalletId = wallet.Id,
            InviterUserId = inviter.Id,
            InviteeEmail = $"invitee-{Guid.NewGuid():N}@example.com",
            Code = "TESTCODE1234567890123456789012",
            ExpiresAt = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(7))
        };
        db.Invitations.Add(invitation);

        await db.SaveChangesAsync();
        return (inviter, wallet, invitation);
    }

    [Fact]
    public async Task CreatesInAppNotification_WhenInviteeAlreadyHasAccount()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (inviter, wallet, invitation) = await SetupAsync(db);

        // Register the invitee as an existing user
        var invitee = new User
        {
            Email = invitation.InviteeEmail,
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(invitee);
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new InvitationSentNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new InvitationSentEvent
        {
            InvitationId = invitation.Id,
            WalletId = wallet.Id,
            InviterUserId = inviter.Id,
            InviteeEmail = invitation.InviteeEmail
        }, ct);

        var notification = await db.Notifications.SingleAsync(
            n => n.UserId == invitee.Id && n.Type == "invitation.sent", ct);

        Assert.Equal("Wallet Invitation", notification.Title);
        Assert.Contains(wallet.Name, notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task SendsInvitationEmail_Regardless_OfWhetherInviteeHasAccount()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (inviter, wallet, invitation) = await SetupAsync(db);

        // Invitee has NO account yet — external invite scenario
        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new InvitationSentNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new InvitationSentEvent
        {
            InvitationId = invitation.Id,
            WalletId = wallet.Id,
            InviterUserId = inviter.Id,
            InviteeEmail = invitation.InviteeEmail
        }, ct);

        await emailService.Received(1).SendWalletInvitationEmailAsync(
            invitation.Id,
            invitation.InviteeEmail,
            wallet.Name,
            inviter.Email,
            invitation.Code,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsPush_WhenInviteeExistsAndPushInvitationsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (inviter, wallet, invitation) = await SetupAsync(db);

        var invitee = new User
        {
            Email = invitation.InviteeEmail,
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(invitee);
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new InvitationSentNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new InvitationSentEvent
        {
            InvitationId = invitation.Id,
            WalletId = wallet.Id,
            InviterUserId = inviter.Id,
            InviteeEmail = invitation.InviteeEmail
        }, ct);

        await pushService.Received(1).SendAsync(
            invitee.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNothing_WhenWalletDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new InvitationSentNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new InvitationSentEvent
        {
            InvitationId = Guid.NewGuid(),
            WalletId = Guid.NewGuid(),
            InviterUserId = Guid.NewGuid(),
            InviteeEmail = "nobody@example.com"
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
        await emailService.DidNotReceive().SendWalletInvitationEmailAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
