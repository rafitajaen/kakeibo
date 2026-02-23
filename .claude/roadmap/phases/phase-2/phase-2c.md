# Phase 2c: Debt Calculation + Settlements Backend + UI

**Status**: Not Started
**Objective**: Implement automatic debt calculation and settlement recording

---

## Scope

### ✅ Included
- Expense split types (Equal, Percentage, Custom)
- Automatic debt calculation from transactions
- Debt simplification (minimize number of debts)
- Settlement recording (external payments)
- Debt visibility for all members
- Frontend: debt view, settlement recording

### ❌ Excluded
- Transaction recording — Phase 3b (debts calculated from existing transactions)
- Multi-currency debts — single currency MVP

---

## Deliverables

### Backend
**Kakeibo.Modules.Wallets/**:
- Services/DebtCalculationService.cs
- Features/RecordSettlement, GetWalletDebts

**Endpoints**:
- `GET /api/wallets/{id}/debts`
- `POST /api/wallets/{id}/settlements`

### Frontend
**sites/Kakeibo.App/src/components/wallets/**:
- DebtList.vue, DebtCard.vue, SettlementForm.vue

---

## Acceptance Criteria

- [ ] Record settlement between members
- [ ] Debt calculation from transaction splits
- [ ] Debt simplification (minimize debts shown)
- [ ] All members see same debt state
- [ ] Frontend: debt view
- [ ] Frontend: settlement recording
- [ ] Integration test: transaction → debt calculation
- [ ] Integration test: settlement → debt reduction
- [ ] E2E test: create transaction → view debts → record settlement

---

## Definition of "Phase 2c Completed"

1. Debt calculation accurate
2. Settlement recording functional
3. All 9 acceptance criteria checked
4. Phase 3 can begin
