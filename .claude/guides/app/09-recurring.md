# 09 — Recurring Patterns

## Purpose

The Recurring section allows users to define transaction patterns that the system automatically executes on a schedule. It also provides a forecast view showing projected transactions for the next 30 or 90 days.

---

## Recurring View

The recurring view (`/recurring`) uses a two-tab interface:

**Patterns tab** — shows the user's recurring pattern definitions. A **New pattern** button at the top right navigates to the pattern creation form.

Each pattern card displays:

- Pattern name.
- Transaction type badge (Income / Expense / Transfer).
- Frequency badge (Daily / Weekly / Biweekly / Monthly / Yearly).
- Next occurrence date.
- Amount formatted as currency.
- Category icon and name.
- Source wallet name.

Actions on each card:

- **Edit** — navigates to the edit pattern view.
- **Delete** — permanently deletes the pattern. A confirmation dialog clarifies that deleting a pattern stops future auto-generation but does not remove already-generated transactions.

**Forecast tab** — shows projected upcoming transactions based on all active patterns. A toggle at the top allows switching between:

- **30 days** — shows projected transactions for the next 30 days.
- **90 days** — shows projected transactions for the next 90 days.

Switching the toggle immediately re-renders the forecast from data already fetched (no additional API call if 90-day data was pre-fetched on mount). Each forecast row shows the projected date, pattern name, amount, and category.

---

## Create Pattern

The create pattern view (`/recurring/new`) displays a centered card with a `RecurringForm` in creation mode.

---

## Edit Pattern

The edit pattern view (`/recurring/:id/edit`) uses the same `RecurringForm` in edit mode, pre-filled with the pattern's current data. All fields are editable. Editing a pattern affects only future occurrences; already-generated transactions are not changed.

---

## RecurringForm

**Fields:**

- **Name** — a text input. Required. Maximum 100 characters. A descriptive label for the recurring pattern (e.g., "Monthly Rent", "Spotify Subscription", "Weekly Grocery Run").

- **Transaction Type** — a select dropdown. Required. Options: Income, Expense, Transfer. When set to Transfer, the Destination Wallet field becomes visible.

- **Frequency** — a select dropdown. Required. Options:
  - Daily — generates a transaction every day.
  - Weekly — generates a transaction once per week on the same weekday as the start date.
  - Biweekly — generates a transaction every two weeks on the same weekday.
  - Monthly — generates a transaction on the same day of the month as the start date.
  - Yearly — generates a transaction on the same month and day each year.

- **Amount** — a number input with the built-in calculator widget (same as TransactionForm). Required. Range: 0.01 to 999,999,999.99.

- **Description** — a text input. Optional. Maximum 500 characters. Appears as the description on each auto-generated transaction.

- **Category** — a select dropdown. Required. Shows active categories with icon and name. Archived categories are excluded.

- **Wallet** — a select dropdown. Required. The wallet from which each generated transaction will be recorded. For Transfer type, this is the source wallet. Shows only active (non-archived) wallets.

- **Destination Wallet** — a select dropdown. Shown only when Type is Transfer. Required when visible. Lists active wallets excluding the selected source wallet.

- **Start Date** — a date input. Required. YYYY-MM-DD format. The date of the first occurrence. The system will generate transactions from this date forward according to the frequency.

- **End Date** — a date input. Optional. YYYY-MM-DD format. If set, the pattern stops generating transactions after this date. If left blank, the pattern runs indefinitely until manually deleted.

**Actions:**

- **Save** (or "Create" in create mode) — submits the form. On success, navigates to the recurring list. The new pattern appears immediately in the Patterns tab and its projected transactions appear in the Forecast tab.
- **Cancel** — navigates back to the recurring list without saving.

**Error handling:**

- HTTP 422: specific validation messages.
- Inline validation errors for required fields.

---

## Auto-Generation

Recurring patterns are executed by a Hangfire background job that runs daily (or more frequently). The job scans all active patterns, identifies those due for generation on the current date, creates the corresponding transactions, and marks each occurrence as generated. Auto-generated transactions appear in the user's transaction history identically to manually recorded ones, with an optional indicator that they were auto-generated.

Users receive an in-app notification when transactions are auto-generated. They can review generated transactions and edit or delete them if the actual amount differs from the pattern (e.g., a utility bill that varies month to month).
