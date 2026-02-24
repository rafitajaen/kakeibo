# Phase 3: Transactions & Categories

**Status**: Not Started
**Blocks**: Phases 4, 5, 6 (Budgets, Goals, Recurring need transactions)
**Requires**: Phase 2 (Wallets)

---

## Prerequisites

| Item | Status | Required For |
|------|--------|--------------|
| Wallets | ⏳ Phase 2 | Transactions affect wallet balances |
| Debt Calculation | ⏳ Phase 2c | Transaction splits trigger debt updates |

---

## Sub-Phase Split

| Phase | Name | Duration | Deliverable |
|-------|------|----------|-------------|
| **3a** | Categories Backend + UI | 1-2 days | 12 system categories + custom categories CRUD |
| **3b** | Transaction Recording Backend + Calculator UI | 3-4 days | Income, expense, transfer recording with calculator interface |

**Total estimated duration**: 4-6 days

> **Note:** Transaction split configuration (Equal, Percentage, Custom) was moved to Phase 2c.
> See [phase-2c.md](../phase-2/phase-2c.md) and [phase-3c.md](./phase-3c.md) for details.

---

## Scope

### ✅ Included

**Categories** (3a):
- 12 built-in system categories (Housing, Transportation, Food, etc.)
- Unlimited custom categories
- Category CRUD (create, rename, archive)
- Category seeding on first run

**Transactions** (3b):
- Three types: Income, Expense, Transfer
- Transaction recording with amount, date, description, category, wallet(s)
- Calculator-style amount input
- Balance updates (automatic, atomic)
- Transaction history with filtering
- Edit and soft delete

### ❌ Excluded

- Import/export of transactions — Phase 8
- Transaction attachments (receipts) — post-MVP
- Geolocation tagging — post-MVP
- Transaction templates — Phase 6

---

## Feature Architecture

**Feature folder**: `Kakeibo.Api/Features/Transactions/`
**Schema**: `public` (single `AppDbContext`)
**Pattern**: Vertical slices

**Key Entities** (in `AppDbContext`):
- `Transaction` — income, expense, transfer
- `Category` — system + custom
- `WalletBalance` — maintained atomically with transactions

**Endpoints**:
- Categories: `GET/POST/PUT/DELETE /api/categories`
- Transactions: `GET/POST/PUT/DELETE /api/transactions`

**Events published** (via `IEventBus`):
- `TransactionRecordedEvent`
- `TransactionUpdatedEvent`
- `TransactionDeletedEvent`

---

## MVP Acceptance Criteria

### Phase 3a — Categories
- [ ] 12 system categories seeded on startup
- [ ] Create custom category
- [ ] List categories (system + custom)
- [ ] Archive custom category
- [ ] Frontend: category selector
- [ ] Frontend: custom category management

### Phase 3b — Transaction Recording
- [ ] Record income transaction
- [ ] Record expense transaction
- [ ] Record transfer transaction
- [ ] Calculator-style amount input
- [ ] Balance updates atomically
- [ ] Transaction list with filters
- [ ] Edit transaction
- [ ] Delete transaction (soft delete)
- [ ] Frontend: transaction list
- [ ] Frontend: record transaction form
- [ ] Frontend: calculator UI

---

## Definition of "Phase 3 Completed"

1. Both sub-phases complete (3a + 3b)
2. Categories functional
3. Transaction recording operational
4. All acceptance criteria checked
5. Phases 4, 5, 6 can begin in parallel
6. Phase 2c can begin (requires 3b events)

---

**Next Phase**: Phases 4, 5, 6 (can run in parallel)
