using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.Events;
using Kakeibo.Api.Features.Wallets.Events;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Notifications.Events;

public sealed class SettlementRecordedNotificationHandlerTests
{
    private static async Task<(User fromUser, User toUser, Wallet wallet)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db)
    {
        var fromUser = new User
        {
            Email = $"from-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
            IsVerified = true,
            Currency = "EUR"
        };
        var toUser = new User
        {
            Email = $"to-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.AddRange(fromUser, toUser);

        var wallet = new Wallet { Name = "Shared Expenses", Currency = "EUR", Type = WalletType.Shared };
        db.Wallets.Add(wallet);

        await db.SaveChangesAsync();
        return (fromUser, toUser, wallet);
    }

    [Fact]
    public async Task CreatesInAppNotification_ForRecipient_WithCorrectFields()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (fromUser, toUser, wallet) = await SetupAsync(db);

        var handler = new SettlementRecordedNotificationHandler(db);

        await handler.HandleAsync(new SettlementRecordedEvent
        {
            SettlementId = Guid.NewGuid(),
            WalletId = wallet.Id,
            FromUserId = fromUser.Id,
            ToUserId = toUser.Id,
            Amount = 75.50m
        }, ct);

        var notification = await db.Notifications.SingleAsync(
            n => n.UserId == toUser.Id && n.Type == "settlement.recorded", ct);

        Assert.Equal("Settlement Received", notification.Title);
        Assert.Contains("75.50", notification.Body);
        Assert.Contains(fromUser.Email, notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task DoesNotCreateNotification_ForSender()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (fromUser, toUser, wallet) = await SetupAsync(db);

        var handler = new SettlementRecordedNotificationHandler(db);

        await handler.HandleAsync(new SettlementRecordedEvent
        {
            SettlementId = Guid.NewGuid(),
            WalletId = wallet.Id,
            FromUserId = fromUser.Id,
            ToUserId = toUser.Id,
            Amount = 50m
        }, ct);

        // Only 1 notification total — for the recipient, not the sender
        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(1, count);

        var senderNotifications = await db.Notifications
            .Where(n => n.UserId == fromUser.Id)
            .ToListAsync(ct);
        Assert.Empty(senderNotifications);
    }

    [Fact]
    public async Task DoesNothing_WhenWalletDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var handler = new SettlementRecordedNotificationHandler(db);

        await handler.HandleAsync(new SettlementRecordedEvent
        {
            SettlementId = Guid.NewGuid(),
            WalletId = Guid.NewGuid(),
            FromUserId = Guid.NewGuid(),
            ToUserId = Guid.NewGuid(),
            Amount = 100m
        }, ct);

        var count = await db.Notifications.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
