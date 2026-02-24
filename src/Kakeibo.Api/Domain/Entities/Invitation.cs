using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// An invitation to join a shared wallet. Code is a 32-char random string valid for 7 days.
public sealed class Invitation : Entity
{
    public required Guid WalletId { get; set; }
    public required Guid InviterUserId { get; set; }

    // Max 254 per RFC 5321
    public required string InviteeEmail { get; set; }

    // Plaintext 32-char random code (not high-value credential — expires in 7 days)
    public required string Code { get; set; }
    public required Instant ExpiresAt { get; set; }

    public Instant? AcceptedAt { get; set; }
    public Instant? RevokedAt { get; set; }

    // Computed properties — EF Core ignores these via InvitationConfiguration
    public bool IsAccepted => AcceptedAt is not null;
    public bool IsRevoked => RevokedAt is not null;

    // Navigation properties
    public Wallet? Wallet { get; set; }
    public User? Inviter { get; set; }
}
