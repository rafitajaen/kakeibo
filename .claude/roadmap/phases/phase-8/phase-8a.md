# Phase 8a: Dashboard

**Status**: Not Started
**Objective**: Implement main dashboard with financial overview

---

## Scope

### ✅ Included
- Balance overview (total balance, per-wallet balances)
- Recent transactions (last 10)
- Budget status (active budgets, % used)
- Goal progress (active goals, % complete)
- Quick actions (record transaction, create wallet)
- Frontend: responsive dashboard layout

### ❌ Excluded
- Customizable dashboard widgets — post-MVP
- Export dashboard as PDF — post-MVP

---

## Deliverables

### Backend
**No new endpoints** — aggregates data from existing modules

### Frontend
**sites/Kakeibo.App/src/views/**:
- DashboardView.vue

**sites/Kakeibo.App/src/components/dashboard/**:
- BalanceOverview.vue, RecentTransactions.vue, BudgetSummary.vue, GoalSummary.vue, QuickActions.vue

---

## Acceptance Criteria

- [ ] Balance overview displays total + per-wallet
- [ ] Recent transactions list (last 10)
- [ ] Budget status shows active budgets
- [ ] Goal progress shows active goals
- [ ] Quick actions: record transaction, create wallet
- [ ] Responsive layout (mobile + desktop)
- [ ] E2E test: dashboard loads with correct data

---

## Definition of "Phase 8a Completed"

1. Dashboard functional
2. All 7 acceptance criteria checked
3. Phase 8b can begin
