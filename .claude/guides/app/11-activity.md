# 11 — Activity Feed

## Purpose

The Activity section provides a chronological, filterable audit trail of everything the user has done in the app. It shows who did what and when, making it easy to review historical actions and investigate unexpected changes.

---

## Activity View

The activity view (`/activity`) has two sections arranged vertically: a filter panel at the top and the activity feed below.

---

## Activity Filters

The filter panel contains two controls:

- **From date** — a date input. When set, shows only activities on or after this date. Rendered as a native date picker in YYYY-MM-DD format.
- **To date** — a date input. When set, shows only activities on or before this date.
- **Action type** — a select dropdown listing all available activity types. The default option is "All types." When a specific type is selected, only activities of that type are shown.

Filters are applied when the user changes a value; no separate submit button is required. Changing any filter resets the pagination to page 1.

---

## Activity Feed

The feed shows a paginated list of activity items. Each item displays:

- A descriptive label for the action (e.g., "Recorded transaction", "Created wallet", "Accepted invitation").
- The affected entity name or identifier where applicable (e.g., wallet name, transaction description).
- A relative or absolute timestamp.
- An icon representing the action category (wallet icon for wallet actions, transaction icon for transaction actions, etc.).

Pagination is handled with **Previous** and **Next** buttons below the list. The page size is 100 items per page. A total count or "showing X–Y of Z" label is shown.

If no activities match the current filters, an empty state message is shown.

---

## Activity Types

The following activity types appear in the feed and the filter dropdown:

**Authentication events:**
- User registered
- Logged in
- Logged out

**Wallet events:**
- Created wallet
- Archived wallet
- Updated wallet

**Transaction events:**
- Recorded transaction
- Updated transaction
- Deleted transaction

**Budget events:**
- Created budget
- Updated budget
- Deleted budget

**Goal events:**
- Created goal
- Updated goal
- Deleted goal

**Collaboration events:**
- Sent invitation
- Accepted invitation
- Member joined wallet
- Member left wallet
- Recorded settlement

**Recurring events:**
- Created recurring pattern
- Generated recurring transaction

---

## Data Source

The activity feed is powered by the backend's auditing module, which records every significant action with the acting user's ID, a timestamp (UTC Instant), the action type, and a reference to the affected entity. The frontend fetches a paginated slice of the audit log filtered to the current user's actions, applies the date and type filters specified by the user, and renders the results.

The activity feed is read-only; no entries can be edited or deleted.
