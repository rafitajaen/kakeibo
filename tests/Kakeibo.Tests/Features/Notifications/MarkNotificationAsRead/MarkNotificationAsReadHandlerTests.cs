using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Notifications.MarkNotificationAsRead;

namespace Kakeibo.Tests.Features.Notifications.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandlerTests
{
    private static async Task<(User user, Notification notification)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db, bool isRead = false)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);

        var notification = new Notification
        {
            UserId = user.Id,
            Type = "budget.warning",
            Title = "Budget Warning",
            Body = "You are at 85%",
            IsRead = isRead
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return (user, notification);
    }

    [Fact]
    public async Task MarksNotificationAsRead_WhenItExists()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, notification) = await SetupAsync(db);

        var handler = new MarkNotificationAsReadHandler(db);
        var result = await handler.HandleAsync(notification.Id, user.Id, ct);

        Assert.True(result.IsSuccess);

        // Verify DB state
        await db.Entry(notification).ReloadAsync(ct);
        Assert.True(notification.IsRead);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, _) = await SetupAsync(db);

        var handler = new MarkNotificationAsReadHandler(db);
        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNotificationBelongsToOtherUser()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (_, notification) = await SetupAsync(db);

        // Different user ID — cannot mark another user's notification
        var handler = new MarkNotificationAsReadHandler(db);
        var result = await handler.HandleAsync(notification.Id, Guid.NewGuid(), ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task IsIdempotent_WhenNotificationIsAlreadyRead()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, notification) = await SetupAsync(db, isRead: true);

        var handler = new MarkNotificationAsReadHandler(db);
        var result = await handler.HandleAsync(notification.Id, user.Id, ct);

        // Should succeed without error even though it was already read
        Assert.True(result.IsSuccess);
        await db.Entry(notification).ReloadAsync(ct);
        Assert.True(notification.IsRead);
    }
}
