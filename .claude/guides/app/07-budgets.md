# 07 — Budgets

## Purpose

The Budgets section allows users to define spending limits for a specific category and time period, monitor progress, and receive visual alerts when spending approaches or exceeds the limit.

---

## Budgets List View

The budgets list view (`/budgets`) shows all of the user's budgets in a card layout managed by a `BudgetList` component.

Each budget card displays:

- Budget name.
- Category icon and name.
- The wallets being monitored (all wallets, or a specific wallet name).
- A progress bar showing current spending relative to the limit. The bar is colored green when on track, yellow when in the warning zone, and red when the limit is exceeded.
- The exact amounts: "spent / limit" formatted as currency values (e.g., "$340 / $400").
- A status badge:
  - **On track** (green) — spending is at or below the expected pace for the current period.
  - **Warning** (yellow) — spending is above the expected pace but below the limit.
  - **Exceeded** (red) — spending has reached or exceeded the limit.

Actions on each card:

- **Edit** — navigates to the edit budget view.
- **Delete** — permanently deletes the budget. A confirmation dialog appears before deletion. Deleting a budget does not affect existing transactions.

A **New budget** button at the top right navigates to the budget creation form.

If the user has no budgets, an empty state message encourages them to create their first spending limit.

On mount, the view fetches budgets, categories, and wallets to populate the list and the forms.

---

## Create Budget

The create budget view (`/budgets/new`) displays a centered card with a `BudgetForm` in creation mode.

---

## Edit Budget

The edit budget view (`/budgets/:id/edit`) uses the same `BudgetForm` in edit mode, pre-filled with the budget's current data. The start date can be changed but the spending history is recalculated based on the new period.

---

## BudgetForm

**Fields:**

- **Name** — a text input. Required. Maximum 100 characters. A descriptive name for the budget (e.g., "Monthly Groceries", "Entertainment Q1").

- **Category** — a select dropdown. Required. Lists all active categories (system + custom). Each option shows the category icon and name. Archived categories are excluded. The selected category determines which transactions are counted toward this budget's spending.

- **Spending Limit** — a number input. Required. Must be greater than 0. Maximum 999,999,999.99. The maximum amount the user wants to spend in this category during the defined period.

- **Start Date** — a date input. Required. YYYY-MM-DD format rendered as a native date picker. The first day of the budget period.

- **End Date** — a date input. Required. YYYY-MM-DD format. Must be on or after the start date. The last day of the budget period. The combination of start and end date can represent any period: a single day, a week, a month, a quarter, or up to 5 years.

- **Wallet** — a select dropdown. Optional. Options:
  - **All wallets** (default, shown as the first option) — the budget monitors spending across every wallet the user has access to.
  - Individual wallet names — the budget monitors spending only in the selected wallet.

**Actions:**

- **Save** (or "Create" in create mode) — submits the form. On success, navigates to the budgets list.
- **Cancel** — navigates back to the budgets list without saving.

**Error handling:**

- HTTP 422: specific validation messages (e.g., "End date must be after start date").
- Inline validation messages for required fields.

---

## Budget Status Calculation

Budget status is computed on the frontend based on data returned by the API:

- **On track** — current spending is less than or equal to the proportional expected spending for the elapsed portion of the period. For example, if 50% of the period has elapsed, "on track" means spending is ≤ 50% of the limit.
- **Warning** — current spending exceeds the expected pace but has not yet reached the limit.
- **Exceeded** — current spending equals or exceeds the spending limit.

The progress bar percentage is capped at 100% visually even if spending exceeds the limit (the bar turns red and fills completely).
