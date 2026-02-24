# Phase 6b: Auto-Generation + Forecast Backend + UI

**Status**: Not Started
**Objective**: Implement automatic transaction generation and forecast visibility

---

## Scope

### ✅ Included
- Hangfire background job for auto-generation (daily)
- Transaction creation from patterns
- Forecast calculation (next 30/90 days)
- Frontend: forecast view, upcoming transactions

### ❌ Excluded
- Projected balance calculation — post-MVP
- Pattern pause/resume — post-MVP

---

## Deliverables

### Backend
**`src/Kakeibo.Api/Features/Recurring/`**:
- Jobs/GenerateRecurringTransactions.cs — Hangfire daily job
- GetForecast/

**Endpoints**:
- `GET /api/recurring-patterns/forecast?days=30`

**Events**:
- `RecurringTransactionGeneratedEvent`

### Frontend
**sites/Kakeibo.App/src/components/recurring/**:
- ForecastList.vue, UpcomingTransactionCard.vue

---

## Acceptance Criteria

- [ ] Hangfire job generates transactions
- [ ] Transactions created from patterns
- [ ] Forecast calculated for 30/90 days
- [ ] Frontend: forecast view
- [ ] E2E test: create pattern → wait for generation → verify transaction

---

## Definition of "Phase 6b Completed"

1. Auto-generation functional
2. All 5 acceptance criteria checked
3. Phase 7 can begin
