using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Goals.Events;

// Raised when a savings goal crosses a 25%, 50%, or 75% progress milestone for the first time.
public sealed record GoalMilestoneReachedEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid GoalId { get; init; }
    public required Guid UserId { get; init; }
    public required int MilestonePercent { get; init; }
    public required decimal CurrentProgress { get; init; }
    public required decimal TargetAmount { get; init; }
}
