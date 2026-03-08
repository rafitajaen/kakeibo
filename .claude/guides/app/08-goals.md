# 08 — Goals

## Purpose

The Goals section lets users define savings targets, track progress over time, and receive milestone notifications. Each goal is linked to a wallet and monitors its balance growth.

---

## Goals List View

The goals list view (`/goals`) shows all of the user's savings goals in a card layout rendered by a `GoalList` component.

Each goal card displays:

- Goal name.
- Linked wallet name.
- A progress bar showing current progress as a percentage of the target amount.
- The exact amounts: "current / target" formatted as currency values (e.g., "$2,500 / $5,000").
- Deadline, if set, shown as a formatted date with a countdown or "X days remaining" label.
- A status badge:
  - **Not started** — no qualifying transactions exist yet (progress is 0).
  - **In progress** — some progress made but target not yet reached.
  - **Near target** — progress is 75% or more of the target amount.
  - **Achieved** — progress equals or exceeds the target amount (badge turns green with a checkmark).

Actions on each card:

- **Edit** — navigates to the edit goal view.
- **Delete** — permanently deletes the goal. A confirmation dialog appears. Deleting a goal does not affect the linked wallet or its transactions.

A **New goal** button at the top right navigates to the goal creation form.

If the user has no goals, an empty state message encourages them to define their first savings target.

On mount, the view fetches the goals list from the goals store.

---

## Create Goal

The create goal view (`/goals/new`) displays a centered card with a `GoalForm` in creation mode.

---

## Edit Goal

The edit goal view (`/goals/:id/edit`) uses the same `GoalForm` in edit mode, pre-filled with the goal's current data. The linked wallet can be changed; progress is recalculated based on the new wallet's balance.

---

## GoalForm

**Fields:**

- **Name** — a text input. Required. Maximum 100 characters. A descriptive name for the savings goal (e.g., "Europe Vacation", "Emergency Fund", "New Laptop").

- **Target Amount** — a number input. Required. Must be greater than 0. Maximum 999,999,999.99. The total amount the user wants to save.

- **Deadline** — a date input. Optional. YYYY-MM-DD format rendered as a native date picker. If provided, must be in the future and no more than 10 years from today. If left empty, the goal has no deadline and runs indefinitely. The field shows a placeholder or hint such as "Optional — leave blank for no deadline."

- **Wallet** — a select dropdown. Required. Lists all active (non-archived) wallets the user has access to. The selected wallet's balance growth is used to measure progress toward the target amount. Archived wallets are excluded.

**Actions:**

- **Save** (or "Create" in create mode) — submits the form. On success, navigates to the goals list.
- **Cancel** — navigates back to the goals list without saving.

**Error handling:**

- HTTP 422: specific validation messages (e.g., "Deadline cannot be more than 10 years in the future").
- HTTP 404: "Wallet not found" if the wallet was deleted before the form was submitted.
- Inline validation messages for required fields.

---

## Progress Tracking

Goal progress is derived from the linked wallet's balance as reported by the Transactions module. The progress percentage is calculated as `(current balance / target amount) × 100`, capped at 100%.

Milestone notifications are triggered at 25%, 50%, 75%, and 100% progress. These are backend events (published as `GoalMilestoneReachedEvent` and `GoalAchievedEvent`) that result in in-app and optional email/push notifications.

The progress bar in the goal card reflects the latest balance from the server. It does not update in real time without a page refresh or store refresh.
