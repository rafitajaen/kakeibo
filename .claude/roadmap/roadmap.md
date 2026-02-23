# Kakeibo Roadmap

**Mindful money management for personal budgeting and collaborative expenses**

**Version:** 3.0 (Single-Tenant MVP)
**Status:** Planning phase
**Last Updated:** 2026-02-21

---

## Document Purpose

This roadmap defines the strategic development plan for the Kakeibo platform — a personal finance and shared expense management system inspired by the traditional Japanese household budgeting method. The roadmap organizes work into 9 phases (0–8) with 32 total deliverables across 3–4 months of development.

**For complete context**, see [kakeibo/overview.md](./overview.md).

---

## Table of Contents

1. [Status at Planning](#status-at-planning)
2. [Core Philosophy](#core-philosophy)
3. [Architecture Overview](#architecture-overview)
4. [Module Dependency Graph](#module-dependency-graph)
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
| 0 — Base Infrastructure | Partially complete | Scaffolding started | Shell created | Email service needed |
| 1 — Identity | Pending | Not started | Not started | Phase 0 |
| 2 — Wallets + Collaboration | Pending | Not started | Not started | Phase 1 |
| 3 — Transactions + Categories | Pending | Not started | Not started | Phase 2 |
| 4 — Budgets | Pending | Not started | Not started | Phase 3 |
| 5 — Goals | Pending | Not started | Not started | Phase 3 |
| 6 — Recurring | Pending | Not started | Not started | Phase 3 |
| 7 — Notifications + Auditing | Pending | Not started | Not started | Phases 2–6 |
| 8 — Dashboard + Launch | Pending | Not started | Not started | Phases 2–7 |

**Existing foundation:**
- Solution structure: `Kakeibo.slnx` with 12 projects defined
- Common abstractions: `Entity`, `Result<T>`, `Error`, `IEndpoint`, `IModuleClient`, `IEventConsumer<T>`
- Infrastructure placeholders: Email service stub, Docker Compose layout
- CI pipeline skeleton: GitHub Actions quality gates

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

Kakeibo is built as a **single-tenant modular monolith** with strict module boundaries:

- **Single deployable unit**: One API, one web app, one email service
- **8 modules in 3 tiers**: Platform Core → Financial Core → Planning
- **Vertical Slices pattern**: Each feature is self-contained (endpoint, handler, validator)
- **Event-driven communication**: Modules communicate via integration events and module requests, never direct references
- **Outbox Pattern**: Reliable event publishing with guaranteed delivery

### Module Structure (8 modules)

| Tier | Module | Description |
|------|--------|-------------|
| **1 — Platform Core** | Identity | Authentication, user accounts, sessions, password recovery |
| **1 — Platform Core** | Notifications | Multi-channel notifications (email, in-app), templates, preferences |
| **1 — Platform Core** | Auditing | Activity logs, audit trail, immutable event recording |
| **2 — Financial Core** | Wallets | Personal and shared wallet management, balance tracking, invitations, splits, debts, settlements |
| **2 — Financial Core** | Transactions | Income, expense, transfer recording, categorization (12 system + unlimited custom) |
| **3 — Planning** | Budgets | Spending limits, budget monitoring, alerts |
| **3 — Planning** | Goals | Savings targets, progress tracking, milestones |
| **3 — Planning** | Recurring | Pattern management, automatic transaction generation |

**Total: 12 projects** (4 infrastructure: Api, Common, Contracts, Infrastructure + 8 modules).

### Key Architectural Decisions

**Merged Modules** (from original 10 → 8):
- **Collaboration merged into Wallets**: Collaboration features (invitations, splits, debts, settlements) only exist for shared wallets. No standalone Collaboration module.
- **Categories merged into Transactions**: Categories only exist to classify transactions. No standalone Categories module.

**Database Strategy**: One PostgreSQL schema per module. All modules share the same connection string — separation is logical (schemas), not physical.

**Communication Patterns**:
- **Sync queries**: `IModuleClient` for request/response patterns
- **Async events**: `IModuleEventBus` + Outbox Pattern for fire-and-forget with guaranteed delivery
- **No cross-module references**: Module A NEVER references Module B's project

**Tech Stack**:
- Backend: .NET 10, PostgreSQL 18, Redis, RustFS, ClickHouse, Hangfire
- Frontend: Vue.js (PWA), Tailwind CSS v4, shadcn-vue, Axios, Pinia
- Email: Bun + Hono + React Email (separate microservice)
- Testing: xUnit v3, Testcontainers, Vitest, Playwright

---

## Module Dependency Graph

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
          Notifications  ←─ All modules emit
          Auditing       ←─ All modules log
```

**Dependency Notes**:
- **Identity**: Foundation layer — no dependencies on other modules
- **Wallets**: Second layer — depends only on Identity
- **Transactions**: Third layer — depends on Wallets (for balance updates)
- **Budgets + Goals + Recurring**: Fourth layer — all depend on Transactions (consume transaction events)
- **Notifications + Auditing**: Cross-cutting — consumed by all modules, depend only on Identity

**Deployment Note**: The diagram shows logical dependencies, not physical deployment boundaries. All modules are deployed together in a single modular monolith. Module boundaries are enforced through architecture tests, not separate processes.

---

## Phase Summaries

### Phase 0: Base Infrastructure
**Objective:** Establish foundational infrastructure for development and deployment.

**Key Deliverables:**
- All 12 projects scaffolded (`Kakeibo.slnx`)
- Docker Compose with 8 infrastructure services (PostgreSQL, Redis, RustFS, ClickHouse, Mailpit, Redis Insight, Aspire Dashboard)
- GitHub Actions CI pipeline (quality gates for API, App, Email, Docker)
- Email renderer service (`Kakeibo.Email` — Bun + Hono + React Email)
- Vue PWA shell (`sites/Kakeibo.App`)
- Architecture tests (module boundary enforcement)
- Pre-commit hooks (`lefthook.yml`)

**Status:** Partially complete
**Link:** [phases/phase-0/phase-0.md](./phases/phase-0/phase-0.md)

---

### Phase 1: Identity (Backend + Frontend)
**Objective:** Complete authentication system with email/password registration, login, email verification, password recovery, and super admin setup.

**Key Deliverables:**
- **Phase 1a: Outbox Pattern** — Infrastructure for reliable event publishing
- **Phase 1b: Audit Logging** — Infrastructure for immutable activity logs
- **Phase 1c: Auth Backend** — Registration, login, JWT tokens, email verification, password recovery
- **Phase 1d: Auth Frontend** — Login screen, Register screen, Email Verification flow, Password Reset flow, Super Admin Onboarding wizard

**Status:** Pending (blocked by Phase 0)
**Link:** [phases/phase-1/phase-1.md](./phases/phase-1/phase-1.md)

---

### Phase 2: Wallets + Collaboration (Backend + UI)
**Objective:** Personal and shared wallet management with invitations, splits, debts, and settlements.

**Key Deliverables:**
- **Phase 2a: Personal Wallets** — Backend: Wallet CRUD, balance tracking. Frontend: Wallet list screen, create/edit modal, detail view
- **Phase 2b: Shared Wallets + Invitations** — Backend: SharedWallet, WalletMember, Invitation entities. Frontend: Shared wallet screen, invitation flow, member list
- **Phase 2c: Debt Calculation + Settlements** — Backend: DebtCalculationService (Splitwise algorithm), Settlement entity. Frontend: Debts screen, settlement modal

**Status:** Pending (blocked by Phase 1)
**Link:** [phases/phase-2/phase-2.md](./phases/phase-2/phase-2.md)

---

### Phase 3: Transactions + Categories (Backend + UI)
**Objective:** Transaction recording (income, expense, transfer) with categorization and split configuration.

**Key Deliverables:**
- **Phase 3a: Categories** — Backend: Category entity with 12 system categories + custom categories. Frontend: Category management screen
- **Phase 3b: Transaction Recording** — Backend: Transaction entity (income, expense, transfer), balance update logic. Frontend: Transaction form with calculator UI
- **Phase 3c: Transaction Splits** — Backend: TransactionSplit entity (equal/percentage/custom). Frontend: Split configuration component

**Status:** Pending (blocked by Phase 2)
**Link:** [phases/phase-3/phase-3.md](./phases/phase-3/phase-3.md)

---

### Phase 4: Budgets (Backend + UI)
**Objective:** Spending limit management with real-time monitoring and alerts.

**Key Deliverables:**
- **Phase 4a: Budget CRUD** — Backend: Budget entity, CRUD endpoints, spending tracking. Frontend: Budget planning screen, create budget modal
- **Phase 4b: Budget Monitoring** — Backend: Budget status calculation (on track/warning/exceeded), alert events. Frontend: Budget status dashboard

**Status:** Pending (blocked by Phase 3b — can run in parallel with Phases 5, 6)
**Link:** [phases/phase-4/phase-4.md](./phases/phase-4/phase-4.md)

---

### Phase 5: Goals (Backend + UI)
**Objective:** Savings target tracking with milestones and projected completion dates.

**Key Deliverables:**
- **Phase 5a: Savings Goal CRUD** — Backend: SavingsGoal entity, CRUD endpoints, 3 tracking modes. Frontend: Goals screen, create goal modal
- **Phase 5b: Goal Progress** — Backend: Milestone detection (25%/50%/75%/100%), projected completion. Frontend: Goal detail view, progress visualization

**Status:** Pending (blocked by Phase 3b — can run in parallel with Phases 4, 6)
**Link:** [phases/phase-5/phase-5.md](./phases/phase-5/phase-5.md)

---

### Phase 6: Recurring Transactions (Backend + UI)
**Objective:** Automated transaction pattern management with forecast visibility.

**Key Deliverables:**
- **Phase 6a: Recurring Patterns** — Backend: RecurringTransaction entity, CRUD endpoints, recurrence rules. Frontend: Recurring configuration screen
- **Phase 6b: Auto-Generation + Forecast** — Backend: Hangfire background job, forecast calculation. Frontend: Forecast timeline view

**Status:** Pending (blocked by Phase 3b — can run in parallel with Phases 4, 5)
**Link:** [phases/phase-6/phase-6.md](./phases/phase-6/phase-6.md)

---

### Phase 7: Notifications + Auditing Modules (Backend + UI)
**Objective:** Multi-channel notification system and immutable activity logging.

**Key Deliverables:**
- **Phase 7a: Notification System** — Backend: Notification entity, consumers for ALL integration events, email templates. Frontend: In-app notification center, preferences screen
- **Phase 7b: Activity Logging** — Backend: Activity entity, query endpoints, CSV export. Frontend: Admin audit trail screen

**Status:** Pending (blocked by Phases 2–6 — comes after all business modules)
**Link:** [phases/phase-7/phase-7.md](./phases/phase-7/phase-7.md)

---

### Phase 8: Dashboard + Onboarding + Launch (Cross-Cutting UI)
**Objective:** Complete cross-cutting UI concerns and production launch.

**Key Deliverables:**
- **Phase 8a: Dashboard** — 6-card overview screen (balance, transactions, budgets, goals, recurring, notifications)
- **Phase 8b: Onboarding Flow** — 3-step wizard (Welcome, Create First Wallet, Record First Transaction)
- **Phase 8c: Settings + Profile** — User profile, notification preferences, language selector, theme preference
- **Phase 8d: E2E Testing + Performance + Launch** — Playwright E2E tests, Lighthouse PWA score > 90, production deployment

**Status:** Pending (blocked by all modules — final phase)
**Link:** [phases/phase-8/phase-8.md](./phases/phase-8/phase-8.md)

---

## Development Order

**Sequential order accounting for dependencies:**

```
Phase 0 (Infrastructure)
  │
  v
Phase 1a (Outbox) → 1b (Audit) → 1c (Auth Backend) → 1d (Auth Frontend)
  │
  v
Phase 2a (Wallets Backend + UI) → 2b (Shared Wallets Backend + UI)
  │
  v
Phase 3a (Categories Backend + UI) → 3b (Transactions Backend + Calculator UI)
  │
  ├──> Phase 2c (Debt Calculation Backend + Debts Screen)  ← requires 3b events
  │
  └──> Phase 3c (Splits Backend + Split Component)
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

**Critical dependency:** Phase 2c (Debt Calculation) **requires** Phase 3b (Transaction Recording) because debt calculation consumes `TransactionRecordedEvent`, `TransactionUpdatedEvent`, `TransactionDeletedEvent`.

**Actual development order:** 0 → 1a → 1b → 1c → 1d → 2a → 2b → 3a → 3b → 2c → 3c → (4 | 5 | 6 parallel) → 7a → 7b → 8a → 8b → 8c → 8d

---

## Parallel Development Opportunities

| Phase | Can Run In Parallel With | Reason |
|-------|--------------------------|--------|
| **4 (Budgets)** | 5 (Goals), 6 (Recurring) | All three consume `TransactionRecordedEvent` but are otherwise independent |
| **5 (Goals)** | 4 (Budgets), 6 (Recurring) | All three consume `TransactionRecordedEvent` but are otherwise independent |
| **6 (Recurring)** | 4 (Budgets), 5 (Goals) | All three consume `TransactionRecordedEvent` but are otherwise independent |
| **2c (Debt Calc)** | 3c (Splits) | Both require Phase 3b complete but are independent of each other |
| **7a (Notifications)** | 7b (Auditing) | Independent consumers of integration events |

**Strategy:** Maximize parallel development after Phase 3b (Transactions) completes. Phases 4, 5, 6 can all be developed simultaneously by different developers or teams.

---

## MVP Summary Per Phase

| Phase | Business Value Delivered (Backend + Frontend) |
|-------|----------------------------------------------|
| **0** | Infrastructure → Development environment ready for feature work |
| **1** | Identity → Users can register, log in, verify email, reset password, and super admin can manage accounts |
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
| **Phase 0** | Foundation | Infrastructure setup (Docker, CI, Email service) | Prerequisite for all phases |
| **Phase 1 (1a-1d)** | Medium | Outbox + Audit + Auth (API + UI) | Sequential: 1a → 1b → 1c → 1d |
| **Phase 2 (2a-2c)** | Medium | Wallets + Collaboration (API + UI) | Sequential: 2a → 2b; Phase 2c deferred until after 3b |
| **Phase 3 (3a-3c)** | Medium | Transactions + Categories (API + Calculator UI) | Sequential: 3a → 3b → 3c; unblocks 2c, 4, 5, 6 |
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
| [kakeibo/architecture.md](./architecture.md) | Module structure, project dependencies, feature slice pattern, DI registration pattern, inter-module communication |
| [kakeibo/platform.md](./platform.md) | Module catalog (8 modules), dependency matrix, integration event catalog, module request/response catalog, entity descriptions |
| [kakeibo/constraints.md](./constraints.md) | Business limits, rate limits, pagination, soft delete, GDPR, timezone handling |
| [kakeibo/tech-stack.md](./tech-stack.md) | Technology choices, prohibited technologies, framework versions |
| [kakeibo/infrastructure.md](./infrastructure.md) | Docker Compose layout, CI/CD pipeline, environment strategy, deployment model |

---

## Key Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Development approach** | Iterative vertical slices (backend + frontend together) | Each phase delivers complete features (API + UI). Enables early user feedback. Aligns with agile development. Reduces risk of API-UI mismatches discovered late. |
| **RBAC implementation** | Simple (SuperAdmin + user isolation) | Kakeibo's permission model is intentionally flat. Full RBAC adds complexity for no business value. Shared wallet permissions are membership checks, not role-based. |
| **Notification module timing** | Late (Phase 7 after business modules) | Consumers need events from all business modules. Building early means constant modification. Building late means implementing once against complete event catalog. |
| **Phase 2c placement** | In Phase 2 with Phase 3b prerequisite | Conceptually belongs in Wallets module. Prerequisite explicitly documented. Actual dev order: 2a → 2b → 3a → 3b → 2c → 3c. |
| **Outbox Pattern timing** | Phase 1a (dedicated sub-phase) | Infrastructure deserves focused implementation with dedicated tests. Phase 0 already large. Enables all subsequent phases to publish events reliably. |
| **Dashboard + Onboarding timing** | Phase 8 (after all modules complete) | Dashboard aggregates data from all 6 business modules. Onboarding guides users through features that must already exist. Settings centralizes preferences from all modules. |
| **OAuth login** | Post-MVP | OAuth listed in platform.md but not in MVP scope. Focus on email/password for MVP. Google/Apple Sign-In deferred. |
| **Multi-currency** | Post-MVP | Single-currency MVP per constraints.md. User selects currency at registration. Multi-currency deferred to post-MVP. |
| **Import/Export** | Post-MVP | CSV/PDF export deferred to post-MVP for scope management. |
| **Activity log detail** | Simplified (who, what, when) | Simplified activity logging for MVP per platform.md. Full change diffs deferred to post-MVP. |

---

## Cross-Cutting Concerns Strategy

### Notifications

- **Phase 0-6:** Integration events published via `IModuleEventBus` and persisted in outbox. No consumer exists — events marked as "no consumer" by `OutboxProcessor`. Safe because outbox pattern handles at-least-once delivery.
- **Phase 7a:** `Kakeibo.Modules.Notifications` implemented with consumers for all business events. `OutboxProcessor` now resolves `IEventConsumer<T>` for notification events.

### Auditing

- **Phase 0:** ClickHouse `audit_logs` table created (infrastructure level).
- **Phase 1b:** Audit pipeline fully operational — `IAuditOutbox.Stage()` and `.PublishAsync()` available to all modules. `AuditOutboxProcessor` running in background.
- **Phase 2-6:** Each module's `DomainEventHandler` implementations call `auditOutbox.Stage()` within transaction. Audit events flow to ClickHouse automatically.
- **Phase 7b:** `Kakeibo.Modules.Auditing` adds Activity entity, query endpoints, and admin UI for browsing audit trail.

### i18n

- **Phase 0:** Vue i18n configured with EN/ES locale files (empty templates).
- **Phases 1-8:** All user-visible strings use `t('key')` per mandatory rule 5. Translation keys added incrementally as screens are built.

---

*Kakeibo is a personal finance platform balancing individual tracking with collaborative expense management. The platform honors traditional Japanese budgeting wisdom while adapting to contemporary digital life and collaborative financial responsibilities.*
