# API Structure Study

> Research document analyzing the current state of `src/Kakeibo.Api/Features/` and
> `src/Kakeibo.Api/Domain/`, identifying inconsistencies, and proposing actionable
> structural improvements. No code changes are made here — this is a planning artifact.

**Date:** 2026-03-08
**Scope:** Simple Monolith · Vertical Slices · Screaming Architecture · light DDD
**Risk tolerance:** Only changes that do not break the API contract or require DB migrations.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current Domain Map](#2-current-domain-map)
3. [Vertical Slice Compliance](#3-vertical-slice-compliance)
4. [Screaming Architecture Compliance](#4-screaming-architecture-compliance)
5. [DDD Alignment](#5-ddd-alignment)
6. [Inconsistencies Catalog](#6-inconsistencies-catalog)
7. [Recommendations](#7-recommendations)
8. [Proposed New Structure](#8-proposed-new-structure)
9. [Migration Strategy](#9-migration-strategy)

---

## 1. Executive Summary

The Kakeibo API is a well-organized Simple Monolith that generally adheres to its own
architectural principles. The Vertical Slice pattern is consistently applied: every operation
lives in its own folder with up to three files (Endpoint, Handler, Validator). The Screaming
Architecture principle holds — the folder tree communicates business domains immediately.

**What works well:**
- 99 operation folders spread across 10 domains, each self-contained
- Events consistently placed in `{Domain}/Events/` subfolders
- Single `AppDbContext`, single schema, single migration history
- `WalletAccessChecker` static utility as reusable access-control primitive
- Event records auto-initialize `Id` and `OccurredAt` via property defaults
- All I/O is async with `CancellationToken` propagation

**What needs improvement (risk-ranked):**
- **User context extraction is split across two incompatible patterns** — `ClaimsPrincipal`
  parse vs `[FromHeader(Name = "X-User-Id")]` — with no documented rationale for when to
  use each. This is the most impactful inconsistency.
- **All 20 entities share one flat `Domain/Entities/` folder** — correct for now, but will
  become noisy as the project grows.
- **Logging gaps** — Only 13 `*Logs.cs` files. Most feature handlers have no operational
  logging, making production debugging harder.
- **`Categories` is the only sub-domain nested inside a parent domain folder** — this is an
  intentional design decision but should be documented as such to avoid cargo-cult repetition.
- **`IResult` used everywhere instead of `Results<T1,T2>`** — acceptable for now but
  OpenAPI schema inference is weaker without the strongly-typed variant.

**Overall risk:** Low. Most issues are cosmetic or additive (new files, new logs). No DB
migrations required. No API contract changes.

---

## 2. Current Domain Map

| Domain | Ops | Entities | Events published | `*Logs.cs` files | Notes |
|--------|-----|----------|-----------------|-------------------|-------|
| Identity | 24 | User, RefreshToken, PasswordResetToken | 3 | 3 | Largest domain; contains Jobs/ subfolder |
| Wallets | 17 | Wallet, WalletMember, WalletBalance, Invitation, Settlement | 7 | 1 | Only domain with an `AccessChecker` utility |
| Transactions | 11+4 | Transaction, TransactionAttachment | 3 | 1 | Only domain with a sub-domain (`Categories/`) |
| Friends | 12 | FriendRequest, Friendship | 4 | 1 | Full lifecycle management |
| Goals | 6 | Goal | 2 | 0 | No logging at all |
| Budgets | 6 | Budget | 2 | 0 | No logging at all |
| Recurring | 7 | RecurringPattern | 1 | 1 | Contains Jobs/ subfolder |
| Notifications | 6 | Notification, NotificationPreferences, PushSubscription | 0 | 1 | Events consumed here, not published |
| Admin | 8 | PlatformPolicy | 0 | 0 | No events, no logging |
| Auditing | 2 | — (uses ClickHouse) | 0 | 1 | Events consumed here, not published |

**Totals:** 99 operation folders · 20 entities · 22 event files · 13 `*Logs.cs` files

---

## 3. Vertical Slice Compliance

The canonical 3-file pattern per operation:

```
{Op}Endpoint.cs   — IEndpoint, nested Request/Response records, route mapping
{Op}Handler.cs    — Plain class, HandleAsync, business logic
{Op}Validator.cs  — AbstractValidator<{Op}Request>, FluentValidation rules (optional)
```

### Compliance audit per domain

| Domain | Fully compliant | Notes |
|--------|-----------------|-------|
| Identity | ✅ | Some ops have no validator (e.g., DeleteAccount has no request body) |
| Wallets | ✅ | Adds `WalletAccessChecker.cs` at domain level — well-motivated shared utility |
| Transactions | ✅ | `Categories/` sub-domain adds one level of nesting |
| Friends | ✅ | `FriendshipLogs.cs` at domain level (not per-op) — acceptable |
| Goals | ✅ | |
| Budgets | ✅ | |
| Recurring | ✅ | `Jobs/` subfolder at domain level — same as Identity |
| Notifications | ✅ | |
| Admin | ✅ | |
| Auditing | ⚠️ | Only 1 op (`GetActivityFeed`). The Events/ folder handles event consumption. Minimal by design. |

**Finding:** Vertical Slice compliance is high across all domains. Deviations are motivated
(no request body → no validator; shared utility → domain-level file).

### Minor gaps

1. **`ArchiveCategory` has no validator** — The operation changes category state without
   validating inputs (only a route param `{id}`). Acceptable since route param validation
   is handled by the framework, but document the decision.

2. **`GetActivityFeed` has no validator** — Query params like `page`, `pageSize`, `from`, `to`
   are not validated. Should add a validator to enforce pagination limits.

---

## 4. Screaming Architecture Compliance

> "The folder names should scream the business domain, not the technical layer."
> — Uncle Bob, *Clean Architecture*

### Assessment

The current tree does scream the business:

```
Features/
├── Identity/        ← "Who you are"
├── Wallets/         ← "Where your money lives"
├── Transactions/    ← "What you did with it"
├── Categories/      ← "How you classify it" (nested under Transactions — debatable)
├── Budgets/         ← "What you planned to spend"
├── Goals/           ← "What you are saving toward"
├── Recurring/       ← "What happens automatically"
├── Notifications/   ← "What alerts you"
├── Friends/         ← "Who you share with"
├── Admin/           ← "Who manages the platform"
└── Auditing/        ← "What was recorded"
```

**Verdict: ✅ Compliant.** The domain names map 1:1 to business concepts from `platform.md`.

### Edge cases

1. **`Friends/` domain exists but is not in the canonical platform spec** — `platform.md`
   describes a 8-module platform. Friends is a social layer added in later phases. The domain
   name is clear but the folder-level placement may not be obvious without reading the roadmap.
   **Recommendation:** Add a brief comment at the top of `FriendshipLogs.cs` or a `README.md`
   explaining the domain's relationship to the Collaboration model.

2. **`Admin/` is a cross-cutting domain** — Platform management lives alongside business
   domains. This is pragmatic but architecturally it is a different category. Current placement
   is fine for a single-developer MVP.

3. **`Auditing/` is almost empty** — One operation folder + one Events consumer. The domain
   exists for structural consistency, but its thinness may cause confusion. Acceptable.

---

## 5. DDD Alignment

### What is present

| DDD Building Block | Status | Notes |
|--------------------|--------|-------|
| Entities | ✅ Present | 20 entities inheriting `Entity` base class |
| Value Objects | ⚠️ Partial | `ValueObjects/` folder exists but is empty. No VOs implemented. |
| Domain Events | ✅ Present | 22 events across 6 domains; consistent `IEvent` contract |
| Bounded Contexts | ✅ Folder-based | 10 domains, each self-contained by folder |
| Ubiquitous Language | ✅ Present | Class names match business vocabulary from `platform.md` |
| Soft Delete | ✅ Present | `DeletedAt`/`IsDeleted` pattern on `Entity` base class |
| Aggregate Roots | ⚠️ Implicit | `Wallet`, `User`, `Transaction` behave as ARs but are not annotated |
| Repositories | ✅ Via EF Core | `AppDbContext` serves as UoW + implicit repositories |
| Domain Services | ❌ Absent | Business logic lives in handlers, not dedicated domain services |
| Factory Methods | ❌ Absent | Entity construction is ad-hoc (`new Wallet { ... }`) |
| Anti-Corruption Layers | N/A | Single deployment; no external domain boundaries |

### What is deliberately omitted (pragmatic DDD)

The project is a single-developer MVP. Full DDD ceremony (explicit ARs, domain services,
factory methods, repository interfaces over EF Core) would add complexity without proportional
benefit. The current approach is **pragmatic DDD**: use the vocabulary, use events, use
bounded contexts, but skip the overhead patterns.

**This is correct for the project's stage.** The study notes the gaps not as defects but as
known trade-offs to revisit post-MVP.

### Aggregate Root analysis

The three clear Aggregate Roots are:

| Entity | Children | Why AR |
|--------|----------|--------|
| `Wallet` | `WalletMember`, `Invitation`, `Settlement`, `WalletBalance` | All child operations go through wallet access check |
| `User` | `RefreshToken`, `PasswordResetToken`, `PushSubscription`, `NotificationPreferences` | Identity lifecycle owned by user |
| `Transaction` | `TransactionAttachment` | Attachments only exist in context of a transaction |

**No explicit AR enforcement** exists. Handlers can modify child entities directly via
`AppDbContext` without going through the AR. This is acceptable for MVP but is a potential
consistency risk if the codebase grows.

---

## 6. Inconsistencies Catalog

### CRITICAL

#### IC-001: Two incompatible user-context extraction patterns

**Severity:** CRITICAL (breaks developer mental model; causes subtle bugs)

**Pattern A — `ClaimsPrincipal` manual parse (~60% of endpoints):**
```csharp
private static async Task<IResult> HandleAsync(
    ClaimsPrincipal principal,
    CreateWalletHandler handler,
    CancellationToken ct)
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return TypedResults.Unauthorized();

    var result = await handler.HandleAsync(request, userId, ct);
    // ...
}
```

**Pattern B — `[FromHeader(Name = "X-User-Id")]` direct binding (~35% of endpoints):**
```csharp
private static async Task<IResult> HandleAsync(
    ListBudgetsHandler handler,
    [FromHeader(Name = "X-User-Id")] Guid userId,
    CancellationToken ct)
{
    var result = await handler.HandleAsync(userId, ct);
    // ...
}
```

**Pattern C — `HttpContext` full access (~5% of endpoints):**
```csharp
private static async Task<IResult> HandleAsync(
    HttpContext httpContext,
    GetCurrentUserHandler handler,
    CancellationToken ct)
```

**Problem:** The `X-User-Id` header approach is fragile — it relies on a header that is NOT
part of the HTTP/JWT standard. If a caller omits it or the value is wrong, the framework
returns 400 (not 401). Worse, there is no guarantee the header value matches the authenticated
user's actual identity. This is a latent security risk.

**Root cause:** Pattern B was likely introduced for convenience (avoid manual parse boilerplate)
but bypasses the JWT claim chain.

**Recommendation:** Standardize on Pattern A (ClaimsPrincipal) for all authenticated endpoints.
Extract into a shared extension method to eliminate the boilerplate:

```csharp
// Common/Extensions/ClaimsPrincipalExtensions.cs
internal static class ClaimsPrincipalExtensions
{
    // Tries to extract the user ID from the NameIdentifier claim.
    internal static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
```

Usage:
```csharp
if (!principal.TryGetUserId(out var userId))
    return TypedResults.Unauthorized();
```

---

### WARNING

#### IC-002: Logging coverage is uneven

**Severity:** WARNING (production debugging gap)

Domains with zero `*Logs.cs` files: **Goals, Budgets, Admin**.

Goals and Budgets are business-critical: budget exceeded, goal milestone reached. Without
operational logging in the handlers, diagnosing production issues (wrong calculation, missed
event) requires adding logs post-incident.

**Recommendation:** Add at minimum one `*Logs.cs` file per domain that contains operational
events (budget calculated, goal updated, admin action taken). Not blocking but should be
addressed before production.

---

#### IC-003: `Categories` sub-domain nesting is undocumented and unique

**Severity:** WARNING (confusion risk for contributors)

`Categories` is the **only** sub-domain nested inside a parent domain:

```
Features/
├── Transactions/
│   ├── Categories/           ← nested sub-domain
│   │   ├── ArchiveCategory/
│   │   ├── CreateCategory/
│   │   ├── ListCategories/
│   │   └── UpdateCategory/
│   ├── RecordTransaction/
│   └── ...
```

All other bounded additions (Friends, Admin) are top-level domains. The nesting is correct
(Categories only exist to classify Transactions) but is an exception to the pattern.

**Recommendation:** Document this as an intentional architectural decision — either in a
comment at the top of `Features/Transactions/Categories/` or in `architecture.md`. Do NOT
elevate `Categories` to a top-level domain; the current placement correctly reflects the
domain model.

---

#### IC-004: `Domain/Entities/` is a flat folder with no domain grouping

**Severity:** WARNING (scalability concern)

All 20 entities share one flat folder:

```
Domain/Entities/
├── Budget.cs
├── Category.cs
├── FriendRequest.cs
├── Friendship.cs
├── Goal.cs
├── Invitation.cs
├── Notification.cs
├── NotificationPreferences.cs
├── PasswordResetToken.cs
├── PlatformPolicy.cs
├── PushSubscription.cs
├── RecurringPattern.cs
├── RefreshToken.cs
├── Settlement.cs
├── Transaction.cs
├── TransactionAttachment.cs
├── User.cs
├── Wallet.cs
├── WalletBalance.cs
└── WalletMember.cs
```

At 20 entities, this is still navigable. At 30+ it becomes a friction point. Grouping by
domain would mirror the `Features/` structure.

**Alternative (low-risk):** Keep the flat structure but accept it as a known trade-off.
Since all entities share a single `AppDbContext` and are lightweight EF Core models, the
flat structure is not broken — just suboptimal for navigation.

**Recommendation (if restructuring):**

```
Domain/Entities/
├── Identity/
│   ├── User.cs
│   ├── RefreshToken.cs
│   └── PasswordResetToken.cs
├── Wallets/
│   ├── Wallet.cs
│   ├── WalletMember.cs
│   ├── WalletBalance.cs
│   ├── Invitation.cs
│   └── Settlement.cs
├── Transactions/
│   ├── Transaction.cs
│   ├── TransactionAttachment.cs
│   └── Category.cs
├── Social/                       ← Friends + Notifications grouped
│   ├── FriendRequest.cs
│   ├── Friendship.cs
│   ├── Notification.cs
│   ├── NotificationPreferences.cs
│   └── PushSubscription.cs
├── Planning/                     ← Budgets + Goals + Recurring
│   ├── Budget.cs
│   ├── Goal.cs
│   └── RecurringPattern.cs
└── Platform/                     ← Admin
    └── PlatformPolicy.cs
```

**Migration risk:** Low — only namespace changes. All using statements must be updated,
plus `Persistence/Configurations/` references. No DB migration required.

---

#### IC-005: `GetActivityFeed` query params are not validated

**Severity:** WARNING (data integrity gap)

`GetActivityFeedEndpoint` accepts `page`, `pageSize`, `from`, `to` query parameters but has
no `GetActivityFeedValidator` to enforce business constraints (e.g., pageSize ≤ 100,
date range ≤ 90 days). Malformed inputs would reach the handler and possibly generate
expensive queries.

**Recommendation:** Add `GetActivityFeedValidator.cs` to `Features/Auditing/GetActivityFeed/`.

---

### INFO

#### IC-006: `IResult` vs `Results<T1,T2>` — no strongly-typed return

**Severity:** INFO (OpenAPI schema quality)

All endpoints return `Task<IResult>`. The strongly-typed `Results<Ok<T>, NotFound<Error>>`
variant would improve OpenAPI schema inference (Scalar shows concrete response types per
status code).

**Current:**
```csharp
private static async Task<IResult> HandleAsync(...)
```

**Better:**
```csharp
private static async Task<Results<Ok<ListWalletsResponse>, UnauthorizedHttpResult>> HandleAsync(...)
```

**Trade-off:** More verbose signatures, more boilerplate for error paths. Acceptable to defer
until a dedicated OpenAPI improvement phase.

---

#### IC-007: No `AccessChecker` equivalent for Friends or Admin

**Severity:** INFO (pattern inconsistency)

`WalletAccessChecker` encapsulates membership checks cleanly. Friends operations (e.g.,
"can user A see user B's profile?") and Admin operations (e.g., "is this user an admin?")
repeat similar checks inline in each handler.

**Recommendation:** Add domain-level utilities:
- `Features/Friends/FriendshipChecker.cs` — `AreFriendsAsync(db, userId, targetId, ct)`
- `Features/Admin/AdminChecker.cs` — `IsAdminAsync(db, userId, ct)` (or check role on User entity)

---

#### IC-008: Event records use `Guid.NewGuid()` instead of `Guid7.NewGuid()`

**Severity:** INFO (minor semantic inconsistency)

Event `Id` fields use `Guid.NewGuid()`:
```csharp
public Guid Id { get; init; } = Guid.NewGuid();
```

The `CLAUDE.md` rule says "Use `Guid7.NewGuid()` for entity IDs. Regular `Guid` allowed
elsewhere." Events are not entities (not persisted to PostgreSQL), so `Guid.NewGuid()` is
technically correct per the rule. This is documented as INFO to confirm the decision is
intentional, not an oversight.

**No change needed.** Rationale: Event IDs are used for correlation/tracing only, not as
database primary keys. Time-ordered sorting is irrelevant for in-memory events.

---

## 7. Recommendations

Ordered from highest value to lowest, within each risk tier.

### Tier 1 — Standardize (no file restructuring)

| # | Change | Impact | Risk |
|---|--------|--------|------|
| R-01 | Add `ClaimsPrincipalExtensions.TryGetUserId` and migrate all `[FromHeader]` endpoints to use it | Closes IC-001; security + consistency | Low |
| R-02 | Add `GetActivityFeedValidator.cs` | Closes IC-005; prevents expensive unbounded queries | Low |
| R-03 | Add `*Logs.cs` for Goals, Budgets, Admin | Closes IC-002; improves production observability | Low (additive) |
| R-04 | Add `FriendshipChecker.cs` and `AdminChecker.cs` | Closes IC-007; DRYs up access-check logic | Low (additive) |

### Tier 2 — Document (no code changes)

| # | Change | Impact | Risk |
|---|--------|--------|------|
| R-05 | Document `Categories/` nesting decision in `architecture.md` | Closes IC-003; prevents wrong cargo-cult repetition | Zero |
| R-06 | Document event `Guid.NewGuid()` rationale in `knowledge.md` | Closes IC-008; removes reader confusion | Zero |
| R-07 | Document the `Friends/` domain's relationship to Collaboration in `platform.md` | Aligns docs with code | Zero |

### Tier 3 — Restructure (requires namespace updates)

| # | Change | Impact | Risk |
|---|--------|--------|------|
| R-08 | Group `Domain/Entities/` by domain subfolder | Closes IC-004; better navigation | Medium (many using statements) |
| R-09 | Migrate endpoints to `Results<T1,T2>` return type | Closes IC-006; better OpenAPI schema | Medium (every endpoint file) |

---

## 8. Proposed New Structure

Changes vs current state are marked with `[NEW]` or `[CHANGED]`.

```
src/Kakeibo.Api/
├── Common/
│   ├── Abstractions/           — Entity, Result<T>, Error, ValueObject
│   ├── Endpoints/              — IEndpoint, ValidationFilter, EndpointExtensions
│   └── Utils/
│       ├── CharSets.cs
│       ├── DefaultSerializer.cs
│       ├── Guid7.cs
│       ├── PasswordHasher.cs
│       └── RandomString.cs
├── Common/
│   └── Extensions/
│       └── ClaimsPrincipalExtensions.cs   [NEW] — TryGetUserId() extension
│
├── Domain/
│   └── Entities/
│       ├── Identity/              [CHANGED] — grouped by domain
│       │   ├── User.cs
│       │   ├── RefreshToken.cs
│       │   └── PasswordResetToken.cs
│       ├── Wallets/               [CHANGED]
│       │   ├── Wallet.cs
│       │   ├── WalletMember.cs
│       │   ├── WalletBalance.cs
│       │   ├── Invitation.cs
│       │   └── Settlement.cs
│       ├── Transactions/          [CHANGED]
│       │   ├── Transaction.cs
│       │   ├── TransactionAttachment.cs
│       │   └── Category.cs
│       ├── Social/                [CHANGED]
│       │   ├── FriendRequest.cs
│       │   ├── Friendship.cs
│       │   ├── Notification.cs
│       │   ├── NotificationPreferences.cs
│       │   └── PushSubscription.cs
│       ├── Planning/              [CHANGED]
│       │   ├── Budget.cs
│       │   ├── Goal.cs
│       │   └── RecurringPattern.cs
│       └── Platform/              [CHANGED]
│           └── PlatformPolicy.cs
│
└── Features/
    ├── Admin/
    │   ├── AdminChecker.cs            [NEW] — IsAdminAsync() static utility
    │   ├── AdminLogs.cs               [NEW] — admin action logging
    │   ├── BlockUser/
    │   ├── CreateAdminUser/
    │   ├── DeleteAdminUser/
    │   ├── GetPlatformSettings/
    │   ├── ListAdminUsers/
    │   ├── UnblockUser/
    │   ├── UpdateAdminUser/
    │   └── UpdatePlatformSettings/
    │
    ├── Auditing/
    │   ├── Events/
    │   └── GetActivityFeed/
    │       ├── GetActivityFeedEndpoint.cs
    │       ├── GetActivityFeedHandler.cs
    │       └── GetActivityFeedValidator.cs  [NEW] — validate page/pageSize/date range
    │
    ├── Budgets/
    │   ├── BudgetLogs.cs              [NEW] — budget calculation logging
    │   ├── Events/
    │   ├── CreateBudget/
    │   ├── DeleteBudget/
    │   ├── GetBudgetStatus/
    │   ├── ListBudgets/
    │   └── UpdateBudget/
    │
    ├── Friends/
    │   ├── FriendshipChecker.cs       [NEW] — AreFriendsAsync() static utility
    │   ├── FriendshipLogs.cs          (exists)
    │   ├── Events/
    │   ├── AcceptFriendRequest/
    │   ├── CancelFriendRequest/
    │   ├── CheckFriendshipImpact/
    │   ├── DeleteFriendship/
    │   ├── GetUserProfile/
    │   ├── ListFriends/
    │   ├── ListReceivedRequests/
    │   ├── ListSentRequests/
    │   ├── RejectFriendRequest/
    │   ├── SearchUsers/
    │   └── SendFriendRequest/
    │
    ├── Goals/
    │   ├── GoalLogs.cs                [NEW] — goal milestone logging
    │   ├── Events/
    │   ├── CreateGoal/
    │   ├── DeleteGoal/
    │   ├── GetGoalProgress/
    │   ├── ListGoals/
    │   └── UpdateGoal/
    │
    ├── Identity/          — unchanged (already well-structured)
    │
    ├── Notifications/     — unchanged
    │
    ├── Recurring/         — unchanged
    │
    ├── Transactions/
    │   ├── TransactionAttachmentLogs.cs  (exists)
    │   ├── Categories/                   (exists — keep as-is, document the decision)
    │   ├── Events/
    │   ├── DeleteAttachment/
    │   ├── DeleteTransaction/
    │   ├── DownloadAttachment/
    │   ├── GetTransaction/
    │   ├── ListAttachments/
    │   ├── ListTransactions/
    │   ├── RecordTransaction/
    │   ├── UpdateTransaction/
    │   └── UploadAttachment/
    │
    └── Wallets/
        ├── WalletAccessChecker.cs     (exists)
        ├── Events/
        ├── AcceptInvitation/
        ├── ArchiveWallet/
        ├── CreateWallet/
        ├── GetWallet/
        ├── GetWalletMembers/
        ├── InviteToWallet/
        ├── ListPublicWallets/
        ├── ListWallets/
        ├── MakeWalletPrivate/
        ├── RecordSettlement/
        ├── RemoveMember/
        ├── RevokeInvitation/
        ├── TransferWallet/
        ├── UpdateWallet/
        ├── UpdateWalletMemberRole/
        └── UpdateWalletVisibility/
```

---

## 9. Migration Strategy

All items are ordered by risk (zero → low → medium). Each item is independent — they can be
applied in any order or selectively.

### Phase A — Zero Risk (documentation only)

These changes require no code edits, no test runs, no builds.

| Step | Action | File(s) |
|------|--------|---------|
| A-1 | Document the `Categories/` nesting rationale | `.claude/rules/architecture.md` — add a "Sub-domain nesting" section |
| A-2 | Document event `Guid.NewGuid()` as intentional | `.claude/rules/knowledge.md` — add KB-014 |
| A-3 | Document `Friends/` domain relationship | `.claude/rules/platform.md` — add Social Layer note |

### Phase B — Low Risk, Additive (new files only)

These changes add new files without modifying existing ones. Build must pass; no behavior
change.

| Step | Action | New File(s) |
|------|--------|-------------|
| B-1 | Add `ClaimsPrincipalExtensions` | `Common/Extensions/ClaimsPrincipalExtensions.cs` |
| B-2 | Add `GetActivityFeedValidator` | `Features/Auditing/GetActivityFeed/GetActivityFeedValidator.cs` |
| B-3 | Add `BudgetLogs` | `Features/Budgets/BudgetLogs.cs` |
| B-4 | Add `GoalLogs` | `Features/Goals/GoalLogs.cs` |
| B-5 | Add `AdminLogs` | `Features/Admin/AdminLogs.cs` |
| B-6 | Add `FriendshipChecker` | `Features/Friends/FriendshipChecker.cs` |
| B-7 | Add `AdminChecker` | `Features/Admin/AdminChecker.cs` |

### Phase C — Low Risk, Modifications (touch existing files)

These changes modify existing files to standardize behavior. Each item should be done as
its own commit.

| Step | Action | Files affected | Verification |
|------|--------|---------------|--------------|
| C-1 | Migrate all `[FromHeader(Name = "X-User-Id")]` endpoints to `ClaimsPrincipal` + `TryGetUserId()` | ~35% of endpoint files | `bun run api:build && bun run api:test` |
| C-2 | Add `logger.X()` calls in Goals handlers using new `GoalLogs` | `GetGoalProgress`, `CreateGoal`, `UpdateGoal` handlers | `bun run api:build` |
| C-3 | Add `logger.X()` calls in Budgets handlers using new `BudgetLogs` | `GetBudgetStatus`, `CreateBudget` handlers | `bun run api:build` |
| C-4 | Add `logger.X()` calls in Admin handlers using new `AdminLogs` | `BlockUser`, `UnblockUser`, `UpdatePlatformSettings` handlers | `bun run api:build` |

### Phase D — Medium Risk, Restructuring

These changes require namespace updates across many files. Run the full build + test suite
after each step. Tackle only after Phase A–C are complete.

| Step | Action | Risk | Verification |
|------|--------|------|--------------|
| D-1 | Group `Domain/Entities/` by domain subfolders | Medium — all `using` statements in handlers and `Persistence/Configurations/` | `bun run api:build && bun run api:test` |
| D-2 | Migrate endpoints to `Results<T1,T2>` return type | Medium — every endpoint file, careful mapping of all error codes | `bun run api:build && bun run api:test` |

**D-1 order of operations:**
1. Create the new subfolders inside `Domain/Entities/`
2. Move entity files one domain at a time
3. Update namespaces in entity files
4. Update all `using` statements in handler files, configuration files, and `AppDbContext`
5. Run `bun run api:build` — fix compilation errors
6. Run `bun run api:test` — verify no behavioral change
7. Commit per domain group (one commit per entity group)

**D-2 order of operations:**
1. Pick one low-traffic endpoint (e.g., `GetGoalProgress`)
2. Change `Task<IResult>` → `Results<Ok<GetGoalProgressResponse>, NotFound<Error>, UnauthorizedHttpResult>`
3. Update the return expressions to use `TypedResults.Ok(...)`, `TypedResults.NotFound(...)`
4. Build and verify Scalar renders the new schema correctly
5. If successful, apply to remaining endpoints domain by domain

---

## Appendix: Architectural Decisions to Preserve

These patterns are working correctly and must NOT be changed:

| Pattern | Why it works | File reference |
|---------|-------------|----------------|
| Single `AppDbContext` for all domains | Single schema, single migrations history, no distributed transactions | `Persistence/AppDbContext.cs` |
| `WalletAccessChecker` as static utility | No DI overhead, testable, reusable, single responsibility | `Features/Wallets/WalletAccessChecker.cs` |
| Event auto-initialize `Id`/`OccurredAt` | Eliminates manual plumbing in handler callsites | All `*Event.cs` files |
| `Categories/` nested under `Transactions/` | Categories are a sub-domain of Transactions, not an independent domain | `Features/Transactions/Categories/` |
| Fire-and-forget events before `SaveChangesAsync` | Decouples side effects from the main write path | All handler files using `IEventBus` |
| User context extracted in endpoint, passed to handler | Handlers are stateless and testable; endpoints own HTTP concerns | All `*Endpoint.cs` files |
