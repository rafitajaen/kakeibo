using Kakeibo.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Infrastructure.Outbox;

// Marker interface for DbContexts that have an OutboxMessages DbSet.
// Allows OutboxInterceptor and OutboxProcessor to access the outbox table generically.
public interface IOutboxSource
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
