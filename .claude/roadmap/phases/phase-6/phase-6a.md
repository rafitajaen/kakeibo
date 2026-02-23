# Phase 6a: Recurring Patterns Backend + UI

**Status**: Not Started
**Objective**: Implement recurring pattern creation and management

---

## Scope

### ✅ Included
- Create recurring pattern (transaction template + schedule)
- Recurrence rules (daily, weekly, biweekly, monthly, yearly)
- Pattern CRUD
- Frontend: pattern list, create/edit forms

### ❌ Excluded
- Auto-generation — Phase 6b

---

## Deliverables

### Backend
**Kakeibo.Modules.Recurring/Features/**:
- CreatePattern, ListPatterns, UpdatePattern, DeletePattern

**Endpoints**:
- `POST /api/recurring-patterns`
- `GET /api/recurring-patterns`
- `PUT /api/recurring-patterns/{id}`
- `DELETE /api/recurring-patterns/{id}`

### Frontend
**sites/Kakeibo.App/src/views/recurring/**:
- RecurringView.vue, CreatePatternView.vue

---

## Acceptance Criteria

- [ ] Create recurring pattern
- [ ] List user's patterns
- [ ] Update pattern
- [ ] Delete pattern
- [ ] Frontend: pattern list
- [ ] Frontend: create/edit pattern form

---

## Definition of "Phase 6a Completed"

1. Pattern CRUD functional
2. All 6 acceptance criteria checked
3. Phase 6b can begin
