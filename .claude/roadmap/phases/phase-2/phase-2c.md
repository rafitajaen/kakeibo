# Phase 2c: Splits + Debt Calculation + Settlements Backend + UI

> **⚠️ Development order note:** Despite the "2c" numbering, this sub-phase is implemented
> **after Phase 3b** (Transaction Recording). Actual order: 2a → 2b → 3a → 3b → **2c** →
> (4 | 5 | 6 parallel). Phase 3c has been absorbed into this phase — all split-related work
> lives here.

**Status**: Not Started
**Objective**: Implement transaction split configuration, automatic debt calculation, and settlement recording for shared wallets

---

## Prerequisites

| Item | Status |
|------|--------|
| Shared Wallets + Invitations | ✅ Phase 2b |
| Transaction Recording | ⏳ Phase 3b (required — debt calc consumes TransactionRecordedEvent) |

---

## Scope

### ✅ Included

**Transaction Splits**:
- Split types: Equal, Percentage (must total 100%), Custom (must match amount ± $0.01)
- Split configuration per transaction (attached at record time)
- Split validation (percentages = 100%, custom amounts = transaction amount ± $0.01)
- Integration with debt calculation (triggers recalculation via `TransactionRecordedEvent`)
- Frontend: `SplitConfigurator.vue`, `SplitTypeSelector.vue`, `SplitItem.vue`

**Debt Calculation**:
- Automatic debt calculation from transactions + splits
- Debt simplification (minimize number of debts — Splitwise algorithm)
- Debt visibility for all shared wallet members (symmetric)
- Debt recalculation on transaction create, update, or delete

**Settlements**:
- Settlement recording (external payments that don't affect wallet balance)
- Settlement amount validation (cannot exceed current debt between two members)
- Debt reduction after settlement recorded

**Frontend**:
- Debt list view per shared wallet
- Settlement recording modal
- Split configurator in transaction form (enabled for shared wallet transactions)

### ❌ Excluded

- Transaction recording itself — Phase 3b
- Multi-currency debts — single currency MVP
- Split templates (save reusable splits) — post-MVP

---

## Deliverables

### Backend

**`Kakeibo.Api/Features/Transactions/`** (updated):
- `TransactionSplit.cs` — value object with SplitType, member allocations
- `SplitType.cs` — enum: Equal, Percentage, Custom
- `RecordTransaction` handler updated — validate split when wallet is shared

**`Kakeibo.Api/Features/Wallets/`** (new slices):
- `Services/DebtCalculationService.cs` — Splitwise debt minimization algorithm
- `Features/RecordSettlement/` — RecordSettlementEndpoint, RecordSettlementHandler, RecordSettlementValidator
- `Features/GetWalletDebts/` — GetWalletDebtsEndpoint, GetWalletDebtsHandler

**`Kakeibo.Api/Features/Wallets/`** (event handlers):
- `TransactionRecordedHandler.cs` — `IEventHandler<TransactionRecordedEvent>` — recalculates debts
- `TransactionUpdatedHandler.cs` — `IEventHandler<TransactionUpdatedEvent>` — recalculates debts
- `TransactionDeletedHandler.cs` — `IEventHandler<TransactionDeletedEvent>` — recalculates debts

**Endpoints**:
- `GET /api/wallets/{id}/debts`
- `POST /api/wallets/{id}/settlements`

### Frontend

**`src/Kakeibo.App/components/wallets/`**:
- `DebtList.vue`, `DebtCard.vue`, `SettlementForm.vue`

**`src/Kakeibo.App/components/transactions/`**:
- `SplitConfigurator.vue`, `SplitTypeSelector.vue`, `SplitItem.vue`

---

## Acceptance Criteria

### Splits
- [ ] Equal split configuration
- [ ] Percentage split configuration (validates total = 100%)
- [ ] Custom split configuration (validates sum = amount ± $0.01)
- [ ] Split validation errors shown in frontend
- [ ] Split triggers debt recalculation (TransactionRecordedEvent handled by Wallets IEventHandler<T>)
- [ ] Frontend: split configurator in transaction form

### Debt Calculation
- [ ] Debt calculation from transaction splits (accurate)
- [ ] Debt simplification (minimize debts shown — Splitwise algorithm)
- [ ] All shared wallet members see same debt state (symmetric visibility)
- [ ] Debts recalculated on transaction create, update, and delete

### Settlements
- [ ] Record settlement between members
- [ ] Settlement amount validated (cannot exceed current debt)
- [ ] Debt reduced after settlement

### Integration Tests
- [ ] Integration test: transaction with split → debt calculation triggered
- [ ] Integration test: settlement → debt reduction
- [ ] Integration test: TransactionUpdatedEvent → debt recalculation
- [ ] Integration test: TransactionDeletedEvent → debt recalculation

### E2E Tests
- [ ] E2E test: record expense with equal split → view debts
- [ ] E2E test: record expense → view debts → record settlement → debts cleared

---

## Definition of "Phase 2c Completed"

1. Split configuration functional (Equal, Percentage, Custom)
2. Debt calculation accurate (Splitwise algorithm)
3. Settlement recording functional
4. All acceptance criteria checked
5. Phases 4, 5, 6 can begin in parallel (all consume TransactionRecordedEvent)
