# Phase 4a: Budget CRUD Backend + UI

**Status**: Not Started
**Objective**: Implement budget creation and management

---

## Scope

### ✅ Included
- Create budget (category, time period, limit amount, wallet(s) to monitor)
- List budgets with filters
- Update budget
- Delete budget
- Frontend: budget list, create/edit forms

### ❌ Excluded
- Budget monitoring (current spending) — Phase 4b

---

## Deliverables

### Backend
**`src/Kakeibo.Api/Features/Budgets/`**:
- CreateBudget/, ListBudgets/, UpdateBudget/, DeleteBudget/

**Endpoints**:
- `POST /api/budgets`
- `GET /api/budgets`
- `PUT /api/budgets/{id}`
- `DELETE /api/budgets/{id}`

### Frontend
**src/Kakeibo.App/views/budgets/**:
- BudgetsView.vue, CreateBudgetView.vue

**src/Kakeibo.App/components/budgets/**:
- BudgetList.vue, BudgetForm.vue

---

## Acceptance Criteria

- [ ] Create budget with category + period + limit
- [ ] List user's budgets
- [ ] Update budget
- [ ] Delete budget
- [ ] Frontend: budget list
- [ ] Frontend: create/edit budget form

---

## Definition of "Phase 4a Completed"

1. Budget CRUD functional
2. All 6 acceptance criteria checked
3. Phase 4b can begin
