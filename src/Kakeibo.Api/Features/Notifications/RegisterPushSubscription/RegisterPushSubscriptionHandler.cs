using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Notifications.RegisterPushSubscription;

// Saves a Web Push subscription for the current user.
// If the endpoint already exists for this user, it is updated (upsert by endpoint).
public sealed class RegisterPushSubscriptionHandler(AppDbContext db)
{
    public async Task<RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionResponse> HandleAsync(
        RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Upsert by endpoint — same browser/device re-subscribing must not create duplicates
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            // Update keys in case they rotated
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            await db.SaveChangesAsync(ct);
            return new RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionResponse(existing.Id);
        }

        var subscription = new PushSubscription
        {
            UserId = userId,
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth
        };

        db.PushSubscriptions.Add(subscription);
        await db.SaveChangesAsync(ct);

        return new RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionResponse(subscription.Id);
    }
}
