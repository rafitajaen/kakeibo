# Phase 1d: Audit Logging

**Status**: Not Started
**Objective**: Implement audit trail for all user actions using ClickHouse

---

## Prerequisites

| Item | Status |
|------|--------|
| ClickHouse running | ✅ Phase 1a |
| Outbox Pattern | ✅ Phase 1c |

---

## Scope

### ✅ Included

- ClickHouse `audit_events` table
- `ClickHouseAuditService` implementation
- `IAuditOutbox` in-memory staging buffer
- Health check for ClickHouse
- Audit event types: Authentication, CRUD, Transaction, Collaboration
- Integration tests for persistence and querying

### ❌ Excluded

- Audit UI (viewing logs) — Phase 7b
- Audit search/filtering — Phase 7b
- Audit retention policies — indefinite for MVP
- Audit event versioning — all v1

---

## Deliverables

### New Files

**Kakeibo.Infrastructure/Audit/**:
```
IAuditService.cs
ClickHouseAuditService.cs
ClickHouseOptions.cs
IAuditOutbox.cs
AuditOutbox.cs
```

**Kakeibo.Infrastructure/HealthChecks/**:
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

## Acceptance Criteria

- [ ] ClickHouse table created
- [ ] `ClickHouseAuditService` writes events
- [ ] `IAuditOutbox` stages events
- [ ] Health check passes
- [ ] Integration test: stage → flush → query

---

## Definition of "Phase 1d Completed"

1. All audit infrastructure functional
2. All acceptance criteria checked (5 items)
3. Integration tests pass
4. Phase 1e can begin (Frontend uses Identity)

---

**Next Sub-Phase:** [Phase 1e: Identity Frontend](./phase-1e.md)
