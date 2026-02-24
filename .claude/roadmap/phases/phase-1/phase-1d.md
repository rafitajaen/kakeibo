# Phase 1d: Identity Frontend

**Status**: Complete
**Objective**: Implement login, register, and password recovery screens with automatic token refresh

---

## Prerequisites

| Item | Status |
|------|--------|
| Identity Backend | ✅ Phase 1b |
| Vue PWA base app | ✅ Phase 1a |
| i18n setup | ✅ Phase 1a |

---

## Scope

### ✅ Included

**Screens**:
- Login (email + password)
- Register (email + password + confirm)
- Email verification (from email link)
- Forgot password (email input)
- Reset password (new password + confirm)

**Infrastructure**:
- Pinia `useAuthStore` (login, register, logout, token refresh)
- Axios interceptor (inject auth headers)
- Axios 401 handler (automatic token refresh)
- Route guards (redirect to login if unauthenticated)
- i18n for all auth messages (English + Spanish)

### ❌ Excluded

- OAuth / social login buttons — post-MVP
- Password strength meter — basic validation only
- "Remember me" checkbox — refresh tokens handle this

---

## Deliverables

### New Files

**src/Kakeibo.App/views/auth/**:
```
LoginView.vue
RegisterView.vue
VerifyEmailView.vue
ForgotPasswordView.vue
ResetPasswordView.vue
```

**src/Kakeibo.App/components/auth/**:
```
LoginForm.vue
RegisterForm.vue
ForgotPasswordForm.vue
ResetPasswordForm.vue
```

**src/Kakeibo.App/stores/**:
```
auth.ts                 — useAuthStore
```

**src/Kakeibo.App/lib/**:
```
axios.ts                — Axios instance with interceptors
```

### Routes

```typescript
{
  path: '/login',
  name: 'login',
  component: () => import('@/views/auth/LoginView.vue'),
  meta: { requiresAuth: false }
},
{
  path: '/register',
  name: 'register',
  component: () => import('@/views/auth/RegisterView.vue'),
  meta: { requiresAuth: false }
},
// ... more auth routes
```

---

## Acceptance Criteria

- [ ] Login screen validates email format and password
- [ ] Register screen validates password confirmation
- [ ] Email verification shows success/failure
- [ ] Password recovery sends email
- [ ] Password reset validates token
- [ ] `useAuthStore` manages tokens
- [ ] Axios interceptor injects auth headers
- [ ] Axios 401 handler triggers refresh
- [ ] Route guards redirect unauthenticated users
- [ ] All text translated (en + es)
- [ ] E2E test: register → verify → login → logout
- [ ] E2E test: password recovery

---

## Definition of "Phase 1d Completed"

1. All 5 screens functional
2. Auth infrastructure complete
3. Tests pass (E2E)
4. Phase 2 can begin

---

**Next Phase:** [Phase 2: Wallets & Collaboration](../phase-2/phase-2.md)
