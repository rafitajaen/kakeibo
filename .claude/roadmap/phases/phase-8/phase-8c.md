# Phase 8c: Settings + Profile

**Status**: Not Started
**Objective**: Implement user settings and profile management

---

## Scope

### ✅ Included
- Profile (name, email, currency preference)
- Password change
- Notification preferences
- Account deletion (30-day grace period, GDPR compliant)
- Session management (view active sessions, revoke session)
- Frontend: settings screens

### ❌ Excluded
- Two-factor authentication — post-MVP
- Email change (requires re-verification) — post-MVP

---

## Deliverables

### Backend
**Kakeibo.Modules.Identity/Features/**:
- UpdateProfile, ChangePassword, DeleteAccount, ListSessions, RevokeSession

**Kakeibo.Modules.Identity/Jobs/**:
- `AccountDeletionJob.cs` — Hangfire daily job that permanently deletes users where
  `DeletionRequestedAt < now() - 30 days` (GDPR 30-day grace period enforcement)

**Endpoints**:
- `PUT /api/users/me/profile`
- `PUT /api/users/me/password`
- `DELETE /api/users/me`
- `GET /api/users/me/sessions`
- `DELETE /api/users/me/sessions/{id}`

### Frontend
**sites/Kakeibo.App/src/views/settings/**:
- SettingsView.vue, ProfileView.vue, SecurityView.vue, SessionsView.vue

---

## Acceptance Criteria

- [ ] Update profile (name, currency)
- [ ] Change password
- [ ] Delete account (marks `DeletionRequestedAt`, starts 30-day grace period)
- [ ] Background job permanently deletes accounts after 30-day grace period (Hangfire `AccountDeletionJob`)
- [ ] View active sessions
- [ ] Revoke session
- [ ] Frontend: settings navigation
- [ ] Frontend: all settings forms
- [ ] E2E test: change password

---

## Definition of "Phase 8c Completed"

1. Settings functional
2. AccountDeletionJob operational (Hangfire daily schedule)
3. All acceptance criteria checked
4. Phase 8d can begin
