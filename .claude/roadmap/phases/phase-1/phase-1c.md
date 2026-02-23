# Phase 1c: Authentication Backend

**Status**: Not Started
**Objective**: Implement JWT authentication with user registration and password recovery

---

## Prerequisites

| Item | Status |
|------|--------|
| Outbox Pattern | ⏳ Phase 1a |
| Audit Logging | ⏳ Phase 1b |
| Email Service | ✅ Phase 0 |
| PostgreSQL | ✅ Phase 0 |

---

## Scope

### ✅ Included

**User Management**:
- Registration (email + password)
- Email verification (confirmation link)
- Login (JWT access + refresh tokens)
- Token refresh with rotation
- Password recovery (email token)
- Password reset
- Session tracking
- OAuth (Google, Apple) — basic

**Security**:
- PBKDF2-SHA512 password hashing
- JWT tokens: access (15min), refresh (7 days, HttpOnly cookie)
- Email verification required for login
- Password strength validation (min 8 chars, upper, lower, digit)

### ❌ Excluded

- MFA — post-MVP
- Account deletion — Phase 8c
- Password change UI — Phase 8c
- Session management UI — Phase 8c

---

## Deliverables

### Module Structure

**Kakeibo.Modules.Identity/**:
```
Entities/
  User.cs
  RefreshToken.cs
  PasswordResetToken.cs
  Session.cs

Features/
  Register/
  VerifyEmail/
  Login/
  RefreshToken/
  Logout/
  ForgotPassword/
  ResetPassword/
  GetCurrentUser/

Services/
  JwtService.cs
  PasswordHasher.cs
```

### Endpoints

- `POST /api/auth/register`
- `POST /api/auth/verify-email`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/me`

### Integration Events

- `UserRegisteredEvent`
- `UserLoggedInEvent`
- `UserLoggedOutEvent`

---

## Acceptance Criteria

- [ ] User registration creates unverified user
- [ ] Verification email sent with token
- [ ] Email verification marks user as verified
- [ ] Login validates credentials + email verification
- [ ] JWT tokens issued (access 15min, refresh 7d)
- [ ] Token refresh rotates refresh token
- [ ] Password recovery sends email with token
- [ ] Password reset validates token and updates password
- [ ] Integration events published
- [ ] Unit tests >= 90% coverage
- [ ] Integration tests: full auth flows

---

## Definition of "Phase 1c Completed"

1. All 8 endpoints functional
2. All security measures implemented
3. Integration events published
4. Tests pass (unit + integration)
5. Phase 1d can begin
