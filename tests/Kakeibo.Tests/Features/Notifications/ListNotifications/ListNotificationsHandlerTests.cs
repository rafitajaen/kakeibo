using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.ListNotifications;

namespace Kakeibo.Tests.Features.Notifications.ListNotifications;

public sealed class ListNotificationsHandlerTests
{
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

    private static Notification MakeNotification(Guid userId, bool isRead = false) =>
        new()
        {
            UserId = userId,
            Type = "budget.warning",
            Title = "Budget Warning",
            Body = "You are at 85%",
            IsRead = isRead
        };

    [Fact]
    public async Task ReturnsEmptyList_WhenUserHasNoNotifications()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new ListNotificationsHandler(db);
        var result = await handler.HandleAsync(user.Id, 1, 20, ct);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.UnreadCount);
    }

    [Fact]
    public async Task ReturnsNotifications_OrderedNewestFirst()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        // Add notifications with a small delay so CreatedAt differs
        var n1 = MakeNotification(user.Id);
        db.Notifications.Add(n1);
        await db.SaveChangesAsync(ct);

        var n2 = MakeNotification(user.Id);
        db.Notifications.Add(n2);
        await db.SaveChangesAsync(ct);

        var handler = new ListNotificationsHandler(db);
        var result = await handler.HandleAsync(user.Id, 1, 20, ct);

        Assert.Equal(2, result.Items.Count);
        // Newest first — n2 was saved after n1
        Assert.Equal(n2.Id, result.Items[0].Id);
        Assert.Equal(n1.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task ReportsCorrectUnreadCount()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        db.Notifications.Add(MakeNotification(user.Id, isRead: false));
        db.Notifications.Add(MakeNotification(user.Id, isRead: false));
        db.Notifications.Add(MakeNotification(user.Id, isRead: true));
        await db.SaveChangesAsync(ct);

        var handler = new ListNotificationsHandler(db);
        var result = await handler.HandleAsync(user.Id, 1, 20, ct);

        Assert.Equal(2, result.UnreadCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task ClampsPageSize_ToMaximumOf50()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        // Add 60 notifications
        for (var i = 0; i < 60; i++)
        {
            db.Notifications.Add(MakeNotification(user.Id));
        }

        await db.SaveChangesAsync(ct);

        var handler = new ListNotificationsHandler(db);
        // Request pageSize 200 — should be clamped to 50
        var result = await handler.HandleAsync(user.Id, 1, 200, ct);

        Assert.Equal(50, result.Items.Count);
    }

    [Fact]
    public async Task PaginatesCorrectly()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        for (var i = 0; i < 5; i++)
        {
            db.Notifications.Add(MakeNotification(user.Id));
        }

        await db.SaveChangesAsync(ct);

        var handler = new ListNotificationsHandler(db);
        var page1 = await handler.HandleAsync(user.Id, 1, 3, ct);
        var page2 = await handler.HandleAsync(user.Id, 2, 3, ct);

        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        // No overlap
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task DoesNotReturnNotifications_FromOtherUsers()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user1 = await CreateUserAsync(db);
        var user2 = await CreateUserAsync(db);

        db.Notifications.Add(MakeNotification(user2.Id));
        await db.SaveChangesAsync(ct);

        var handler = new ListNotificationsHandler(db);
        var result = await handler.HandleAsync(user1.Id, 1, 20, ct);

        Assert.Empty(result.Items);
    }
}
