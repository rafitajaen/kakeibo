# Phase 5a: Savings Goal CRUD Backend + UI

**Status**: Not Started
**Objective**: Implement savings goal creation and management

---

## Scope

### ✅ Included
- Create goal (name, target amount, deadline, linked wallet)
- List goals with filters
- Update goal
- Delete goal
- Frontend: goal list, create/edit forms

### ❌ Excluded
- Progress tracking — Phase 5b

---

## Deliverables

### Backend
**Kakeibo.Modules.Goals/Features/**:
- CreateGoal, ListGoals, UpdateGoal, DeleteGoal

**Endpoints**:
- `POST /api/goals`
- `GET /api/goals`
- `PUT /api/goals/{id}`
- `DELETE /api/goals/{id}`

### Frontend
**sites/Kakeibo.App/src/views/goals/**:
- GoalsView.vue, CreateGoalView.vue

---

## Acceptance Criteria

- [ ] Create goal
- [ ] List user's goals
- [ ] Update goal
- [ ] Delete goal
- [ ] Frontend: goal list
- [ ] Frontend: create/edit goal form

---

## Definition of "Phase 5a Completed"

1. Goal CRUD functional
2. All 6 acceptance criteria checked
3. Phase 5b can begin
