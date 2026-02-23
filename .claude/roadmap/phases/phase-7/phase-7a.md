# Phase 7a: Notification System Backend + UI

**Status**: Not Started
**Objective**: Implement multi-channel notification delivery

---

## Scope

### ✅ Included
- Email notifications (via Email service)
- In-app notifications (stored in DB)
- Push notifications (basic web push)
- Notification types: Budget alerts, Goal milestones, Invitations, System messages
- User preferences (opt-in/opt-out per type)
- Frontend: notification bell, notification list, preferences

### ❌ Excluded
- SMS notifications — post-MVP
- WhatsApp notifications — post-MVP
- Notification grouping — post-MVP

---

## Deliverables

### Backend
**Kakeibo.Modules.Notifications/Features/**:
- SendNotification, ListNotifications, MarkAsRead, UpdatePreferences

**Consumers:**:
- BudgetExceededConsumer, GoalMilestoneConsumer, InvitationSentConsumer, etc.

**Endpoints**:
- `GET /api/notifications`
- `PUT /api/notifications/{id}/read`
- `GET /api/notifications/preferences`
- `PUT /api/notifications/preferences`

### Frontend
**sites/Kakeibo.App/src/components/notifications/**:
- NotificationBell.vue, NotificationList.vue, PreferencesForm.vue

---

## Acceptance Criteria

- [ ] Email notifications sent for budget alerts
- [ ] In-app notifications displayed
- [ ] Push notifications sent (web push)
- [ ] User can opt-out per notification type
- [ ] Frontend: notification bell with unread count
- [ ] Frontend: notification list
- [ ] Frontend: preferences form

---

## Definition of "Phase 7a Completed"

1. Notification system functional
2. All 7 acceptance criteria checked
3. Phase 7b can begin
