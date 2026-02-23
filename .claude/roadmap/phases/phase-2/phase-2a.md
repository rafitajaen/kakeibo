# Phase 2a: Personal Wallets Backend + UI

**Status**: Not Started
**Objective**: Implement CRUD for personal wallets with balance tracking

---

## Scope

### ✅ Included
- Create, read, update, archive personal wallets
- Balance tracking (current, historical, projected)
- Wallet metadata (name, currency, balance)
- Wallet list with filtering
- Frontend: wallet list, create/edit forms

### ❌ Excluded
- Shared wallets — Phase 2b
- Transaction recording — Phase 3

---

## Deliverables

### Backend
**Kakeibo.Modules.Wallets/Features/**:
- CreateWallet, GetWallet, ListWallets, UpdateWallet, ArchiveWallet

**Endpoints**:
- `POST /api/wallets`
- `GET /api/wallets`
- `GET /api/wallets/{id}`
- `PUT /api/wallets/{id}`
- `DELETE /api/wallets/{id}`

### Frontend
**sites/Kakeibo.App/src/views/wallets/**:
- WalletsView.vue, CreateWalletView.vue, EditWalletView.vue

**sites/Kakeibo.App/src/components/wallets/**:
- WalletCard.vue, WalletForm.vue, WalletList.vue

---

## Acceptance Criteria

- [ ] Create personal wallet with initial balance
- [ ] List user's personal wallets
- [ ] Update wallet name and metadata
- [ ] Archive wallet (soft delete via `DeletedAt`)
- [ ] Balance tracking accurate
- [ ] Frontend: wallet list screen
- [ ] Frontend: create wallet form
- [ ] Frontend: edit wallet form
- [ ] Integration test: CRUD flows
- [ ] E2E test: create → edit → archive wallet

---

## Definition of "Phase 2a Completed"

1. All wallet CRUD functional
2. All 10 acceptance criteria checked
3. Tests pass
4. Phase 2b can begin
