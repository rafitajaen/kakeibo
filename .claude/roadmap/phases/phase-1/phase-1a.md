# Phase 1a: Outbox Pattern Implementation

**Status**: Not Started
**Objective**: Implement transactional outbox pattern for reliable event delivery across modules

---

## Prerequisites

| Item | Status |
|------|--------|
| PostgreSQL 18 + EF Core | ✅ Phase 0 |
| `Kakeibo.Common` (base entities) | ✅ Phase 0 |
| `Kakeibo.Contracts` (event interfaces) | ✅ Phase 0 |
| `Kakeibo.Infrastructure` project | ✅ Phase 0 |

---

## Scope

### ✅ Included

- `OutboxInterceptor` (SaveChangesInterceptor that harvests domain events from entities)
- `DomainEventDispatcher` (resolves and invokes `IDomainEventHandler<T>` via DI)
- `ModuleEventBus` (scoped service that buffers integration events in-memory)
- `OutboxProcessor` (BackgroundService that polls outbox tables and dispatches to consumers)
- `OutboxMessage` entity in each module's schema
- Polly retry policy (3 attempts: 1s, 5s, 15s exponential backoff)
- `IOutboxSource` interface for per-module DbContext access
- Integration tests for end-to-end event delivery

### ❌ Excluded

- Event deduplication (handled by consumer idempotency)
- Outbox archiving/cleanup (events remain indefinitely in MVP)
- Event versioning (all events start at v1)
- Distributed tracing for events (basic logging only)

---

## Deliverables

### New Files

**Kakeibo.Infrastructure/Outbox/**:
```
OutboxInterceptor.cs           — Harvests domain events + persists outbox messages
OutboxProcessor.cs             — Background polling + dispatch to consumers
OutboxOptions.cs               — Configuration (polling interval, batch size, retry)
IOutboxSource.cs               — Interface for module DbContext outbox access
```

**Kakeibo.Infrastructure/Messaging/**:
```
DomainEventDispatcher.cs       — Resolves IDomainEventHandler<T>, invokes sequentially
ModuleEventBus.cs              — Scoped buffer for integration events
ModuleClient.cs                — Sync request dispatcher (IModuleRequestHandler<,>)
```

**Kakeibo.Common/Abstractions/**:
```
IDomainEvent.cs                — Internal module event interface
IDomainEventHandler.cs         — Handler for domain events
IIntegrationEvent.cs           — Cross-module event interface
IEventConsumer.cs              — Consumer for integration events
OutboxMessage.cs               — Entity for outbox persistence
```

### Modified Files

**Each module's DbContext** (e.g., `IdentityDbContext.cs`):
- Add `DbSet<OutboxMessage> OutboxMessages`
- Implement `IOutboxSource`

**Program.cs**:
- Register `OutboxInterceptor` as EF Core interceptor
- Register `OutboxProcessor` as hosted service
- Configure `OutboxOptions` from appsettings

### Database

Per-module outbox tables:
```sql
CREATE TABLE identity.outbox_messages (
  id UUID PRIMARY KEY,
  occurred_at TIMESTAMPTZ NOT NULL,
  event_type VARCHAR(500) NOT NULL,
  payload JSONB NOT NULL,
  processed_at TIMESTAMPTZ NULL,
  processing_attempts INT NOT NULL DEFAULT 0,
  last_error TEXT NULL
);

CREATE INDEX idx_outbox_unprocessed
  ON identity.outbox_messages(occurred_at)
  WHERE processed_at IS NULL;
```

---

## Technical Detail

### Outbox Flow

1. **Entity raises domain event**: `entity.AddDomainEvent(new WalletCreatedDomainEvent(...))`
2. **Handler calls SaveChangesAsync**: EF Core triggers `OutboxInterceptor`
3. **OutboxInterceptor**:
   - Harvests domain events from `ChangeTracker.Entries<Entity>()`
   - Dispatches to `DomainEventDispatcher`
4. **DomainEventDispatcher**:
   - Resolves all `IDomainEventHandler<T>` for the event type
   - Invokes each handler sequentially
5. **Domain event handler**:
   - Publishes integration events via `eventBus.PublishAsync()`
   - Stages audit events via `auditOutbox.Stage()`
6. **OutboxInterceptor** (continued):
   - Reads buffered integration events from `ModuleEventBus`
   - Inserts `OutboxMessage` rows in same transaction
   - Transaction commits (entity changes + outbox messages atomic)
7. **OutboxProcessor** (background):
   - Polls outbox tables every 10s (dev) / 5s (prod)
   - For each unprocessed message: resolves `IEventConsumer<T>`, invokes
   - Marks message as processed on success
   - Increments attempts + logs error on failure
   - Polly retries: 3x (1s, 5s, 15s)

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| Per-module outbox tables | Logical separation, easier to debug per module |
| Sequential domain event dispatch | Predictable ordering, simpler error handling |
| In-memory event buffer (scoped) | Simple, no external queue dependency |
| Polly retry in OutboxProcessor | Handles transient failures (network, DB locks) |
| No deduplication | Consumers must be idempotent (design principle) |

---

## Acceptance Criteria

- [ ] `OutboxInterceptor` harvests domain events from entities via `ChangeTracker`
- [ ] `DomainEventDispatcher` resolves all `IDomainEventHandler<T>` for event type
- [ ] Domain event handlers can call `eventBus.PublishAsync()` to stage integration events
- [ ] `OutboxInterceptor` reads buffered events and inserts `OutboxMessage` rows
- [ ] Entity changes + outbox messages committed in single transaction
- [ ] `OutboxProcessor` polls outbox tables at configured interval
- [ ] `OutboxProcessor` resolves `IEventConsumer<T>` via DI for each message
- [ ] Processed messages marked with `processed_at` timestamp
- [ ] Failed messages increment `processing_attempts` and log `last_error`
- [ ] Polly retry policy: 3 attempts with exponential backoff (1s, 5s, 15s)
- [ ] Integration test: Domain event → integration event → consumer invoked
- [ ] Integration test: Failed consumer → retry → eventual success
- [ ] Integration test: Consumer idempotency (same event twice → no duplicate effects)

---

## Definition of "Phase 1a Completed"

1. All outbox infrastructure implemented
2. All acceptance criteria checked (13 items)
3. Integration tests pass (3 scenarios)
4. Manual test: Domain event in Identity → integration event → Auditing consumer
5. Code review complete
6. Documentation: Outbox pattern documented in `/docs/architecture.md`
7. Phase 1b can begin (Audit uses outbox for staging)
