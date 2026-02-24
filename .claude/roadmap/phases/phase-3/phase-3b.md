# Phase 3b: Transaction Recording Backend + Calculator UI

**Status**: Not Started
**Objective**: Implement transaction recording (income, expense, transfer) with calculator interface

---

## Scope

### ✅ Included
- Three transaction types: Income, Expense, Transfer
- Transaction recording with amount, date, description, category, wallet(s)
- Calculator-style amount input UI
- Automatic balance updates (atomic) — **balance lives in `AppDbContext.WalletBalances`** (not in the `Wallet` entity)
- `WalletBalance` entity updated atomically with each transaction record/edit/delete in a single `SaveChangesAsync()` call
- Transfer transactions update both source and destination `WalletBalance` rows atomically (single `SaveChangesAsync()`)
- Transaction history with filtering
- Edit and soft delete transactions
- Events published via `IEventBus` for Auditing, Budgets, Goals consumers

### ❌ Excluded
- Transaction attachments — post-MVP
- Geolocation tagging — post-MVP
- Bulk transaction import — Phase 8

---

## Deliverables

### Backend
**`Kakeibo.Api/Features/Transactions/Entities/`** (or `Kakeibo.Api/Domain/Entities/`):
- `WalletBalance.cs` — WalletId (FK), Balance (decimal), UpdatedAt (Instant)

**`Kakeibo.Api/Features/Transactions/`**:
- RecordTransaction, UpdateTransaction, DeleteTransaction, ListTransactions, GetTransaction

**`Kakeibo.Api/Persistence/Configurations/`**:
- `WalletBalanceConfiguration.cs` — EF Core entity mapping

**Endpoints**:
- `POST /api/transactions`
- `GET /api/transactions`
- `GET /api/transactions/{id}`
- `PUT /api/transactions/{id}`
- `DELETE /api/transactions/{id}`

### Frontend
**`sites/Kakeibo.App/views/transactions/`**:
- TransactionsView.vue, RecordTransactionView.vue

**`sites/Kakeibo.App/components/transactions/`**:
- TransactionList.vue, TransactionForm.vue, CalculatorInput.vue

---

## Acceptance Criteria

- [ ] Record income transaction
- [ ] Record expense transaction
- [ ] Record transfer transaction (affects 2 wallets)
- [ ] Calculator-style amount input
- [ ] WalletBalance updated atomically in same SaveChangesAsync() call as transaction
- [ ] Transfer transaction updates both wallet balances atomically (single transaction)
- [ ] Transaction list with filters (date range, category, wallet)
- [ ] Edit transaction
- [ ] Delete transaction (soft delete via `DeletedAt`)
- [ ] Frontend: transaction list
- [ ] Frontend: record transaction form
- [ ] Frontend: calculator UI
- [ ] Integration test: record → WalletBalance updated atomically in same SaveChangesAsync()
- [ ] Integration test: transfer → both WalletBalance rows updated atomically (single transaction)
- [ ] Integration test: Wallets feature reads correct balance from AppDbContext.WalletBalances
- [ ] E2E test: record income → view in list

---

## Definition of "Phase 3b Completed"

1. Transaction recording functional
2. WalletBalance entity maintained atomically in AppDbContext
3. Wallets feature reads balance directly from AppDbContext.WalletBalances
4. All acceptance criteria checked
5. Phase 2c can begin (reacts to TransactionRecordedEvent via IEventHandler<T> for debt calculation)
