# Phase 8b: Onboarding Flow

**Status**: Not Started
**Objective**: Implement first-time user onboarding experience

---

## Scope

### ✅ Included
- Welcome screen (intro to Kakeibo philosophy)
- Guided tour (wallets, transactions, budgets, goals)
- First wallet creation wizard
- Skip option (return to dashboard)
- Frontend: multi-step wizard, progress indicator

### ❌ Excluded
- Interactive tutorial — post-MVP
- Video walkthrough — post-MVP

---

## Deliverables

### Frontend
**src/Kakeibo.App/views/onboarding/**:
- OnboardingView.vue

**src/Kakeibo.App/components/onboarding/**:
- WelcomeStep.vue, WalletSetupStep.vue, TourStep.vue, ProgressIndicator.vue

---

## Acceptance Criteria

- [ ] Welcome screen shows on first login
- [ ] Guided tour explains key concepts
- [ ] First wallet creation wizard
- [ ] Skip option available
- [ ] Progress indicator shows steps
- [ ] E2E test: complete onboarding flow

---

## Definition of "Phase 8b Completed"

1. Onboarding flow functional
2. All 6 acceptance criteria checked
3. Phase 8c can begin
