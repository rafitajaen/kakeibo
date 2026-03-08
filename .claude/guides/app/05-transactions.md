# 05 — Transactions

## Purpose

The Transactions section is where users record, view, filter, edit, and delete financial events. Every income, expense, and transfer is captured here, and every transaction is linked to a wallet and a category.

---

## Transactions List View

The transactions list view (`/transactions`) shows all transactions across all the user's wallets, with filter controls to narrow the results.

**Filter controls** are displayed in a responsive two-by-two grid above the list:

- **From date** — a date input. When set, only transactions on or after this date are shown. The format is YYYY-MM-DD, rendered as a browser-native date picker.
- **To date** — a date input. When set, only transactions on or before this date are shown. Must be on or after the From date.
- **Type** — a select dropdown. Options: All types, Income, Expense, Transfer. Filters transactions by type.
- **Category** — a select dropdown. Options: All categories, followed by the user's complete category list (system + custom, excluding archived). Filters transactions by category.

Filters are applied reactively: as soon as the user changes any filter, the list updates without requiring a separate "Apply" button. All filters are independent and can be combined.

A **New transaction** button at the top right navigates to the transaction creation form.

The list itself is rendered by a `TransactionList` component. Each row in the list shows:

- Date (formatted per the user's display preferences).
- Description.
- Category icon and name.
- Wallet name (and destination wallet for transfers).
- Amount, color-coded: green for income, red for expense, neutral/gray for transfers.
- A context menu or row actions: Edit, Delete.

Pagination is applied at 50 transactions per page. Previous and Next buttons navigate between pages.

If no transactions match the current filters, an empty state message is shown.

---

## Record Transaction

The record transaction view (`/transactions/new`) displays a centered card with a `TransactionForm` in creation mode. The wallet can optionally be pre-selected via a URL query parameter (used when navigating from the wallet detail "Record transaction" button).

The form pre-loads the user's active categories and wallets on mount. While loading, a skeleton is shown.

---

## Edit Transaction

The edit transaction view (`/transactions/:id/edit`) uses the same `TransactionForm` but in edit mode, pre-filled with the existing transaction's data. The **Type** field is read-only in edit mode — the transaction type cannot be changed after recording.

---

## TransactionForm

The transaction form is the shared form component used for both creating and editing transactions.

**Fields:**

- **Type** — a select dropdown. Options: Income, Expense, Transfer. In edit mode, this field is read-only and the value cannot be changed. When set to Transfer, the Destination Wallet field becomes visible.

- **Amount** — a number input with a built-in calculator widget. When the user focuses the field, a small calculator overlay appears that supports addition, subtraction, multiplication, and division, allowing the user to compute amounts before entering them. The final computed value is filled into the input. Minimum: 0.01. Maximum: 999,999,999.99. Required.

- **Description** — a text input. Required. Maximum 500 characters. Placeholder examples help the user understand what to enter (e.g., "Monthly rent", "Grocery shopping").

- **Date** — a date input rendered as a native date picker. Required. Defaults to today's date. Cannot be more than 1 year in the future.

- **Wallet** — a select dropdown. Required. Shows only non-archived wallets the user has access to. Displays each wallet's name and current balance. For a transfer, this is the source wallet.

- **Category** — a select dropdown. Required. Shows active categories (system + user's custom), each with its icon and name. Filtered to show categories appropriate for the wallet type. Archived categories are excluded.

- **Destination Wallet** — a select dropdown. Shown only when Type is Transfer. Required when visible. Shows the same wallet list as Wallet but excludes the currently selected source wallet (a transfer cannot be from and to the same wallet).

- **Notes** — a textarea. Optional. Maximum 1000 characters. For longer descriptions or memos.

**Form behavior:**

- All required fields show inline validation errors below the input when left blank on submit attempt.
- The calculator overlay on the Amount field can be dismissed by pressing Escape or clicking outside.
- The Category dropdown shows category icons next to names for visual identification.
- When changing from Income/Expense to Transfer (in create mode), the Destination Wallet field animates into view.

**Actions:**

- **Save** (or "Record" in create mode) — submits the form. On success, navigates to the transactions list. While submitting, the button shows a loading state.
- **Cancel** — navigates back without saving. In create mode, goes to the transactions list. In edit mode, goes back to the transactions list.

**Error handling:**

- HTTP 403: "You don't have access to this wallet."
- HTTP 422: specific field validation messages from the server.
- Generic API errors: inline error message above the submit button.
