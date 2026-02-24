# Phase 3c: Transaction Splits Backend + UI

> **⚠️ This sub-phase has been absorbed into Phase 2c.**
>
> Transaction split configuration (Equal, Percentage, Custom) is now fully defined and implemented
> in [Phase 2c: Splits + Debt Calculation + Settlements](../../phase-2/phase-2c.md).
>
> **Rationale:** Splits are the mechanism that feeds debt calculation. Having both in the same
> phase (2c) avoids a circular dependency where 2c (debt calc) would need 3c (splits) to already
> exist. Implementing splits alongside debt calculation and settlements produces a complete,
> testable slice: configure split → record transaction → view debts → record settlement.
>
> See [phase-2c.md](../../phase-2/phase-2c.md) for all split-related deliverables and
> acceptance criteria.
