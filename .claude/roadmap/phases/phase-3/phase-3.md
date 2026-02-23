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
| **3c** | Transaction Splits Backend + UI | 2-3 days | Split configuration for shared expenses |

**Total estimated duration**: 6-9 days

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

**Splits** (3c):
- Split types: Equal, Percentage (must total 100%), Custom (must match amount)
- Split configuration per transaction
- Integration with debt calculation (Phase 2c)

### ❌ Excluded

- Import/export of transactions — Phase 8
- Transaction attachments (receipts) — post-MVP
- Geolocation tagging — post-MVP
- Transaction templates — Phase 6

---

## Module Architecture

**Module**: `Kakeibo.Modules.Transactions`
**Schema**: `transactions`
**Pattern**: Vertical slices

**Key Entities**:
- `Transaction` (aggregate root: income, expense, transfer)
- `Category` (system + custom)

**Endpoints**:
- Categories: `GET/POST/PUT/DELETE /api/categories`
- Transactions: `GET/POST/PUT/DELETE /api/transactions`

**Integration Events**:
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

### Phase 3c — Transaction Splits
- [ ] Equal split configuration
- [ ] Percentage split configuration
- [ ] Custom split configuration
- [ ] Split validation (percentages = 100%, custom = amount)
- [ ] Integration with debt calculation
- [ ] Frontend: split configurator

---

## Definition of "Phase 3 Completed"

1. All three sub-phases complete
2. Categories functional
3. Transaction recording operational
4. Split configuration working
5. All 23 acceptance criteria checked
6. Phases 4, 5, 6 can begin in parallel

---

**Next Phase**: Phases 4, 5, 6 (can run in parallel)
