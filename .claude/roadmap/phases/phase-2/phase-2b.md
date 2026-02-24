# Phase 2b: Shared Wallets + Invitations Backend + UI

**Status**: Not Started
**Objective**: Implement shared wallet creation, invitations, and member management

---

## Scope

### ✅ Included
- Create shared wallets
- Invitation system (generate, send, accept, expire, revoke)
- Member management (add, remove, list)
- Equal rights for all members
- Frontend: shared wallet creation, invitation flow, member list

### ❌ Excluded
- Debt calculation — Phase 2c
- Transaction splits — Phase 2c (absorbed from Phase 3c)

---

## Deliverables

### Backend
**`Kakeibo.Api/Features/Wallets/`**:
- InviteToWallet, AcceptInvitation, GetWalletMembers, RemoveMember

**Endpoints**:
- `POST /api/wallets/{id}/invite`
- `POST /api/wallets/invitations/{code}/accept`
- `GET /api/wallets/{id}/members`
- `DELETE /api/wallets/{id}/members/{userId}`

### Frontend
**`sites/Kakeibo.App/views/wallets/`**:
- SharedWalletView.vue, InviteMemberView.vue

**`sites/Kakeibo.App/components/wallets/`**:
- InvitationForm.vue, MemberList.vue, MemberCard.vue

---

## Acceptance Criteria

- [ ] Create shared wallet
- [ ] Generate invitation with 7-day expiration
- [ ] Send invitation email
- [ ] Accept invitation (user joins wallet)
- [ ] List wallet members
- [ ] All members have equal rights
- [ ] Frontend: shared wallet creation
- [ ] Frontend: invitation flow
- [ ] Frontend: member list
- [ ] Integration test: invitation flow
- [ ] E2E test: create shared wallet → invite → accept

---

## Definition of "Phase 2b Completed"

1. Shared wallets functional
2. Invitation system operational
3. All 11 acceptance criteria checked
4. Phase 2c can begin
