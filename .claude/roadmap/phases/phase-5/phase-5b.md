# Phase 5b: Goal Progress Tracking Backend + UI

**Status**: Not Started
**Objective**: Implement goal progress monitoring and milestone notifications

---

## Scope

### ✅ Included
- Wallet-linked progress tracking
- Milestone detection (25%, 50%, 75%, 100%)
- Milestone events published
- Frontend: progress visualization, percentage complete

### ❌ Excluded
- Cross-wallet tracking — post-MVP
- Manual progress updates — post-MVP
- Projected completion date — post-MVP

---

## Deliverables

### Backend
**`src/Kakeibo.Api/Features/Goals/`**:
- Events/TransactionRecordedHandler.cs — `IEventHandler<TransactionRecordedEvent>` for progress updates
- GetGoalProgress/

**Endpoints**:
- `GET /api/goals/{id}/progress`

**Events**:
- `GoalMilestoneReachedEvent`
- `GoalAchievedEvent`

### Frontend
**sites/Kakeibo.App/src/components/goals/**:
- GoalProgressBar.vue, GoalMilestoneIndicator.vue

---

## Acceptance Criteria

- [ ] Listen to `TransactionRecordedEvent` → update progress
- [ ] Calculate current progress from wallet balance
- [ ] Detect milestones (25%, 50%, 75%, 100%)
- [ ] Publish milestone events
- [ ] Frontend: progress bars
- [ ] Frontend: milestone indicators

---

## Definition of "Phase 5b Completed"

1. Goal tracking operational
2. All 6 acceptance criteria checked
3. Phase 7 can use goal alerts
