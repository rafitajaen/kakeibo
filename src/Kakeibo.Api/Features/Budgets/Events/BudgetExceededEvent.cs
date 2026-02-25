using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Budgets.Events;

// Raised when a budget's spending crosses the 100% limit threshold.
public sealed record BudgetExceededEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid BudgetId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid CategoryId { get; init; }
    public required decimal Limit { get; init; }
    public required decimal CurrentSpending { get; init; }
}
