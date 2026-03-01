# Phase 4b: Budget Monitoring Backend + UI

**Status**: Not Started
**Objective**: Implement budget spending tracking and alerts

---

## Scope

### ✅ Included
- Current spending calculation (queries Transactions module)
- Remaining budget calculation
- Percentage used
- Budget status (on track, warning, exceeded)
- Alert events published (BudgetWarningEvent, BudgetExceededEvent)
- Frontend: budget progress bars, status indicators

### ❌ Excluded
- Projected overage (based on daily average) — post-MVP
- Budget rollover — post-MVP

---

## Deliverables

### Backend
**`src/Kakeibo.Api/Features/Budgets/`**:
- Events/TransactionRecordedHandler.cs — `IEventHandler<TransactionRecordedEvent>` for spending updates
- Events/TransactionUpdatedHandler.cs — `IEventHandler<TransactionUpdatedEvent>` for spending recalculation
- Events/TransactionDeletedHandler.cs — `IEventHandler<TransactionDeletedEvent>` for spending recalculation
- GetBudgetStatus/

**Endpoints**:
- `GET /api/budgets/{id}/status`

**Listens to**:
- `TransactionRecordedEvent`
- `TransactionUpdatedEvent`
- `TransactionDeletedEvent`

**Publishes**:
- `BudgetWarningEvent` (75% threshold)
- `BudgetExceededEvent` (100% threshold)

### Frontend
**src/Kakeibo.App/components/budgets/**:
- BudgetProgressBar.vue, BudgetStatusBadge.vue

---

## Acceptance Criteria

- [ ] Listen to `TransactionRecordedEvent` → update spending
- [ ] Listen to `TransactionUpdatedEvent` → recalculate spending
- [ ] Listen to `TransactionDeletedEvent` → recalculate spending
- [ ] Budget spending recalculates correctly when a transaction is edited
- [ ] Budget spending recalculates correctly when a transaction is deleted
- [ ] Calculate current spending from Transactions module
- [ ] Calculate remaining budget
- [ ] Calculate percentage used
- [ ] Publish `BudgetWarningEvent` at 75%
- [ ] Publish `BudgetExceededEvent` at 100%
- [ ] Frontend: budget progress bars
- [ ] Frontend: status indicators (on track / warning / exceeded)

---

## Definition of "Phase 4b Completed"

1. Budget monitoring operational
2. All 12 acceptance criteria checked
3. Phase 7 can use budget alerts
