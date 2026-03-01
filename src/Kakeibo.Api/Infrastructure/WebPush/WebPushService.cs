using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Persistence;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kakeibo.Api.Infrastructure.WebPush;

// Sends Web Push notifications to all active push subscriptions for a user.
// Handles 410 Gone responses by removing stale subscriptions automatically.
public sealed class WebPushService(
    IOptions<WebPushOptions> webPushOptions,
    PushServiceClient pushClient,
    AppDbContext db,
    ILogger<WebPushService> logger) : IWebPushService
{
    public async Task SendAsync(Guid userId, string title, string body, CancellationToken ct = default)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(ps => ps.UserId == userId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var payload = DefaultSerializer.Serialize(new { title, body });

        // Configure VAPID authentication once before sending
        var opts = webPushOptions.Value;
        pushClient.DefaultAuthentication = new VapidAuthentication(opts.VapidPublicKey, opts.VapidPrivateKey)
        {
            Subject = opts.VapidSubject
        };

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new PushSubscription
                {
                    Endpoint = sub.Endpoint
                };
                pushSubscription.SetKey(PushEncryptionKeyName.P256DH, sub.P256dh);
                pushSubscription.SetKey(PushEncryptionKeyName.Auth, sub.Auth);

                var message = new PushMessage(payload)
                {
                    Topic = title,
                    // 24-hour TTL for notification delivery
                    TimeToLive = 86400
                };

                await pushClient.RequestPushMessageDeliveryAsync(pushSubscription, message, ct);
            }
            catch (PushServiceClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                // Subscription expired — remove it so we don't keep sending to stale endpoints
                db.PushSubscriptions.Remove(sub);
                await db.SaveChangesAsync(ct);
                WebPushServiceLogs.ExpiredSubscriptionRemoved(logger, userId);
            }
            catch (Exception ex)
            {
                // Push delivery failure must not break the notification handler
                WebPushServiceLogs.PushNotificationFailed(logger, userId, ex);
            }
        }
    }
}
