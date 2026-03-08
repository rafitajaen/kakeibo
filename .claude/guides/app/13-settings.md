# 13 — Settings

## Purpose

The Settings section is where users manage their personal account configuration. It is organized into six tabs: Profile, Display, Security, Sessions, Data, and Import/Export.

---

## Settings View

The settings view (`/settings`) renders a six-tab interface. The active tab is preserved in the URL hash or query parameter so that sharing or refreshing the URL returns to the same tab. All tabs load their data independently on first access.

---

## Profile Tab

The Profile tab lets the user update their personal information.

**Fields:**

- **Username** — a text input. The user's unique public identifier (used in friend search and shared wallet contexts). Constraints: lowercase letters, numbers, and underscores only. Minimum 3 characters, maximum 30. Must be unique across all users.
- **Display name** — a text input. The user's friendly name shown in the UI (not required to be unique). Maximum 100 characters.
- **Currency** — a select dropdown showing the same currency list as registration. Changing the currency updates the display format for all wallets and transactions going forward but does not convert existing amounts.
- **Avatar** — an avatar upload control. The user can drag and drop an image file onto the upload area or click to open a file picker. Accepted formats: JPEG, PNG, WebP. Uploaded images are stored in object storage (MinIO). A preview is shown after selection. The currently uploaded avatar is displayed if one exists.

**Actions:**

- **Save changes** — submits the profile update to the API. On success, a success toast is shown and the user's name and avatar in the sidebar `NavUser` footer update immediately via the auth store.

**Error handling:**

- HTTP 409: "This username is already taken."
- HTTP 422: specific validation messages for each field.

---

## Display Tab

The Display tab controls how dates, numbers, and currencies are shown throughout the app.

**Fields:**

- **Week start day** — a select dropdown. Options: Monday through Sunday. Affects calendar views and weekly budget/goal displays.
- **Month start day** — a select dropdown. Options: 1 through 31. The day of the month considered the start of a financial month (affects month-relative calculations and dashboards).
- **Currency format section:**
  - **Decimal separator** — a select. Options: Period (.), Comma (,), Space, None.
  - **Thousands separator** — a select. Options: Period (.), Comma (,), Space, None.
  - **Symbol position** — a select. Options: Before amount, After amount.
  - **Display mode** — a select. Options: Symbol (e.g., "$"), Code (e.g., "USD"), None (shows number only).
- **Live preview** — a text area below the currency format controls showing an example amount formatted with all currently selected options. Updates in real time as the user adjusts settings.

**Actions:**

- **Save preferences** — submits the display settings to the API. On success, the format preferences are applied globally across the app immediately.

---

## Security Tab

The Security tab has two sections: password change and account deletion.

**Change Password section:**

Fields:
- **Current password** — a password input. Required.
- **New password** — a password input. Required. Must meet the same strength rules as registration (8+ characters, uppercase, lowercase, digit).
- **Confirm new password** — a password input. Required. Must match the new password.

Action:
- **Update password** — submits the password change request. On success, a success message is shown. If the current password is wrong, an inline error is shown.

**Delete Account section:**

Displayed below the password change form with a red destructive-styled border or card.

Content: a warning explaining that deleting the account is irreversible, that all personal data (wallets, transactions, budgets, goals, categories, recurring patterns) will be permanently deleted after a 30-day grace period, and that membership in shared wallets will be removed immediately.

Action:
- **Delete my account** — opens a confirmation dialog that requires the user to type their email address to confirm the deletion. Only after correct confirmation is the deletion request submitted. After submission, the user is logged out and redirected to the login screen with a message that their account is scheduled for deletion.

---

## Sessions Tab

The Sessions tab shows all active login sessions for the current account.

Each session is shown as a row in a `SessionsList` component with:

- IP address.
- Device description (browser and OS derived from the user agent).
- Sign-in date and time.
- A label indicating whether this is the **current session**.

**Actions:**

- **Revoke** button on each non-current session — invalidates that session's refresh token, forcing the device to log in again. The session row is removed immediately after revocation.
- The current session cannot be revoked from this view (the user must log out normally).

On mount, the view fetches the list of active sessions from the API.

---

## Data Tab

The Data tab provides tools for loading and clearing test/sample data.

**Load test data section:**

- Explanation that this will populate the account with sample wallets, categories, transactions, budgets, and goals for exploration purposes.
- **Load test data** button — triggers a POST request to `/api/users/me/seed-data`. On success, a success message is shown and the stores are refreshed to reflect the new data.

**Delete test data section:**

- Explanation that this removes all data that was created by the seed data loader.
- **Delete test data** button — triggers a DELETE request to `/api/users/me/seed-data`. A confirmation dialog appears before deletion. On success, a success message is shown and the stores are refreshed (now empty).

Both buttons show loading states while their requests are in progress.

---

## Import/Export Tab

The Import/Export tab provides data portability tools.

**Export section:**

Two export format buttons:

- **Export as SQLite (.db)** — downloads a SQLite database file containing all of the user's data (wallets, transactions, categories, budgets, goals, recurring patterns). Useful for advanced analysis in any SQLite-compatible tool.
- **Export as CSV (.zip)** — downloads a ZIP archive containing multiple CSV files, one per data type (transactions.csv, wallets.csv, categories.csv, etc.). Useful for importing into spreadsheet applications.

Both buttons trigger a download immediately. While the export is being generated, the button shows a loading state.

**Import section:**

Fields:
- **File selector** — a file input. The user selects a file to import.
- **Source format** — a select dropdown specifying the format of the file being imported. Options include the app's own SQLite and CSV formats, and potentially third-party export formats (e.g., from other budgeting apps).

Action:
- **Import** — uploads the file and source format to the API. The API processes the import asynchronously and notifies the user when complete via an in-app notification. While uploading, the button shows a loading state.
