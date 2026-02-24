# Phase 1: Foundation & Authentication

**Status**: Partially Complete (1a in progress)
**Blocks**: All other phases
**Requires**: None (foundation phase)

---

## Prerequisites

All prerequisites are part of Phase 1a (Infrastructure Base).

---

## Sub-Phase Split

| Phase | Name | Duration | Deliverable |
|-------|------|----------|-------------|
| **1a** | Infrastructure Base | 2-3 days | Docker Compose, CI/CD, project scaffolding, Common interfaces, Events System (ChannelEventBus + EventDispatcher) |
| **1b** | Identity Backend | 4-5 days | User registration, login, JWT tokens, password recovery |
| **1c** | Audit Logging | 1-2 days | ClickHouse integration for audit trail, IEventHandler<T> implementations |
| **1d** | Identity Frontend | 2-3 days | Login/register screens, token refresh, route guards |

**Total estimated duration**: 10-15 days

**Sequential dependencies**: 1a → 1b → 1c → 1d

---

## Scope

### ✅ Included

**Infrastructure (1a)**:
- 2 projects scaffolded (`Kakeibo.Api` + `Kakeibo.Tests`)
- Docker Compose with 8 services
- CI/CD pipeline
- Email renderer service
- Vue PWA shell
- Common abstractions (`Entity`, `Result<T>`, `Error`, `IEndpoint`, `IEvent`, `IEventBus`, `IEventHandler<T>`)

**Authentication Backend (1b)**:
- User registration with email verification
- JWT access tokens (15min) + HttpOnly refresh tokens (7 days)
- Password recovery flow with email tokens
- PBKDF2-SHA512 password hashing
- Session tracking
- OAuth (Google, Apple) — basic implementation

**Event Infrastructure (1a + 1c)**:
- In-memory event bus (`IEventBus` / `ChannelEventBus`) with `System.Threading.Channels` — implemented in 1a
- `EventDispatcher` BackgroundService dispatches to `IEventHandler<T>` implementations — implemented in 1a
- ClickHouse audit trail for all user actions — implemented in 1c
- Audit handlers registered as `IEventHandler<T>` — implemented in 1c

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

**Module folder**: `Kakeibo.Api/Features/Identity/`
**Schema**: `public` (single schema via `AppDbContext`)
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

### Phase 1a — Infrastructure Base
- [ ] 2 projects (`Kakeibo.Api` + `Kakeibo.Tests`) build successfully
- [ ] Docker Compose starts 8 infrastructure services
- [ ] Email renderer responds on /health
- [ ] API responds on /health
- [ ] Vue PWA builds successfully
- [ ] CI pipeline has all 4 jobs defined
- [ ] Pre-commit hooks configured
- [ ] Common abstractions exist (`Entity`, `Result<T>`, `Error`, `IEndpoint`, `IEvent`, `IEventBus`, `IEventHandler<T>`)

### Phase 1b — Identity Backend
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

### Phase 1a — Infrastructure Base (Events System included)
- [ ] `ChannelEventBus` (singleton) writes events to `Channel<IEvent>`
- [ ] `EventDispatcher` (BackgroundService) reads channel and dispatches to `IEventHandler<T>`
- [ ] All `IEventHandler<T>` implementations auto-registered via Scrutor
- [ ] Publishing with no registered handler discards silently (no exception)
- [ ] DI scope created per event in dispatcher
- [ ] Architecture tests: Events infrastructure naming conventions

### Phase 1c — Audit Logging
- [ ] ClickHouse `audit_events` table with indefinite retention
- [ ] `ClickHouseAuditService` writes batched events
- [ ] `IEventHandler<T>` implementations in `Features/Auditing/` receive events from `IEventBus`
- [ ] Health check for ClickHouse connectivity
- [ ] Integration test: event persistence and query

### Phase 1d — Identity Frontend
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

**Total acceptance criteria**: 40 items

---

## Definition of "Phase 1 Completed"

1. All four sub-phases (1a, 1b, 1c, 1d) complete
2. Infrastructure fully functional (Docker, CI, Email service)
3. Authentication works end-to-end (register → verify → login → logout)
4. Events System delivers events via in-memory channel to registered handlers
5. Audit logging captures all actions in ClickHouse
6. All 40 acceptance criteria checked
7. CI pipeline green (all tests pass)
8. Manual testing complete in Docker Compose
9. Code review complete (all PRs merged)
10. Documentation updated (API endpoints in Scalar)
11. Phase 2 can begin (Wallets depends on authentication)

---

**Next Phase**: [Phase 2 — Wallets & Collaboration](../phase-2/phase-2.md)
