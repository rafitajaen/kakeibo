# Kakeibo - Business Rules

Exhaustive reference for every business invariant, edge case, validation rule, state transition, domain event, and calculation formula in the Kakeibo financial management platform.

**Version:** 1.0
**Last Updated:** 2026-02-21
**Related Documents:**
- [overview.md](./overview.md) -- Core philosophy and user flows
- [constraints.md](./constraints.md) -- Numeric limits, rate limits, pagination
- [architecture.md](./architecture.md) -- Module structure, inter-module communication
- [platform.md](./platform.md) -- Module catalog, integration events

---

## Table of Contents

1. [Critical Invariants (NEVER Violate)](#1-critical-invariants-never-violate)
   - [1.1 Wallet Invariants](#11-wallet-invariants)
   - [1.2 Transaction Invariants](#12-transaction-invariants)
   - [1.3 Debt Invariants](#13-debt-invariants)
   - [1.4 Budget Invariants](#14-budget-invariants)
   - [1.5 Goal Invariants](#15-goal-invariants)
   - [1.6 Category Invariants](#16-category-invariants)
   - [1.7 Recurring Pattern Invariants](#17-recurring-pattern-invariants)
2. [Edge Cases and How to Handle](#2-edge-cases-and-how-to-handle)
3. [Validation Rules Summary](#3-validation-rules-summary)
4. [State Transition Rules](#4-state-transition-rules)
5. [Domain Event Triggers](#5-domain-event-triggers)
6. [Calculation Formulas](#6-calculation-formulas)

---

## 1. Critical Invariants (NEVER Violate)

These rules are absolute. Any code path that violates them is a bug. They must be enforced at the database level (constraints, triggers), the application level (handlers, validators), or both.

---

### 1.1 Wallet Invariants

#### INV-W01: Balance Accuracy

The wallet balance is a derived value, never stored independently. It must always satisfy:

```
wallet.currentBalance =
    wallet.initialBalance
  + SUM(income transactions in this wallet)
  - SUM(expense transactions in this wallet)
```

For shared wallets the same formula applies. Splits are tracked separately and do not affect the wallet balance.

**Enforcement:**
- **Database:** `current_balance` is a computed field, not a stored column. Calculated at query time via `initial_balance + SUM(transactions)`.
- **Application:** Every `SaveChangesAsync` call that creates, edits, or deletes a transaction must produce a consistent balance when re-queried.
- **Audit:** Balance recalculation can be triggered on-demand as a consistency check.

**Why:** A wallet balance that disagrees with its transaction history is the most dangerous bug in a financial application. Users lose trust immediately.

---

#### INV-W02: Ownership Immutability

| Wallet Type | Owner Count | Rule |
|-------------|-------------|------|
| Personal | Exactly 1 | `wallet.user_id` is set, immutable after creation |
| Shared | 1+ members | `shared_wallet_members` join table, creator auto-added |

- A personal wallet belongs to exactly one user. The `user_id` foreign key is `NOT NULL` and never changes.
- A shared wallet has one or more members via the `shared_wallet_members` table. The creator is automatically inserted as the first member.

**Enforcement:**
- **Database:** `wallets.user_id NOT NULL`, `shared_wallet_members(shared_wallet_id, user_id) UNIQUE`.
- **Application:** No endpoint or handler exposes a "transfer ownership" operation for personal wallets.

---

#### INV-W03: Type Immutability

A wallet's type (personal vs. shared) **cannot change** after creation.

- A personal wallet cannot become a shared wallet.
- A shared wallet cannot become a personal wallet.

**Enforcement:**
- **Database:** Personal wallets live in the `wallets` table. Shared wallets live in the `shared_wallets` table. There is no `type` column to flip.
- **Application:** No endpoint exposes a "convert wallet type" operation.

**Why:** Converting a personal wallet to shared would require retroactive split generation for every historical transaction. Converting shared to personal would orphan splits and debts. Both are destructive operations with no clean semantics.

---

#### INV-W04: Archival Rules

| Rule | Behavior |
|------|----------|
| Active wallet | Accepts new transactions |
| Archived wallet | No new transactions allowed; existing transactions remain visible and queryable |
| Deletion guard | Wallet with 1+ transactions cannot be deleted, only archived |
| Empty deletion | Wallet with 0 transactions can be permanently deleted |

**Enforcement:**
- **Application:** `CreateTransactionHandler` must reject transactions targeting archived wallets with `Error.Validation("Wallet.Archived")`.
- **Application:** `DeleteWalletHandler` must check `db.Transactions.AnyAsync(t => t.WalletId == walletId)` and return `Error.Conflict("Wallet.HasTransactions")` if true.

---

#### INV-W05: Transaction-Wallet Exclusivity

Every transaction belongs to **exactly one** wallet: either a personal wallet OR a shared wallet. Never both, never neither.

```sql
CONSTRAINT chk_transactions_wallet_xor CHECK (
  (wallet_id IS NOT NULL AND shared_wallet_id IS NULL) OR
  (wallet_id IS NULL AND shared_wallet_id IS NOT NULL)
)
```

**Enforcement:**
- **Database:** XOR check constraint on `transactions` table.
- **Application:** Validator rejects requests that specify both or neither.

---

#### INV-W06: Shared Wallet Minimum Members

A shared wallet must have at least 2 members. If a member removal would bring the count below 2, the operation is rejected.

**Enforcement:**
- **Database:** Trigger `prevent_last_shared_wallet_member_removal()` checks count before DELETE on `shared_wallet_members`.
- **Application:** `RemoveMemberHandler` pre-checks member count before attempting removal.

---

#### INV-W07: Default Wallet Uniqueness

Each user has exactly one default personal wallet.

- The first wallet created is automatically set as default.
- Setting a new wallet as default must atomically clear the previous default.

**Enforcement:**
- **Database:** Partial unique index `UNIQUE (user_id) WHERE is_default = TRUE`.
- **Application:** `SetDefaultWalletHandler` wraps the operation in a transaction: clear old default, set new default.

---

### 1.2 Transaction Invariants

#### INV-T01: Amount Validation

| Constraint | Value |
|------------|-------|
| Minimum | 0.01 |
| Maximum | 999,999,999.99 |
| Precision | 2 decimal places |
| Sign | Always positive (type inferred from category) |

**Enforcement:**
- **Database:** `CHECK (amount > 0)`, `DECIMAL(10, 2)`.
- **Application:** FluentValidation rule: `RuleFor(x => x.Amount).GreaterThanOrEqualTo(0.01m).LessThanOrEqualTo(999_999_999.99m)`.
- **Frontend (TypeScript):**

```typescript
const transactionAmountSchema = z.number()
  .min(0.01, 'Amount must be at least 0.01')
  .max(999_999_999.99, 'Amount exceeds maximum')
  .multipleOf(0.01, 'Amount must have at most 2 decimal places');
```

---

#### INV-T02: Date Validation

| Transaction State | Date Rule |
|-------------------|-----------|
| Consolidated (normal) | Cannot be in the future |
| Forecasted | Must be in the future; generated from recurring pattern |
| Any transaction | Cannot be more than 1 year in the future |

**Enforcement:**
- **Application:** Validator checks `request.Date <= today` for consolidated transactions, `request.Date <= today.PlusDays(365)` for any transaction.

---

#### INV-T03: Transfer Atomicity

A transfer moves money between two wallets. Both balance changes must succeed or both must fail. There is no state where one wallet is debited but the other is not credited.

In the current MVP, transfers are modeled as two transactions (one expense, one income) within the same database transaction. If the handler fails after creating the first transaction but before the second, the database transaction is rolled back.

**Enforcement:**
- **Application:** `CreateTransferHandler` wraps both INSERT operations within a single `IUnitOfWork` / `DbContext.SaveChangesAsync()` call.
- **Database:** Both rows are inserted within the same PostgreSQL transaction.

---

#### INV-T04: Single Category

Every transaction must have **exactly one** category. Subcategory is optional.

```
transaction.category_id IS NOT NULL
transaction.subcategory_id IS NULL OR belongs to transaction.category_id
```

**Enforcement:**
- **Database:** `category_id VARCHAR(25) NOT NULL`, `FOREIGN KEY (subcategory_id) REFERENCES subcategories(id)`.
- **Application:** Validator checks subcategory belongs to the specified parent category if provided.

---

#### INV-T05: Split Validation for Shared Wallet Expenses

When a transaction is recorded in a shared wallet:

1. Splits MUST be created for all participating members.
2. `SUM(split.amount)` MUST equal `transaction.amount`.
3. If `split_type = percentage`, `SUM(split.percentage)` MUST equal 100%.
4. If `split_type = custom_amount`, `SUM(split.amount)` MUST equal `transaction.amount`.
5. The payer's split is automatically marked `status = settled`, `is_payer = true`.
6. Non-payer splits are created with `status = pending`, `owed_to_user_id = payer_user_id`.

**Enforcement:**
- **Application:** `CreateTransactionHandler` (shared wallet path) validates split sums before persisting.
- **Application:** Rounding correction is applied to the last member to ensure exact match (see [Section 6.3](#63-split-calculations)).

---

#### INV-T06: Splits Only in Shared Wallets

Splits exist only for shared wallet transactions. A personal wallet transaction must never have associated splits.

**Enforcement:**
- **Application:** Validator rejects split configuration when `wallet_id` (personal) is set.
- **Database:** Application-level enforcement; no direct FK between `transaction_splits` and `wallets`.

---

#### INV-T07: Forecasted Transaction Immutability

Forecasted transactions (generated from recurring patterns) are **read-only**. Users cannot directly edit them. Available actions:

| Action | Result |
|--------|--------|
| Confirm Now | Convert to consolidated, date = today |
| Confirm for Date | Convert to consolidated, keep scheduled date |
| Skip | Delete the occurrence; pattern continues |
| Edit | Break link to pattern; transaction becomes independent consolidated |

**Enforcement:**
- **Application:** `UpdateTransactionHandler` rejects if `transaction.is_forecast == true` with `Error.Validation("Transaction.ForecastedReadOnly")`.

---

#### INV-T08: Creator Authorization

A user can only create transactions in:
- Their own personal wallets (`wallet.user_id == currentUser.id`).
- Shared wallets they are a member of (`shared_wallet_members` contains their `user_id`).

**Enforcement:**
- **Application:** `CreateTransactionHandler` queries membership before persisting.

---

### 1.3 Debt Invariants

#### INV-D01: Calculation Formula

Debt is calculated from transaction splits, never set manually.

```
debt(UserA owes UserB in SharedWalletX) =
    SUM(splits WHERE user_id = UserA AND owed_to_user_id = UserB AND status = 'pending')
```

The aggregated debt view groups pending splits by `(user_id, owed_to_user_id)` pairs.

**Enforcement:**
- **Database:** No `debts` table exists. Debt is a derived view over `transaction_splits`.
- **Application:** Debt queries aggregate pending splits at read time.

---

#### INV-D02: Debt Is Never Manually Set

No API endpoint exists to create, update, or delete a debt record. Debts are a projection of pending splits. When a split's status changes from `pending` to `settled`, the debt automatically decreases.

---

#### INV-D03: Debt Simplification

The system presents simplified debts. For a given shared wallet:

```
Algorithm (net-balance simplification):
1. For each pair (UserA, UserB), calculate:
   netDebt(A, B) = totalOwed(A -> B) - totalOwed(B -> A)
2. If netDebt(A, B) > 0: A owes B that amount
3. If netDebt(A, B) < 0: B owes A |netDebt| amount
4. If netDebt(A, B) == 0: No debt between A and B

Graph reduction for 3+ users:
1. Build debt graph (nodes = users, edges = net debts)
2. Calculate net balance for each user:
   net(User) = SUM(owed TO user) - SUM(owed BY user)
3. Users with positive net are creditors; negative are debtors
4. Match debtors to creditors, largest amounts first
5. Result: minimum number of transfers to settle all debts

Example:
  Before: A owes B $100, B owes C $100
  After simplification: A owes C $100 (B removed as intermediary)
```

**Enforcement:**
- **Application:** `DebtCalculationService` implements the net-balance algorithm and returns simplified results.

---

#### INV-D04: Settlement Constraints

| Constraint | Rule |
|------------|------|
| Amount | Settlement amount must be <= current pending split amount |
| No balance impact | Settlement does NOT create wallet transactions |
| No debt without split | Settlement rejected if no pending splits exist for the pair |
| One-way | Once settled, a split cannot return to pending |
| External | Settlement is acknowledgment of payment made outside the app |

**Enforcement:**
- **Application:** `SettleSplitHandler` validates pending status and updates only the split record.

---

### 1.4 Budget Invariants

#### INV-B01: Period Validation

| Constraint | Rule |
|------------|------|
| Period type | Monthly (`YYYY-MM`) or yearly (`YYYY`) |
| Past budgets | Immutable -- cannot be edited or deleted |
| Current/future budgets | Can be modified |

**Enforcement:**
- **Application:** `UpdateBudgetHandler` checks if the budget period is in the past and rejects with `Error.Validation("Budget.PastPeriodImmutable")`.

---

#### INV-B02: Spending Calculation

```
budget.spent = SUM(
  transactions.amount
  WHERE category_id = budget.category_id
    AND date WITHIN budget.period
    AND scope matches (user_id for personal, shared_wallet_id for shared)
    AND transaction belongs to expense category
    AND is_forecast = false
)
```

Only **expense** transactions count toward budget spending. Income and transfers are excluded. Forecasted transactions are excluded.

**Enforcement:**
- **Application:** Budget status endpoint computes `spent` at query time from transaction data.

---

#### INV-B03: Non-Overlapping Budgets

For the same scope (user or shared wallet) and category, only one budget can exist per period.

```sql
UNIQUE (user_id, category_id, period_type, period_year, period_month)
UNIQUE (shared_wallet_id, category_id, period_type, period_year, period_month)
```

**Enforcement:**
- **Database:** Unique constraints on the `budgets` table.
- **Application:** `CreateBudgetHandler` checks for existing budget before INSERT.

---

#### INV-B04: Expense Categories Only

Only expense-type categories can have budgets. Income categories cannot have spending limits.

**Enforcement:**
- **Application:** `CreateBudgetHandler` queries `category.type` and rejects if `type != "expense"`.

---

#### INV-B05: Scope Exclusivity

A budget belongs to **either** a user (personal) **or** a shared wallet. Never both, never neither.

```sql
CONSTRAINT chk_budgets_scope_xor CHECK (
  (user_id IS NOT NULL AND shared_wallet_id IS NULL) OR
  (user_id IS NULL AND shared_wallet_id IS NOT NULL)
)
```

**Enforcement:**
- **Database:** XOR check constraint.
- **Application:** Validator ensures exactly one scope is provided.

---

### 1.5 Goal Invariants

#### INV-G01: Target Amount

Target amount must be a positive number greater than zero.

```sql
CHECK (target_amount > 0)
```

---

#### INV-G02: Deadline Validation

| Constraint | Value |
|------------|-------|
| Required | No (optional) |
| Maximum | 10 years in the future |
| Minimum | Today or later |

**Enforcement:**
- **Application:** `CreateGoalValidator` checks `targetDate <= today.PlusYears(10)` if provided.

---

#### INV-G03: Progress Calculation Modes

| Mode | Condition | Behavior |
|------|-----------|----------|
| **Wallet-linked** | `linked_wallet_id IS NOT NULL` | `current_amount = wallet.currentBalance`. Auto-updates when wallet balance changes. |
| **Manual** | `linked_wallet_id IS NULL` | User manually updates `current_amount` via API. |

If a linked wallet is deleted (FK `ON DELETE SET NULL`), the goal automatically degrades to manual mode, retaining the last known `current_amount`.

---

#### INV-G04: Completion Definition

```
goal.isAchieved = (goal.currentAmount >= goal.targetAmount)
```

Achievement is automatic and recalculated on every read or after every wallet balance change. The user does not need to manually mark a goal as achieved.

**Edge case:** If the user increases the target amount above the current amount, `isAchieved` reverts to `false`. If the user withdraws funds from a linked wallet, `isAchieved` reverts to `false`.

---

### 1.6 Category Invariants

#### INV-C01: System Categories Are Immutable

The 12 system categories (8 expense + 4 income) cannot be:
- Deleted
- Renamed
- Archived
- Have their type changed

They are read-only for all users, forever.

**Enforcement:**
- **Application:** All category mutation endpoints check `category.is_system` and reject with `Error.Validation("Category.SystemImmutable")`.

---

#### INV-C02: Referenced Categories Cannot Be Deleted

A custom category that has 1+ associated transactions cannot be deleted. It can only be archived.

| Transactions | Delete Allowed | Archive Allowed |
|--------------|---------------|-----------------|
| 0 | Yes (permanent) | Yes |
| 1+ | No | Yes |

**Enforcement:**
- **Application:** `DeleteCategoryHandler` checks `db.Transactions.AnyAsync(t => t.CategoryId == id)`.

---

#### INV-C03: One Category Per Transaction

Every transaction must reference exactly one category (`category_id NOT NULL`). Optionally, it can reference one subcategory that belongs to the same parent category.

---

#### INV-C04: Unique Names Per User Per Type

Custom category names must be unique within a user's categories of the same type (income or expense).

```sql
UNIQUE (user_id, name, type) WHERE is_system = FALSE
```

System category names must be globally unique per type:

```sql
UNIQUE (name, type) WHERE is_system = TRUE
```

---

#### INV-C05: Type Immutability

A category's type (`income` or `expense`) is set at creation and can never change. Changing it would retroactively reclassify all historical transactions and break balance calculations.

---

### 1.7 Recurring Pattern Invariants

#### INV-R01: Pattern Duration

Maximum pattern duration is 10 years. If `recurrence_end_date` is set, it must be:
- After `start_date`
- Within 10 years of `start_date`

---

#### INV-R02: Frequency Validation

| Frequency | Required Fields | Validation |
|-----------|----------------|------------|
| `daily` | None extra | -- |
| `weekly` | `day_of_week` | 1 (Monday) to 7 (Sunday) |
| `biweekly` | `day_of_week` | 1 to 7 |
| `monthly` | `day_of_month` | 1 to 31 |
| `yearly` | `day_of_month`, `month_of_year` | day 1-31, month 1-12 |

---

#### INV-R03: Generation Timing

- Background job (Hangfire) runs daily.
- Generates forecasted transactions up to **90 days** ahead.
- Tracks `last_generated_date` to prevent duplicate generation.
- Respects `is_active` flag: paused patterns do not generate new forecasts.
- Stops at `recurrence_end_date` if set.

---

#### INV-R04: Pattern Editing Affects Future Only

Editing a recurring pattern's template (amount, category, description) affects only future occurrences. Past consolidated transactions are preserved unchanged.

| Scenario | Past Consolidated | Existing Forecasted | Future Generated |
|----------|-------------------|--------------------|--------------------|
| Edit pattern amount | Unchanged | Regenerated with new amount | Use new amount |
| Pause pattern | Unchanged | Remain visible | Not generated |
| Delete pattern | Unchanged | Removed | Not generated |

---

## 2. Edge Cases and How to Handle

### 2.1 Wallet Operations

| Edge Case | Resolution |
|-----------|------------|
| Creating wallet with same name as archived wallet | **Allowed.** Name uniqueness is not enforced. Users may reuse names. |
| Creating wallet with same name as active wallet | **Allowed.** No name uniqueness constraint. Users differentiate by icon/color. |
| Archiving wallet with pending recurring patterns | **Allowed.** Pattern's wallet FK uses `ON DELETE SET NULL`. Pattern becomes orphaned and stops generating. Activity log records the archival. |
| Deleting last member from shared wallet (below 2) | **Rejected.** Database trigger `prevent_last_shared_wallet_member_removal()` raises exception. Application returns `Error.Validation("SharedWallet.MinimumMembers")`. |
| Member leaves shared wallet with pending debts | **Allowed.** Debt history (pending splits) persists. The departed member's splits remain in `pending` status and are still visible to remaining members. The departed member can no longer access the wallet but their historical data remains. |
| Recording transaction in archived wallet | **Rejected.** `CreateTransactionHandler` checks `wallet.is_archived` and returns `Error.Validation("Wallet.Archived")`. |
| Balance going negative | **Allowed.** No constraint prevents negative balances. Users may track credit card accounts or overdraft scenarios. The `initial_balance` field accepts negative values. |
| Two users create shared wallet simultaneously and invite each other | **Both succeed.** Each creates their own shared wallet. Invitations are per-wallet. No conflict. |

---

### 2.2 Transaction Operations

| Edge Case | Resolution |
|-----------|------------|
| Transaction with future date (consolidated) | **Rejected.** Consolidated transactions must have `date <= today`. Only forecasted transactions can have future dates (within 1 year). |
| Transaction with date exactly today | **Allowed.** Today is not "future". |
| Editing transaction that created debt | **Allowed.** Editing the transaction amount triggers recalculation of all associated splits. If the transaction is in a shared wallet, splits must be re-validated to ensure `SUM(splits) == new_amount`. The handler must update or regenerate splits. |
| Deleting transaction in shared wallet | **Allowed.** All associated splits are cascade-deleted (`ON DELETE CASCADE`). Debt calculations automatically adjust because the pending splits no longer exist. Activity log records the deletion. |
| Transfer to same wallet | **Rejected.** Validator checks `sourceWalletId != destinationWalletId` and returns `Error.Validation("Transfer.SameWallet")`. |
| Transaction with split in personal wallet | **Rejected.** Splits only exist for shared wallet transactions (INV-T06). |
| Transaction with archived category | **Rejected for new transactions.** `CreateTransactionHandler` checks `category.is_archived`. Historical transactions retain their archived category reference for display. |
| Transaction amount with 3+ decimal places | **Rejected.** Validator enforces `multipleOf(0.01)`. Database enforces `DECIMAL(10, 2)`. |
| Transaction with amount 0 | **Rejected.** `CHECK (amount > 0)` at database level. Validator enforces `amount >= 0.01`. |
| Bulk transaction creation (import) | **MVP excluded.** Import/export deferred to post-MVP. |

---

### 2.3 Debt and Settlement Operations

| Edge Case | Resolution |
|-----------|------------|
| Settling more than owed | **Rejected.** Settlement operates per-split, not per-aggregate-debt. Each split has a fixed `amount`. Marking a split as settled means the exact `split.amount` is acknowledged as paid. There is no "partial settlement" or "overpayment" in MVP. |
| Settling when no debt exists | **Rejected.** If `split.status == 'settled'`, the handler returns `Error.Validation("Split.AlreadySettled")`. If no pending splits exist for the pair, there is nothing to settle. |
| Multiple debts simplified (A owes B owes C) | **Display-level simplification.** The `DebtCalculationService` computes net balances across all pending splits in a shared wallet and presents the minimum set of transfers needed. Underlying split records remain unchanged. |
| Negative debt (reversal) | **Handled by net calculation.** If B owes A $50 and A owes B $30, the net debt shown is: A owes B $0, B owes A $20. The sign flip is handled in the aggregation layer. |
| Settlement for specific split vs. aggregate | **Per-split.** Each `transaction_split` record is settled individually. Users pick a specific pending split and mark it as settled. |
| User settles via external app (Bizum, Venmo, cash) | **Expected flow.** Settlement notes field records the method: `"Paid via Bizum"`, `"Cash"`, `"Bank transfer"`. No wallet balance changes occur. |

---

### 2.4 Budget Operations

| Edge Case | Resolution |
|-----------|------------|
| Budget exceeded mid-period | **Warning only.** The system sends a notification (`budget_alert`) when spending reaches the alert threshold (default 80%) and again at 100%. Budgets never block transactions. |
| Overlapping budgets for same category + wallet | **Rejected at creation.** Unique constraint on `(scope, category_id, period_type, period_year, period_month)` prevents duplicates. |
| Budget with no transactions in period | **Valid state.** `spent = 0`, `percentUsed = 0%`, `status = "On Track"`. |
| Retroactive budget creation | **Allowed.** Creating a budget for the current month calculates spending from the month's start. All existing transactions in the period are immediately reflected. |
| Budget for category with no historical transactions | **Valid state.** Budget exists with `spent = 0`. Future transactions in that category will be tracked. |
| Deleting budget for current month | **Allowed.** Removes spending limit. No alert notifications will fire for that category. |
| Editing budget for past month | **Rejected.** Past budgets are immutable per INV-B01. |
| Budget for income category | **Rejected.** Only expense categories can have budgets per INV-B04. |

---

### 2.5 Goal Operations

| Edge Case | Resolution |
|-----------|------------|
| Goal achieved (100%) | `is_achieved = true`. Milestone notification sent at 100%. Goal remains active -- user can continue tracking or archive manually. |
| Goal deadline passed without completion | **Warning only.** Goal shows as "Overdue" in the UI. No automatic archival. User decides whether to extend deadline, remove deadline, or archive. |
| Linked wallet deleted | Goal transitions to manual mode (`linked_wallet_id = NULL` via `ON DELETE SET NULL`). `current_amount` retains the last calculated value. User can manually update going forward. |
| Manual progress exceeds target | **Allowed.** `current_amount` can exceed `target_amount`. Progress displays as 100%+ (e.g., 120%). `is_achieved = true`. |
| Linked wallet balance goes negative | **Allowed.** `current_amount` reflects the wallet balance, which can be negative. `is_achieved = false` since negative < target. Progress shows 0%. |
| User unlinks wallet from goal | Goal transitions to manual mode. `current_amount` freezes at the wallet's balance at the time of unlinking. |
| Multiple goals linked to same wallet | **Allowed.** Each goal independently tracks the same wallet balance. Different targets and deadlines per goal. |

---

### 2.6 Recurring Operations

| Edge Case | Resolution |
|-----------|------------|
| Pattern due date falls on weekend | **Generate on the actual day.** No business-day adjustment. The recurring pattern generates the transaction on the exact calculated date regardless of weekday. |
| Monthly pattern for day 31 in a 28/30-day month | **Use last day of month.** If `day_of_month = 31` and the month has 30 days, generate on the 30th. If February (28/29 days), generate on the 28th or 29th. |
| Yearly pattern for Feb 29 in non-leap year | **Use Feb 28.** The pattern generates on February 28 in non-leap years. |
| Pattern amount varies (user edits after generation) | **Editing a generated forecasted transaction** breaks the link to the pattern (`recurring_transaction_id = NULL`). The transaction becomes independent. Future pattern occurrences continue with the pattern's original amount. |
| Pattern occurrence skipped (user deletes generated transaction) | **Pattern continues.** Deleting a single forecasted transaction does not affect the pattern. The next occurrence generates normally. |
| Pattern end date in past | **Stops generation.** No retroactive transaction creation. If the end date is today or earlier, no new forecasts are generated. Existing forecasted transactions for dates before the end date remain. |
| Pausing pattern | **Existing forecasted transactions remain visible.** No new ones are generated. Resuming the pattern resumes generation from the current date forward. |
| Pattern for shared wallet after member leaves | **Pattern continues for remaining members.** If the pattern creator leaves the shared wallet, the pattern becomes orphaned (depends on FK behavior). In MVP, patterns are user-owned and tied to the wallet. If the wallet FK becomes NULL, the pattern stops generating. |

---

### 2.7 Collaboration Operations

| Edge Case | Resolution |
|-----------|------------|
| Invitation expiry | Invitations expire after **7 days**. Background job marks expired invitations (`status = 'expired'`). |
| Accepting expired invitation | **Rejected.** Handler checks `invitation.expires_at < now` and returns `Error.Validation("Invitation.Expired")`. |
| Accepting invitation with used/invalid token | **Rejected.** Handler queries by token, checks status is `pending`, and rejects otherwise. |
| Invitation to already-member | **Rejected.** Handler checks `shared_wallet_members` for existing membership and returns `Error.Conflict("Invitation.AlreadyMember")`. |
| Self-invitation | **Rejected.** Handler checks `invitee_email != currentUser.email` and returns `Error.Validation("Invitation.SelfInvite")`. |
| Invitation to unregistered user | Email sent with invitation link. Link redirects to registration page. After registration, invitation is automatically processed and user is added to the shared wallet. |
| Multiple pending invitations for same email + wallet | **Rejected.** Only one pending invitation per email per shared wallet. Resending creates a new invitation (new token) and invalidates the old one. |
| Member leaves shared wallet | **Allowed at any time.** Member is removed from `shared_wallet_members`. Their historical transactions and splits remain. Pending debts persist in the split records. The departed member can no longer access the wallet. |
| Last two members: one leaves | **Only allowed if it does not breach minimum of 2.** If there are exactly 2 members and one tries to leave, the trigger rejects it. The wallet must be archived instead. |

---

## 3. Validation Rules Summary

### 3.1 Transaction Fields

| Field | Entity | Type | Required | Min | Max | Precision | Format / Regex | Example |
|-------|--------|------|----------|-----|-----|-----------|---------------|---------|
| `amount` | Transaction | DECIMAL(10,2) | Yes | 0.01 | 999,999,999.99 | 2 decimals | Positive number | `45.99` |
| `date` | Transaction | DATE | Yes | -- | today + 365 days | Day | `YYYY-MM-DD` | `2026-03-15` |
| `concept` | Transaction | VARCHAR(200) | Yes | 1 char | 200 chars | -- | No leading/trailing whitespace | `"Morning coffee"` |
| `notes` | Transaction | TEXT | No | -- | 2000 chars | -- | Free text | `"Paid with credit card"` |
| `category_id` | Transaction | VARCHAR(25) | Yes | -- | -- | -- | Valid CUID reference | `"sys-cat-food"` |
| `subcategory_id` | Transaction | VARCHAR(25) | No | -- | -- | -- | Valid CUID, must belong to parent category | `"sub-groceries"` |
| `wallet_id` | Transaction | VARCHAR(25) | XOR | -- | -- | -- | Valid CUID (personal wallet) | `"wal_abc123"` |
| `shared_wallet_id` | Transaction | VARCHAR(25) | XOR | -- | -- | -- | Valid CUID (shared wallet) | `"sw_def456"` |

### 3.2 Wallet Fields

| Field | Entity | Type | Required | Min | Max | Format | Example |
|-------|--------|------|----------|-----|-----|--------|---------|
| `name` | Wallet / Shared Wallet | VARCHAR(100) | Yes | 1 char | 100 chars | Trimmed, no empty | `"Checking Account"` |
| `description` | Wallet / Shared Wallet | TEXT | No | -- | 500 chars | Free text | `"Main bank account"` |
| `initial_balance` | Wallet | DECIMAL(10,2) | Yes | -999,999,999.99 | 999,999,999.99 | 2 decimals, can be negative | `1500.00` |
| `currency` | Wallet | VARCHAR(3) | Yes | 3 chars | 3 chars | ISO 4217 | `"EUR"`, `"USD"` |
| `icon` | Wallet | VARCHAR(50) | No | -- | 50 chars | Icon identifier | `"wallet"` |
| `color` | Wallet | VARCHAR(7) | No | 7 chars | 7 chars | `#RRGGBB` hex | `"#3B82F6"` |

### 3.3 Category Fields

| Field | Entity | Type | Required | Min | Max | Format | Example |
|-------|--------|------|----------|-----|-----|--------|---------|
| `name` | Category | VARCHAR(50) | Yes | 1 char | 50 chars | Unique per user per type, trimmed | `"Pet Care"` |
| `description` | Category | TEXT | No | -- | 500 chars | Free text | `"Vet visits, food, supplies"` |
| `type` | Category | VARCHAR(10) | Yes | -- | -- | `"income"` or `"expense"` | `"expense"` |
| `icon` | Category | VARCHAR(50) | Yes | 1 char | 50 chars | Icon identifier | `"paw-print"` |
| `color` | Category | VARCHAR(7) | No | 7 chars | 7 chars | `#RRGGBB` hex | `"#F59E0B"` |

### 3.4 Budget Fields

| Field | Entity | Type | Required | Min | Max | Format | Example |
|-------|--------|------|----------|-----|-----|--------|---------|
| `amount` | Budget | DECIMAL(10,2) | Yes | 0.01 | 999,999,999.99 | Positive, 2 decimals | `400.00` |
| `period_type` | Budget | VARCHAR(10) | Yes | -- | -- | `"monthly"` or `"yearly"` | `"monthly"` |
| `period_year` | Budget | INTEGER | Yes | 2020 | 2099 | 4-digit year | `2026` |
| `period_month` | Budget | INTEGER | Conditional | 1 | 12 | Required if monthly, NULL if yearly | `3` |
| `alert_threshold` | Budget | DECIMAL(5,2) | No | 1.00 | 100.00 | Percentage, default 80 | `80.00` |
| `category_id` | Budget | VARCHAR(25) | Yes | -- | -- | Must be expense category | `"sys-cat-food"` |

### 3.5 Goal Fields

| Field | Entity | Type | Required | Min | Max | Format | Example |
|-------|--------|------|----------|-----|-----|--------|---------|
| `name` | Savings Goal | VARCHAR(100) | Yes | 1 char | 100 chars | Trimmed | `"Europe Vacation"` |
| `target_amount` | Savings Goal | DECIMAL(10,2) | Yes | 0.01 | 999,999,999.99 | Positive, 2 decimals | `5000.00` |
| `current_amount` | Savings Goal | DECIMAL(10,2) | Yes | 0.00 | 999,999,999.99 | Non-negative, 2 decimals | `1200.00` |
| `target_date` | Savings Goal | DATE | No | today | today + 10 years | `YYYY-MM-DD` | `2026-12-31` |
| `linked_wallet_id` | Savings Goal | VARCHAR(25) | No | -- | -- | Must belong to goal owner | `"wal_abc123"` |

### 3.6 Recurring Pattern Fields

| Field | Entity | Type | Required | Min | Max | Format | Example |
|-------|--------|------|----------|-----|-----|--------|---------|
| `concept` | Recurring | VARCHAR(200) | Yes | 1 char | 200 chars | Trimmed | `"Monthly rent"` |
| `amount` | Recurring | DECIMAL(10,2) | Yes | 0.01 | 999,999,999.99 | Positive, 2 decimals | `1200.00` |
| `frequency` | Recurring | VARCHAR(20) | Yes | -- | -- | Enum (see INV-R02) | `"monthly"` |
| `start_date` | Recurring | DATE | Yes | -- | today + 10 years | `YYYY-MM-DD` | `2026-01-01` |
| `recurrence_end_date` | Recurring | DATE | No | after start_date | start_date + 10 years | `YYYY-MM-DD` | `2026-12-31` |
| `day_of_week` | Recurring | INTEGER | Conditional | 1 | 7 | Required for weekly/biweekly | `1` (Monday) |
| `day_of_month` | Recurring | INTEGER | Conditional | 1 | 31 | Required for monthly/yearly | `15` |
| `month_of_year` | Recurring | INTEGER | Conditional | 1 | 12 | Required for yearly | `6` (June) |

### 3.7 Split Fields

| Field | Entity | Type | Required | Min | Max | Constraint | Example |
|-------|--------|------|----------|-----|-----|-----------|---------|
| `amount` | Split | DECIMAL(10,2) | Yes | 0.01 | transaction.amount | `SUM(amounts) = transaction.amount` | `33.34` |
| `percentage` | Split | DECIMAL(5,2) | Conditional | 0.01 | 100.00 | `SUM(percentages) = 100.00` | `33.33` |
| `split_type` | Split | VARCHAR(20) | Yes | -- | -- | `"equal"`, `"percentage"`, `"custom_amount"` | `"equal"` |
| `settlement_notes` | Split | TEXT | No | -- | 500 chars | Free text | `"Bizum transfer"` |

### 3.8 Invitation Fields

| Field | Entity | Type | Required | Min | Max | Format | Example |
|-------|--------|------|----------|-----|-----|--------|---------|
| `invitee_email` | Invitation | VARCHAR(255) | Yes | -- | 255 chars | Valid email regex | `"bob@example.com"` |
| `token` | Invitation | VARCHAR(128) | Auto | 128 bits min | -- | Cryptographically random | `"a1b2c3d4..."` |
| `expires_at` | Invitation | TIMESTAMP | Auto | -- | -- | `created_at + 7 days` | `2026-03-01T00:00:00Z` |

---

## 4. State Transition Rules

### 4.1 Wallet States

```
                    ┌──────────────────────┐
                    │                      │
                    v                      │
  ┌─────────┐  archive  ┌──────────┐  restore  │
  │ Active  │ ────────> │ Archived │ ──────────┘
  └─────────┘           └──────────┘
       │
       │ delete (only if 0 transactions)
       v
  ┌─────────┐
  │ Deleted │  (permanent, no recovery)
  └─────────┘
```

| Transition | Condition | Side Effects |
|------------|-----------|-------------|
| Active -> Archived | Owner/member decision | No new transactions allowed. Existing data preserved. Balance still queryable. |
| Archived -> Active | Owner/member decision | Wallet accepts transactions again. Name conflict check not enforced (names are not unique). |
| Active -> Deleted | 0 transactions in wallet | Permanent removal. Cannot be undone. |
| **Personal <-> Shared** | **PROHIBITED** | Cannot change wallet type. Ever. |

---

### 4.2 Transaction States

```
  ┌────────────┐  confirm    ┌────────────┐
  │ Forecasted │ ──────────> │ Recorded   │
  └────────────┘             └────────────┘
       │                          │
       │ skip                     │ edit
       v                          v
  ┌────────────┐             ┌────────────┐
  │  Deleted   │             │  Updated   │
  └────────────┘             └────────────┘
       │                          │
       │ (after 30 days)          │ delete
       v                          v
  ┌──────────────────┐       ┌────────────┐
  │ Permanently      │       │  Deleted   │ ─── (after 30 days) ──> Permanently Deleted
  │ Deleted          │       └────────────┘
  └──────────────────┘
```

| State | `transaction_type` | `is_forecast` | Editable | Deletable |
|-------|--------------------|---------------|----------|-----------|
| Forecasted | `"forecasted"` | `true` | No (confirm/skip/edit-break-link only) | Yes (skip) |
| Recorded | `"normal"` | `false` | Yes (by creator) | Yes (soft delete) |
| Updated | `"normal"` | `false` | Yes | Yes |
| Deleted (soft) | -- | -- | No (recoverable within 30 days) | Already deleted |
| Permanently Deleted | -- | -- | No | Irreversible |

**Key transitions:**

| From | To | Trigger | Side Effects |
|------|----|---------|-------------|
| Forecasted -> Recorded | User confirms | `is_forecast = false`, `transaction_type = 'normal'`, date optionally changed to today |
| Forecasted -> Deleted | User skips occurrence | Transaction removed; pattern continues |
| Forecasted -> Independent Recorded | User edits forecasted | `recurring_transaction_id = NULL`, becomes normal editable transaction |
| Recorded -> Updated | User edits | Balance recalculated, splits regenerated if shared wallet, debt recalculated |
| Recorded -> Deleted | User deletes | Soft delete, balance reversal, split cascade delete, 30-day recovery window |
| Deleted -> Recorded | User restores within 30 days | Balance recalculated, splits restored |
| Deleted -> Permanently Deleted | 30 days elapse | Background job purges. Irreversible. |

---

### 4.3 Budget States

```
  ┌─────────┐  month begins  ┌─────────┐  month ends  ┌───────────┐
  │  Draft  │ ─────────────> │ Active  │ ───────────> │ Completed │
  └─────────┘                └─────────┘              └───────────┘
                                  │
                                  │ spending >= limit
                                  v
                             ┌──────────┐
                             │ Exceeded │ (warning state, still Active)
                             └──────────┘
```

| State | Period Relation | Editable | Description |
|-------|----------------|----------|-------------|
| Draft | Future month | Yes | Budget planned but period not yet started |
| Active | Current month | Yes | Budget being monitored; spending tracked in real-time |
| Completed | Past month | No (immutable) | Period ended; final spending recorded |
| Exceeded | Current month, `spent >= limit` | Yes (limit can be adjusted) | Warning state; transactions not blocked |
| On Track | Current month, `spent < expectedPace` | Yes | Spending within expected range |

**Budget status calculation:**

```
expectedPace = (daysElapsed / totalDaysInPeriod) * budget.amount

if spent >= limit:        status = "Exceeded"
else if spent > expectedPace: status = "Warning"
else:                     status = "On Track"
```

---

### 4.4 Goal States

```
  ┌─────────────┐  current >= target  ┌──────────┐
  │ In Progress │ ──────────────────> │ Achieved │
  │ (< 100%)    │ <────────────────── │ (>= 100%)│
  └─────────────┘  target increased   └──────────┘
       │                                   │
       │ user pauses                       │ user archives
       v                                   v
  ┌──────────┐                        ┌──────────┐
  │ Inactive │ ──── user resumes ──> In Progress
  └──────────┘                        └──────────┘
       │
       │ deadline passes (while < 100%)
       v
  ┌──────────┐
  │ Overdue  │ (visual indicator, still In Progress)
  └──────────┘
```

| State | `is_active` | `is_achieved` | Description |
|-------|-------------|---------------|-------------|
| In Progress | `true` | `false` | Actively saving, below target |
| Achieved | `true` | `true` | Current >= target, automatic |
| Overdue | `true` | `false` | Deadline passed, still below target |
| Inactive | `false` | `false` | User paused goal |
| Archived | `false` | any | User decided to stop tracking |

---

### 4.5 Invitation States

```
                  ┌─────────┐
                  │ Pending │
                  └─────────┘
                 /     |     \
       accept   /      |      \  7 days
               v       v       v
        ┌──────────┐ ┌──────────┐ ┌─────────┐
        │ Accepted │ │ Declined │ │ Expired │
        └──────────┘ └──────────┘ └─────────┘
        (terminal)   (terminal)   (terminal)
```

| Transition | Trigger | Side Effects |
|------------|---------|-------------|
| Pending -> Accepted | User accepts | User added to `shared_wallet_members`. Notification sent to all existing members. |
| Pending -> Declined | User declines | No membership change. Inviter can re-invite with new token. |
| Pending -> Expired | 7 days pass | Background job updates status. Token becomes invalid. |

All terminal states are final. No re-opening of accepted, declined, or expired invitations.

---

### 4.6 Split States

```
  ┌─────────┐  mark settled  ┌─────────┐
  │ Pending │ ─────────────> │ Settled │
  └─────────┘                └─────────┘
                             (terminal, irreversible)
```

| Transition | Trigger | Side Effects |
|------------|---------|-------------|
| (creation) -> Settled | Payer's split auto-settled | `is_payer = true`, `status = 'settled'` |
| Pending -> Settled | Non-payer marks as settled | `settlement_date` set, optional `settlement_notes`. No wallet transactions created. |

**Irreversible:** Once settled, a split cannot return to pending. If the settlement was recorded in error, the transaction itself must be deleted and re-created.

---

### 4.7 Category States

```
  ┌────────┐  archive  ┌──────────┐  restore  ┌────────┐
  │ Active │ ────────> │ Archived │ ────────> │ Active │
  └────────┘           └──────────┘           └────────┘
       │
       │ delete (only if 0 transactions)
       v
  ┌─────────┐
  │ Deleted │ (permanent, no recovery)
  └─────────┘
```

| State | Visible in Category Selector | Visible in Transaction History | Editable |
|-------|------------------------------|-------------------------------|----------|
| Active | Yes | Yes | Yes (custom only) |
| Archived | No | Yes (historical references) | No |
| Deleted | No | No | No |

**System categories** are always Active. They cannot transition to any other state.

---

### 4.8 Recurring Pattern States

```
  ┌────────┐  pause    ┌────────┐  resume   ┌────────┐
  │ Active │ ────────> │ Paused │ ────────> │ Active │
  └────────┘           └────────┘           └────────┘
       │
       │ delete
       v
  ┌─────────┐
  │ Deleted │ (removes future forecasts, preserves consolidated)
  └─────────┘
```

| State | `is_active` | Generates Forecasts | Existing Forecasts |
|-------|-------------|--------------------|--------------------|
| Active | `true` | Yes (daily job) | Visible |
| Paused | `false` | No | Remain visible, no new ones |
| Deleted | -- | No | Future forecasts removed; consolidated preserved |

---

## 5. Domain Event Triggers

### 5.1 Wallet Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `WalletCreatedEvent` | User creates personal wallet | `WalletId`, `UserId`, `Name`, `Currency`, `InitialBalance` | Auditing, Notifications (if first wallet -> onboarding milestone) |
| `WalletArchivedEvent` | User archives wallet | `WalletId`, `UserId` | Auditing, Goals (unlink if wallet-linked goal exists) |
| `WalletRestoredEvent` | User restores archived wallet | `WalletId`, `UserId` | Auditing |
| `SharedWalletCreatedEvent` | User creates shared wallet | `SharedWalletId`, `CreatorUserId`, `Name`, `Currency` | Auditing |
| `SharedWalletMemberJoinedEvent` | User accepts invitation | `SharedWalletId`, `UserId`, `InvitedByUserId` | Auditing, Notifications (notify all members) |
| `SharedWalletMemberLeftEvent` | Member leaves shared wallet | `SharedWalletId`, `UserId` | Auditing, Notifications (notify remaining members) |
| `SharedWalletArchivedEvent` | Members archive shared wallet | `SharedWalletId` | Auditing, Recurring (stop patterns for this wallet) |

### 5.2 Transaction Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `TransactionRecordedEvent` | Transaction created | `TransactionId`, `UserId`, `WalletId`/`SharedWalletId`, `CategoryId`, `Amount`, `Date`, `Type` | Budgets (spending update), Goals (wallet balance change), Auditing |
| `TransactionUpdatedEvent` | Transaction edited | `TransactionId`, `UserId`, `OldAmount`, `NewAmount`, `OldCategoryId`, `NewCategoryId` | Budgets (spending recalculation), Goals (balance recalculation), Auditing |
| `TransactionDeletedEvent` | Transaction deleted | `TransactionId`, `UserId`, `WalletId`/`SharedWalletId`, `Amount`, `CategoryId` | Budgets (spending reversal), Goals (balance recalculation), Auditing |
| `SharedExpenseRecordedEvent` | Transaction with splits created | `TransactionId`, `SharedWalletId`, `PayerUserId`, `Amount`, `SplitDetails[]` | Notifications (notify non-payer members), Auditing |
| `ForecastConfirmedEvent` | Forecasted transaction confirmed | `TransactionId`, `RecurringPatternId`, `ConfirmedDate` | Recurring (update `last_generated_date`), Auditing |
| `ForecastSkippedEvent` | Forecasted transaction skipped | `TransactionId`, `RecurringPatternId` | Auditing |

### 5.3 Budget Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `BudgetCreatedEvent` | Budget created | `BudgetId`, `UserId`/`SharedWalletId`, `CategoryId`, `Amount`, `Period` | Auditing |
| `BudgetUpdatedEvent` | Budget limit changed | `BudgetId`, `OldAmount`, `NewAmount` | Auditing |
| `BudgetThresholdReachedEvent` | Spending >= alert_threshold% | `BudgetId`, `UserId`/`SharedWalletId`, `CategoryName`, `Spent`, `Limit`, `Percentage` | Notifications (send budget_alert) |
| `BudgetExceededEvent` | Spending >= 100% of limit | `BudgetId`, `UserId`/`SharedWalletId`, `CategoryName`, `Spent`, `Limit` | Notifications (send budget_alert critical) |

### 5.4 Goal Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `GoalCreatedEvent` | Goal created | `GoalId`, `UserId`, `Name`, `TargetAmount`, `TargetDate` | Auditing |
| `GoalMilestoneReachedEvent` | Progress crosses 25%, 50%, 75%, or 100% | `GoalId`, `UserId`, `Name`, `MilestonePercent`, `CurrentAmount`, `TargetAmount` | Notifications (congratulatory notification) |
| `GoalAchievedEvent` | `current_amount >= target_amount` | `GoalId`, `UserId`, `Name`, `TargetAmount`, `AchievedDate` | Notifications (achievement notification) |
| `GoalOverdueEvent` | Deadline passes with `current < target` | `GoalId`, `UserId`, `Name`, `TargetDate`, `CurrentAmount`, `TargetAmount` | Notifications (overdue warning) |

### 5.5 Recurring Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `RecurringPatternCreatedEvent` | Pattern created | `PatternId`, `UserId`, `WalletId`, `Frequency`, `Amount` | Auditing |
| `RecurringPatternPausedEvent` | Pattern paused | `PatternId`, `UserId` | Auditing |
| `RecurringPatternResumedEvent` | Pattern resumed | `PatternId`, `UserId` | Auditing |
| `RecurringPatternDeletedEvent` | Pattern deleted | `PatternId`, `UserId` | Auditing |
| `RecurringTransactionDueEvent` | Forecasted transaction due tomorrow | `TransactionId`, `PatternId`, `UserId`, `Amount`, `Concept` | Notifications (recurring_due notification) |

### 5.6 Debt and Settlement Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `DebtCalculatedEvent` | Transaction in shared wallet creates/modifies splits | `SharedWalletId`, `DebtorUserId`, `CreditorUserId`, `Amount` | Notifications (debt notification) |
| `SplitSettledEvent` | Member marks split as settled | `SplitId`, `TransactionId`, `DebtorUserId`, `CreditorUserId`, `Amount`, `SettlementNotes` | Auditing, Notifications (settlement confirmation) |

### 5.7 Invitation Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `InvitationSentEvent` | Member sends invitation | `InvitationId`, `SharedWalletId`, `InviterUserId`, `InviteeEmail` | Notifications (email + in-app if registered) |
| `InvitationAcceptedEvent` | Invitee accepts | `InvitationId`, `SharedWalletId`, `UserId` | Auditing, Notifications (notify wallet members) |
| `InvitationDeclinedEvent` | Invitee declines | `InvitationId`, `SharedWalletId`, `InviteeEmail` | Auditing |
| `InvitationExpiredEvent` | 7-day timer expires | `InvitationId`, `SharedWalletId` | -- |

### 5.8 Identity Events

| Event | Trigger | Payload | Consumers |
|-------|---------|---------|-----------|
| `UserRegisteredEvent` | New account created | `UserId`, `Email`, `Name` | Auditing, Notifications (welcome email) |
| `UserEmailVerifiedEvent` | Email verification completed | `UserId`, `Email` | Auditing |
| `UserPasswordResetEvent` | Password successfully reset | `UserId` | Auditing |
| `UserDeactivatedEvent` | Account deactivated | `UserId` | Auditing |

---

## 6. Calculation Formulas

### 6.1 Balance Calculation

#### Personal Wallet Balance

```
personalWallet.currentBalance =
    personalWallet.initialBalance
  + SUM(transactions WHERE wallet_id = personalWallet.id AND category.type = 'income')
  - SUM(transactions WHERE wallet_id = personalWallet.id AND category.type = 'expense')
```

**Notes:**
- Only consolidated transactions (`is_forecast = false`) affect the balance.
- Forecasted transactions are shown separately in projections but do not change the current balance.
- Transfers are modeled as two transactions: an expense in the source wallet and an income in the destination wallet.

#### Shared Wallet Balance

```
sharedWallet.currentBalance =
    sharedWallet.initialBalance
  + SUM(transactions WHERE shared_wallet_id = sharedWallet.id AND category.type = 'income')
  - SUM(transactions WHERE shared_wallet_id = sharedWallet.id AND category.type = 'expense')
```

**Notes:**
- Splits do not affect the wallet balance. Splits only track who owes whom.
- Settlements do not affect the wallet balance. Settlements only update split status.

#### Projected Balance (with recurring forecasts)

```
projectedBalance(date) =
    currentBalance
  + SUM(forecasted income WHERE date <= targetDate)
  - SUM(forecasted expenses WHERE date <= targetDate)
```

---

### 6.2 Debt Calculation

#### Raw Debt Per Split

```
For each pending split in shared wallet X:
  debtor = split.user_id (non-payer)
  creditor = split.owed_to_user_id (payer)
  amount = split.amount
```

#### Aggregated Debt Between Two Users

```
rawDebt(A owes B, walletX) =
  SUM(splits WHERE user_id = A AND owed_to_user_id = B AND status = 'pending')

rawDebt(B owes A, walletX) =
  SUM(splits WHERE user_id = B AND owed_to_user_id = A AND status = 'pending')

netDebt(A, B, walletX) = rawDebt(A owes B) - rawDebt(B owes A)

if netDebt > 0: A owes B netDebt
if netDebt < 0: B owes A |netDebt|
if netDebt = 0: No debt between A and B
```

#### Debt Simplification Algorithm (3+ users)

```
// Input: all pending splits in a shared wallet
// Output: minimum set of transfers to settle all debts

function simplifyDebts(splits):
  // Step 1: Calculate net balance for each user
  balances = {}
  for each split where status = 'pending':
    balances[split.owed_to_user_id] += split.amount   // creditor gains
    balances[split.user_id] -= split.amount            // debtor loses

  // Step 2: Separate into creditors (positive) and debtors (negative)
  creditors = sorted([user for user in balances if balances[user] > 0], descending)
  debtors = sorted([user for user in balances if balances[user] < 0], by abs, descending)

  // Step 3: Greedy matching
  simplifiedDebts = []
  while creditors AND debtors:
    creditor = creditors[0]
    debtor = debtors[0]
    transferAmount = min(balances[creditor], abs(balances[debtor]))

    simplifiedDebts.append({
      from: debtor,
      to: creditor,
      amount: transferAmount
    })

    balances[creditor] -= transferAmount
    balances[debtor] += transferAmount

    if balances[creditor] == 0: remove from creditors
    if balances[debtor] == 0: remove from debtors

  return simplifiedDebts

// Example:
// A paid $300, B paid $60, C paid $90 for shared expenses totaling $450
// Equal split: each should pay $150
// Net: A is owed $150, B owes $90, C owes $60
// Simplified: B pays A $90, C pays A $60
```

---

### 6.3 Split Calculations

#### Equal Split

```csharp
// Distributes amount equally, assigning remainder cents to first members.
public static decimal[] CalculateEqualSplit(decimal totalAmount, int memberCount)
{
    var baseAmount = Math.Floor(totalAmount / memberCount * 100) / 100;
    var remainder = totalAmount - (baseAmount * memberCount);
    var remainderCents = (int)(remainder * 100);

    var splits = new decimal[memberCount];
    for (int i = 0; i < memberCount; i++)
    {
        splits[i] = baseAmount + (i < remainderCents ? 0.01m : 0m);
    }
    return splits;
}

// Example: $100 / 3 = [$33.34, $33.33, $33.33]
// Verification: 33.34 + 33.33 + 33.33 = 100.00 ✓
```

#### Percentage Split

```csharp
// Distributes amount by percentages, adjusting last member for rounding.
public static decimal[] CalculatePercentageSplit(
    decimal totalAmount, decimal[] percentages)
{
    // Validate: SUM(percentages) must equal 100
    if (percentages.Sum() != 100m)
        throw new ValidationException("Percentages must sum to 100%");

    var splits = percentages
        .Select(p => Math.Round(totalAmount * p / 100m, 2))
        .ToArray();

    // Rounding correction on last member
    var diff = totalAmount - splits.Sum();
    splits[^1] += diff;

    return splits;
}

// Example: $100, [60%, 40%] = [$60.00, $40.00]
// Example: $100, [33.33%, 33.33%, 33.34%] = [$33.33, $33.33, $33.34]
```

#### Custom Amount Split

```csharp
// Validates custom amounts sum to transaction total.
public static void ValidateCustomSplit(decimal totalAmount, decimal[] amounts)
{
    if (amounts.Sum() != totalAmount)
        throw new ValidationException(
            $"Custom amounts must sum to {totalAmount}. Got {amounts.Sum()}.");
}

// Example: $100 -> [$70, $30] ✓
// Example: $100 -> [$70, $25] ✗ (sum = 95, not 100)
```

**TypeScript equivalent:**

```typescript
function calculateEqualSplit(total: number, count: number): number[] {
  const base = Math.floor((total / count) * 100) / 100;
  const remainderCents = Math.round((total - base * count) * 100);
  return Array.from({ length: count }, (_, i) =>
    +(base + (i < remainderCents ? 0.01 : 0)).toFixed(2)
  );
}

// Percentage split
function calculatePercentageSplit(total: number, pcts: number[]): number[] {
  const splits = pcts.map(p => +(total * p / 100).toFixed(2));
  const diff = +(total - splits.reduce((a, b) => a + b, 0)).toFixed(2);
  splits[splits.length - 1] = +(splits[splits.length - 1] + diff).toFixed(2);
  return splits;
}
```

---

### 6.4 Budget Progress

#### Spending Calculation

```
// Personal budget
budget.spent = SUM(
  transactions.amount
  WHERE wallet_id IN (user's personal wallets)
    AND category_id = budget.category_id
    AND date WITHIN budget.period
    AND category.type = 'expense'
    AND is_forecast = false
)

// Shared wallet budget
budget.spent = SUM(
  transactions.amount
  WHERE shared_wallet_id = budget.shared_wallet_id
    AND category_id = budget.category_id
    AND date WITHIN budget.period
    AND category.type = 'expense'
    AND is_forecast = false
)
```

#### Status Calculation

```
budget.percentUsed = (budget.spent / budget.amount) * 100
budget.remaining = budget.amount - budget.spent

// Expected pace (linear projection)
daysInPeriod = totalDaysInBudgetPeriod
daysElapsed = daysSincePeriodStart
expectedPace = (daysElapsed / daysInPeriod) * budget.amount

// Status determination
if budget.spent >= budget.amount:
    status = "Exceeded"
elif budget.spent >= expectedPace:
    status = "Warning"
else:
    status = "On Track"
```

#### Projected Overspend

```
dailyRate = budget.spent / daysElapsed
projectedTotal = budget.spent + (dailyRate * daysRemaining)

if projectedTotal > budget.amount:
    projectedOverage = projectedTotal - budget.amount
    alert = "At current rate, will overspend by {projectedOverage}"
```

---

### 6.5 Goal Progress

#### Progress Percentage

```
goal.percentComplete = (goal.currentAmount / goal.targetAmount) * 100

// Display: cap at 100% for progress bar, show actual for text
displayPercent = MIN(goal.percentComplete, 100)
textPercent = goal.percentComplete  // can exceed 100
```

#### Projected Completion Date

```
if goal.currentAmount >= goal.targetAmount:
    projectedCompletion = "Already achieved"
    return

// Calculate savings rate from recent history
recentSavings = SUM(deposits to linked wallet in last 90 days)
dailySavingsRate = recentSavings / 90

if dailySavingsRate <= 0:
    projectedCompletion = null  // Cannot project without positive trend
    return

amountRemaining = goal.targetAmount - goal.currentAmount
daysNeeded = CEIL(amountRemaining / dailySavingsRate)
projectedCompletion = today + daysNeeded
```

#### Daily Savings Needed (if deadline set)

```
if goal.targetDate is null:
    dailyNeeded = null
    return

amountRemaining = goal.targetAmount - goal.currentAmount
daysRemaining = MAX(goal.targetDate - today, 1)

dailyNeeded = amountRemaining / daysRemaining
monthlySuggested = dailyNeeded * 30
```

---

### 6.6 Recurrence Calculations

#### Next Occurrence Date

```
function nextOccurrence(current, frequency, pattern):
  switch frequency:
    case 'daily':
      return current + 1 day

    case 'weekly':
      return current + 7 days
      // Or: next occurrence of pattern.dayOfWeek after current

    case 'biweekly':
      return current + 14 days

    case 'monthly':
      nextMonth = current.month + 1
      nextYear = current.year + (nextMonth > 12 ? 1 : 0)
      nextMonth = nextMonth > 12 ? 1 : nextMonth
      targetDay = MIN(pattern.dayOfMonth, daysInMonth(nextYear, nextMonth))
      return Date(nextYear, nextMonth, targetDay)

    case 'yearly':
      nextYear = current.year + 1
      targetDay = MIN(pattern.dayOfMonth, daysInMonth(nextYear, pattern.monthOfYear))
      return Date(nextYear, pattern.monthOfYear, targetDay)
```

#### Generation Window

```
function generateForecasts(pattern):
  horizon = today + 90 days
  startFrom = MAX(pattern.startDate, pattern.lastGeneratedDate + 1 occurrence)

  occurrences = []
  current = startFrom

  while current <= horizon:
    if pattern.recurrenceEndDate AND current > pattern.recurrenceEndDate:
      break
    if NOT pattern.isActive:
      break

    occurrences.append(current)
    current = nextOccurrence(current, pattern.frequency, pattern)

  // Create forecasted transactions for each occurrence
  for each date in occurrences:
    createForecastedTransaction(pattern, date)

  pattern.lastGeneratedDate = occurrences.last (or unchanged if empty)
```

---

### 6.7 Period Aggregation Formulas

#### Monthly Totals (Personal)

```
totalIncome = SUM(transactions WHERE
  user_id = currentUser
  AND wallet_id IN (personal wallets)
  AND category.type = 'income'
  AND date WITHIN targetMonth
  AND is_forecast = false)

totalExpenses = SUM(transactions WHERE
  user_id = currentUser
  AND wallet_id IN (personal wallets)
  AND category.type = 'expense'
  AND date WITHIN targetMonth
  AND is_forecast = false)

netBalance = totalIncome - totalExpenses
savingsRate = totalIncome > 0 ? (netBalance / totalIncome) * 100 : 0
dailyAvgExpense = totalExpenses / daysInMonth
dailyAvgIncome = totalIncome / daysInMonth
```

#### Category Breakdown

```
categoryBreakdown = GROUP BY category_id:
  categoryName
  totalAmount = SUM(transactions.amount)
  transactionCount = COUNT(transactions)
  percentOfTotal = (totalAmount / totalExpenses) * 100

ORDER BY totalAmount DESC
```

---

*This document is the authoritative reference for all business rules in Kakeibo. Any code that contradicts a rule in this document contains a bug. When adding new features, update this document first, then implement.*
