# Phase 3a: Categories Backend + UI

**Status**: Not Started
**Objective**: Implement category system (12 system + unlimited custom)

---

## Scope

### ✅ Included
- 12 system categories (non-deletable)
- Unlimited custom categories per user
- Category CRUD (create, rename, archive)
- Category seeding via `IOnboardingSeeder`
- Frontend: category selector, management UI

### ❌ Excluded
- Category hierarchies (parent/child) — post-MVP
- Category icons — use default colors only

---

## System Categories

1. Housing
2. Transportation
3. Food & Dining
4. Health & Wellness
5. Entertainment & Leisure
6. Shopping & Personal
7. Education
8. Subscriptions & Bills
9. Savings & Investments
10. Debt & Loans
11. Gifts & Donations
12. Other

---

## Deliverables

### Backend
**Kakeibo.Modules.Transactions/**:
- Entities/Category.cs
- Features/CreateCategory, ListCategories, UpdateCategory, ArchiveCategory
- Seeders/SystemCategoriesSeeder.cs

**Endpoints**:
- `GET /api/categories`
- `POST /api/categories`
- `PUT /api/categories/{id}`
- `DELETE /api/categories/{id}`

### Frontend
**sites/Kakeibo.App/src/components/categories/**:
- CategorySelector.vue, CategoryList.vue, CategoryForm.vue

---

## Acceptance Criteria

- [ ] 12 system categories seeded
- [ ] Create custom category
- [ ] List categories (system + custom)
- [ ] Archive custom category (system categories protected)
- [ ] Frontend: category selector
- [ ] Frontend: custom category management
- [ ] Integration test: category CRUD

---

## Definition of "Phase 3a Completed"

1. Category system functional
2. All 7 acceptance criteria checked
3. Phase 3b can begin
