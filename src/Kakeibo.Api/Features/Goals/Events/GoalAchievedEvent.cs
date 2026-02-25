using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Goals.Events;

// Raised when a savings goal reaches 100% of its target amount for the first time.
public sealed record GoalAchievedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid GoalId { get; init; }
    public required Guid UserId { get; init; }
    public required decimal TargetAmount { get; init; }
    public required decimal CurrentProgress { get; init; }
}
