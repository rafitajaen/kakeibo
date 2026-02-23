# Phase 3c: Transaction Splits Backend + UI

**Status**: Not Started
**Objective**: Implement split configuration for shared expenses

---

## Scope

### ✅ Included
- Split types: Equal, Percentage, Custom
- Split validation (percentages = 100%, custom amounts = transaction amount ± $0.01)
- Integration with debt calculation (triggers recalculation via event)
- Frontend: split configurator UI

### ❌ Excluded
- Split templates (save reusable splits) — post-MVP
- Unequal split by number of people (e.g., 1 person pays 60%, 2 people pay 20% each) — Phase 6

---

## Deliverables

### Backend
**Kakeibo.Modules.Transactions/ValueObjects/**:
- Split.cs, SplitType.cs

**Modified**:
- Transaction.cs (add Split property)
- RecordTransaction handler (validate split)

### Frontend
**sites/Kakeibo.App/src/components/transactions/**:
- SplitConfigurator.vue, SplitTypeSelector.vue, SplitItem.vue

---

## Acceptance Criteria

- [ ] Equal split configuration
- [ ] Percentage split configuration (validates total = 100%)
- [ ] Custom split configuration (validates sum = amount ± $0.01)
- [ ] Split validation errors shown
- [ ] Integration with debt calculation (event consumed by Wallets module)
- [ ] Frontend: split configurator
- [ ] Integration test: transaction with split → debt calculation triggered
- [ ] E2E test: record expense with equal split → view debts

---

## Definition of "Phase 3c Completed"

1. Split configuration functional
2. All 8 acceptance criteria checked
3. Phases 4, 5, 6 can begin
