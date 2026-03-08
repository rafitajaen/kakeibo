# 14 — Admin Panel

## Purpose

The Admin Panel provides platform-level management tools available exclusively to users with the Admin role. It covers user account management and platform-wide policy configuration.

---

## Access Control

The admin view (`/admin`) is protected by a role guard. When the route is activated, the app checks the current user's role. If the user does not have the `Admin` role, they are immediately redirected to the dashboard. The Admin navigation item in the sidebar is only rendered for admin users, so non-admin users typically never see the link.

---

## Admin View Structure

The admin view uses a two-tab interface: **Users** and **Platform**.

---

## Users Tab

The Users tab is rendered by a `UserManagement` component. It provides a searchable table of all registered users in the platform.

**Search:**

A text input at the top of the tab. The user can search by email address or username. The search is performed on input change (debounced) against the admin user search API endpoint. Results replace the full user list.

**User table:**

Each row in the table shows:

- Email address.
- Username.
- Account status badge:
  - **Active** — account is in good standing.
  - **Unverified** — registered but email not yet verified.
  - **Blocked** — account has been blocked by an admin.
- Registration date.
- Actions:
  - **Block** — shown for Active or Unverified users. Blocks the account, preventing login. A confirmation dialog appears before blocking. After blocking, the status badge changes to Blocked.
  - **Unblock** — shown for Blocked users. Restores the account to Active status.
  - **Delete** — permanently deletes the user account and all associated data. A strongly-worded confirmation dialog appears with explicit warning about irreversibility. Requires confirmation before proceeding.

Confirmation dialogs for block, unblock, and delete actions show the affected user's email to prevent accidental actions on the wrong account.

---

## Platform Tab

The Platform tab is rendered by a `PlatformPolicyForm` component. It controls global platform behavior via toggle switches.

**Fields:**

- **Allow new registrations** — a checkbox or toggle switch. When unchecked, the registration endpoint returns a 403 error to new sign-up attempts, effectively closing the platform to new users. A description below the toggle explains: "Uncheck to prevent new users from creating accounts." The current state is fetched from the API on mount.

- **Maintenance mode** — a checkbox or toggle switch. When checked, the platform enters maintenance mode. In maintenance mode, all API requests from non-admin users receive a 503 Service Unavailable response. Admin users are not affected and can continue using the platform normally. A description explains: "When enabled, all non-admin users will see a maintenance screen." The backend caches this setting for 30 seconds, so changes take effect within 30 seconds of saving.

**Actions:**

- **Save** — submits the updated platform policy to the API. On success, a success toast is shown. Changes take effect immediately (subject to the 30-second maintenance mode cache window).

The Platform Policy form loads current policy values from the API on mount so that the displayed state always reflects the server's authoritative configuration.
