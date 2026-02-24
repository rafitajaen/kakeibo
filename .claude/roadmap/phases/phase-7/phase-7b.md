# Phase 7b: Activity Logging Backend + UI

**Status**: Not Started
**Objective**: Implement user activity feed and action history

---

## Scope

### ✅ Included
- Activity logging for user actions (create wallet, record transaction, etc.)
- Activity feed (chronological list of actions)
- Filtering by date range, action type
- Frontend: activity feed, filters

### ❌ Excluded
- Audit trail export — Phase 8
- Activity analytics — post-MVP

---

## Deliverables

### Backend
**Uses existing Auditing infrastructure** (ClickHouse audit events from Phase 1c)
**`src/Kakeibo.Api/Features/Auditing/`**:
- GetActivityFeed/

**Endpoints**:
- `GET /api/activity?start=...&end=...&type=...`

### Frontend
**src/Kakeibo.App/views/activity/**:
- ActivityView.vue

**src/Kakeibo.App/components/activity/**:
- ActivityFeed.vue, ActivityItem.vue, ActivityFilters.vue

---

## Acceptance Criteria

- [ ] Activity feed shows user actions
- [ ] Filter by date range
- [ ] Filter by action type
- [ ] Frontend: activity feed
- [ ] Frontend: filter controls

---

## Definition of "Phase 7b Completed"

1. Activity logging functional
2. All 5 acceptance criteria checked
3. Phase 8 can begin
