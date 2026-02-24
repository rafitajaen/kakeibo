using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

public sealed class RefreshToken : Entity
{
    // SHA-256 hash of the actual token — never store the plain token
    public required string TokenHash { get; set; }
    public required Guid UserId { get; set; }
    public required Instant ExpiresAt { get; set; }
    public Instant? RevokedAt { get; set; }

    // Device/session metadata
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Derived state
    public bool IsRevoked => RevokedAt is not null;

    public User? User { get; set; }
}
