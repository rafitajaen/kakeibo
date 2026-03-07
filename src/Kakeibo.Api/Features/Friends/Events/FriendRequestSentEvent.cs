using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Friends.Events;

public sealed record FriendRequestSentEvent : IEvent
{
    public Guid Id { get; init; }
    public Instant OccurredAt { get; init; }
    public required Guid RequestId { get; init; }
    public required Guid SenderUserId { get; init; }
    public required Guid ReceiverUserId { get; init; }
}
