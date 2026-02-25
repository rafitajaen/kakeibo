using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

// Stores a Web Push subscription for a user's browser/device.
public sealed class PushSubscription : Entity
{
    public required Guid UserId { get; set; }

    // The push service endpoint URL (up to 2048 chars).
    public required string Endpoint { get; set; }

    // ECDH public key for payload encryption (base64url-encoded).
    public required string P256dh { get; set; }

    // HMAC authentication secret (base64url-encoded).
    public required string Auth { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
