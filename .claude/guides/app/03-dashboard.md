# 03 — Dashboard

## Purpose

The dashboard is the home screen for authenticated users. It provides a financial overview combining key metrics, a trend chart, budget and goal summaries, and recent transactions — all in one glanceable layout.

---

## When It Appears

The dashboard is the default route (`/`) for authenticated users. If the user has no wallets (typically on first login), the route guard redirects them to the onboarding wizard instead.

---

## Data Loading

On mount, the dashboard fetches data from four stores concurrently: wallets, transactions (for the last 90 days), budgets, and goals. While any of these are loading, the corresponding sections display skeleton loaders. Once all data is available, the full layout is rendered.

---

## SectionCards — Metric Summary

Three metric cards are displayed in a responsive grid (3 columns on desktop, stacked on mobile).

**Income This Month** — the total amount of income transactions in the current calendar month across all wallets. Displayed as a formatted currency amount in the user's base currency. A trend badge below the amount shows the percentage change compared to the previous month, colored green for positive change and red for negative.

**Expenses This Month** — the total amount of expense transactions in the current calendar month across all wallets. Same format and trend badge logic as Income.

**Net Savings** — the difference between Income and Expenses for the current month. Positive values are shown in green; negative (overspending) values are shown in red. The trend badge compares to the previous month's net savings.

---

## ChartAreaInteractive — Income vs Expenses Over Time

An area chart that visualizes income and expenses as two overlapping areas over a selectable time window. The chart uses the Unovis library (`@unovis/vue`).

**Toggle buttons** at the top of the chart section allow the user to select the time window:

- **Last 7 days** — shows daily data points for the past 7 days.
- **Last 30 days** — shows daily data points for the past 30 days.
- **Last 3 months** — shows weekly or daily aggregated data for the past 90 days.

The selected window is highlighted in the toggle group. The chart redraws immediately when the user switches windows. The underlying data covers 90 days and is sliced client-side when switching windows, so no additional API call is needed.

The chart shows two series: Income (typically a teal or blue area) and Expenses (typically a red or orange area). Both are semi-transparent to allow visual overlap. Hovering over a data point shows a tooltip with the exact date and values for both series.

---

## BudgetSummary

A card showing the user's active budgets at a glance. Each budget is listed as a row containing:

- The budget name and category icon.
- A progress bar showing current spending as a percentage of the limit.
- The spent amount and limit formatted as currency fractions (e.g., "$340 / $400").
- A status badge: "On track" (green), "Warning" (yellow), or "Exceeded" (red).

If the user has no budgets, this section shows an empty state message with a link to create the first budget.

A "View all" link at the bottom navigates to the full Budgets view.

---

## GoalSummary

A card showing the user's active savings goals at a glance. Each goal is listed as a row containing:

- The goal name.
- A progress bar showing current progress as a percentage of the target amount.
- The current saved amount and target formatted as currency fractions.
- Optionally, a deadline label if the goal has a deadline set.

If the user has no goals, this section shows an empty state message with a link to create the first goal.

A "View all" link navigates to the full Goals view.

---

## RecentTransactions

A compact table showing the most recent transactions across all wallets. Each row shows:

- Transaction date.
- Description.
- Category name with icon.
- Wallet name.
- Amount, color-coded (green for income, red for expense, neutral for transfer).

The table shows a fixed number of rows (typically 5–10 most recent). It does not have pagination — it is a summary only.

A "View all transactions" link at the bottom navigates to the full Transactions view with no pre-applied filters.

If the user has no transactions yet, an empty state message is shown with a link to record the first transaction.

---

## useDashboardData Composable

The dashboard uses a single composable (`useDashboardData`) that aggregates data from the four stores and computes the derived metrics:

- `sectionMetrics` — computed object with this-month income, expenses, net savings, and their respective previous-month values for trend calculation.
- `chartData` — computed array of daily data points over 90 days, with each point containing a date, total income for that day, and total expenses for that day.
- `recentTransactions` — the 10 most recent transactions sorted by date descending.

The composable also exposes an `isLoading` flag that is true while any of the underlying stores are fetching.
