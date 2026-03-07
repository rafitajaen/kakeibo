# Registration Flow

Complete authentication flow documentation covering email+password, Google OAuth, email verification, and username auto-generation.

---

## Authentication Methods

### 1. Email + Password Registration

```
POST /api/auth/register { email, password, currency }
```

1. Validate email uniqueness (case-insensitive, normalized to lowercase)
2. Hash password with PBKDF2-SHA512 via `PasswordHasher`
3. Auto-generate username: `user_` + 7 random lowercase alphanumeric chars (e.g., `user_abc1234`)
4. Generate email verification token (32-char random string, stored as SHA-256 hash)
5. Create `User` with `IsVerified = false`
6. Publish `UserRegisteredEvent`
7. Send verification email asynchronously (non-blocking)
8. Return `{ id, email }` — user must verify email before login

### 2. Google OAuth (Login or Register)

```
POST /api/auth/google { idToken, currency }
```

Three flows based on existing data:

| Scenario | Lookup | Action |
|----------|--------|--------|
| **Flow 1**: Existing user by GoogleId | `GoogleId == payload.Subject` | Login — issue tokens |
| **Flow 2**: Existing user by email | `Email == payload.Email` | Link GoogleId, mark verified if not, login |
| **Flow 3**: New user | No match | Create user (`PasswordHash = null`, `IsVerified = true`, `GoogleId = payload.Subject`), auto-generate username, publish `UserRegisteredEvent` |

**Token validation**: `GoogleJsonWebSignature.ValidateAsync()` with `Audience = [GoogleAuthOptions.ClientId]`. Invalid tokens return 401.

**Key behaviors:**
- Google accounts are always verified (`IsVerified = true`, `VerifiedAt = now`)
- `PasswordHash` is `null` for Google-only accounts
- Users can later set a password via a separate flow (not yet implemented)
- Account deletion cancellation: if `DeletionRequestedAt` is set, clear it on login

### 3. Email Verification

```
POST /api/auth/verify-email { token }
```

- Validates raw token against stored SHA-256 hash
- Checks expiry (24 hours from registration)
- Sets `IsVerified = true`, `VerifiedAt = now`
- Clears token fields

### 4. Email + Password Login

```
POST /api/auth/login { email, password }
```

- Requires `IsVerified = true` (returns 400 if not)
- Requires `PasswordHash is not null` (Google-only accounts return 401)
- Constant-time password verification via `PasswordHasher.VerifyPassword`
- Issues JWT access token + refresh token via HttpOnly cookies

---

## Username System

| Rule | Details |
|------|---------|
| Format | `user_` prefix + 7 chars from `[a-z0-9]` |
| Generation | Automatic at registration (both email+password and Google OAuth) |
| Uniqueness | Enforced by unique database index |
| Update | `PUT /api/users/me/username` — user can change anytime |
| Validation | 3-50 chars, `^[a-z0-9_]+$`, normalized to lowercase |
| Migration | Existing users backfilled with `user_` + first 7 hex chars of their UUID |

---

## Token & Cookie Strategy

| Cookie | Path | MaxAge | Purpose |
|--------|------|--------|---------|
| `access_token` | `/` | 15 min | JWT — identifies user, carries claims |
| `refresh_token` | `/api/auth/refresh` | 7 days | Opaque — rotated on each use |

Both cookies: `HttpOnly`, `Secure` (in HTTPS), `SameSite=Strict`.

Token management centralized in `TokenCookieService` (used by `LoginUserHandler`, `GoogleLoginHandler`, `RefreshTokenHandler`).

---

## Configuration

| Setting | Source | Required |
|---------|--------|----------|
| `GoogleAuth:ClientId` | `.env` → `GOOGLE_CLIENT_ID` | Only for Google OAuth |
| `Jwt:SecretKey` | `.env` → `JWT_SECRET_KEY` | Always |
| Frontend | `VITE_GOOGLE_CLIENT_ID` | Only for Google button |

---

## Entity Changes (Phase A)

**`User` entity:**
- `Username` (string, required, max 50, unique) — auto-generated
- `GoogleId` (string?, max 255, unique partial index where not null)
- `PasswordHash` changed from `required` to nullable (`string?`)

**Migration**: `AddUsernameAndGoogleId` — adds columns, backfills existing users, creates indexes.
