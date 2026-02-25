# Kakeibo Roadmap

**Mindful money management for personal budgeting and collaborative expenses**

**Version:** 3.0 (Single-Tenant MVP)
**Status:** Phase 8 complete — MVP Ready
**Last Updated:** 2026-02-25

---

## Document Purpose

This roadmap defines the strategic development plan for the Kakeibo platform — a personal finance and shared expense management system inspired by the traditional Japanese household budgeting method. The roadmap organizes work into 8 phases (1–8) with 21 sub-phases across 3–4 months of development.

**For complete context**, see [kakeibo/overview.md](./overview.md).

---

## Table of Contents

1. [Status at Planning](#status-at-planning)
2. [Core Philosophy](#core-philosophy)
3. [Architecture Overview](#architecture-overview)
4. [Domain Dependency Graph](#domain-dependency-graph)
5. [Phase Summaries](#phase-summaries)
6. [Development Order](#development-order)
7. [Parallel Development Opportunities](#parallel-development-opportunities)
8. [MVP Summary Per Phase](#mvp-summary-per-phase)
9. [Strategic Timeline](#strategic-timeline)
10. [Critical Reference Documents](#critical-reference-documents)

---

## Status at Planning

| Phase | Status | Backend | Frontend | Blockers |
|-------|--------|---------|----------|----------|
| 1 — Foundation & Authentication | ✅ Complete | Done | Done | None |
| 2 — Wallets + Collaboration | ✅ Complete | Done | Done | — |
| 3 — Transactions + Categories | ✅ Complete | Done | Done | — |
| 4 — Budgets | ✅ Complete | Done | Done | — |
| 5 — Goals | ✅ Complete | Done | Done | — |
| 6 — Recurring | ✅ Complete | Done | Done | — |
| 7 — Notifications + Auditing | ✅ Complete | Done | Done | — |
| 8 — Dashboard + Launch | ✅ Complete | Done | Done | — |

**Existing foundation:**
- Solution structure: `Kakeibo.slnx` with 2 projects (Kakeibo.Api + Kakeibo.Tests)
- Common abstractions: `Entity`, `Result<T>`, `Error`, `IEndpoint`, `IEvent`, `IEventBus`, `IEventHandler<T>`
- Infrastructure: Email service, Docker Compose, ChannelEventBus, EventDispatcher
- CI pipeline skeleton: GitHub Actions quality gates
- Architecture tests: Naming convention enforcement (3 tests, all green)

---

## Core Philosophy

Kakeibo embodies three fundamental principles adapted from the traditional Japanese budgeting method:

### 1. Conscious Spending
Every transaction is an opportunity for awareness. By recording and categorizing each financial event, users develop a deeper understanding of their spending patterns and make more intentional choices about where their money goes.

### 2. Reflection Through Categorization
The traditional Kakeibo method organizes expenses into four essential categories: Survival, Culture, Optional, and Extra. This platform extends this philosophy with a system of twelve standard categories that cover the full spectrum of modern life, while allowing unlimited custom categories for personal nuance.

### 3. Savings Through Awareness
Financial health begins with seeing clearly. By tracking income, expenses, and progress toward goals in one unified view, users naturally identify opportunities to save and grow their wealth.

**Modern Adaptation**: While honoring traditional principles, Kakeibo adapts to contemporary needs with digital convenience, collaborative contexts, automation support, and intelligent forecasting.

**Balance Between Individual and Collective**: Kakeibo recognizes that modern financial life exists in two simultaneous dimensions: personal autonomy and shared responsibility. The platform treats both as equally important, allowing users to manage their individual finances while participating in collaborative expense pools without friction or complexity.

---

## Architecture Overview

### Single-Tenant MVP Approach

Kakeibo is built as a **simple monolith** with vertical slices and screaming architecture:

- **Single deployable unit**: One API project, one web app, one email service
- **2 .NET projects**: `src/Kakeibo.Api/` (all domains) + `tests/Kakeibo.Tests/` (all tests)
- **8 business domains in 3 tiers**: Platform Core → Financial Core → Planning
- **Vertical Slices**: Each feature is self-contained in `Features/{Domain}/{Operation}/`
- **In-memory events**: `System.Threading.Channels` (IEventBus, ChannelEventBus, EventDispatcher)
- **Single AppDbContext**: One context, one schema, one migrations history

### Domain Structure (8 domains in `Features/`)

| Tier | Domain | Description |
|------|--------|-------------|
| **1 — Platform Core** | Identity | Authentication, user accounts, sessions, password recovery |
| **1 — Platform Core** | Notifications | Multi-channel notifications (email, in-app), templates, preferences |
| **1 — Platform Core** | Auditing | Activity logs, audit trail, immutable event recording |
| **2 — Financial Core** | Wallets | Personal and shared wallet management, balance tracking, invitations, splits, debts, settlements |
| **2 — Financial Core** | Transactions | Income, expense, transfer recording, categorization (12 system + unlimited custom) |
| **3 — Planning** | Budgets | Spending limits, budget monitoring, alerts |
| **3 — Planning** | Goals | Savings targets, progress tracking, milestones |
| **3 — Planning** | Recurring | Pattern management, automatic transaction generation |

**Total: 2 projects** (Kakeibo.Api + Kakeibo.Tests).

### Key Architectural Decisions

**Domain consolidation** (from original 10 → 8):
- **Collaboration merged into Wallets**: Collaboration features (invitations, splits, debts, settlements) only exist for shared wallets.
- **Categories merged into Transactions**: Categories only exist to classify transactions.

**Database Strategy**: Single `AppDbContext`. Single `public` schema. One `__ef_migrations_history` table.

**Communication Patterns**:
- **Async events**: `IEventBus.Publish()` → `ChannelEventBus` → `EventDispatcher` BackgroundService → `IEventHandler<T>`
- **No cross-assembly contracts**: All code lives in one project

**Tech Stack**:
- Backend: .NET 10, PostgreSQL 18, Redis, RustFS, ClickHouse, Hangfire
- Frontend: Vue.js (PWA), Tailwind CSS v4, shadcn-vue, Axios, Pinia
- Email: Bun + Hono + React Email (separate microservice)
- Testing: xUnit v3, Testcontainers, Vitest, Playwright

---

## Domain Dependency Graph

```
                    Identity (foundation)
                         │
                         v
               Wallets (includes Collaboration)
                         │
                         v
          Transactions (includes Categories)
                         │
        ┌────────────────┼────────────────┐
        v                v                v
    Budgets          Goals           Recurring
        │                │                │
        └────────┬───────┴────────────────┘
                 v
          Notifications  ←─ All domains emit
          Auditing       ←─ All domains log
```

**Dependency Notes**:
- **Identity**: Foundation layer — no dependencies on other domains
- **Wallets**: Second layer — depends only on Identity
- **Transactions**: Third layer — depends on Wallets (for balance updates)
- **Budgets + Goals + Recurring**: Fourth layer — all depend on Transactions (consume transaction events)
- **Notifications + Auditing**: Cross-cutting — consumed by all domains, depend only on Identity

**Deployment Note**: The diagram shows logical dependencies, not physical deployment boundaries. All domains are deployed together in a single monolith. Domain boundaries are enforced through folder structure, not separate assemblies.

---

## Phase Summaries

### Phase 1: Foundation & Authentication
**Objective:** Establish foundational infrastructure and implement complete authentication system

**Sub-Phases:**
- **1a - Infrastructure Base** (complete): Docker Compose, CI/CD, project scaffolding, core abstractions, Events system (ChannelEventBus + EventDispatcher)
- **1b - Identity Backend** (4-5 days): User registration, login, JWT tokens, password recovery
- **1c - Audit Logging** (1-2 days): ClickHouse integration for audit trail, IEventHandler<T> implementations
- **1d - Identity Frontend** (2-3 days): Login/register screens, token refresh, route guards

**Key Deliverables:**
- 2 projects (Kakeibo.Api + Kakeibo.Tests) built and tested
- Docker Compose with 8 infrastructure services
- Complete authentication flow (backend + frontend)
- Events system (System.Threading.Channels)
- CI/CD pipeline functional
- Architecture tests (naming convention enforcement)

**Integration Events Note:** `UserLoggedInEvent` and `UserLoggedOutEvent` (published in Phase 1b) are consumed by the Auditing module (Phase 1d) to record session activity in the audit trail. They have no other consumers.

**Status:** ✅ Complete
**Duration:** 10-15 days total
**Link:** [phases/phase-1/phase-1.md](./phases/phase-1/phase-1.md)

---

### Phase 2: Wallets + Collaboration (Backend + UI)
**Objective:** Personal and shared wallet management with invitations, splits, debts, and settlements.

**Key Deliverables:**
- **Phase 2a: Personal Wallets** — Backend: Wallet CRUD. Frontend: Wallet list screen, create/edit modal, detail view
- **Phase 2b: Shared Wallets + Invitations** — Backend: SharedWallet, WalletMember, Invitation entities. Frontend: Shared wallet screen, invitation flow, member list
- **Phase 2c: Splits + Debt Calculation + Settlements** — Backend: TransactionSplit (Equal/Percentage/Custom), DebtCalculationService (Splitwise algorithm), Settlement entity. Frontend: Split configurator, debts screen, settlement modal. *Implemented after Phase 3b.*

**Status:** ✅ Complete
**Link:** [phases/phase-2/phase-2.md](./phases/phase-2/phase-2.md)

---

### Phase 3: Transactions + Categories (Backend + UI)
**Objective:** Transaction recording (income, expense, transfer) with categorization and split configuration.

**Key Deliverables:**
- **Phase 3a: Categories** — Backend: Category entity with 12 system categories + custom categories. Frontend: Category management screen
- **Phase 3b: Transaction Recording** — Backend: Transaction entity (income, expense, transfer), WalletBalance entity (atomic balance tracking in Transactions module). Frontend: Transaction form with calculator UI

**Status:** ✅ Complete
**Link:** [phases/phase-3/phase-3.md](./phases/phase-3/phase-3.md)

---

### Phase 4: Budgets (Backend + UI)
**Objective:** Spending limit management with real-time monitoring and alerts.

**Key Deliverables:**
- **Phase 4a: Budget CRUD** — Backend: Budget entity, CRUD endpoints, spending tracking. Frontend: Budget planning screen, create budget modal
- **Phase 4b: Budget Monitoring** — Backend: Budget status calculation (on track/warning/exceeded), alert events. Frontend: Budget status dashboard

**Status:** ✅ Complete
**Link:** [phases/phase-4/phase-4.md](./phases/phase-4/phase-4.md)

---

### Phase 5: Goals (Backend + UI)
**Objective:** Savings target tracking with milestones and projected completion dates.

**Key Deliverables:**
- **Phase 5a: Savings Goal CRUD** — Backend: SavingsGoal entity, CRUD endpoints, 3 tracking modes. Frontend: Goals screen, create goal modal
- **Phase 5b: Goal Progress** — Backend: Milestone detection (25%/50%/75%/100%), projected completion. Frontend: Goal detail view, progress visualization

**Status:** ✅ Complete
**Link:** [phases/phase-5/phase-5.md](./phases/phase-5/phase-5.md)

---

### Phase 6: Recurring Transactions (Backend + UI)
**Objective:** Automated transaction pattern management with forecast visibility.

**Key Deliverables:**
- **Phase 6a: Recurring Patterns** — Backend: RecurringPattern entity, CRUD endpoints, recurrence rules (daily, weekly, biweekly, monthly, yearly). Frontend: Recurring configuration screen
- **Phase 6b: Auto-Generation + Forecast** — Backend: Hangfire background job, forecast calculation. Frontend: Forecast timeline view

**Status:** ✅ Complete
**Blocks:** Phase 7 (Phase 7a requires `RecurringTransactionGeneratedEvent` from Phase 6b)
**Link:** [phases/phase-6/phase-6.md](./phases/phase-6/phase-6.md)

---

### Phase 7: Notifications + Auditing Modules (Backend + UI)
**Objective:** Multi-channel notification system and immutable activity logging.

**Key Deliverables:**
- **Phase 7a: Notification System** — Backend: Notification entity, consumers for ALL integration events, email templates. Frontend: In-app notification center, preferences screen
- **Phase 7b: Activity Logging** — Backend: Activity entity, query endpoints, CSV export. Frontend: Admin audit trail screen

**Status:** ✅ Complete
**Link:** [phases/phase-7/phase-7.md](./phases/phase-7/phase-7.md)

---

### Phase 8: Dashboard + Onboarding + Launch (Cross-Cutting UI)
**Objective:** Complete cross-cutting UI concerns and production launch.

**Key Deliverables:**
- **Phase 8a: Dashboard** — 6-card overview screen (balance, transactions, budgets, goals, recurring, notifications)
- **Phase 8b: Onboarding Flow** — 3-step wizard (Welcome, Create First Wallet, Record First Transaction)
- **Phase 8c: Settings + Profile** — User profile, notification preferences, language selector, theme preference
- **Phase 8d: E2E Testing + Performance + Launch** — Playwright E2E tests, Lighthouse PWA score > 90, production deployment

**Status:** ✅ Complete
**Link:** [phases/phase-8/phase-8.md](./phases/phase-8/phase-8.md)

---

## Development Order

**Sequential order accounting for dependencies:**

```
Phase 1 (Foundation & Authentication)
  1a (Infrastructure + Events) → 1b (Identity Backend) → 1c (Audit) → 1d (Frontend)
  │
  v
Phase 2a (Wallets Backend + UI) → 2b (Shared Wallets Backend + UI)
  │
  v
Phase 3a (Categories Backend + UI) → 3b (Transactions Backend + Calculator UI)
  │
  └──> Phase 2c (Splits + Debt Calculation + Settlements Backend + UI)  ← requires 3b events
  │
  v
┌────────────────────────────────────────────────────────────────┐
│ Phase 4 (Budgets Backend + UI)                                 │
│ Phase 5 (Goals Backend + UI)                 ← PARALLEL        │
│ Phase 6 (Recurring Backend + UI)                               │
└────────────────────────────────────────────────────────────────┘
  │
  v
Phase 7a (Notifications Backend + In-App Center) → 7b (Activity Backend + Admin Screen)
  │
  v
Phase 8a (Dashboard) → 8b (Onboarding) → 8c (Settings) → 8d (Testing + Launch)
```

**Critical dependency:** Phase 2c (Splits + Debt Calculation) **requires** Phase 3b (Transaction Recording) because debt calculation consumes `TransactionRecordedEvent`, `TransactionUpdatedEvent`, `TransactionDeletedEvent`, and splits are configured at transaction record time.

**Actual development order:** 1a → 1b → 1c → 1d → 2a → 2b → 3a → 3b → 2c → (4 | 5 | 6 parallel) → 7a → 7b → 8a → 8b → 8c → 8d

---

## Parallel Development Opportunities

| Phase | Can Run In Parallel With | Reason |
|-------|--------------------------|--------|
| **4 (Budgets)** | 5 (Goals), 6 (Recurring) | All three consume `TransactionRecordedEvent` but are otherwise independent |
| **5 (Goals)** | 4 (Budgets), 6 (Recurring) | All three consume `TransactionRecordedEvent` but are otherwise independent |
| **6 (Recurring)** | 4 (Budgets), 5 (Goals) | All three consume `TransactionRecordedEvent` but are otherwise independent |
| **7a (Notifications)** | 7b (Auditing) | Independent consumers of integration events. Recommended: start 7a first (larger scope), then begin 7b once notification infrastructure is established. |

**Strategy:** Maximize parallel development after Phase 3b (Transactions) completes. Phases 4, 5, 6 can all be developed simultaneously by different developers or teams.

---

## MVP Summary Per Phase

| Phase | Business Value Delivered (Backend + Frontend) |
|-------|----------------------------------------------|
| **1** | Identity → Users can register, log in, verify email, reset password, and admin can manage accounts |
| **2** | Wallets + Collaboration → Users can create personal/shared wallets, invite others, view balances, see debts, record settlements |
| **3** | Transactions + Categories → Users can record income/expense/transfer with calculator UI, categorize transactions, split shared expenses |
| **4** | Budgets → Users can create spending limits, monitor progress in real-time, receive warnings |
| **5** | Goals → Users can set savings targets, track progress, receive milestone notifications |
| **6** | Recurring → Users can automate predictable transactions, view forecasts, reduce manual recording |
| **7** | Notifications + Auditing → Users receive in-app + email notifications for all events, admins can view full audit trail |
| **8** | Dashboard + Launch → Users see unified financial overview, new users guided through onboarding, app ready for production |

**Key takeaway:** Each phase (except 0) delivers **both** backend (API + entities + events) **and** frontend (screens + components + Pinia stores) in the same iteration. This enables early user feedback and reduces the risk of API-UI mismatches.

---

## Strategic Timeline

| Phase | Relative Effort | Deliverables | Sequencing Notes |
|-------|----------------|--------------|------------------|
| **Phase 1 (1a-1d)** | Medium | Infrastructure + Events + Identity + Audit (API + UI) | Sequential: 1a → 1b → 1c → 1d |
| **Phase 2 (2a-2c)** | Medium | Wallets + Collaboration (API + UI) | Sequential: 2a → 2b; Phase 2c deferred until after 3b |
| **Phase 3 (3a-3b)** | Medium | Transactions + Categories (API + Calculator UI) | Sequential: 3a → 3b; unblocks 2c, 4, 5, 6 |
| **Phase 4 (4a-4b)** | Medium (parallel) | Budgets (API + UI) | Can be developed concurrently with 5, 6 after Phase 3b |
| **Phase 5 (5a-5b)** | Medium (parallel) | Goals (API + UI) | Can be developed concurrently with 4, 6 after Phase 3b |
| **Phase 6 (6a-6b)** | Medium (parallel) | Recurring (API + UI) | Can be developed concurrently with 4, 5 after Phase 3b |
| **Phase 7 (7a-7b)** | Medium | Notifications + Auditing (API + UI) | Consumes events from all prior phases |
| **Phase 8 (8a-8d)** | Medium | Dashboard + Onboarding + Settings + Launch | Cross-cutting UI; all modules must be complete |

**Total estimated timeline:** 3–4 months (15 weeks) with 1–2 developers working iteratively.

**Approach:** Iterative vertical slices. Each phase delivers complete features (API + UI). Enables early user feedback, reduces integration risk.

---

## Critical Reference Documents

During implementation, every phase should reference these source files from the `kakeibo/` folder:

| File | Purpose |
|------|---------|
| [kakeibo/overview.md](./overview.md) | Project vision, core philosophy, user flows, business context |
| [kakeibo/architecture.md](./architecture.md) | Domain structure, project dependencies, feature slice pattern, DI registration pattern, inter-domain communication |
| [kakeibo/platform.md](./platform.md) | Domain catalog (8 domains), dependency matrix, integration event catalog, domain request/response catalog, entity descriptions |
| [kakeibo/constraints.md](./constraints.md) | Business limits, rate limits, pagination, soft delete, GDPR, timezone handling |
| [kakeibo/tech-stack.md](./tech-stack.md) | Technology choices, prohibited technologies, framework versions |
| [kakeibo/infrastructure.md](./infrastructure.md) | Docker Compose layout, CI/CD pipeline, environment strategy, deployment model |

---

## Key Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Development approach** | Iterative vertical slices (backend + frontend together) | Each phase delivers complete features (API + UI). Enables early user feedback. Aligns with agile development. Reduces risk of API-UI mismatches discovered late. |
| **RBAC implementation** | Simple (Admin + user isolation) | Kakeibo's permission model is intentionally flat. Full RBAC adds complexity for no business value. Shared wallet permissions are membership checks, not role-based. |
| **Notification module timing** | Late (Phase 7 after business modules) | Consumers need events from all business modules. Building early means constant modification. Building late means implementing once against complete event catalog. |
| **Phase 2c placement** | In Phase 2 with Phase 3b prerequisite | Conceptually belongs in Wallets module (debt calc) and Transactions module (splits). Prerequisite explicitly documented. Actual dev order: 2a → 2b → 3a → 3b → 2c. Phase 3c absorbed into 2c. |
| **Events System timing** | Phase 1a (infrastructure) | The in-memory event bus (`IEventBus` / `ChannelEventBus` / `EventDispatcher`) is part of the infrastructure base. It is wired and operational from Phase 1a. Identity events (`UserRegisteredEvent`, `UserLoggedInEvent`) are the first real events dispatched through it in Phase 1b. |
| **Dashboard + Onboarding timing** | Phase 8 (after all modules complete) | Dashboard aggregates data from all 6 business modules. Onboarding guides users through features that must already exist. Settings centralizes preferences from all modules. |
| **OAuth login** | Post-MVP | OAuth listed in platform.md but not in MVP scope. Focus on email/password for MVP. Google/Apple Sign-In deferred. |
| **Multi-currency** | Post-MVP | Single-currency MVP per constraints.md. User selects currency at registration. Multi-currency deferred to post-MVP. |
| **Import/Export** | Post-MVP | CSV/PDF import and export deferred to post-MVP for scope management. |
| **Activity log detail** | Simplified (who, what, when) | Simplified activity logging for MVP per platform.md. Full change diffs deferred to post-MVP. |

---

## Cross-Cutting Concerns Strategy

### Notifications

- **Phase 1-6:** Business events published via `IEventBus.Publish()`. The `ChannelEventBus` writes to an in-memory `Channel<IEvent>`. The `EventDispatcher` (BackgroundService) reads the channel and resolves all `IEventHandler<TEvent>` implementations from DI. If no handler is registered for an event, the dispatcher discards the event silently. When Notifications handlers are registered in Phase 7a, events are dispatched automatically.
- **Phase 7a:** `Features/Notifications/` implemented with `IEventHandler<T>` implementations for all business events.

### Auditing

- **Phase 1a:** ClickHouse service started (Docker Compose). No schema created yet.
- **Phase 1c:** ClickHouse `audit_events` table created. `IEventHandler<T>` implementations in `Features/Auditing/` receive events published via `IEventBus` and write to ClickHouse. Audit pipeline fully operational.
- **Phase 1d:** No additional audit work. Audit pipeline is already operational from Phase 1c.
- **Phase 2-6:** Each feature handler publishes domain events (`IEvent`) via `IEventBus`. Auditing handlers react asynchronously via `EventDispatcher`.
- **Phase 7b:** `Features/Auditing/` adds Activity query endpoints and admin UI for browsing audit trail.

### i18n

- **Phase 1a:** Vue i18n configured with EN/ES locale files (empty templates).
- **Phases 1-8:** All user-visible strings use `t('key')` per mandatory rule 5. Translation keys added incrementally as screens are built.

---

*Kakeibo is a personal finance platform balancing individual tracking with collaborative expense management. The platform honors traditional Japanese budgeting wisdom while adapting to contemporary digital life and collaborative financial responsibilities.*
