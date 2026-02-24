using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

public sealed class PasswordResetToken : Entity
{
    // SHA-256 hash of the actual token — never store the plain token
    public required string TokenHash { get; set; }
    public required Guid UserId { get; set; }
    public required Instant ExpiresAt { get; set; }

    // Marked when used — token is single-use
    public Instant? UsedAt { get; set; }

    // Derived state
    public bool IsUsed => UsedAt is not null;

    public User? User { get; set; }
}
