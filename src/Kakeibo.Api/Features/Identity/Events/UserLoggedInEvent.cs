using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.Events;

public sealed record UserLoggedInEvent : IEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Instant OccurredAt { get; init; } = SystemClock.Instance.GetCurrentInstant();
    public required Guid UserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
