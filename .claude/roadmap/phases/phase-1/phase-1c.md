# Phase 1c: Audit Logging

**Status**: Not Started
**Objective**: Implement audit trail for all user actions using ClickHouse

---

## Prerequisites

| Item | Status |
|------|--------|
| ClickHouse running | ✅ Phase 1a |
| Identity Backend | ✅ Phase 1b |
| Events System (ChannelEventBus + EventDispatcher) | ✅ Phase 1a |

---

## Scope

### ✅ Included

- ClickHouse `audit_events` table
- `IAuditService` + `ClickHouseAuditService` implementation
- `IEventHandler<T>` implementations in `Features/Auditing/` for Identity events
- Health check for ClickHouse
- Audit event types: Authentication (login, logout, register), CRUD (future phases add handlers)
- Integration tests for persistence and querying

### ❌ Excluded

- Audit UI (viewing logs) — Phase 7b
- Audit search/filtering — Phase 7b
- Audit retention policies — indefinite for MVP
- Audit event versioning — all v1

---

## Deliverables

### New Files

**`src/Kakeibo.Api/Infrastructure/Audit/`**:
```
IAuditService.cs
ClickHouseAuditService.cs
ClickHouseOptions.cs
```

**`src/Kakeibo.Api/Features/Auditing/`**:
```
Events/
  UserRegisteredAuditHandler.cs   — IEventHandler<UserRegisteredEvent>
  UserLoggedInAuditHandler.cs     — IEventHandler<UserLoggedInEvent>
  UserLoggedOutAuditHandler.cs    — IEventHandler<UserLoggedOutEvent>
```

**`src/Kakeibo.Api/Infrastructure/HealthChecks/`**:
```
ClickHouseHealthCheck.cs
```

### Database

```sql
CREATE TABLE audit.audit_events (
  id UUID,
  user_id UUID NOT NULL,
  action VARCHAR(100) NOT NULL,
  entity_type VARCHAR(100),
  entity_id UUID,
  occurred_at DateTime64(3) NOT NULL,
  ip_address VARCHAR(45),
  user_agent VARCHAR(500),
  changes String,
  INDEX idx_user (user_id),
  INDEX idx_action (action),
  INDEX idx_occurred (occurred_at)
) ENGINE = MergeTree()
ORDER BY (user_id, occurred_at);
```

---

## Event Handler Pattern

Each audit handler receives events dispatched by `EventDispatcher` (BackgroundService) via `ChannelEventBus`.
No outbox or intermediary staging buffer — events flow directly from the feature handler through
the in-memory channel to the audit handler.

```csharp
namespace Kakeibo.Api.Features.Auditing.Events;

// Records a user registration event in the ClickHouse audit trail.
public sealed class UserRegisteredAuditHandler(IAuditService auditService)
    : IEventHandler<UserRegisteredEvent>
{
    public async Task HandleAsync(UserRegisteredEvent @event, CancellationToken ct)
    {
        await auditService.RecordAsync(new AuditEntry
        {
            UserId = @event.UserId,
            Action = AuditActions.UserRegistered,
            OccurredAt = @event.OccurredAt
        }, ct);
    }
}
```

---

## Acceptance Criteria

- [ ] ClickHouse `audit_events` table created
- [ ] `ClickHouseAuditService` writes audit entries
- [ ] `IEventHandler<UserRegisteredEvent>` handler registered and functional
- [ ] `IEventHandler<UserLoggedInEvent>` handler registered and functional
- [ ] `IEventHandler<UserLoggedOutEvent>` handler registered and functional
- [ ] ClickHouse health check passes
- [ ] Integration test: publish event → EventDispatcher dispatches → audit entry persisted → query confirms

---

## Definition of "Phase 1c Completed"

1. All audit infrastructure functional
2. All acceptance criteria checked (7 items)
3. Integration tests pass
4. Phase 1d (Identity Frontend) can begin

---

**Next Sub-Phase:** [Phase 1d: Identity Frontend](./phase-1d.md)
