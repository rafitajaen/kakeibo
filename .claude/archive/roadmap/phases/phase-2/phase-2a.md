# Phase 2a: Personal Wallets Backend + UI

**Status**: Not Started
**Objective**: Implement CRUD for personal wallets with balance tracking

---

## Scope

### ✅ Included
- Create, read, update, archive personal wallets
- Balance display (read from `AppDbContext.WalletBalances` — `WalletBalance` entity implemented in Phase 3b)
- Wallet metadata (name, currency)
- Wallet list with filtering
- Frontend: wallet list, create/edit forms

> **Architectural note:** The `Wallet` entity does **NOT** store a `Balance` field. Balance is
> maintained by the `WalletBalance` entity in `AppDbContext` and updated atomically with every
> transaction. Wallets feature handlers query `AppDbContext.WalletBalances` directly — no
> cross-module indirection needed in a Simple Monolith. This is implemented in Phase 3b.
>
> **Initial balance:** There is no "initial balance" field on wallet creation. A wallet starts
> with zero balance. Users set an opening balance by recording an income transaction in Phase 3b.

### ❌ Excluded
- Shared wallets — Phase 2b
- Transaction recording — Phase 3

---

## Deliverables

### Backend
**`Kakeibo.Api/Features/Wallets/`**:
- CreateWallet, GetWallet, ListWallets, UpdateWallet, ArchiveWallet

**Endpoints**:
- `POST /api/wallets`
- `GET /api/wallets`
- `GET /api/wallets/{id}`
- `PUT /api/wallets/{id}`
- `DELETE /api/wallets/{id}`

### Frontend
**`src/Kakeibo.App/views/wallets/`**:
- WalletsView.vue, CreateWalletView.vue, EditWalletView.vue

**`src/Kakeibo.App/components/wallets/`**:
- WalletCard.vue, WalletForm.vue, WalletList.vue

---

## Acceptance Criteria

- [ ] Create personal wallet (balance starts at zero; initial balance set via transaction in Phase 3b)
- [ ] List user's personal wallets
- [ ] Update wallet name and metadata
- [ ] Archive wallet (soft delete via `DeletedAt`)
- [ ] Frontend: wallet list screen
- [ ] Frontend: create wallet form
- [ ] Frontend: edit wallet form
- [ ] Integration test: CRUD flows
- [ ] E2E test: create → edit → archive wallet

---

## Definition of "Phase 2a Completed"

1. All wallet CRUD functional
2. All 9 acceptance criteria checked
3. Tests pass
4. Phase 2b can begin
