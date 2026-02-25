# Phase 2: Wallets & Collaboration

**Status**: Complete
**Blocks**: Phase 3 (Transactions need wallets)
**Requires**: Phase 1 (Authentication)

---

## Prerequisites

| Item | Status | Required For |
|------|--------|--------------|
| Authentication | ⏳ Phase 1b | User identification for wallet ownership |
| Events System | ✅ Phase 1a | In-process event delivery via ChannelEventBus |
| Audit Logging | ⏳ Phase 1c | Action traceability |

---

## Sub-Phase Split

| Phase | Name | Duration | Deliverable |
|-------|------|----------|-------------|
| **2a** | Personal Wallets Backend + UI | 2-3 days | CRUD for personal wallets, balance tracking |
| **2b** | Shared Wallets + Invitations Backend + UI | 3-4 days | Shared wallet creation, member invitations, access control |
| **2c** | Debt Calculation + Settlements Backend + UI | 2-3 days | Automatic debt calculation, settlement recording, Splitwise-style balances |

**Total estimated duration**: 7-10 days

---

## Scope

### ✅ Included

**Personal Wallets** (2a):
- Create, read, update, archive personal wallets
- Balance tracking (current, historical, projected)
- Wallet metadata (name, currency, balance)
- Wallet list with filtering

**Shared Wallets** (2b):
- Create shared wallets
- Invitation system (generate, send, accept, expire, revoke)
- Member management (add, remove, list)
- Equal rights for all members (no owner/admin)

**Collaboration** (2c):
- Expense split types (Equal, Percentage, Custom)
- Automatic debt calculation from transactions
- Debt simplification (minimize number of debts)
- Settlement recording (external payments)
- Debt visibility for all members

### ❌ Excluded

- Multi-currency wallets — single currency MVP
- Wallet sharing with non-users — invitations require account
- Import/export for wallets — post-MVP

---

## Feature Structure

**Location**: `src/Kakeibo.Api/Features/Wallets/`
**Schema**: `public` (single schema, shared with all domains)
**Pattern**: Vertical slices within single project

**Key Entities**:
- `Wallet` (aggregate root, personal + shared)
- `WalletMember` (shared wallet membership)
- `Invitation` (access grant)
- `TransactionSplit` (expense division config — in Transactions domain)
- `Settlement` (external payment)

> **Note:** `Debt` is **not** a persisted EF Core entity. It is a calculated runtime DTO returned by `DebtCalculationService.CalculateDebts(walletId)`, which reads `TransactionSplit` records and computes net balances in memory using the Splitwise algorithm.

**Endpoints**:
- `POST /api/wallets` — Create wallet
- `GET /api/wallets` — List wallets
- `GET /api/wallets/{id}` — Get wallet
- `PUT /api/wallets/{id}` — Update wallet
- `DELETE /api/wallets/{id}` — Archive wallet
- `POST /api/wallets/{id}/invite` — Create invitation
- `POST /api/wallets/invitations/{code}/accept` — Accept invitation
- `GET /api/wallets/{id}/members` — List members
- `GET /api/wallets/{id}/debts` — Get debts
- `POST /api/wallets/{id}/settlements` — Record settlement

**Integration Events**:
- `WalletCreatedEvent`
- `WalletArchivedEvent`
- `InvitationSentEvent`
- `InvitationAcceptedEvent`
- `MemberJoinedEvent`
- `MemberLeftEvent`
- `SettlementRecordedEvent`

---

## MVP Acceptance Criteria

### Phase 2a — Personal Wallets
- [ ] Create personal wallet (initial balance is set via a transaction in Phase 3b)
- [ ] List user's personal wallets
- [ ] Update wallet name and metadata
- [ ] Archive wallet (soft delete)
- [ ] Frontend: wallet list screen
- [ ] Frontend: create wallet form
- [ ] Frontend: edit wallet form

### Phase 2b — Shared Wallets + Invitations
- [ ] Create shared wallet
- [ ] Generate invitation with expiration
- [ ] Send invitation email
- [ ] Accept invitation (user joins wallet)
- [ ] List wallet members
- [ ] All members have equal rights
- [ ] Frontend: shared wallet creation
- [ ] Frontend: invitation flow
- [ ] Frontend: member list

### Phase 2c — Debt Calculation + Settlements
- [ ] Record settlement between members
- [ ] Debt calculation from transaction splits
- [ ] Debt simplification (minimize debts)
- [ ] All members see same debt state
- [ ] Frontend: debt view
- [ ] Frontend: settlement recording

---

## Definition of "Phase 2 Completed"

1. All three sub-phases (2a, 2b, 2c) complete
2. Personal and shared wallets functional
3. Invitation system operational
4. Debt calculation accurate (runtime DTO, not persisted entity)
5. All 21 acceptance criteria checked
6. CI pipeline green
7. Manual testing complete
8. Phase 3 can begin (Transactions depend on Wallets)

---

**Next Phase**: Phase 3 — Transactions & Categories
