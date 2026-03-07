using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// A pending, accepted, or rejected friend request between two users.
public sealed class FriendRequest : Entity
{
    public Guid SenderUserId { get; set; }
    public Guid ReceiverUserId { get; set; }

    public Instant? AcceptedAt { get; set; }
    public Instant? RejectedAt { get; set; }

    // Derived — no redundant bool flags (see TD-016)
    public bool IsAccepted => AcceptedAt is not null;
    public bool IsRejected => RejectedAt is not null;
    public bool IsPending => AcceptedAt is null && RejectedAt is null;

    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}
