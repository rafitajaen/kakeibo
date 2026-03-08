# 01 — Authentication

## Purpose

This document describes the five authentication screens: login, registration, email verification, forgot password, and reset password. It also covers the invitation acceptance flow, which is accessible to unauthenticated users.

---

## Login

The login view is a centered card page. It displays the Kakeibo logo at the top, followed by the login form.

**Fields:**

- **Email** — text input, autocomplete set to `email`. Required. Validated as a properly formatted email address.
- **Password** — password input, autocomplete set to `current-password`. Required.

**Actions:**

- **Sign in button** — submits the form. While submitting, the button shows a loading state and is disabled to prevent double submission. On success, the app redirects to the dashboard (or the originally requested URL if the user was redirected here from a protected route).
- **Forgot password link** — navigates to the forgot password screen.
- **Google OAuth button** — initiates Google OAuth flow. Redirects to Google's authentication page. On return, the app completes the OAuth handshake and logs the user in.
- **Sign up link** — navigates to the register screen.

**Error handling:**

- HTTP 401: displays "Invalid email or password" message below the form.
- HTTP 400 with unverified email flag: displays a message telling the user to verify their email first, with a link to resend the verification email.
- Network or server errors: displays a generic error message.

The form is validated client-side before submission. Both fields are required. The email must be a valid email format.

---

## Register

The registration view is a centered card page. It allows new users to create an account.

**Fields:**

- **Email** — text input, autocomplete set to `email`. Required. Validated as a valid email address.
- **Password** — password input, autocomplete set to `new-password`. Required. Must be at least 8 characters, contain at least one uppercase letter, one lowercase letter, and one digit. A password strength indicator is shown below the field.
- **Confirm password** — password input. Required. Must match the password field exactly.
- **Currency** — a select dropdown. Required. Default value is EUR. Available options: EUR, USD, GBP, JPY, CAD, AUD, CHF, CNY, INR, BRL, MXN. This sets the user's base currency for all wallets and transactions.

**Actions:**

- **Create account button** — submits the form. Shows loading state while the API call is in progress. On success, displays a message prompting the user to check their email for a verification link. After a two-second delay, automatically redirects to the login screen.
- **Sign in link** — navigates back to the login screen.

**Error handling:**

- HTTP 409: displays "An account with this email already exists" message.
- Validation errors: each field shows its inline error below the input.

---

## Email Verification

The email verification view handles the link the user clicks in their verification email. The URL contains a verification token as a query parameter.

On mount, the view automatically submits the verification token to the API. While processing, a loading spinner is shown.

On success, the view displays a "Email verified successfully" message and a link to proceed to login.

On failure (expired or invalid token), the view displays an error message and a button to resend the verification email. The resend action requires the user to enter their email address.

---

## Forgot Password

The forgot password view allows users who cannot access their account to request a password reset link.

**Fields:**

- **Email** — text input. Required. Validated as a valid email address.

**Actions:**

- **Send reset link button** — submits the form. On success, displays a confirmation message telling the user to check their email. The form is hidden after success to prevent duplicate submissions.
- **Back to login link** — navigates to the login screen.

The API always returns a success response for this endpoint regardless of whether the email exists (to prevent email enumeration). The user sees the same confirmation message either way.

---

## Reset Password

The reset password view is reached by clicking the link in the password reset email. The URL contains a reset token as a query parameter.

**Fields:**

- **New password** — password input. Required. Must meet the same strength requirements as registration (8+ characters, uppercase, lowercase, digit).
- **Confirm new password** — password input. Required. Must match the new password field.

**Actions:**

- **Reset password button** — submits the form along with the token from the URL. On success, displays a "Password reset successfully" message and automatically redirects to login after a brief delay.
- **Back to login link** — navigates to the login screen without completing the reset.

**Error handling:**

- HTTP 400 with expired token: displays "This reset link has expired" message with an option to request a new one.
- HTTP 400 with invalid token: displays "This reset link is invalid" message.
- Validation errors: inline messages under each field.

---

## Accept Invitation

The invitation acceptance view is a public route (accessible without authentication) reached via a link in an invitation email. The URL contains an invitation code.

On mount, the view fetches the invitation details using the code. It displays the name of the shared wallet the user is being invited to, and the name of the person who sent the invitation.

**Actions:**

- **Accept invitation button** — if the user is authenticated, immediately accepts the invitation and redirects to the wallet. If the user is not authenticated, redirects to the login screen with the invitation URL preserved as a redirect target, so that after login or registration the acceptance flow continues.
- **Decline or ignore** — the view does not have an explicit decline button; the user can simply navigate away.

**Error handling:**

- Expired invitation: displays "This invitation has expired" message.
- Already a member: displays "You are already a member of this wallet" message.
- Invalid code: displays "This invitation is invalid" message.
