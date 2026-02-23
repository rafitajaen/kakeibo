# Phase 3b: Transaction Recording Backend + Calculator UI

**Status**: Not Started
**Objective**: Implement transaction recording (income, expense, transfer) with calculator interface

---

## Scope

### ✅ Included
- Three transaction types: Income, Expense, Transfer
- Transaction recording with amount, date, description, category, wallet(s)
- Calculator-style amount input UI
- Automatic balance updates (atomic)
- Transaction history with filtering
- Edit and soft delete transactions
- Integration events published

### ❌ Excluded
- Transaction attachments — post-MVP
- Geolocation tagging — post-MVP
- Bulk transaction import — Phase 8

---

## Deliverables

### Backend
**Kakeibo.Modules.Transactions/Features/**:
- RecordTransaction, UpdateTransaction, DeleteTransaction, ListTransactions, GetTransaction

**Endpoints**:
- `POST /api/transactions`
- `GET /api/transactions`
- `GET /api/transactions/{id}`
- `PUT /api/transactions/{id}`
- `DELETE /api/transactions/{id}`

### Frontend
**sites/Kakeibo.App/src/views/transactions/**:
- TransactionsView.vue, RecordTransactionView.vue

**sites/Kakeibo.App/src/components/transactions/**:
- TransactionList.vue, TransactionForm.vue, CalculatorInput.vue

---

## Acceptance Criteria

- [ ] Record income transaction
- [ ] Record expense transaction
- [ ] Record transfer transaction (affects 2 wallets)
- [ ] Calculator-style amount input
- [ ] Balance updates atomically
- [ ] Transaction list with filters (date range, category, wallet)
- [ ] Edit transaction
- [ ] Delete transaction (soft delete via `DeletedAt`)
- [ ] Frontend: transaction list
- [ ] Frontend: record transaction form
- [ ] Frontend: calculator UI
- [ ] Integration test: record → balance update
- [ ] Integration test: transfer → both balances update
- [ ] E2E test: record income → view in list

---

## Definition of "Phase 3b Completed"

1. Transaction recording functional
2. All 14 acceptance criteria checked
3. Phase 3c can begin
