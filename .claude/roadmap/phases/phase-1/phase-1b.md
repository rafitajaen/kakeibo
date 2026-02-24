# Phase 1b: Identity Backend

**Status**: Not Started
**Objective**: Implement JWT authentication with user registration and password recovery

---

## Prerequisites

| Item | Status |
|------|--------|
| Infrastructure Base | ✅ Phase 1a |
| Email Service | ✅ Phase 1a |
| PostgreSQL | ✅ Phase 1a |

---

## Scope

### ✅ Included

**User Management**:
- Registration (email + password + currency preference, default: EUR)
- Email verification (confirmation link)
- Login (JWT access + refresh tokens)
- Token refresh with rotation
- Password recovery (email token)
- Password reset
- Session tracking (Session entity with device info)
- Logout all sessions (`POST /api/auth/logout-all`)

**Security**:
- PBKDF2-SHA512 password hashing
- JWT tokens: access (15min), refresh (7 days, HttpOnly cookie)
- Email verification required for login
- Password strength validation (min 8 chars, upper, lower, digit)

### ❌ Excluded

- OAuth (Google, Apple) — Post-MVP
- MFA — post-MVP
- Account deletion — Phase 8c
- Password change UI — Phase 8c
- Session management UI — Phase 8c

---

## Deliverables

### Feature Structure

**`src/Kakeibo.Api/`** (vertical slices within single project):
```
Domain/Entities/
  User.cs              — includes Currency field (selected at registration)
  RefreshToken.cs
  PasswordResetToken.cs
  Session.cs           — Id, UserId, RefreshTokenHash, IpAddress, UserAgent, CreatedAt, ExpiresAt, RevokedAt

Features/Identity/
  RegisterUser/
    RegisterUserEndpoint.cs
    RegisterUserHandler.cs
    RegisterUserValidator.cs
  VerifyEmail/
    VerifyEmailEndpoint.cs
    VerifyEmailHandler.cs
    VerifyEmailValidator.cs
  LoginUser/
    LoginUserEndpoint.cs
    LoginUserHandler.cs
    LoginUserValidator.cs
  RefreshToken/
    RefreshTokenEndpoint.cs
    RefreshTokenHandler.cs
  LogoutUser/
    LogoutUserEndpoint.cs
    LogoutUserHandler.cs
  ForgotPassword/
    ForgotPasswordEndpoint.cs
    ForgotPasswordHandler.cs
    ForgotPasswordValidator.cs
  ResetPassword/
    ResetPasswordEndpoint.cs
    ResetPasswordHandler.cs
    ResetPasswordValidator.cs
  GetCurrentUser/
    GetCurrentUserEndpoint.cs
    GetCurrentUserHandler.cs
  Events/
    UserRegisteredEvent.cs
    UserLoggedInEvent.cs
    UserLoggedOutEvent.cs

Infrastructure/Auth/
  JwtService.cs

Persistence/Configurations/
  UserConfiguration.cs
  SessionConfiguration.cs
```

### Endpoints

- `POST /api/auth/register` — includes `currency` field (required)
- `POST /api/auth/verify-email`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/logout-all` — revokes all active sessions for current user
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
- [ ] User selects currency at registration (stored in User.Currency field)
- [ ] Verification email sent with token
- [ ] Email verification marks user as verified
- [ ] Login validates credentials + email verification
- [ ] JWT tokens issued (access 15min, refresh 7d)
- [ ] Token refresh rotates refresh token
- [ ] Password recovery sends email with token
- [ ] Password reset validates token and updates password
- [ ] Logout-all revokes all active sessions for current user
- [ ] Integration events published
- [ ] Unit tests >= 90% coverage
- [ ] Integration tests: full auth flows

---

## Definition of "Phase 1b Completed"

1. All 9 endpoints functional
2. All security measures implemented
3. Integration events published
4. Tests pass (unit + integration)
5. Phase 1c (Audit Logging) can begin

---

**Next Sub-Phase:** [Phase 1c: Audit Logging](./phase-1c.md)
