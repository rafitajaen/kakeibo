namespace Kakeibo.Api.Infrastructure.WebPush;

// Delivers push notifications to all active subscriptions for a user.
public interface IWebPushService
{
    Task SendAsync(Guid userId, string title, string body, CancellationToken ct = default);
}
