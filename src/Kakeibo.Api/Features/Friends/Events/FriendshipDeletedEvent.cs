using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.Events;

public sealed record FriendshipDeletedEvent : IEvent
{
    public Guid Id { get; init; }
    public Instant OccurredAt { get; init; }
    public required Guid FriendshipId { get; init; }
    public required Guid UserAId { get; init; }
    public required Guid UserBId { get; init; }
    public required Guid DeletedByUserId { get; init; }
}
