using Kakeibo.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Common.Persistence;

// Entity for outbox messages
public sealed class OutboxMessage : Entity
{
    public required string Type { get; init; }
    public required string Content { get; init; }
    public Instant? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
