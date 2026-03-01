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

**`src/Kakeibo.Api/Domain/Entities/`** (shared entities):
- `Notification.cs` — in-app notification entity
- `PushSubscription.cs` — web push endpoint + keys per user/device (Id, UserId, Endpoint, P256dh, Auth, CreatedAt)

**`src/Kakeibo.Api/Features/Notifications/`**:
- SendNotification/, ListNotifications/, MarkAsRead/, UpdatePreferences/, RegisterPushSubscription/

**`src/Kakeibo.Api/Features/Notifications/Events/`** — `IEventHandler<T>` implementations for ALL events from Phases 2–6:
- `BudgetExceededHandler.cs`, `BudgetWarningHandler.cs`
- `GoalMilestoneReachedHandler.cs`, `GoalAchievedHandler.cs`
- `InvitationSentHandler.cs`, `MemberJoinedHandler.cs`, `SettlementRecordedHandler.cs`
- `RecurringTransactionGeneratedHandler.cs`

**`src/Kakeibo.Api/Infrastructure/WebPush/`**:
- `WebPushOptions.cs` — `VapidPublicKey`, `VapidPrivateKey`, `VapidSubject`
- `WebPushService.cs` — sends web push notifications using VAPID keys

**Endpoints**:
- `GET /api/notifications`
- `PUT /api/notifications/{id}/read`
- `GET /api/notifications/preferences`
- `PUT /api/notifications/preferences`
- `POST /api/users/me/push-subscriptions` — register browser push subscription

### Frontend

**src/Kakeibo.App/public/**:
- `sw.js` — service worker with `push` event handler (displays notification)

**src/Kakeibo.App/components/notifications/**:
- `NotificationBell.vue`, `NotificationList.vue`, `PreferencesForm.vue`

**Push permission flow** (in app initialization):
- `Notification.requestPermission()` called after user first login
- On grant: subscribe to push via `PushManager.subscribe()` with VAPID public key
- POST subscription to `POST /api/users/me/push-subscriptions`

---

## Acceptance Criteria

- [ ] Email notifications sent for budget alerts
- [ ] In-app notifications stored and displayed
- [ ] Web push: VAPID key pair generated and configured
- [ ] Web push: service worker (`sw.js`) registered and handles `push` events
- [ ] Web push: browser permission request flow works after login
- [ ] Web push: `PushSubscription` saved per user/device via `POST /api/users/me/push-subscriptions`
- [ ] Web push: notification sent to all registered subscriptions for user
- [ ] User can opt-out per notification type
- [ ] Event handlers (`IEventHandler<T>`) registered for ALL integration events from Phases 2–6
- [ ] Frontend: notification bell with unread count
- [ ] Frontend: notification list
- [ ] Frontend: preferences form

---

## Definition of "Phase 7a Completed"

1. Notification system functional (email + in-app + web push)
2. VAPID infrastructure configured and working
3. All event handlers (`IEventHandler<T>`) registered for Phases 2–6 events
4. All acceptance criteria checked
5. Phase 7b can begin
