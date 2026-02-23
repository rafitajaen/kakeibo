# Phase 1: Foundation & Authentication

**Status**: Not Started
**Blocks**: All other phases
**Requires**: Phase 0 (Infrastructure & Project Setup)

---

## Prerequisites

| Item | Status | Description |
|------|--------|-------------|
| Development environment | ✅ Complete (Phase 0) | Docker Compose running all infrastructure services |
| Solution structure | ✅ Complete (Phase 0) | `Kakeibo.slnx` with all 12 projects created |
| CI/CD pipeline | ✅ Complete (Phase 0) | GitHub Actions quality gates functional |
| Email renderer service | ✅ Complete (Phase 0) | `Kakeibo.Email` service running on port 3050 |

---

## Sub-Phase Split

| Phase | Name | Duration | Deliverable |
|-------|------|----------|-------------|
| **1a** | Outbox Pattern Implementation | 2-3 days | Reliable event persistence & dispatching infrastructure |
| **1b** | Audit Logging | 1-2 days | ClickHouse integration + audit trail recording |
| **1c** | Authentication Backend | 3-4 days | JWT auth, user registration, password recovery |
| **1d** | Authentication Frontend | 2-3 days | Login/register screens, token refresh, route guards |

**Total estimated duration**: 8-12 days

---

## Scope

### ✅ Included

**Infrastructure (1a + 1b)**:
- Transactional outbox pattern with guaranteed delivery
- Domain event dispatching via `IDomainEventHandler<T>`
- Integration event publishing via `IModuleEventBus`
- ClickHouse audit trail for all user actions
- Background processing with Polly retry

**Authentication Backend (1c)**:
- User registration with email verification
- JWT access tokens (15min) + HttpOnly refresh tokens (7 days)
- Password recovery flow with email tokens
- PBKDF2-SHA512 password hashing
- Session tracking
- OAuth (Google, Apple) — basic implementation

**Authentication Frontend (1d)**:
- Login, register, password recovery screens
- Pinia auth store with automatic token refresh
- Axios interceptors for auth headers
- Route guards for protected routes
- i18n for all auth messages (English + Spanish)

### ❌ Excluded

- Multi-factor authentication (MFA) — post-MVP
- Account deletion — Phase 8c
- Session management UI — Phase 8c
- Password strength meter — basic validation only
- Social login UI refinement — Phase 8b

---

## Module Architecture

**Module**: `Kakeibo.Modules.Identity`
**Schema**: `identity`
**Pattern**: Vertical slices

**Key Entities**:
- `User` (aggregate root)
- `RefreshToken`
- `PasswordResetToken`
- `Session`

**Endpoints**:
- `POST /api/auth/register`
- `POST /api/auth/verify-email`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/me`

---

## MVP Acceptance Criteria

### Phase 1a — Outbox Pattern
- [ ] `OutboxInterceptor` harvests domain events from entities
- [ ] `DomainEventDispatcher` resolves and invokes `IDomainEventHandler<T>`
- [ ] `ModuleEventBus` buffers integration events (scoped)
- [ ] `OutboxProcessor` polls outbox tables every 10s (dev), 5s (prod)
- [ ] Polly retry: 3 attempts (1s, 5s, 15s exponential backoff)
- [ ] Integration test: domain event → integration event → consumer
- [ ] Integration test: failed consumer → retry → success
- [ ] Integration test: idempotent consumer handling

### Phase 1b — Audit Logging
- [ ] ClickHouse `audit_events` table with indefinite retention
- [ ] `ClickHouseAuditService` writes batched events
- [ ] `IAuditOutbox` stages events in-memory
- [ ] Health check for ClickHouse connectivity
- [ ] Integration test: event persistence and query

### Phase 1c — Authentication Backend
- [ ] User registration creates unverified user
- [ ] Email verification flow functional
- [ ] Login validates credentials and email verification
- [ ] JWT tokens issued (access 15min, refresh 7d)
- [ ] Token refresh with automatic rotation
- [ ] Password recovery generates email token
- [ ] Password reset validates token and updates password
- [ ] Integration events: `UserRegisteredEvent`, `UserLoggedInEvent`, `UserLoggedOutEvent`
- [ ] Unit tests >= 90% coverage
- [ ] Integration tests: full auth flows

### Phase 1d — Authentication Frontend
- [ ] Login screen with validation (email format, password required)
- [ ] Register screen with password confirmation
- [ ] Email verification success/failure handling
- [ ] Password recovery request form
- [ ] Password reset form with new password
- [ ] `useAuthStore` manages tokens and user state
- [ ] Axios interceptor injects auth headers
- [ ] Axios 401 handler triggers token refresh
- [ ] Route guards redirect to `/login` if unauthenticated
- [ ] All text translated (English + Spanish)
- [ ] E2E test: register → verify → login → logout
- [ ] E2E test: password recovery flow

---

## Definition of "Phase 1 Completed"

1. All four sub-phases (1a, 1b, 1c, 1d) complete
2. Outbox Pattern delivers events reliably with retry
3. Audit logging captures all actions in ClickHouse
4. Full authentication flow functional (backend + frontend)
5. All 33 acceptance criteria checked
6. CI pipeline green (all tests pass)
7. Manual testing complete in Docker Compose
8. Code review complete (all PRs merged)
9. Documentation updated (API endpoints in Scalar)
10. Phase 2 can begin (Wallets depends on authentication)

---

**Next Phase**: Phase 2 — Wallets & Collaboration
