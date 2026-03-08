# Kakeibo Glossary

Exhaustive glossary of terms, concepts, and conventions used in the Kakeibo financial management platform. This document serves as the single source of truth for terminology across all documentation, codebase, and team communication.

**Version:** 1.0
**Last Updated:** 2026-02-21
**Related Documents:**
- [overview.md](./overview.md) -- Core philosophy and user flows
- [architecture.md](./architecture.md) -- Feature folder structure, in-process event system
- [platform.md](./platform.md) -- Domain catalog, event catalog, key flows
- [business-rules.md](./business-rules.md) -- Invariants, validation rules, calculation formulas
- [constraints.md](./constraints.md) -- Numeric limits, rate limits, pagination
- [tech-stack.md](./tech-stack.md) -- Technology choices and prohibited technologies

---

## Table of Contents

1. [Business Domain Terms](#1-business-domain-terms)
2. [Technical Terms](#2-technical-terms)
3. [Infrastructure Terms](#3-infrastructure-terms)
4. [Process Terms](#4-process-terms)
5. [Acronyms](#5-acronyms)
6. [Prohibited Terms](#6-prohibited-terms)
7. [Japanese Terms](#7-japanese-terms)

---

## 1. Business Domain Terms

### Entity

**Definition:** The base class for all domain objects in the Kakeibo system. Every entity has a globally unique identifier (Guid7), creation and update timestamps (NodaTime `Instant`), and a soft-delete timestamp. Entities are the building blocks of the domain model and are persisted to the database via Entity Framework Core.

**Domain:** Kakeibo.Api

**Related Concepts:** **Value Object**, **Event**, **Guid7**

**Examples:**
- `Wallet` extends `Entity` with `Name`, `Balance`, `Type`
- `Transaction` extends `Entity` with `Amount`, `Date`, `CategoryId`
- `Invitation` extends `Entity` with `Token`, `ExpiresAt`, `Status`

**Technical Notes:** The `Entity` base class (in `Kakeibo.Api.Common.Abstractions`) provides: `Guid Id` initialized with `Guid7.NewGuid()`, `Instant CreatedAt` and `Instant UpdatedAt` initialized with `SystemClock.Instance.GetCurrentInstant()`, `Instant? DeletedAt` for soft delete, and `bool IsDeleted => DeletedAt is not null`. All entities use NodaTime -- BCL `DateTime` is prohibited. There is no `AggregateRoot` subclass -- all domain objects extend `Entity` directly.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Common.Abstractions.Entity`
- **Frontend:** N/A (backend concept only)
- **Database:** Every entity table has `id`, `created_at`, `updated_at`, `deleted_at` columns

---

### Archiving

**Definition:** A soft-hide operation that removes an entity from the active view without deleting its data. Archived entities retain all historical references, remain queryable for reporting, and can be restored to active status at any time. Archiving is the preferred alternative to deletion when an entity has associated records.

**Module:** Wallets, Transactions (Categories)

**Related Concepts:** **Wallet**, **Category**, **Transaction**

**Examples:**
- Archiving a wallet hides it from the daily dashboard but preserves all transaction history and balance data
- Archiving a custom category removes it from the category selector but historical transactions still display their archived category
- A wallet with 1+ transactions cannot be deleted, only archived

**Technical Notes:** Archived entities have `is_archived = true`. Archived wallets reject new transactions with `Error.Validation("Wallet.Archived")`. Archived categories are excluded from the category selector but remain visible on historical transactions. Restoration flips `is_archived` back to `false`.

**Usage in Code:**
- **Backend:** `is_archived` property on `Wallet`, `Category` entities; `ArchiveWallet/` and `ArchiveCategory/` feature folders
- **Frontend:** Filtered out of active lists via `isArchived` flag; toggle in settings to show/hide archived items
- **Database:** `is_archived BOOLEAN NOT NULL DEFAULT FALSE` column

---

### Audit Trail

**Definition:** An immutable, append-only log of every action performed on the platform. Each audit entry records who performed the action, what changed, when it occurred, and the before/after state for updates. Audit records can never be modified or deleted.

**Module:** Auditing

**Related Concepts:** **Domain Event**, **Integration Event**, **Activity**

**Examples:**
- User registers an account -> `UserRegisteredEvent` logged
- Transaction recorded in shared wallet -> `TransactionRecordedEvent` logged with full payload (amount, category, wallet, splits)
- Member joins shared wallet -> `MemberJoinedEvent` logged with inviter and invitee details

**Technical Notes:** Audit events are stored in ClickHouse (analytical database) for high-volume write performance and efficient time-range queries. Event handlers receive `IEvent` notifications via `IEventBus` / `ChannelEventBus` and write audit entries asynchronously. Audit logs have indefinite retention and support filtering by user, date range, action type, and affected entity.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Features.Auditing`; `ClickHouseAuditService` implementing `IEventHandler<T>`
- **Frontend:** Activity feed component displaying recent actions per wallet or user
- **Database:** ClickHouse `audit_events` table (separate from PostgreSQL)

---

### Balance

**Definition:** The amount of money currently held in a wallet. Balance is a derived value computed from the wallet's initial balance plus the sum of all income transactions minus the sum of all expense transactions. It is never stored independently -- it is always recalculated from transaction history to ensure accuracy.

**Module:** Wallets

**Related Concepts:** **Wallet**, **Transaction**, **Forecast**

**Examples:**
- **Current balance:** Wallet's money right now, computed from consolidated transactions only
- **Historical balance:** Wallet's money at any past point in time, reconstructed from transactions up to that date
- **Projected balance:** Wallet's money in the future, calculated by adding forecasted income and subtracting forecasted expenses from the current balance

**Technical Notes:**
```
wallet.currentBalance =
    wallet.initialBalance
  + SUM(income transactions)
  - SUM(expense transactions)
```
Only consolidated transactions (`is_forecast = false`) affect the current balance. Forecasted transactions appear in projections but do not change the current balance. Splits do not affect the wallet balance -- they only track who owes whom. Negative balances are allowed (credit card accounts, overdraft scenarios). Balance accuracy is a critical invariant (INV-W01).

**Usage in Code:**
- **Backend:** Computed field on `Wallet` entity; `GetWalletBalanceRequest` handled by Wallets module
- **Frontend:** Displayed on wallet cards, dashboard summary, and balance trend charts
- **Database:** `current_balance` is a computed field (not a stored column); `initial_balance DECIMAL(10,2)` is the stored seed value

---

### Budget

**Definition:** A spending limit assigned to a specific category over a defined time period. Budgets help users answer "am I spending too much on X?" by tracking actual spending against the limit in real-time. Budgets are advisory -- they emit warnings but never block transactions.

**Module:** Budgets

**Related Concepts:** **Category**, **Transaction**, **Wallet**

**Examples:**
- "Food & Dining" budget of $400/month monitoring the user's Checking Account
- "Entertainment" budget of $200/month across all personal wallets
- A shared wallet budget for "Housing" to monitor apartment expenses

**Technical Notes:** Budget spending is computed at query time from transaction data. Only expense transactions with `is_forecast = false` count toward budget spending. Budget statuses are: "On Track" (spending below expected pace), "Warning" (spending above expected pace but below limit), "Exceeded" (spending at or above limit). Past budgets are immutable (INV-B01). Budgets belong to either a user (personal) or a shared wallet, never both (INV-B05). Only expense categories can have budgets (INV-B04). Non-overlapping constraint prevents duplicate budgets for the same scope, category, and period (INV-B03).

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Features.Budgets`; `Budget` entity; `CreateBudget/`, `GetBudgetStatus/` feature folders
- **Frontend:** Budget cards with progress bars, spending trend charts, alert badges
- **Database:** `budgets` table; `UNIQUE (user_id, category_id, period_type, period_year, period_month)`

---

### Category

**Definition:** A classification label that answers "what kind of transaction is this?" Categories group similar transactions for budget organization, spending analysis, and pattern recognition. Every transaction must have exactly one category.

**Module:** Transactions

**Related Concepts:** **System Category**, **Transaction**, **Budget**

**Examples:**
- System category: "Food & Dining" (covers groceries and restaurants)
- System category: "Housing" (covers rent, mortgage, utilities)
- Custom category: "Pet Care" (user-created for vet visits, food, supplies)

**Technical Notes:** Two category types exist: 12 system categories (immutable, non-deletable, shared by all users) and unlimited custom categories (user-created, can be archived or deleted if unreferenced). Category names must be unique per user per type (income or expense). A category's type is immutable after creation (INV-C05). In shared wallets, all members see the same category for a transaction, set by the transaction creator.

**Usage in Code:**
- **Backend:** `Category` entity in `Kakeibo.Api.Features.Transactions`; `SystemCategory` value object; `CreateCategory/`, `ListCategories/` feature folders
- **Frontend:** Category selector dropdown with icons and colors; category breakdown pie charts
- **Database:** `categories` table; `is_system BOOLEAN NOT NULL DEFAULT FALSE`

---

### Conscious Spending

**Definition:** The core Kakeibo philosophy that every financial transaction is an opportunity for awareness. By recording and categorizing each expense, users develop a deeper understanding of their spending patterns and make more intentional choices about where their money goes. This principle transforms passive spending into active, reflective decision-making.

**Module:** N/A (foundational philosophy)

**Related Concepts:** **Kakeibo**, **Category**, **Budget**, **Reflection Through Categorization**

**Examples:**
- Recording a $4.50 coffee purchase creates a moment of reflection: "Is this a daily habit? How much am I spending on coffee per month?"
- Reviewing category breakdowns at month-end reveals that "Entertainment" consumed 30% of discretionary spending
- Setting a budget for "Shopping & Personal" makes the user aware of impulse purchases before they happen

**Technical Notes:** Conscious spending is not a feature -- it is the design principle that guides all feature decisions. The act of recording a transaction has value beyond the data captured. This is why Kakeibo emphasizes manual recording over automatic bank sync imports in the MVP.

**Usage in Code:**
- **Backend:** N/A (philosophy, not code)
- **Frontend:** Transaction recording flow designed for minimal friction; category selection is mandatory, not optional
- **Database:** N/A

---

### Custom Split

**Definition:** An expense division mechanism where each member's share is specified as an exact monetary amount. The sum of all custom amounts must equal the transaction total exactly. Used when members purchased different items or when the division does not follow a simple mathematical pattern.

**Module:** Wallets

**Related Concepts:** **Split**, **Equal Split**, **Percentage Split**, **Debt**

**Examples:**
- $75 shopping trip split $45/$30 based on individual items purchased
- $200 dinner where one person had an expensive dish: $80/$60/$60

**Technical Notes:** Validation enforces `SUM(amounts) == transaction.amount`. No rounding correction is needed because amounts are specified explicitly. The payer's split is automatically marked `status = settled, is_payer = true`.

**Usage in Code:**
- **Backend:** `SplitType.Custom` value object; validated in `CreateTransactionHandler`
- **Frontend:** Custom amount input fields per member in the split configuration form
- **Database:** `transaction_splits` table; `split_type = 'custom_amount'`

---

### Debt

**Definition:** A calculated amount of money that one user owes another user, derived automatically from shared wallet transaction history and split records. Debts are never set manually -- they are a projection of pending (unsettled) splits. When all splits for a pair of users are settled, the debt between them becomes zero.

**Module:** Wallets

**Related Concepts:** **Split**, **Settlement**, **Shared Wallet**, **Debt Simplification**

**Examples:**
- Alice pays $1,200 rent with equal split between Alice and Bob -> Bob owes Alice $600
- After Bob also pays $150 for groceries with equal split -> Net debt: Bob owes Alice $525
- Bob settles the $525 -> Debt between Alice and Bob becomes $0

**Technical Notes:** Debt is calculated using the formula: `debt(A owes B) = SUM(splits WHERE user_id = A AND owed_to_user_id = B AND status = 'pending')`. No `debts` table exists -- debt is a derived view over `transaction_splits`. The `DebtCalculationService` implements a net-balance simplification algorithm that minimizes the number of transfers needed to settle all debts in a shared wallet (see INV-D03). Debts are symmetric: both parties see the same debt information.

**Usage in Code:**
- **Backend:** `DebtCalculationService` in `Kakeibo.Api.Features.Wallets`; `GetWalletDebts/` feature folder; `Debt` entity for the computed view
- **Frontend:** Debt summary cards per shared wallet showing who owes whom and how much
- **Database:** Derived from `transaction_splits` table; no separate `debts` table

---

### Event

**Definition:** An in-process, fire-and-forget signal that something meaningful has happened. Events are published via `IEventBus` and dispatched asynchronously by `EventDispatcher` (a `BackgroundService`) to registered `IEventHandler<T>` implementations. They are used to trigger side effects like audit logging and notifications without blocking the main request path.

**Domain:** Kakeibo.Api (infrastructure)

**Related Concepts:** **Event Handler**, **ChannelEventBus**, **EventDispatcher**, **Entity**

**Examples:**
- `WalletCreatedEvent` -- published when a new wallet is created
- `TransactionRecordedEvent` -- published when a transaction is recorded
- `InvitationSentEvent` -- published when an invitation is sent to a new member

**Technical Notes:** Events implement `IEvent` with `Guid Id` and `Instant OccurredAt`. Feature handlers call `eventBus.Publish(new SomeEvent { ... })` before `SaveChangesAsync`. The `ChannelEventBus` (singleton) writes to a `Channel<IEvent>`. The `EventDispatcher` BackgroundService reads from the channel and resolves `IEventHandler<T>` implementations in a new DI scope. Events are in-memory only -- no outbox table, no guaranteed delivery. If the process crashes before the handler runs, the event is lost.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Infrastructure.Events.IEvent`; implementations in `Kakeibo.Api.Features.{Domain}.Events`; handlers in the consuming domain
- **Frontend:** N/A (backend concept only)
- **Database:** Events are not persisted; they are consumed in-memory

---

### Entity (base class)

**Definition:** The base class for all domain objects in the Kakeibo system. Every entity has a globally unique identifier (Guid7), creation and update timestamps (NodaTime `Instant`), and a soft-delete timestamp. Entities are the building blocks of the domain model and are persisted to the database via Entity Framework Core.

**Domain:** Kakeibo.Api

**Related Concepts:** **Value Object**, **Event**, **Guid7**

**Examples:**
- `Wallet` extends `Entity` with `Name`, `Balance`, `Type`
- `Transaction` extends `Entity` with `Amount`, `Date`, `CategoryId`
- `Invitation` extends `Entity` with `Token`, `ExpiresAt`, `Status`

**Technical Notes:** The `Entity` base class provides: `Guid Id` initialized with `Guid7.NewGuid()`, `Instant CreatedAt` and `Instant UpdatedAt` initialized with `SystemClock.Instance.GetCurrentInstant()`, `Instant? DeletedAt` for soft delete, and `bool IsDeleted => DeletedAt is not null`. There is no `AggregateRoot` subclass and no domain events list -- events are published explicitly via `IEventBus`. All entities use NodaTime -- BCL `DateTime` is prohibited.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Common.Abstractions.Entity`
- **Frontend:** N/A (backend concept only)
- **Database:** Every entity table has `id`, `created_at`, `updated_at`, `deleted_at` columns

---

### Equal Split

**Definition:** An expense division mechanism that divides the total cost evenly among all participating members of a shared wallet. When the division produces fractional cents, the remainder is distributed one cent at a time to the first members in the list.

**Module:** Wallets

**Related Concepts:** **Split**, **Percentage Split**, **Custom Split**, **Debt**

**Examples:**
- $100 split equally among 3 members: $33.34, $33.33, $33.33
- $60 dinner split equally among 3 people: $20.00 each (no remainder)
- $10 split equally among 3 people: $3.34, $3.33, $3.33

**Technical Notes:** The rounding algorithm uses floor division and distributes remainder cents to the first N members: `baseAmount = Math.Floor(totalAmount / memberCount * 100) / 100`. Verification: the sum of all splits must equal the original transaction amount exactly (INV-T05).

**Usage in Code:**
- **Backend:** `SplitType.Equal` value object; `CalculateEqualSplit()` method; validated in `CreateTransactionHandler`
- **Frontend:** Default split type selection; auto-calculated amounts displayed per member
- **Database:** `transaction_splits` table; `split_type = 'equal'`

---

### Expense

**Definition:** A transaction type representing money leaving a wallet. Expenses decrease the wallet balance by the transaction amount. Every expense must be categorized, and only expenses count toward budget spending calculations.

**Module:** Transactions

**Related Concepts:** **Transaction**, **Income**, **Transfer**, **Budget**, **Category**

**Examples:**
- $4.50 coffee purchase categorized as "Food & Dining"
- $1,200 rent payment categorized as "Housing"
- $45 dinner split among shared wallet members

**Technical Notes:** Expense transactions have `category.type = 'expense'`. Balance impact: `wallet.balance -= transaction.amount`. Only consolidated expenses (not forecasts) affect the current balance and budget spending. In shared wallets, expenses can have splits attached to divide the cost among members.

**Usage in Code:**
- **Backend:** `TransactionType.Expense` value object in `Kakeibo.Api.Features.Transactions`
- **Frontend:** Red-colored transaction entries; expense recording form with category selector
- **Database:** `transactions` table; `category_type = 'expense'`

---

### Forecast

**Definition:** A projected future transaction generated automatically from a recurring pattern. Forecasts are read-only placeholders that show users what transactions are expected in the coming days. They do not affect the current wallet balance but are included in projected balance calculations.

**Module:** Recurring

**Related Concepts:** **Recurring Pattern**, **Transaction**, **Balance**

**Examples:**
- Forecasted $1,200 rent payment on the 1st of next month, generated from a monthly recurring pattern
- Three forecasted Spotify charges ($9.99 each) for the next 3 months
- Forecasted paycheck deposits on biweekly schedule

**Technical Notes:** Forecasts have `is_forecast = true` and are generated by a Hangfire background job up to 90 days ahead. Users can: (1) confirm a forecast to convert it to a consolidated transaction, (2) skip it to delete the occurrence without affecting the pattern, (3) edit it to break the link to the pattern and create an independent consolidated transaction. Forecasts are excluded from budget spending and current balance calculations. The `last_generated_date` field on the recurring pattern prevents duplicate generation (INV-R03).

**Usage in Code:**
- **Backend:** `is_forecast` flag on `Transaction` entity; `ForecastConfirmedEvent`, `ForecastSkippedEvent`
- **Frontend:** Dashed/dimmed transaction rows in the transaction list; "Confirm" and "Skip" action buttons
- **Database:** `transactions` table; `is_forecast BOOLEAN NOT NULL DEFAULT FALSE`

---

### Goal

**Definition:** A savings target representing financial progress toward a desired outcome. Goals help users answer "am I making progress toward X?" by tracking current savings against a target amount, with an optional deadline. Goals track progress automatically when linked to a wallet, or manually when tracking external accounts.

**Module:** Goals

**Related Concepts:** **Milestone**, **Wallet**, **Balance**

**Examples:**
- "Europe Vacation" goal: $5,000 target, December 31st deadline, linked to "Vacation Fund" wallet
- "Emergency Fund" goal: $10,000 target, no deadline, tracks total across all wallets
- "Pay Off Credit Card" goal: $3,500 target, manual progress updates

**Technical Notes:** Three tracking modes: (1) wallet-linked (auto-tracks balance growth in a specific wallet), (2) cross-wallet (tracks total across all wallets), (3) manual (user updates progress). Achievement is automatic: `isAchieved = (currentAmount >= targetAmount)`. If the target increases above the current amount, `isAchieved` reverts to `false`. Linked wallet deletion causes the goal to degrade to manual mode via `ON DELETE SET NULL`. Deadline is optional; maximum 10 years in the future (INV-G02). Target amount must be positive (INV-G01).

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Features.Goals`; `SavingsGoal` entity; `CreateGoal/`, `UpdateGoalProgress/` feature folders
- **Frontend:** Goal cards with progress bars, projected completion date, milestone badges
- **Database:** `savings_goals` table; `linked_wallet_id` FK with `ON DELETE SET NULL`

---

### Income

**Definition:** A transaction type representing money entering a wallet. Income transactions increase the wallet balance by the transaction amount. Income is excluded from budget spending calculations because budgets only monitor expense categories.

**Module:** Transactions

**Related Concepts:** **Transaction**, **Expense**, **Transfer**, **Balance**

**Examples:**
- $2,000 salary deposit categorized as "Salary" (custom category)
- $150 gift received categorized as "Gifts & Donations"
- $50 refund categorized as "Shopping & Personal"

**Technical Notes:** Income transactions have `category.type = 'income'`. Balance impact: `wallet.balance += transaction.amount`. Income categories cannot have budgets (INV-B04). Only consolidated income (not forecasts) affects the current balance.

**Usage in Code:**
- **Backend:** `TransactionType.Income` value object in `Kakeibo.Api.Features.Transactions`
- **Frontend:** Green-colored transaction entries; income recording form
- **Database:** `transactions` table; `category_type = 'income'`

---

### Event Handler

**Definition:** A class that reacts to a published `IEvent` and performs a side effect such as sending a notification, writing an audit log entry, or updating derived state. Event handlers implement `IEventHandler<TEvent>` and are auto-registered by Scrutor. They run asynchronously in a dedicated DI scope managed by `EventDispatcher`.

**Domain:** Kakeibo.Api (infrastructure + features)

**Related Concepts:** **Event**, **EventDispatcher**, **ChannelEventBus**

**Examples:**
- `TransactionRecordedHandler` (in Auditing) -- writes an audit entry when a transaction is recorded
- `GoalMilestoneReachedHandler` (in Notifications) -- sends a push notification when a goal milestone is reached
- `InvitationAcceptedHandler` (in Notifications) -- sends an email when an invitation is accepted

**Technical Notes:** Event handlers implement `IEventHandler<TEvent>` with a single method `Task HandleAsync(TEvent, CancellationToken)`. They are scoped services (new instance per event dispatch). Multiple handlers for the same event type are all invoked. Handlers must be idempotent because delivery is at-most-once (no retry on failure). Failures are logged but do not propagate to the caller.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Infrastructure.Events.IEventHandler<T>`; implementations in consuming feature folders (e.g., `Kakeibo.Api.Features.Auditing`, `Kakeibo.Api.Features.Notifications`)
- **Frontend:** N/A (backend concept only)
- **Database:** N/A (handlers perform application-level side effects, not persistence of the event itself)

---

### Invitation

**Definition:** An access grant that allows a person to join a shared wallet. Invitations are the only mechanism through which users can gain access to a shared wallet they do not already belong to. Each invitation has a cryptographically random token, an expiration date (7 days), and a lifecycle that transitions through pending, accepted, declined, or expired states.

**Module:** Wallets

**Related Concepts:** **Shared Wallet**, **Member**

**Examples:**
- Alice creates a shared wallet and generates an invitation link for Bob
- Bob receives the link, clicks it, and accepts the invitation -- he is now a member of the shared wallet
- An invitation to Carol expires after 7 days without acceptance

**Technical Notes:** Invitation lifecycle: Pending -> Accepted | Declined | Expired (all terminal). Token is 128+ bits, cryptographically random. Constraints: no self-invitation, no invitation to existing members, only one pending invitation per email per shared wallet (resending creates a new token and invalidates the old one). Inviting an unregistered user sends an email; after registration, the invitation is automatically processed. Invitations are managed in the Wallets module (post-consolidation from the former Collaboration module).

**Usage in Code:**
- **Backend:** `Invitation` entity in `Kakeibo.Api.Features.Wallets`; `InviteToWallet/`, `AcceptInvitation/` feature folders
- **Frontend:** Invitation creation form with shareable link/code; pending invitations list; accept/decline actions
- **Database:** `shared_wallet_invitations` table; `token VARCHAR(128)`, `expires_at TIMESTAMP`, `status VARCHAR(20)`

---

### Kakeibo

**Definition:** A traditional Japanese household budgeting method created in 1904 by Hani Motoko, Japan's first female journalist. The word "kakeibo" (family-account-book) translates to "household financial ledger" and represents a philosophy of conscious spending through reflection and planning. This platform adapts the traditional method to modern digital life while preserving its core principles of awareness, categorization, and intentional financial behavior.

**Module:** N/A (foundational identity)

**Related Concepts:** **Conscious Spending**, **Category**, **Reflection Through Categorization**, **Savings Through Awareness**

**Examples:**
- The traditional Kakeibo method organizes expenses into four categories: Survival, Culture, Optional, Extra
- This platform extends the philosophy with 12 system categories covering modern life
- The emphasis on manual recording (vs. automatic bank sync) honors the original principle that the act of recording is itself an act of awareness

**Technical Notes:** The platform name is "Kakeibo" (capitalized). The Japanese characters are included in formal references. The solution files use the `Kakeibo.*` namespace prefix throughout.

**Usage in Code:**
- **Backend:** `Kakeibo.*` namespace prefix on all projects
- **Frontend:** Application title and branding
- **Database:** Database name: `kakeibo`

---

### Member

**Definition:** A user who participates in a shared wallet. All members of a shared wallet have equal rights -- there is no owner, administrator, or hierarchy. Every member can view all transactions, record new transactions, edit or delete any transaction, invite new members, and view debts and settlements.

**Module:** Wallets

**Related Concepts:** **Shared Wallet**, **Invitation**, **Debt**, **Split**

**Examples:**
- Alice creates a shared wallet and is automatically added as the first member
- Bob accepts an invitation and becomes a member with identical rights to Alice
- Any member can leave a shared wallet at any time; the wallet continues for remaining members

**Technical Notes:** Membership is stored in the `shared_wallet_members` join table with `(shared_wallet_id, user_id) UNIQUE`. Shared wallets must have at least 2 members (INV-W06) -- a removal that would bring the count below 2 is rejected. When a member leaves, their historical transactions and splits persist; pending debts remain visible to remaining members. The departed member can no longer access the wallet. Creator is auto-added as first member.

**Usage in Code:**
- **Backend:** `WalletMember` entity in `Kakeibo.Api.Features.Wallets`; `GetWalletMembers/` feature folder
- **Frontend:** Member list in shared wallet settings; member avatars on shared transactions
- **Database:** `shared_wallet_members` table; `(shared_wallet_id, user_id) UNIQUE`

---

### Milestone

**Definition:** A progress marker on a savings goal that triggers a notification when crossed. Milestones are fixed at 25%, 50%, 75%, and 100% of the goal's target amount. They provide positive reinforcement to encourage consistent saving behavior.

**Module:** Goals

**Related Concepts:** **Goal**, **Notification**

**Examples:**
- Goal "Europe Vacation" ($5,000 target): milestone at $1,250 (25%), $2,500 (50%), $3,750 (75%), $5,000 (100%)
- Crossing the 50% milestone triggers a `GoalMilestoneReachedEvent` which sends a congratulatory notification
- The 100% milestone is equivalent to goal achievement and triggers `GoalAchievedEvent`

**Technical Notes:** Milestones are not separate entities -- they are calculated thresholds. When `currentAmount` crosses a milestone percentage boundary, the Goals module publishes `GoalMilestoneReachedEvent` with `MilestonePercent`, `CurrentAmount`, and `TargetAmount`. Each milestone fires only once (tracked to prevent duplicate notifications).

**Usage in Code:**
- **Backend:** Milestone logic in goal progress update handler; `GoalMilestoneReachedEvent` defined in `Kakeibo.Api.Features.Goals.Events`
- **Frontend:** Milestone markers on goal progress bars; celebration animation when a milestone is reached
- **Database:** No separate table; milestone state derived from `current_amount` vs `target_amount`

---

### ChannelEventBus

**Definition:** The in-process, fire-and-forget event bus that implements `IEventBus`. It is backed by a `System.Threading.Channels.Channel<IEvent>` and is registered as a singleton. Feature handlers call `eventBus.Publish(event)` to write an event to the channel without blocking. The `EventDispatcher` BackgroundService reads from the channel and dispatches events to `IEventHandler<T>` handlers.

**Domain:** Kakeibo.Api (infrastructure)

**Related Concepts:** **Event**, **Event Handler**, **EventDispatcher**

**Examples:**
- A wallet is created → handler calls `eventBus.Publish(new WalletCreatedEvent { ... })` before `SaveChangesAsync`
- The event is written to the channel; the main request returns immediately
- `EventDispatcher` picks up the event and calls `WalletCreatedHandler.HandleAsync(...)`

**Technical Notes:** `ChannelEventBus` is a singleton. `EventDispatcher` is a hosted `BackgroundService` that loops indefinitely reading from the channel. Events are in-memory only -- no outbox table, no guaranteed delivery. If the process crashes between `Publish()` and handler execution, the event is lost. This is an acceptable tradeoff for the MVP. The `IEventBus` interface allows swapping to a durable implementation later.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Infrastructure.Events.ChannelEventBus`; `Kakeibo.Api.Infrastructure.Events.EventDispatcher`
- **Frontend:** N/A (backend concept only)
- **Database:** No persistence; events exist only in the in-memory channel

---

### Percentage Split

**Definition:** An expense division mechanism where each member's share is specified as a percentage of the total cost. All percentages must sum to exactly 100%. Used when members should pay different proportions of a shared expense.

**Module:** Wallets

**Related Concepts:** **Split**, **Equal Split**, **Custom Split**, **Debt**

**Examples:**
- $1,000 rent split 60%/40% between two roommates: $600/$400
- $150 groceries split 33.33%/33.33%/33.34% among three people: $33.33/$33.33/$33.34
- $500 utility bill split 50%/30%/20%: $250/$150/$100

**Technical Notes:** Validation enforces `SUM(percentages) == 100.00`. Amounts are calculated as `Math.Round(totalAmount * percentage / 100, 2)`. Rounding correction is applied to the last member to ensure the sum of calculated amounts equals the transaction total exactly (INV-T05).

**Usage in Code:**
- **Backend:** `SplitType.Percentage` value object; `CalculatePercentageSplit()` method; validated in `CreateTransactionHandler`
- **Frontend:** Percentage slider or input fields per member; live preview of calculated amounts
- **Database:** `transaction_splits` table; `split_type = 'percentage'`; `percentage DECIMAL(5,2)`

---

### Personal Wallet

**Definition:** A wallet owned by exactly one user, representing a personal financial account such as a checking account, savings account, cash envelope, or credit card. The owner has full control over the wallet and its transactions. No other user can see or access a personal wallet.

**Module:** Wallets

**Related Concepts:** **Wallet**, **Shared Wallet**, **Transaction**

**Examples:**
- "Checking Account" with $3,500 balance
- "Cash Wallet" for tracking physical cash
- "Credit Card" with a negative initial balance representing existing debt

**Technical Notes:** Personal wallets have `wallet.user_id NOT NULL` (immutable). The wallet type cannot change after creation -- a personal wallet can never become a shared wallet (INV-W03). Each user has exactly one default personal wallet, enforced by a partial unique index `UNIQUE (user_id) WHERE is_default = TRUE` (INV-W07). Personal wallet transactions cannot have splits (INV-T06). Negative balances are allowed.

**Usage in Code:**
- **Backend:** `Wallet` entity with `WalletType.Personal` in `Kakeibo.Api.Features.Wallets`
- **Frontend:** Personal wallet card with balance, name, icon, color
- **Database:** `wallets` table; `user_id VARCHAR(25) NOT NULL`

---

### Recurring Pattern

**Definition:** A template that generates transactions automatically on a schedule. Recurring patterns handle predictable financial events like monthly rent, biweekly salary, or annual subscriptions. Each pattern defines the transaction details (amount, category, description, wallet) and a recurrence rule (frequency and timing).

**Module:** Recurring

**Related Concepts:** **Forecast**, **Transaction**, **Hangfire**

**Examples:**
- Monthly rent: $1,200 expense, Housing category, 1st of every month
- Biweekly paycheck: $2,000 income, Salary category, every 2 weeks
- Annual subscription: $99.99 expense, Subscriptions & Bills, January 15th yearly

**Technical Notes:** A Hangfire background job runs daily and generates forecasted transactions up to 90 days ahead. Patterns track `last_generated_date` to prevent duplicate generation (INV-R03). Maximum pattern duration is 10 years (INV-R01). Editing a pattern affects only future occurrences -- past consolidated transactions are preserved (INV-R04). Pausing a pattern stops new forecast generation but keeps existing forecasts visible. Deleting a pattern removes future forecasts but preserves consolidated transactions. Edge cases: day 31 in a 30-day month uses the last day; Feb 29 in non-leap years uses Feb 28.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Features.Recurring`; `RecurringPattern` entity; Hangfire job for daily generation
- **Frontend:** Pattern creation wizard with frequency selector, start/end date pickers, preview of next occurrences
- **Database:** `recurring_transactions` table; `frequency VARCHAR(20)`, `last_generated_date DATE`

---

### Screaming Architecture

**Definition:** An architectural style where the folder structure of the codebase directly reflects the business domain, not technical concerns. By looking at the folder structure, you should immediately understand what business capabilities the system provides. In Kakeibo, feature folders like `Features/Wallets`, `Features/Budgets`, and `Features/Goals` communicate the domain, not names like `Services`, `Repositories`, or `Controllers`.

**Domain:** N/A (architectural principle)

**Related Concepts:** **Vertical Slice**, **Simple Monolith**

**Examples:**
- `src/Kakeibo.Api/Features/Wallets/` tells you this area handles wallets -- not `Services/WalletService.cs`
- `src/Kakeibo.Api/Features/Transactions/RecordTransaction/` tells you exactly what this feature does
- Business domains are named by capability: Identity, Wallets, Transactions, Budgets, Goals, Recurring, Notifications, Auditing

**Technical Notes:** All feature code lives under `src/Kakeibo.Api/Features/{Domain}/{Operation}/`. Each operation folder contains up to three files: `{Op}Endpoint.cs`, `{Op}Handler.cs`, `{Op}Validator.cs`. Feature folders are named by operation (`CreateWallet/`, `RecordTransaction/`, `InviteToWallet/`), not by technical layer. Architecture tests live in `tests/Kakeibo.Tests/Architecture/`.

**Usage in Code:**
- **Backend:** Folder and namespace organization under `Kakeibo.Api.Features.{Domain}`
- **Frontend:** N/A (backend architectural concept)
- **Database:** Single `public` schema; table names reflect the business entity (`wallets`, `transactions`, `budgets`, etc.)

---

### Settlement

**Definition:** An external payment record acknowledging that one user has paid another user to settle a debt. Settlements are recorded in Kakeibo to update debt calculations but do not create wallet transactions or affect wallet balances -- the money moved outside the application (cash, bank transfer, Bizum, Venmo, etc.).

**Module:** Wallets

**Related Concepts:** **Debt**, **Split**, **Shared Wallet**

**Examples:**
- Bob owes Alice $525. Bob sends Alice $525 via bank transfer. Alice records a settlement in Kakeibo. The debt becomes $0.
- David owes Carol $180. David pays in cash. Carol records a settlement with notes "Cash payment at dinner."

**Technical Notes:** Settlements operate per-split, not per-aggregate-debt. Each `transaction_split` record is settled individually by marking `status = 'settled'` with `settlement_date` and optional `settlement_notes`. Settlement is irreversible -- once settled, a split cannot return to pending. If the settlement was recorded in error, the transaction itself must be deleted and re-created. Settlement amount cannot exceed the current pending split amount (INV-D04). No wallet balance changes occur.

**Usage in Code:**
- **Backend:** `Settlement` entity in `Kakeibo.Api.Features.Wallets`; `RecordSettlement/` feature folder; `SettlementRecordedEvent`
- **Frontend:** "Settle" action button on pending debts; settlement confirmation dialog with notes field
- **Database:** Settlement data on `transaction_splits` table; `settlement_date`, `settlement_notes` columns

---

### Shared Wallet

**Definition:** A collaborative financial space where multiple users (2-20 members) participate with equal rights. Shared wallets represent group financial responsibilities such as roommate expenses, couple finances, family budgets, or trip costs. All members can view all transactions, record new transactions, invite new members, and see debt calculations.

**Module:** Wallets

**Related Concepts:** **Personal Wallet**, **Member**, **Invitation**, **Split**, **Debt**, **Settlement**

**Examples:**
- "Apartment Expenses" shared by two roommates for rent, utilities, and groceries
- "Weekend Trip - Lake Tahoe" shared by three friends for travel expenses
- "Family Budget" shared by a couple for joint household costs

**Technical Notes:** Shared wallets live in a separate `shared_wallets` table (not the same as `wallets`). Type is immutable -- a shared wallet can never become a personal wallet (INV-W03). Minimum 2 members enforced by database trigger (INV-W06). Creator is auto-added as first member. All members have identical permissions (no hierarchy). Members can leave at any time, but historical data persists. Debt calculations are per-shared-wallet. Settlements are per-split within the shared wallet context.

**Usage in Code:**
- **Backend:** `Wallet` entity with `WalletType.Shared` in `Kakeibo.Api.Features.Wallets`; `WalletMember` entity for membership
- **Frontend:** Shared wallet cards with member avatars, debt summary, and invitation management
- **Database:** `shared_wallets` table; `shared_wallet_members` join table

---

### Split

**Definition:** The mechanism for dividing a shared expense among wallet members. Splits determine how much each member should pay for a transaction and are used to calculate debts. Three split types exist: **Equal Split** (divide evenly), **Percentage Split** (divide by percentages summing to 100%), and **Custom Split** (specify exact amounts summing to the transaction total).

**Module:** Wallets

**Related Concepts:** **Equal Split**, **Percentage Split**, **Custom Split**, **Debt**, **Settlement**, **Shared Wallet**

**Examples:**
- $100 dinner with equal split among 3 people: $33.34 + $33.33 + $33.33
- $1,000 rent with percentage split: 60% ($600) + 40% ($400)
- $75 shopping with custom split: $45 + $30

**Technical Notes:** Splits only exist for shared wallet transactions (INV-T06). The payer's split is automatically marked `status = settled, is_payer = true`. Non-payer splits start with `status = pending, owed_to_user_id = payer_user_id`. Split validation enforces `SUM(split.amount) == transaction.amount` (INV-T05). Splits are cascade-deleted when the parent transaction is deleted. The `transaction_splits` table drives all debt calculations.

**Usage in Code:**
- **Backend:** `Split` entity in `Kakeibo.Api.Features.Wallets`; `SplitType` value object (Equal, Percentage, Custom)
- **Frontend:** Split configuration UI during shared expense recording; split summary on transaction detail view
- **Database:** `transaction_splits` table; `split_type VARCHAR(20)`, `amount DECIMAL(10,2)`, `percentage DECIMAL(5,2)`, `status VARCHAR(20)`

---

### System Category

**Definition:** One of the 12 built-in, non-deletable categories that provide comprehensive coverage of common financial transaction types. System categories are shared by all users, cannot be renamed, archived, or have their type changed, and serve as the foundation for transaction classification.

**Module:** Transactions

**Related Concepts:** **Category**, **Budget**, **Transaction**

**Examples:**

| # | Category | Examples |
|---|----------|----------|
| 1 | Housing | Rent, mortgage, utilities |
| 2 | Transportation | Fuel, maintenance, public transit |
| 3 | Food & Dining | Groceries, restaurants |
| 4 | Health & Wellness | Medical, fitness |
| 5 | Entertainment & Leisure | Hobbies, recreation |
| 6 | Shopping & Personal | Clothing, personal care |
| 7 | Education | Courses, books, supplies |
| 8 | Subscriptions & Bills | Streaming, memberships |
| 9 | Savings & Investments | Transfers to savings, investment contributions |
| 10 | Debt & Loans | Loan payments, interest |
| 11 | Gifts & Donations | Presents, charitable giving |
| 12 | Other | Miscellaneous |

**Technical Notes:** System categories have `is_system = true` and are seeded during database initialization by `SystemCategoriesSeeder`. All mutation endpoints check `category.is_system` and reject changes with `Error.Validation("Category.SystemImmutable")` (INV-C01). System category names are globally unique per type.

**Usage in Code:**
- **Backend:** `SystemCategory` value object in `Kakeibo.Api.Features.Transactions`; `SystemCategoriesSeeder` in `Kakeibo.Api.Persistence`
- **Frontend:** System categories shown first in category selector, visually distinguished from custom categories
- **Database:** `categories` table; `is_system = TRUE` rows seeded at startup

---

### Transaction

**Definition:** A financial event that changes one or more wallet balances. Transactions are the fundamental building blocks of financial tracking in Kakeibo. Every transaction captures an amount, date, category, description, and the wallet(s) involved.

**Module:** Transactions

**Related Concepts:** **Income**, **Expense**, **Transfer**, **Wallet**, **Category**, **Split**

**Examples:**

| Type | Example | Balance Impact |
|------|---------|----------------|
| Income | $2,000 salary deposit | Wallet balance increases |
| Expense | $45 restaurant dinner | Wallet balance decreases |
| Transfer | $500 from Checking to Savings | Source decreases, destination increases |

**Technical Notes:** Transaction amount must be 0.01 to 999,999,999.99 with 2 decimal places (INV-T01). Consolidated transactions cannot have future dates (INV-T02). Every transaction belongs to exactly one wallet (personal or shared, never both -- INV-W05). Every transaction has exactly one category (INV-T04). Transfers are modeled as two transactions within the same database transaction for atomicity (INV-T03). Soft-deleted transactions are recoverable for 30 days, then permanently purged by a background job.

**Usage in Code:**
- **Backend:** `Transaction` entity in `Kakeibo.Api.Features.Transactions`; `TransactionType` value object (Income, Expense, Transfer)
- **Frontend:** Transaction list, recording form, detail view, edit form
- **Database:** `transactions` table; `amount DECIMAL(10,2)`, `category_id VARCHAR(25) NOT NULL`

---

### Transfer

**Definition:** A transaction type representing money moving between two wallets. Transfers atomically decrease the source wallet balance and increase the destination wallet balance by the same amount within a single database transaction. Transfers can occur between the same user's wallets or between wallets in different contexts.

**Module:** Transactions

**Related Concepts:** **Transaction**, **Income**, **Expense**, **Wallet**

**Examples:**
- Moving $500 from Checking Account to Savings Account (same user)
- Contributing $300 from personal wallet to a shared wallet (cross-context)

**Technical Notes:** Transfers are modeled as two transactions (one expense in the source, one income in the destination) within the same `DbContext.SaveChangesAsync()` call. If either fails, the database transaction is rolled back (INV-T03). Transfer to the same wallet is rejected with `Error.Validation("Transfer.SameWallet")`. Both transactions share a correlation ID for traceability.

**Usage in Code:**
- **Backend:** `TransactionType.Transfer` value object; `CreateTransferHandler` wraps both operations in a single unit of work
- **Frontend:** Transfer form with source and destination wallet selectors
- **Database:** Two rows in `transactions` table linked by a transfer correlation ID

---

### Value Object

**Definition:** A domain object that is defined by its structural properties rather than a unique identity. Two value objects are equal if all their properties are equal, regardless of reference identity. Value objects are immutable and used to model concepts like wallet types, split types, and phone numbers.

**Module:** Kakeibo.Common

**Related Concepts:** **Entity**

**Examples:**
- `WalletType` -- Personal vs. Shared (compared by value, not identity)
- `SplitType` -- Equal, Percentage, Custom
- `TransactionType` -- Income, Expense, Transfer

**Technical Notes:** Value objects extend the `ValueObject` base class, which provides structural equality through `GetEqualityComponents()`. The base class overrides `Equals()`, `GetHashCode()`, and the `==`/`!=` operators. Value objects should be immutable -- all properties should be `init`-only or readonly.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Common.Abstractions.ValueObject`; implementations in feature folders alongside the entities that own them
- **Frontend:** Typically represented as TypeScript union types or enums
- **Database:** Stored as columns on the owning entity's table (e.g., `wallet_type VARCHAR(20)`)

---

### Vertical Slice

**Definition:** An architectural pattern where each feature is organized as a self-contained folder containing all the files needed to implement that feature: an endpoint, a handler, and a validator. This eliminates horizontal layers (Controllers, Services, Repositories) in favor of cohesive, independently modifiable feature units.

**Module:** N/A (architectural principle)

**Related Concepts:** **Screaming Architecture**, **Endpoint**, **Handler**, **Validator**

**Examples:**
- `CreateWallet/` folder contains `CreateWalletEndpoint.cs`, `CreateWalletHandler.cs`, `CreateWalletValidator.cs`
- `RecordTransaction/` folder contains `RecordTransactionEndpoint.cs`, `RecordTransactionHandler.cs`, `RecordTransactionValidator.cs`
- Adding a new feature means creating a new folder with 1-3 files, not modifying horizontal layers

**Technical Notes:** Each feature has up to 3 files. Handlers are plain classes with a `HandleAsync` method (no MediatR, no CQRS interfaces). Scrutor auto-registers handlers by name convention (`*Handler`). Cross-cutting concerns (validation, authorization, rate limiting) are applied via endpoint filters, not decorator chains.

**Usage in Code:**
- **Backend:** `src/Kakeibo.Api/Features/{Domain}/{Operation}/` folder pattern
- **Frontend:** N/A (backend architectural concept, though frontend pages follow a similar feature-folder pattern)
- **Database:** N/A

---

### Wallet

**Definition:** A financial container that holds money and organizes transactions. Wallets represent real-world financial accounts such as bank accounts, cash envelopes, credit cards, or shared expense pools. Each wallet maintains a balance derived from its transaction history and belongs to either one user (personal) or multiple users (shared).

**Module:** Wallets

**Related Concepts:** **Personal Wallet**, **Shared Wallet**, **Balance**, **Transaction**, **Archiving**

**Examples:**
- Personal wallets: "Checking Account", "Savings Account", "Cash", "Credit Card"
- Shared wallets: "Apartment Expenses", "Weekend Trip", "Family Budget"

**Technical Notes:** Wallet type is immutable after creation (INV-W03). Balance accuracy is a critical invariant (INV-W01). Wallets with 1+ transactions cannot be deleted, only archived (INV-W04). Each user has exactly one default personal wallet (INV-W07). Wallet names are not required to be unique -- users differentiate by icon and color. Currency is set at wallet creation (single-currency MVP). Shared wallets must maintain at least 2 members (INV-W06).

**Usage in Code:**
- **Backend:** `Wallet` entity in `Kakeibo.Api.Features.Wallets`; `WalletType` value object
- **Frontend:** Wallet cards on dashboard, wallet selector in transaction forms, wallet management page
- **Database:** `wallets` table (personal) and `shared_wallets` table (shared)

---

## 2. Technical Terms

### AsNoTracking

**Definition:** An Entity Framework Core query optimization that tells the change tracker not to track the returned entities. This reduces memory consumption and improves performance for read-only queries where the entities will not be modified or saved back to the database.

**Module:** All modules (query optimization)

**Related Concepts:** **DbContext**, **Handler**

**Examples:**
- Listing transactions for display: `db.Transactions.AsNoTracking().Where(...).ToListAsync(ct)`
- Getting wallet balance for a read-only endpoint
- Budget status queries that aggregate transaction data

**Technical Notes:** Use `AsNoTracking()` on all read-only queries (GET endpoints, module request handlers). Do NOT use it on queries where the returned entities will be modified and saved (e.g., update handlers). In module request handlers that serve cross-module queries, always use `AsNoTracking()` since the calling module cannot modify the returned data.

**Usage in Code:**
- **Backend:** `.AsNoTracking()` method chain on EF Core `DbSet<T>` queries
- **Frontend:** N/A (backend concept only)
- **Database:** N/A (query-level optimization, not schema-level)

---

### Circuit Breaker

**Definition:** A Polly resilience pattern that monitors the failure rate of an operation and automatically "opens" the circuit (stops calling the operation) when failures exceed a threshold. After a recovery period, the circuit "half-opens" to test if the operation has recovered. This prevents cascading failures when a dependency is unhealthy.

**Module:** Kakeibo.Infrastructure

**Related Concepts:** **Polly**, **Outbox Processor**

**Examples:**
- Circuit breaker on email service calls: if the SMTP server is down, stop attempting to send emails and fail fast
- Circuit breaker on ClickHouse audit writes: if ClickHouse is unreachable, buffer audit events instead of blocking

**Technical Notes:** Configured via Polly in the infrastructure layer. Works in conjunction with retry policies (retry first, then circuit break if retries fail consistently). The circuit breaker pattern is applied to external service calls, not to database operations (which use the outbox pattern for reliability instead).

**Usage in Code:**
- **Backend:** Polly `CircuitBreakerPolicy` in `Kakeibo.Infrastructure`
- **Frontend:** N/A
- **Database:** N/A

---

### Composable

**Definition:** A Vue 3 Composition API function that encapsulates and reuses stateful logic. Composables follow the naming convention `use{Name}` and use Vue's reactivity system (`ref`, `computed`, `watch`) to create reusable units of functionality that can be shared across components.

**Module:** Kakeibo.App (frontend)

**Related Concepts:** **Pinia** (state management)

**Examples:**
- `useAuth()` -- manages authentication state, token refresh, and logout
- `useWallets()` -- fetches and caches wallet list, provides wallet creation
- `useBudgetStatus()` -- computes budget progress and alerts from transaction data

**Technical Notes:** Composables are located in `src/Kakeibo.App/composables/`. They use the `@/` path alias for imports. Composables should use TypeScript with strict typing. State shared across the entire application belongs in Pinia stores, not composables.

**Usage in Code:**
- **Backend:** N/A (frontend concept only)
- **Frontend:** `src/Kakeibo.App/composables/use{Name}.ts`
- **Database:** N/A

---

### DbContext

**Definition:** The single Entity Framework Core class (`AppDbContext`) that represents a session with the database for the entire application. It combines the Unit of Work and Repository patterns and exposes all `DbSet<T>` properties for all domains. All domains share a single PostgreSQL schema (`public`) and a single migrations history table.

**Domain:** Kakeibo.Api (persistence layer)

**Related Concepts:** **Entity**

**Examples:**
- `AppDbContext` with `DbSet<Wallet>`, `DbSet<Transaction>`, `DbSet<Budget>`, `DbSet<Goal>`, etc.
- A single migration history table manages all schema changes across all domains
- All entities use `UseSnakeCaseNamingConvention()` and `UseNodaTime()`

**Technical Notes:** `AppDbContext` is registered once in `Program.cs`. Migrations are generated with `--context AppDbContext --output-dir Persistence/Migrations`. `ApplyConfigurationsFromAssembly` picks up all `IEntityTypeConfiguration<T>` classes automatically. There is no `OutboxInterceptor` -- events are published explicitly by feature handlers before `SaveChangesAsync`.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Persistence.AppDbContext`; configurations in `Kakeibo.Api.Persistence.Configurations`
- **Frontend:** N/A (backend concept only)
- **Database:** Single `public` schema; single `__EFMigrationsHistory` table

---

### Feature Handler

**Definition:** A plain class with a `HandleAsync` method that contains all the business logic for a single feature operation. Feature handlers are automatically registered as scoped services by Scrutor (scanning for `*Handler` by name convention). They are injected directly into endpoint delegates via .NET's DI container.

**Domain:** Kakeibo.Api

**Related Concepts:** **Endpoint**, **Validator**, **Vertical Slice**

**Examples:**
- `CreateWalletHandler` -- validates no duplicate name, creates the wallet, publishes `WalletCreatedEvent`, calls `SaveChangesAsync`
- `ListTransactionsHandler` -- queries transactions with filters, returns a paginated response
- `RecordTransactionHandler` -- validates wallet membership, creates the transaction, updates balance, publishes event

**Technical Notes:** Handlers are plain classes with no base class and no interface. They use primary constructors for DI injection. The naming convention `*Handler` is what Scrutor uses to auto-register them. Feature handlers must NOT implement `IEventHandler<T>` -- that interface is reserved for side-effect handlers in `Infrastructure/Events`.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Features.{Domain}.{Operation}.{Op}Handler`; injected into endpoint delegate via DI
- **Frontend:** N/A
- **Database:** N/A (handlers call `AppDbContext.SaveChangesAsync()` directly)

---

### Endpoint

**Definition:** A sealed class implementing `IEndpoint` that defines a single API endpoint with its route, HTTP method, request/response types, authorization requirements, and validation configuration. Endpoints contain nested `sealed record` types for request and response models, following the REPR (Request-Endpoint-Response) pattern.

**Module:** All modules (one endpoint per feature operation)

**Related Concepts:** **Handler**, **Validator**, **Vertical Slice**

**Examples:**
- `CreateWalletEndpoint` -- `POST /api/wallets` with nested `CreateWalletRequest` and `CreateWalletResponse`
- `ListTransactionsEndpoint` -- `GET /api/transactions` with nested `ListTransactionsResponse`
- `InviteToWalletEndpoint` -- `POST /api/wallets/{id}/invitations`

**Technical Notes:** Endpoints implement `static abstract void MapEndpoint(IEndpointRouteBuilder app)` from the `IEndpoint` interface. Request/response records are nested inside the endpoint class and follow `{Operation}Request`/`{Operation}Response` naming (TD-013). Cross-cutting concerns are applied via endpoint filters: `.WithValidation<TRequest>()`, `.RequireAuthorization()`, `.RequireRateLimiting("standard")`. Endpoints delegate all business logic to handlers.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Common.Endpoints.IEndpoint`; implementations in `src/Kakeibo.Api/Features/{Domain}/{Op}/{Op}Endpoint.cs`
- **Frontend:** N/A (backend concept, but frontend consumes these endpoints via Axios)
- **Database:** N/A

---

### Event Consumer

**Definition:** Replaced by **Event Handler** (`IEventHandler<T>`) in the Simple Monolith architecture. See the **Event Handler** entry in this section. The old `IEventConsumer<T>` / Outbox pattern no longer exists.

**Related Concepts:** **Event Handler**, **ChannelEventBus**, **EventDispatcher**

---

### FusionCache

**Definition:** A distributed caching library that combines in-memory (L1) and distributed (L2) cache layers with advanced features like stale data serving, cache stampede prevention, and soft/hard timeouts. In Kakeibo, FusionCache uses Redis as its L2 distributed cache backend.

**Module:** `Kakeibo.Api.Infrastructure.Caching`

**Related Concepts:** **Redis**, **ICacheService**

**Examples:**
- Caching wallet balances to avoid recomputing from transaction history on every request
- Caching user profile data to reduce database queries
- Cache invalidation when a transaction is recorded (balance changes)

**Technical Notes:** FusionCache is configured via `CachingOptions` with `const string SectionName = "Caching"`. The `ICacheService` interface abstracts caching operations. Cache entries have configurable durations. Redis connection is configured via `REDIS_PASSWORD` environment variable.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Infrastructure.Caching.ICacheService`, `FusionCacheService`, `CachingOptions`
- **Frontend:** N/A (backend concept; frontend uses Pinia for client-side state)
- **Database:** N/A (cache is stored in Redis, not PostgreSQL)

---

### Handler

**Definition:** A plain C# class with a `HandleAsync` method that contains the business logic for a feature operation. Handlers are NOT interfaces, abstract classes, or MediatR request handlers -- they are concrete classes registered by Scrutor via the `*Handler` naming convention. Handlers use primary constructors for dependency injection.

**Module:** All domains (one handler per feature operation)

**Related Concepts:** **Endpoint**, **Validator**, **Vertical Slice**, **Result<T>**

**Examples:**
- `CreateWalletHandler` -- validates wallet name uniqueness, creates the wallet entity, publishes events, saves to database
- `RecordTransactionHandler` -- validates wallet access, creates transaction, calculates splits for shared wallets, publishes events
- `GetBudgetStatusHandler` -- queries transaction data, computes spending and budget status

**Technical Notes:** Handlers return `Result<T>` to communicate success or failure to the endpoint. They use primary constructors for injected dependencies (`AppDbContext`, `IEventBus`, and any other service). No explicit `private readonly` fields or constructor bodies. Handlers are auto-registered by Scrutor with scoped lifetime. Events are published via `eventBus.Publish()` before `SaveChangesAsync` -- the `ChannelEventBus` dispatches them asynchronously via `EventDispatcher`.

**Usage in Code:**
- **Backend:** `src/Kakeibo.Api/Features/{Domain}/{Op}/{Op}Handler.cs`
- **Frontend:** N/A
- **Database:** N/A

---

### HttpOnly Cookie

**Definition:** A browser cookie with the `HttpOnly` flag set, making it inaccessible to JavaScript. In Kakeibo's web application, the refresh token is stored as an HttpOnly cookie to prevent XSS attacks from stealing the token. The access token is stored in memory (Pinia ref) for the session lifetime.

**Module:** Identity (backend), Kakeibo.App (frontend)

**Related Concepts:** **JWT**, **Refresh Token**

**Examples:**
- Server sets `Set-Cookie: refreshToken=...; HttpOnly; Secure; SameSite=Strict` on login
- Browser automatically sends the cookie on subsequent requests to `/api/auth/refresh`
- On logout, the server clears the cookie

**Technical Notes:** The web app uses HttpOnly cookies for refresh tokens. The API's `POST /api/auth/refresh` endpoint extracts the refresh token from: (1) HttpOnly cookie (web, checked first), (2) request body (reserved for future mobile client). The `Secure` flag ensures the cookie is only sent over HTTPS. `SameSite=Strict` prevents CSRF attacks.

**Usage in Code:**
- **Backend:** Cookie configuration in `src/Kakeibo.Api/Features/Identity/` auth endpoints
- **Frontend:** Axios interceptor automatically retries failed requests by calling `/api/auth/refresh`
- **Database:** Refresh tokens stored in the `users` table (single `public` schema)

---

### Idempotency Key

**Definition:** An HTTP header (`Idempotency-Key`) sent by the client to prevent duplicate processing of the same request. If the server receives a second request with the same idempotency key, it returns the result of the first request without executing the operation again. This protects against network retries creating duplicate transactions.

**Module:** Kakeibo.Infrastructure (middleware)

**Related Concepts:** **Transaction**, **ChannelEventBus**

**Examples:**
- Client sends `POST /api/transactions` with `Idempotency-Key: abc-123`; if the network fails and the client retries with the same key, the server returns the original response
- Prevents double-charging when a user taps "Pay" twice quickly

**Technical Notes:** Idempotency keys are typically UUIDv4 strings generated by the client. The server stores the key and response for a configurable TTL. Duplicate requests return the cached response with the same status code. This is particularly important for transaction recording and settlement operations.

**Usage in Code:**
- **Backend:** Idempotency middleware in `src/Kakeibo.Api/Infrastructure/`
- **Frontend:** Axios interceptor generates and attaches idempotency keys for mutating requests
- **Database:** Idempotency key cache (Redis or PostgreSQL, depending on implementation)

---

### Module Client

**Definition:** Replaced by **direct handler injection** in the Simple Monolith architecture. Because all domains live in the same assembly (`Kakeibo.Api`), cross-domain data queries are handled by injecting the target handler or service directly into the calling handler via DI -- no dispatcher or `IModuleClient` interface is needed.

**Related Concepts:** **Handler**, **Feature Handler**, **AppDbContext**

**Examples:**
- A Budgets handler that needs transaction data injects `GetTransactionsInPeriodHandler` directly
- A Goals handler that needs wallet balance queries `AppDbContext.WalletBalances` directly

**Technical Notes:** There are no cross-assembly boundaries in the Simple Monolith. All feature handlers are in the same `Kakeibo.Api` project and can be injected through normal ASP.NET Core DI. `IModuleClient`, `IModuleRequest`, and `IModuleRequestHandler` no longer exist.

**Usage in Code:**
- **Backend:** Direct constructor injection in `src/Kakeibo.Api/Features/{Domain}/{Op}/{Op}Handler.cs`
- **Frontend:** N/A
- **Database:** N/A

---

### Module Event Bus

**Definition:** Replaced by **`IEventBus` / `ChannelEventBus`** in the Simple Monolith architecture. See the **ChannelEventBus** entry in this section. `IModuleEventBus`, `IIntegrationEvent`, and the outbox table no longer exist.

**Related Concepts:** **ChannelEventBus**, **Event Handler**, **EventDispatcher**

---

### Outbox Interceptor

**Definition:** Replaced by **`ChannelEventBus` + `EventDispatcher`** in the Simple Monolith architecture. There is no EF Core interceptor, no outbox table, and no `OutboxMessage` entity. Events are published via `IEventBus.Publish()` before `SaveChangesAsync` and dispatched asynchronously by the `EventDispatcher` BackgroundService. See **ChannelEventBus** and **EventDispatcher** entries.

**Related Concepts:** **ChannelEventBus**, **EventDispatcher**, **Event Handler**

---

### Outbox Processor

**Definition:** Replaced by **`EventDispatcher`** in the Simple Monolith architecture. `EventDispatcher` is a `BackgroundService` that reads from an in-memory `Channel<IEvent>` and dispatches events to `IEventHandler<T>` implementations. There is no database polling, no `outbox_messages` table, and no Polly retry. See the **EventDispatcher** entry (under **ChannelEventBus**) and **Event Handler**.

**Related Concepts:** **ChannelEventBus**, **Event Handler**, **EventDispatcher**

---

### Primary Constructor

**Definition:** A C# 12 language feature that allows constructor parameters to be declared directly on the class declaration, eliminating the need for explicit `private readonly` fields and constructor bodies. In Kakeibo, primary constructors are mandatory for all classes in `src/` (enforced by `.editorconfig` with `IDE0290:warning` and `TreatWarningsAsErrors`).

**Module:** All domains (coding convention)

**Related Concepts:** **Handler**, **Endpoint**, **Validator**

**Examples:**
```csharp
// Good: primary constructor
public sealed class CreateWalletHandler(AppDbContext db, IEventBus eventBus)

// Bad: traditional constructor (prohibited)
public sealed class CreateWalletHandler
{
    private readonly AppDbContext _db;
    public CreateWalletHandler(AppDbContext db) { _db = db; }
}
```

**Technical Notes:** Enforced by `.editorconfig` rule `csharp_style_prefer_primary_constructors = true:warning`. With `TreatWarningsAsErrors` enabled, any traditional constructor triggers a build error. This is a mandatory rule (see mandatory.md Rule 8).

**Usage in Code:**
- **Backend:** All `*.cs` files under `src/`
- **Frontend:** N/A (C# concept)
- **Database:** N/A

---

### Result\<T\>

**Definition:** A discriminated union type that represents the outcome of an operation as either a success (with a value of type `T`) or a failure (with an `Error` record). Handlers return `Result<T>` instead of throwing exceptions for expected failure cases, enabling the endpoint to map different error types to appropriate HTTP status codes.

**Module:** `Kakeibo.Api.Common.Abstractions`

**Related Concepts:** **Error**, **Handler**, **Endpoint**

**Examples:**
```csharp
// Success path
return new CreateWalletResponse(wallet.Id, wallet.Name);  // implicit conversion to Result<T>

// Failure path
return Error.Conflict("A wallet with that name already exists.");  // implicit conversion to Result<T>

// Endpoint mapping
return result.IsSuccess
    ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
    : result.Error.Code switch { "conflict" => TypedResults.Conflict(result.Error), ... };
```

**Technical Notes:** `Result<T>` uses `[MemberNotNullWhen]` attributes so the compiler enforces null safety through flow analysis -- no `!` operator needed when checking `IsSuccess`/`IsFailure` (see KB-002). Implicit conversion operators allow returning `T` or `Error` directly. The `Error` record has factory methods: `NotFound()`, `Validation()`, `Conflict()`, `Unauthorized()`, `Forbidden()`, `Internal()`.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Common.Abstractions.Result<T>`, `Kakeibo.Api.Common.Abstractions.Error`
- **Frontend:** N/A (backend concept; frontend receives HTTP status codes)
- **Database:** N/A

---

### Scrutor

**Definition:** A .NET library that provides assembly scanning for automatic dependency injection registration. In Kakeibo, Scrutor auto-registers feature handlers (by `*Handler` name convention) and event handlers (by `IEventHandler<>` interface) in `Program.cs` with scoped lifetime.

**Module:** `Kakeibo.Api` (DI registration in `Program.cs`)

**Related Concepts:** **Handler**, **Event Handler**, **Feature Handler**, **DI Registration**

**Examples:**
```csharp
// Auto-register all classes ending in "Handler" as themselves
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Handler")))
    .AsSelf()
    .WithScopedLifetime());

// Auto-register all IEventHandler<T> implementations
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

**Technical Notes:** There are no per-module registration classes. All scanning is done once in `Program.cs` scanning the single `Kakeibo.Api` assembly. Two scan patterns: (1) feature handlers by `*Handler` name convention, (2) event handlers by `IEventHandler<>` interface. `IModuleRequestHandler`, `IEventConsumer`, and `IDomainEventHandler` no longer exist.

**Usage in Code:**
- **Backend:** NuGet package `Scrutor`; used in `src/Kakeibo.Api/Program.cs`
- **Frontend:** N/A
- **Database:** N/A

---

### Testcontainers

**Definition:** A .NET library that provides lightweight, throwaway instances of Docker containers for integration testing. In Kakeibo, Testcontainers spins up real PostgreSQL instances for integration tests instead of using in-memory fakes, ensuring tests exercise the actual database engine with real SQL, constraints, and migrations.

**Module:** All test projects

**Related Concepts:** **xUnit v3**, **DbContext**, **PostgreSQL**

**Examples:**
- `PostgreSqlContainer` provides a real PostgreSQL 18 instance for each test run
- Integration tests create a fresh database per test class, apply migrations, and run against real SQL
- Tests automatically skip (not fail) in CI environments without Docker access (KB-008)

**Technical Notes:** `.WithReuse(true)` is PROHIBITED (mandatory.md Rule 4) because it causes Docker validation at class load time, breaking CI. Tests must use `Assert.Skip()` in a `try-catch` around container startup to handle environments without Docker. The `Lazy<Task>` pattern ensures containers start at most once per test class. EF Core InMemory and SQLite in-memory are prohibited alternatives.

**Usage in Code:**
- **Backend:** NuGet package `Testcontainers.PostgreSql`; used in `tests/Kakeibo.Tests/`
- **Frontend:** N/A
- **Database:** Ephemeral PostgreSQL containers created and destroyed per test run

---

### Validator

**Definition:** A FluentValidation class that defines validation rules for an endpoint's request model. Validators are auto-registered by FluentValidation's assembly scanning and are invoked automatically via the `ValidationFilter<T>` endpoint filter before the handler executes.

**Module:** All modules (one validator per feature operation)

**Related Concepts:** **Endpoint**, **Handler**, **Vertical Slice**

**Examples:**
```csharp
public sealed class CreateWalletValidator
    : AbstractValidator<CreateWalletEndpoint.CreateWalletRequest>
{
    public CreateWalletValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
    }
}
```

**Technical Notes:** Validators extend `AbstractValidator<TRequest>` where `TRequest` is the endpoint's nested request record. They are applied via `.WithValidation<TRequest>()` on the endpoint. If validation fails, the endpoint returns a 400 Bad Request with validation errors before the handler is invoked. Validators are registered via `builder.Services.AddValidatorsFromAssemblyContaining<Program>()` in `Program.cs`.

**Usage in Code:**
- **Backend:** `src/Kakeibo.Api/Features/{Domain}/{Op}/{Op}Validator.cs`; `Kakeibo.Api.Common.Endpoints.ValidationFilter<T>`
- **Frontend:** VeeValidate + Zod for client-side validation (mirrors backend rules)
- **Database:** N/A (validation occurs before database access)

---

## 3. Infrastructure Terms

### ClickHouse

**Definition:** A columnar analytical database optimized for high-volume writes and fast aggregation queries. In Kakeibo, ClickHouse stores audit logs and activity events separately from the transactional PostgreSQL database. Its append-only nature makes it ideal for immutable audit trails.

**Module:** Kakeibo.Infrastructure (Audit)

**Related Concepts:** **Audit Trail**, **Integration Event**

**Examples:**
- All audit events (user actions, transaction changes, member joins) are written to ClickHouse
- Time-range queries for "show all actions in this shared wallet last month"
- Aggregate queries for "how many transactions were recorded per day this year"

**Technical Notes:** ClickHouse runs as a Docker container with custom configuration files in `.docker/clickhouse/` for log levels, IPv4-only mode, and low-resource development settings. Port 8123 (HTTP interface) is used for queries. In production, ClickHouse is internal only -- accessible through SSH tunnels. Health check integration via `ClickHouseHealthCheck`.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Infrastructure.Audit.ClickHouseAuditService`, `ClickHouseOptions`
- **Frontend:** N/A (audit data consumed via API endpoints in the Auditing feature)
- **Database:** Separate ClickHouse instance; `audit_events` table

---

### Docker Compose

**Definition:** The runtime orchestrator for all development and production services. Kakeibo uses a single `docker-compose.yml` file with profiles to separate infrastructure services (always running) from application services (started with `--profile app`). There is no Kubernetes -- the current scale does not justify that complexity.

**Module:** Infrastructure (deployment)

**Related Concepts:** **PostgreSQL**, **Redis**, **RustFS**, **ClickHouse**

**Examples:**
- `docker compose up -d` starts infrastructure only (PostgreSQL, Redis, RustFS, ClickHouse, Mailpit, email renderer)
- `docker compose --profile app up -d` starts the full stack including API and frontend
- Named networks enforce service isolation: `postgres-network`, `redis-network`, `clickhouse-network`

**Technical Notes:** Infrastructure services define health checks; application services use `depends_on` with `condition: service_healthy`. Named volumes provide persistence. Production deployments are handled by CI scripts -- images are built, pushed to a private registry, and pulled by the server. Port 5432 for PostgreSQL is flagged `## REMOVE ON PRODUCTION`.

**Usage in Code:**
- **Backend:** `docker-compose.yml` at repo root
- **Frontend:** `kakeibo-app` container serves the built Vue SPA via Nginx
- **Database:** `postgresdb` container with `postgres-data` named volume

---

### Guid7

**Definition:** A wrapper around the `Medo.Uuid7` library that generates UUIDv7 identifiers with correct big-endian byte order for PostgreSQL B-tree index compatibility. The .NET built-in `Guid.CreateVersion7()` has little-endian byte order that breaks PostgreSQL sorting, so it is PROHIBITED throughout the codebase.

**Module:** Kakeibo.Common

**Related Concepts:** **Entity**

**Examples:**
```csharp
// Good: use Guid7 wrapper
var id = Guid7.NewGuid();

// Bad: PROHIBITED - broken byte order
var id = Guid.CreateVersion7();
```

**Technical Notes:** UUIDv7 is time-ordered, meaning new IDs sort after older IDs in PostgreSQL B-tree indexes. This provides near-sequential insert performance. The `Guid7` wrapper delegates to `Uuid7.NewUuid7()` from the `Medo.Uuid7` NuGet package. The `Entity` base class initializes `Id` with `Guid7.NewGuid().ToGuid()`. Regular `Guid` is allowed for non-entity purposes (e.g., correlation IDs, idempotency keys).

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Common.Utils.Guid7`; `Medo.Uuid7` NuGet package
- **Frontend:** IDs are received as strings from the API
- **Database:** `UUID` column type in PostgreSQL; stored with big-endian byte order for correct B-tree sorting

---

### Hangfire

**Definition:** A background job processing library with PostgreSQL storage for scheduled and recurring jobs. In Kakeibo, Hangfire runs the daily recurring transaction generation job, invitation expiration cleanup, soft-deleted transaction purging (30-day retention), and other scheduled operations.

**Module:** `Kakeibo.Api.Infrastructure`, `Kakeibo.Api.Features.Recurring`

**Related Concepts:** **Recurring Pattern**, **Forecast**, **EventDispatcher**

**Examples:**
- Daily job: scan active recurring patterns and generate forecasted transactions up to 90 days ahead
- Daily job: mark expired invitations (`status = 'expired'` after 7 days)
- Daily job: permanently purge soft-deleted transactions older than 30 days

**Technical Notes:** Hangfire uses `Hangfire.PostgreSql` for job storage (same PostgreSQL instance as the application). `ChannelEventBus` handles async event dispatch (not Hangfire) -- Hangfire is specifically for scheduled/recurring background work. Quartz.NET is a prohibited alternative.

**Usage in Code:**
- **Backend:** NuGet packages `Hangfire`, `Hangfire.PostgreSql`; job registration in `Program.cs`
- **Frontend:** N/A
- **Database:** Hangfire creates its own tables in the PostgreSQL instance for job metadata

---

### NodaTime

**Definition:** A date and time library for .NET that replaces the error-prone BCL `DateTime`/`DateTimeOffset` types with explicit, unambiguous types. In Kakeibo, NodaTime is mandatory -- BCL date/time types are PROHIBITED (TD-004). All timestamps are stored as `Instant` (UTC point in time), and date-only values use `LocalDate`.

**Module:** `Kakeibo.Api.Common`, all domains

**Related Concepts:** **Entity**, **PostgreSQL**

**Examples:**
```csharp
// Good: NodaTime
var now = SystemClock.Instance.GetCurrentInstant();
var today = now.InUtc().Date; // LocalDate

// Bad: PROHIBITED
var now = DateTime.UtcNow;
var today = DateOnly.FromDateTime(DateTime.Now);
```

**Technical Notes:** EF Core uses `UseNodaTime()` in the Npgsql configuration to map NodaTime types to PostgreSQL `TIMESTAMP WITH TIME ZONE` columns. The `Entity` base class uses `Instant` for `CreatedAt` and `UpdatedAt`. Timezone conversion for display is handled at the API layer using the user's timezone preference. Shared wallet timestamps are shown in each user's own timezone.

**Usage in Code:**
- **Backend:** NuGet package `NodaTime`; `Instant`, `LocalDate`, `SystemClock` throughout `src/`
- **Frontend:** Timestamps received as ISO 8601 strings; converted using `date-fns` for display
- **Database:** `TIMESTAMP WITH TIME ZONE` columns; stored in UTC

---

### PBKDF2-SHA512

**Definition:** The password hashing algorithm used in Kakeibo. PBKDF2 (Password-Based Key Derivation Function 2) with SHA-512 applies a pseudorandom function iteratively to the password and a random salt, producing a derived key that is computationally expensive to brute-force. BCrypt and Argon2id are prohibited alternatives.

**Module:** Kakeibo.Common

**Related Concepts:** **Identity**, **PasswordHasher**

**Examples:**
- User registers with password "MyP@ssw0rd" -> hashed with 350,000 iterations of PBKDF2-SHA512 -> stored as base64 string
- Login attempt: password verified against stored hash using constant-time comparison (`CryptographicOperations.FixedTimeEquals`)

**Technical Notes:** Configuration: 16-byte salt (128 bits), 32-byte key (256 bits), 350,000 iterations (OWASP recommended minimum). Salt and hash are concatenated and stored as a single base64 string. Verification extracts the salt, re-derives the hash, and compares in constant time to prevent timing attacks.

**Usage in Code:**
- **Backend:** `Kakeibo.Common.Utils.PasswordHasher` with `HashPassword()` and `VerifyPassword()` static methods
- **Frontend:** N/A (password hashing is server-side only)
- **Database:** `password_hash VARCHAR(255)` column in `users` table

---

### PostgreSQL Schema

**Definition:** A logical namespace within a PostgreSQL database. In the Simple Monolith, Kakeibo uses a **single `public` schema** for all tables. All entities from all domains share the same schema and are managed by the single `AppDbContext`. There are no per-domain schemas, no schema-scoped migrations, and no `outbox_messages` table.

**Module:** `Kakeibo.Api.Persistence` (single `AppDbContext`)

**Related Concepts:** **AppDbContext**, **Simple Monolith**

**Examples:**
```sql
-- All tables in the single public schema
CREATE TABLE users (...);
CREATE TABLE wallets (...);
CREATE TABLE transactions (...);
CREATE TABLE budgets (...);
```

**Technical Notes:** `AppDbContext.OnModelCreating` applies `UseSnakeCaseNamingConvention()` and `UseNodaTime()`. Configurations are loaded via `ApplyConfigurationsFromAssembly`. Migrations are stored in a single `__ef_migrations_history` table. There is one migration history, one DbContext, and one schema.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Persistence.AppDbContext`; migrations in `src/Kakeibo.Api/Persistence/Migrations/`
- **Frontend:** N/A
- **Database:** All tables in the `public` schema of the `kakeibo` database

---

### Redis

**Definition:** An in-memory data store used as the distributed cache backend for FusionCache. Redis provides the L2 cache layer that is shared across application instances, ensuring cache consistency in multi-instance deployments.

**Module:** Kakeibo.Infrastructure (Caching)

**Related Concepts:** **FusionCache**, **ICacheService**

**Examples:**
- Cached wallet balance keyed by wallet ID with configurable TTL
- Cached user preferences keyed by user ID
- Cache invalidation on transaction recording

**Technical Notes:** Redis runs as a Docker container with `redis:8.4-alpine` image. Password-protected via `REDIS_PASSWORD` environment variable. Data persisted to `redis-data` named volume. Redis Insight GUI available at port 5540 for development. In production, Redis is internal only (no port exposed to host).

**Usage in Code:**
- **Backend:** FusionCache configured with Redis backend; `CachingOptions`
- **Frontend:** N/A
- **Database:** N/A (Redis is a separate data store from PostgreSQL)

---

### RustFS

**Definition:** An S3-compatible object storage server used for file storage (avatars, documents, receipts). RustFS is an open-source (Apache 2.0) alternative to MinIO, which is prohibited due to being archived with no security patches. The Minio NuGet SDK is still used as the S3 client library.

**Module:** `Kakeibo.Api.Infrastructure.Storage`

**Related Concepts:** **IStorageService**

**Examples:**
- User avatar uploads stored in an `avatars` bucket
- Transaction receipt images stored in a `receipts` bucket

**Technical Notes:** RustFS alpha.83 has a known limitation: SSE (Server-Side Encryption) is broken -- data is stored in plaintext on disk (KB-009). This is accepted for the MVP but must be re-evaluated before handling sensitive documents. Ports: 9000 (API), 9001 (console). In production, only port 9000 may be exposed; console is internal only.

**Usage in Code:**
- **Backend:** `Kakeibo.Api.Infrastructure.Storage.IStorageService`, `StorageService`, `StorageOptions`; Minio NuGet SDK
- **Frontend:** File upload components that POST to storage endpoints
- **Database:** N/A (files stored in RustFS, not PostgreSQL; metadata may be in PostgreSQL)

---

### Scalar

**Definition:** An API documentation tool that replaces Swagger/Swashbuckle for OpenAPI visualization. Scalar provides a modern, interactive interface for exploring and testing API endpoints. Swagger is a prohibited alternative.

**Module:** Kakeibo.Api

**Related Concepts:** **Endpoint**, **Minimal APIs**

**Examples:**
- API documentation available at `http://localhost:5000/scalar`
- Interactive request testing from the browser
- Auto-generated from Minimal API endpoint definitions

**Technical Notes:** Registered in `Program.cs` with `app.MapScalarApiReference()`. Scalar reads the OpenAPI spec generated by ASP.NET Core's built-in OpenAPI support (`.NET 10` native, no Swashbuckle needed).

**Usage in Code:**
- **Backend:** `app.MapScalarApiReference()` in `Program.cs`
- **Frontend:** N/A (Scalar is a standalone documentation UI)
- **Database:** N/A

---

## 4. Process Terms

### Conventional Commits

**Definition:** A commit message format that structures commit messages as `type(scope): description` to enable automated versioning, changelog generation, and CI pipeline optimization. This format is enforced by commitlint via the pre-commit hook.

**Module:** N/A (development process)

**Related Concepts:** **Semantic Release**, **Lefthook**, **Squash Merge**

**Examples:**
```
feat(wallets): add shared wallet archiving endpoint
fix(transactions): prevent duplicate category names per user
docs(api): update budget status calculation formula
refactor(infrastructure): extract outbox configuration to options class
test(goals): add milestone notification integration test
```

**Technical Notes:** Commit types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`, `style`, `perf`, `build`, `revert`. Scopes are defined in `commitlint.config.ts` and must match registered projects. Breaking changes use `!` suffix: `feat(api)!: change wallet balance calculation`. The `commit-msg` hook runs commitlint to reject non-conforming messages.

**Usage in Code:**
- **Backend:** `commitlint.config.ts` at repo root
- **Frontend:** Same commit format applies to frontend changes
- **Database:** N/A

---

### GitHub Flow

**Definition:** A trunk-based branching strategy where `main` is always deployable. Feature branches are created from `main`, developed, and merged back via pull/merge requests. Kakeibo uses GitHub as the hosting platform, and the branching strategy follows standard GitHub Flow principles.

**Module:** N/A (development process)

**Related Concepts:** **Squash Merge**, **Conventional Commits**, **Semantic Release**

**Examples:**
- Developer creates branch `feat/shared-wallet-archiving` from `main`
- CI runs quality gates on merge request (format check, build, tests, Docker build)
- After approval and passing CI, the branch is squash-merged into `main`
- Semantic release creates a version tag and changelog entry on `main`

**Technical Notes:** Pipelines run in two situations: (1) Merge Request -- all quality gate jobs, (2) Push to `main` -- release job only. This prevents duplicate pipelines. The `main` branch is protected -- direct pushes are prohibited.

**Usage in Code:**
- **Backend:** `.gitlab-ci.yml` defines the CI pipeline
- **Frontend:** Same branching strategy applies
- **Database:** Database migrations follow the same branch/merge workflow

---

### Lefthook

**Definition:** A Git hooks manager that runs automated checks before commits and on commit messages. In Kakeibo, Lefthook enforces two hooks: `commit-msg` (runs commitlint for conventional commit format) and `pre-commit` (runs oxlint and oxfmt on staged files in parallel).

**Module:** N/A (development tooling)

**Related Concepts:** **Conventional Commits**, **oxlint**, **oxfmt**

**Examples:**
- `commit-msg` hook rejects commits that do not follow `type(scope): description` format
- `pre-commit` hook rejects commits with lint errors or formatting issues in staged `.ts/.vue/.js/.css/.json` files
- Both hooks run in check mode only -- they never auto-fix

**Technical Notes:** Configuration in `lefthook.yml` at repo root. Pre-commit runs two parallel commands on staged files in `src/Kakeibo.App/`: `oxlint --deny-warnings` and `oxfmt --check`. Always run auto-fix commands (`bun run app:format && bun run app:lint`) and re-stage files before committing (KB-004).

**Usage in Code:**
- **Backend:** `lefthook.yml` at repo root
- **Frontend:** Pre-commit hooks target frontend files
- **Database:** N/A

---

### Semantic Release

**Definition:** An automated versioning tool that determines the next version number (patch, minor, major) from conventional commit messages, generates or updates the changelog, creates a GitLab release with release notes, and tags the commit. It runs only on pushes to `main`.

**Module:** N/A (release process)

**Related Concepts:** **Conventional Commits**, **GitHub Flow**

**Examples:**
- `fix(wallets): prevent negative member count` -> patch bump (e.g., 1.2.3 -> 1.2.4)
- `feat(budgets): add projected overspend warning` -> minor bump (e.g., 1.2.4 -> 1.3.0)
- `feat(api)!: change wallet balance calculation` -> major bump (e.g., 1.3.0 -> 2.0.0)

**Technical Notes:** Runs in the `release` stage of the CI pipeline using `node:22-alpine`. Uses `semantic-release` with GitLab plugin suite. Only runs on `main` branch (quality already passed in the MR pipeline).

**Usage in Code:**
- **Backend:** Semantic release configuration in the CI pipeline
- **Frontend:** Same versioning applies to the monorepo
- **Database:** N/A

---

### Squash Merge

**Definition:** A pull/merge request merge strategy that combines all commits from a feature branch into a single commit on the target branch. This produces a clean, linear history on `main` where each commit represents one complete feature, bug fix, or change.

**Module:** N/A (development process)

**Related Concepts:** **GitHub Flow**, **Conventional Commits**, **Semantic Release**

**Examples:**
- Feature branch with 15 work-in-progress commits is squash-merged into `main` as a single `feat(wallets): add shared wallet archiving` commit
- The squash commit message follows conventional commit format for semantic release

**Technical Notes:** Squash merge is the required merge strategy for all merge requests. Individual commit messages on feature branches do not need to follow conventional commit format (only the final squash commit does). This allows developers to make frequent, informal commits during development.

**Usage in Code:**
- **Backend:** GitLab merge request settings
- **Frontend:** Same strategy applies
- **Database:** N/A

---

## 5. Acronyms

| Acronym | Full Name | Description |
|---------|-----------|-------------|
| **DDD** | Domain-Driven Design | Software design approach that models code around the business domain, using concepts like entities, value objects, aggregate roots, and domain events |
| **DI** | Dependency Injection | Design pattern where dependencies are provided to a class rather than created internally; managed by the ASP.NET Core DI container and Scrutor |
| **DTO** | Data Transfer Object | **PROHIBITED naming convention** -- use nested `{Op}Request`/`{Op}Response` records inside endpoint classes instead. The `Dto` suffix is prohibited everywhere in the codebase (TD-013) |
| **EF Core** | Entity Framework Core | The ORM (Object-Relational Mapper) used to interact with PostgreSQL; configured with snake_case naming, NodaTime, and a single `AppDbContext` (public schema) |
| **JWT** | JSON Web Token | Token format used for authentication; access tokens are short-lived (in-memory), refresh tokens are long-lived (HttpOnly cookies) |
| **OTLP** | OpenTelemetry Protocol | The protocol used to export traces, metrics, and logs to the Aspire Dashboard (port 18889) for observability |
| **PWA** | Progressive Web App | The web application (Kakeibo.App) is PWA-capable, installable on devices from the browser |
| **REPR** | Request-Endpoint-Response | The Minimal API pattern used in Kakeibo: each endpoint has a Request record, an Endpoint class, and a Response record |
| **SPA** | Single Page Application | The Vue.js frontend (Kakeibo.App) is a SPA that loads once and handles routing client-side via Vue Router |
| **TDD** | Test-Driven Design | Development approach where tests are written before implementation; used for domain logic and handlers |
| **TTL** | Time To Live | The duration a cached value or token remains valid before expiration (e.g., cache entries, invitation tokens, JWT access tokens) |
| **UoW** | Unit of Work | Pattern for tracking changes and committing them atomically; in Kakeibo, the `DbContext` IS the UoW -- no separate repository pattern is used |

---

## 6. Prohibited Terms

Terms that are explicitly banned from the codebase and documentation, along with their required replacements.

| Prohibited Term | Required Replacement | Reason |
|-----------------|---------------------|--------|
| **BCrypt** | `PasswordHasher` (PBKDF2-SHA512) | BCrypt is a prohibited technology. Use PBKDF2-SHA512 with 350,000 iterations |
| **Argon2id** | `PasswordHasher` (PBKDF2-SHA512) | Argon2id is a prohibited technology. Use PBKDF2-SHA512 with 350,000 iterations |
| **DateTime** | `Instant` (NodaTime) | BCL DateTime is ambiguous and error-prone. Use NodaTime `Instant` for timestamps |
| **DateTimeOffset** | `Instant` (NodaTime) | Same reasoning as DateTime. NodaTime `Instant` is unambiguous UTC |
| **DateOnly** | `LocalDate` (NodaTime) | BCL DateOnly is prohibited. Use NodaTime `LocalDate` |
| **Dto / DTO suffix** | Nested `{Op}Request` / `{Op}Response` | DTOs are prohibited for endpoint types (TD-013). Use nested records inside the endpoint class. The `Dto` suffix is only allowed in `Kakeibo.Contracts` for inter-module shared types |
| **Guid.CreateVersion7()** | `Guid7.NewGuid()` | The .NET built-in method has broken little-endian byte order that breaks PostgreSQL B-tree sorting (TD-005) |
| **MediatR** | Plain handler classes | MediatR is a prohibited technology. Use plain classes with `HandleAsync` methods, auto-registered by Scrutor |
| **Repository Pattern** | `AppDbContext` directly | No repository abstraction layer. Handlers inject `AppDbContext` and query `DbSet<T>` directly |
| **Settings / Config suffix** | `Options` suffix | Configuration binding classes must use `{Name}Options` naming (TD-009). `*Settings` and `*Config` suffixes are prohibited |
| **npx** | `bunx` (or `bunx --bun`) | The project uses Bun as the package manager. `npx` may resolve the wrong registry (mandatory.md Rule 9) |


---

## 7. Japanese Terms

Terms from the traditional Japanese budgeting method that inspired this platform.

### Family-Account-Book (Kakeibo)

**Romaji:** Kakeibo

**Definition:** Literally "household financial ledger." A Japanese budgeting method created in 1904 by Hani Motoko, Japan's first female journalist. The method emphasizes recording every transaction by hand as an act of conscious spending and reflection. The platform takes its name from this method.

---

### Ishikiteki Shishutsu (Conscious Spending)

**Definition:** The practice of being deliberately aware of every financial decision. In the traditional Kakeibo method, this awareness is cultivated through the physical act of writing down each expense. In the digital platform, it is achieved through the intentional recording and categorization of every transaction.

---

### Hansei (Reflection)

**Definition:** Self-examination and reflection. In the Kakeibo context, Hansei is the practice of reviewing financial activity at the end of each day, week, or month to understand spending patterns, identify areas for improvement, and set intentions for the future. The platform supports Hansei through category breakdowns, budget comparisons, and spending trend visualizations.

---

### Chochiku (Savings)

**Definition:** The act and practice of saving money. In the Kakeibo philosophy, savings are not a byproduct of spending less but an intentional practice cultivated through awareness. The platform's Goals module directly supports Chochiku by providing savings targets, progress tracking, and milestone celebrations.

---

*This glossary is the authoritative reference for all terminology in the Kakeibo platform. When a term is used inconsistently across documentation, this document is the canonical definition. When adding new concepts to the platform, add their definitions here first.*
