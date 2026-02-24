# Phase 7: Notifications & Activity Logging

**Status**: Not Started
**Blocks**: Phase 8
**Requires**: Phases 2, 3, 4, 5, 6 (Phase 7a consumes InvitationSentEvent from Phase 2b and TransactionRecordedEvent from Phase 3b)

---

## Sub-Phase Split

| Phase | Name | Duration |
|-------|------|----------|
| **7a** | Notification System Backend + UI | 2-3 days |
| **7b** | Activity Logging Backend + UI | 1-2 days |

**Total**: 3-5 days

> **Parallelism note:** 7a and 7b can be implemented in parallel — both are independent consumers of integration events. Recommended: start 7a first due to its larger scope (consumes all business events and sets up the notification infrastructure), then begin 7b once the notification infrastructure is established.

---

## Scope

### ✅ Included
- Multi-channel notifications (email, in-app, push)
- Notification preferences
- Activity logs (user actions, system events)
- Frontend: notification bell, activity feed, preferences

### ❌ Excluded
- SMS notifications — post-MVP
- WhatsApp notifications — post-MVP

---

## MVP Acceptance Criteria

- [ ] Email notifications sent
- [ ] In-app notifications displayed
- [ ] User preferences saved
- [ ] Activity logs recorded

---

**Next Phase**: Phase 8 (Polish & Launch)
