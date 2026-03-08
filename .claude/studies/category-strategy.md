# Category Strategy Study

## Problem Statement

Kakeibo has two tiers of categories:
- **12 system categories** — built-in, non-deletable, visible to all users
- **Custom categories** — user-created, unlimited, personal

The challenge: when users share wallets, they have different custom category sets.
A transaction in a shared wallet needs exactly one category — but whose categories
are used? This study compares strategies and recommends the simplest approach.

---

## Strategy Comparison

### Strategy 1: User-owned pool (current implicit model)

**How it works:**
- Each user has their own pool: 12 system + their own custom categories
- Shared wallet transactions use the category from whoever recorded the transaction
- Other members see the category name — even if it's not in their own pool

**DB impact:**
- Categories are already scoped by `UserId IS NULL` (system) or `UserId = X` (custom)
- No changes needed to the model
- When member B records a transaction in a shared wallet using B's custom category,
  member A sees the name but doesn't have that category in their filter dropdowns

**Pros:**
- Already how the system works today
- Simple model, no coordination needed
- Each user controls their own naming

**Cons:**
- Member A can't filter by or budget for a category they didn't create
- Over time, shared wallet transactions use inconsistent categories across members
- Budgets on shared wallets become unreliable if members use different category names

---

### Strategy 2: Category per wallet

**How it works:**
- Each wallet has its own category set
- Creating a wallet creates a copy of the system categories for that wallet
- Personal wallets: owner adds custom categories per-wallet
- Shared wallets: any member can add categories, all members see the same set

**DB impact:**
- Add `WalletId` nullable FK to `Category`
- Category is owned by a wallet OR by a user (mutually exclusive)
- Need to show the merged set (system + wallet categories) when recording

**Pros:**
- Shared wallets have a single consistent category vocabulary

**Cons:**
- High complexity: categories per wallet multiply quickly
- User must manage categories in each wallet separately
- Cross-wallet reports are harder (same concept, different category record per wallet)
- Adds significant UI complexity

---

### Strategy 3 (RECOMMENDED): Shared pool for friends

**How it works:**
- System categories are global (unchanged)
- Custom categories remain user-owned
- When recording a transaction in a **shared wallet**, all member categories are merged
  in the UI dropdown — the picker shows "your categories + the other members' categories"
- The saved `CategoryId` FK points to whichever category was selected (from any member's pool)
- In the shared wallet view, everyone sees all categories actually used (by name)
- There is no ownership enforcement on which category a transaction uses

**DB impact:**
- Zero schema changes
- No migration needed
- Category query for shared wallets: fetch categories belonging to any wallet member

**Pros:**
- Zero schema changes — works with the existing model
- Simplest possible UX: the dropdown in a shared wallet shows all members' categories
- Users implicitly coordinate by naming their categories consistently
- System categories provide the default vocabulary, reducing need for custom ones
- Works well for typical 2-person households where naming overlap is high

**Cons:**
- Two members can have separate "Food" categories that show as duplicates in the picker
- No deduplication across members — user discipline needed

**How to handle the duplicate concern:**
- In the picker, show categories grouped: "Shared categories" (system) first,
  then "Your categories", then "Wallet member categories"
- Highlight when two members have same-named categories so users can consolidate

---

### Strategy 4: Unified pool per collaboration (copy-on-share)

**How it works:**
- When two users first share a wallet, the system merges their custom categories
  into a shared pool (deduplication by name)
- The shared pool is owned by the friendship/collaboration, not by either user
- Subsequent custom categories added inside the shared wallet are shared-pool categories

**DB impact:**
- New entity: `CategoryPool` with optional `FriendshipId` or `WalletId`
- Complex migration: backfill pools for existing shared wallets
- Categories gain `PoolId` nullable FK

**Pros:**
- Clean separation: personal categories vs. shared categories
- Eliminates the duplicate picker problem

**Cons:**
- Very high complexity
- Confusing for users: "where did my category go?" after merging
- Overkill for MVP — most shared wallets are between 2 people who quickly align naming

---

## Recommendation: Strategy 3

**Implement "shared pool for friends" as a UI convention, not a schema change.**

### Why Strategy 3 wins:

1. **Zero migration** — works today with no DB or backend changes
2. **Simplest UX** — users just see more categories in the picker when in a shared wallet
3. **Flexible** — two members can have similar-named categories; the system doesn't force merging
4. **Progressive** — if naming conflicts become a problem, Strategy 4 can be layered on top
   without breaking existing data

### Implementation steps (no DB migration needed):

**Backend:** Update `ListCategoriesHandler` to accept an optional `walletId` parameter.
When provided and the wallet is shared, return categories from all wallet members instead
of just the authenticated user's categories.

```
GET /api/categories?walletId=<id>
```

The handler adds to the `WHERE` clause:
```sql
WHERE category.user_id IS NULL                   -- system categories
   OR category.user_id = :callerId               -- caller's custom categories
   OR category.user_id IN (                      -- co-members' categories
       SELECT user_id FROM wallet_members
       WHERE wallet_id = :walletId
   )
```

**Frontend:** The transaction recording form already passes `walletId` — pass it to the
category fetch so the dropdown auto-expands to include co-member categories.

**UX grouping** (optional enhancement):
- Group "System" categories first
- Then "Your categories"
- Then "Team categories" (from other members)

### Impact on Budgets & Reports

With Strategy 3, a budget set to "Food & Dining" (system category) works across all
members of a shared wallet because everyone sees and can use the system categories.
Custom categories in budgets only work if the budget owner created those categories.

This is acceptable for MVP — power users who want cross-member budget tracking should
use system categories for shared wallet expenses.

---

## Decision Record

| Decision | Choice | Reason |
|----------|--------|--------|
| Schema changes | None | Strategy 3 needs no migration |
| Category picker in shared wallet | Show all member categories | Merged by `walletId` query param |
| System categories | Always shown | Global, non-deletable, deduplication anchor |
| Budget categories | Owner's categories only | Acceptable MVP constraint |
| Future path | Strategy 4 if needed | Can add shared pools without breaking Strategy 3 |
