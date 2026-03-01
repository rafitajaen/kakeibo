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
- Events/TransactionUpdatedHandler.cs — `IEventHandler<TransactionUpdatedEvent>` for progress recalculation
- Events/TransactionDeletedHandler.cs — `IEventHandler<TransactionDeletedEvent>` for progress recalculation
- GetGoalProgress/

**Endpoints**:
- `GET /api/goals/{id}/progress`

**Listens to**:
- `TransactionRecordedEvent`
- `TransactionUpdatedEvent`
- `TransactionDeletedEvent`

**Publishes**:
- `GoalMilestoneReachedEvent`
- `GoalAchievedEvent`

### Frontend
**src/Kakeibo.App/components/goals/**:
- GoalProgressBar.vue, GoalMilestoneIndicator.vue

---

## Acceptance Criteria

- [ ] Listen to `TransactionRecordedEvent` → update progress
- [ ] Listen to `TransactionUpdatedEvent` → recalculate progress
- [ ] Listen to `TransactionDeletedEvent` → recalculate progress
- [ ] Goal progress recalculates correctly when a transaction is edited
- [ ] Goal progress recalculates correctly when a transaction is deleted
- [ ] Calculate current progress from wallet balance
- [ ] Detect milestones (25%, 50%, 75%, 100%)
- [ ] Publish milestone events
- [ ] Frontend: progress bars
- [ ] Frontend: milestone indicators

---

## Definition of "Phase 5b Completed"

1. Goal tracking operational
2. All 10 acceptance criteria checked
3. Phase 7 can use goal alerts
