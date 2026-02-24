# Phase 4: Budgets

**Status**: Not Started
**Blocks**: Phase 7 (Notifications need budget alerts)
**Requires**: Phase 3b (Transactions)

---

## Sub-Phase Split

| Phase | Name | Duration |
|-------|------|----------|
| **4a** | Budget CRUD Backend + UI | 2-3 days |
| **4b** | Budget Monitoring Backend + UI | 2-3 days |

**Total**: 4-6 days

---

## Scope

### ✅ Included
- Budget creation (category, period, limit, wallet(s))
- Budget monitoring (current spending, remaining, percentage)
- Budget alerts (warning, exceeded)
- Frontend: budget list, create/edit, progress bars

### ❌ Excluded
- Budget templates — post-MVP
- Budget rollover — post-MVP

---

## Module Architecture

**Location**: `src/Kakeibo.Api/Features/Budgets/`
**Schema**: `public` (single schema, shared with all domains)

**Endpoints**:
- `GET/POST/PUT/DELETE /api/budgets`
- `GET /api/budgets/{id}/status`

**Events**:
- `BudgetExceededEvent`
- `BudgetWarningEvent`

---

## MVP Acceptance Criteria

- [ ] Create budget
- [ ] Monitor spending vs limit
- [ ] Budget alerts published
- [ ] Frontend: budget dashboard

---

## Definition of "Phase 4 Completed"

1. Both sub-phases complete
2. All acceptance criteria checked
3. Phase 7 can proceed

---

**Next Phase**: Phase 5 (Goals) or Phase 6 (Recurring) — can run in parallel
