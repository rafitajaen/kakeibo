using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class MemberJoinedNotificationHandlerTests
{
    private static ILogger<MemberJoinedNotificationHandler> CreateFakeLogger() =>
        Substitute.For<ILogger<MemberJoinedNotificationHandler>>();

    private static User MakeUser(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        return user;
    }

    [Fact]
    public async Task CreatesInAppNotification_ForEachExistingMember_ExcludingNewMember()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var owner = MakeUser(db);
        var member2 = MakeUser(db);
        var newMember = MakeUser(db);

        var wallet = new Wallet { Name = "House Expenses", OwnerId = owner.Id, Currency = "EUR", Type = WalletType.Shared };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);

        // owner and member2 are existing members before newMember joins
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = owner.Id });
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = member2.Id });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new MemberJoinedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new MemberJoinedEvent
        {
            WalletId = wallet.Id,
            UserId = newMember.Id
        }, ct);

        // Two notifications: one for owner, one for member2 (not newMember)
        var notifications = await db.Notifications
            .Where(n => n.Type == "member.joined")
            .ToListAsync(ct);

        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.UserId == owner.Id);
        Assert.Contains(notifications, n => n.UserId == member2.Id);
        Assert.DoesNotContain(notifications, n => n.UserId == newMember.Id);
    }

    [Fact]
    public async Task SendsEmail_ToMembersWithEmailInvitationsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var owner = MakeUser(db);
        var newMember = MakeUser(db);

        var wallet = new Wallet { Name = "Trip", OwnerId = owner.Id, Currency = "EUR", Type = WalletType.Shared };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = owner.Id });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new MemberJoinedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new MemberJoinedEvent
        {
            WalletId = wallet.Id,
            UserId = newMember.Id
        }, ct);

        // Default preferences have email enabled
        await emailService.Received(1).SendMemberJoinedEmailAsync(
            owner.Id, owner.Email, wallet.Name, newMember.Email, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsPush_ToMembersWithPushInvitationsEnabled()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var owner = MakeUser(db);
        var newMember = MakeUser(db);

        var wallet = new Wallet { Name = "Family", OwnerId = owner.Id, Currency = "EUR", Type = WalletType.Shared };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);

        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = owner.Id });
        await db.SaveChangesAsync(ct);

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new MemberJoinedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new MemberJoinedEvent
        {
            WalletId = wallet.Id,
            UserId = newMember.Id
        }, ct);

        // Default preferences have push enabled
        await pushService.Received(1).SendAsync(
            owner.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNothing_WhenWalletDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var emailService = Substitute.For<IEmailService>();
        var pushService = Substitute.For<IWebPushService>();
        var handler = new MemberJoinedNotificationHandler(db, emailService, pushService, CreateFakeLogger());

        await handler.HandleAsync(new MemberJoinedEvent
        {
            WalletId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
