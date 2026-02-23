# Kakeibo (家計簿)

🏯 **Mindful money management for personal budgeting and collaborative expenses**

[![Build Status](https://github.com/rafitajaen/kakeibo/actions/workflows/quality.yml/badge.svg)](https://github.com/rafitajaen/kakeibo/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Transform your relationship with money through mindful tracking and intentional spending.

---

## 📖 About

Kakeibo (家計簿, "household financial ledger") is a traditional Japanese budgeting method created over a century ago by Hani Motoko, Japan's first female journalist. The word represents a philosophy of conscious spending through reflection and planning.

This digital implementation brings the Kakeibo philosophy to modern users while adapting to contemporary needs: digital convenience, collaborative expense management, automation support, and intelligent forecasting.

### Core Philosophy

**Conscious Spending** — Every transaction is an opportunity for awareness. By recording and categorizing each financial event, users develop a deeper understanding of their spending patterns and make more intentional choices.

**Reflection Through Categorization** — Understanding spending patterns through systematic classification. The platform provides twelve standard categories covering the full spectrum of modern life, while allowing unlimited custom categories for personal nuance.

**Savings Through Awareness** — Financial health begins with seeing clearly. By tracking income, expenses, and progress toward goals in one unified view, users naturally identify opportunities to save and grow their wealth.

**Balance Between Individual and Collective** — Modern financial life exists in two simultaneous dimensions: personal autonomy and shared responsibility. Kakeibo treats both as equally important, allowing users to manage their individual finances while participating in collaborative expense pools without friction or complexity.

---

## 🚧 Version 3.0 - Single-Tenant MVP

**Current Status:** 🔨 **Phase 1a (Infrastructure Base) - Partially Complete**

⚠️ **Early Development Notice:** This project is in the foundation phase. Most features are planned but not yet implemented. The current focus is establishing the architectural foundation, development environment, and core infrastructure.

### What Works Now
- ✅ Solution structure with 12 projects (4 infrastructure + 8 modules)
- ✅ Docker Compose environment (8 infrastructure services)
- ✅ CI/CD pipeline skeleton (GitHub Actions)
- ✅ Development tooling (lefthook, commitlint, semantic-release)
- ✅ Email rendering service (Bun + Hono + React Email)

### What's Coming
- 🔨 **Phase 1 (In Progress):** Identity module - Authentication, registration, email verification
- ⏳ **Phases 2-6 (Planned):** Business modules - Wallets, Transactions, Budgets, Goals, Recurring
- ⏳ **Phase 7 (Planned):** Notifications + Auditing
- ⏳ **Phase 8 (Planned):** Dashboard + Production launch

For detailed roadmap, see [`.claude/roadmap/roadmap.md`](./.claude/roadmap/roadmap.md).

---

## 🏗️ Architecture

### Single-Tenant Modular Monolith

Version 3.0 adopts a **single-tenant architecture** where each user operates within an isolated financial environment:

- **Isolation:** Users cannot see or affect each other's personal financial data
- **Personal Wallets:** Each user manages their own wallets, transactions, budgets, and goals
- **Shared Contexts:** Users can create shared wallets for collaborative expense management
- **Equal Participation:** All shared wallet members have identical permissions—no hierarchy

### Module Structure (8 Modules)

| Tier | Module | Status | Description |
|------|--------|--------|-------------|
| **Platform Core** | Identity | 🔨 In Progress | Authentication, user accounts, sessions, password recovery |
| **Platform Core** | Notifications | ⏳ Planned | Multi-channel notifications (email, push, in-app) |
| **Platform Core** | Auditing | ⏳ Planned | Activity logs, audit trail, immutable event recording |
| **Financial Core** | Wallets | ⏳ Planned | Personal + shared wallets, invitations, splits, debts, settlements |
| **Financial Core** | Transactions | ⏳ Planned | Income, expense, transfer recording + categorization |
| **Planning** | Budgets | ⏳ Planned | Spending limits, budget monitoring, alerts |
| **Planning** | Goals | ⏳ Planned | Savings targets, progress tracking, milestones |
| **Planning** | Recurring | ⏳ Planned | Pattern management, automatic transaction generation |

**Note:** Collaboration features are integrated into the Wallets module. Categories are integrated into the Transactions module.

### Communication Patterns

- **Sync (IModuleClient):** Request-response for immediate data needs (e.g., Budgets queries Transactions for spending data)
- **Async (IModuleEventBus + Outbox):** Fire-and-forget with guaranteed delivery via transactional outbox (e.g., TransactionRecordedEvent triggers debt recalculation, budget updates)

### Key Architectural Decisions

- **Vertical Slices:** Each feature lives in its own folder with endpoint, handler, and validator
- **Screaming Architecture:** Folder names reflect business capabilities, not technical layers
- **Event-Driven:** Modules communicate via integration events, never direct references
- **One Schema Per Module:** Each module has its own PostgreSQL schema for logical separation
- **No Cross-Module References:** Module A never references Module B's project (enforced by architecture tests)

---

## 🛠️ Tech Stack

### Backend (.NET 10)

- **Runtime:** .NET 10 (LTS)
- **API:** ASP.NET Core Minimal APIs with native REPR pattern (IEndpoint interface)
- **Database:** PostgreSQL 18 with Entity Framework Core 10.0
- **Validation:** FluentValidation 12.1
- **Caching:** FusionCache + Redis
- **Background Jobs:** Hangfire + PostgreSQL storage
- **Observability:** Serilog + OpenTelemetry + Aspire Dashboard
- **Resilience:** Polly (retries, circuit breaker, timeouts)
- **Event Reliability:** Outbox Pattern (transactional event publishing)
- **Health Checks:** AspNetCore.HealthChecks (PostgreSQL, Redis, RustFS, ClickHouse)
- **API Docs:** Scalar (replaces Swagger)
- **Email:** MailKit (SMTP client)
- **Storage:** MinIO SDK (S3-compatible client for RustFS)
- **Time:** NodaTime (replaces DateTime/DateTimeOffset)
- **UUIDs:** Medo.Uuid7 (UUIDv7 for entity IDs)

### Frontend (Vue 3 PWA)

- **Framework:** Vue.js 3 with Composition API (`<script setup>`)
- **Build Tool:** Vite
- **Language:** TypeScript (strict mode)
- **State Management:** Pinia (setup function style)
- **HTTP Client:** Axios (with interceptors for auth)
- **Routing:** Vue Router
- **UI Components:** shadcn-vue (accessible, customizable)
- **Styling:** Tailwind CSS v4 (utility-first, no config file)
- **Icons:** @hugeicons/vue (4,600+ free icons, tree-shakeable)
- **Forms:** VeeValidate + Zod
- **Dates:** date-fns
- **Charts:** Radix UI
- **i18n:** vue-i18n (English + Spanish)
- **Testing:** Vitest (unit) + Playwright (E2E)
- **Linting & Formatting:** oxlint + oxfmt
- **Package Manager:** Bun

### Email Rendering Service

- **Runtime:** Bun
- **HTTP Server:** Hono (micro framework)
- **Templates:** React Email (type-safe components)
- **Linting & Formatting:** oxlint + oxfmt

### Infrastructure

- **Database:** PostgreSQL 18 (Alpine)
- **Cache:** Redis 8 (Alpine)
- **Storage:** RustFS 1.0.0-alpha.83 (S3-compatible, Apache 2.0)
- **Analytics:** ClickHouse 24 (Alpine)
- **Email Dev:** Mailpit (SMTP capture + web UI)
- **Observability:** Aspire Dashboard (OpenTelemetry UI)
- **Container Orchestration:** Docker Compose
- **CI/CD:** GitHub Actions

### Testing

- **Backend:** xUnit v3 + NSubstitute + Testcontainers (PostgreSQL)
- **Frontend:** Vitest + Playwright
- **Architecture:** NetArchTest.Rules (module boundary enforcement)
- **Coverage:** Built-in .NET coverage + Vitest coverage

---

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Bun 1.1+** — [Install](https://bun.sh/)
- **Docker + Docker Compose** — [Install](https://docs.docker.com/get-docker/)
- **Git** — [Install](https://git-scm.com/downloads)

### First-Time Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/rafitajaen/kakeibo.git
   cd kakeibo
   ```

2. **Copy environment files**
   ```bash
   cp .env.example .env
   cp src/Kakeibo.Api/.env.local.example src/Kakeibo.Api/.env.local
   ```

3. **Edit `.env` with your secrets**
   - `POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `JWT_SECRET_KEY`, etc.
   - Never commit `.env` to version control

4. **Install dependencies**
   ```bash
   bun install                # Root + all sub-projects
   bun run api:restore        # .NET packages
   ```

5. **Start infrastructure services**
   ```bash
   docker compose up -d       # PostgreSQL, Redis, RustFS, ClickHouse, Mailpit, Email renderer
   ```

6. **Run the API locally** (outside Docker)
   ```bash
   cd src/Kakeibo.Api
   dotnet run
   ```

7. **Access services**
   - API: http://localhost:5000
   - Scalar (API docs): http://localhost:5000/scalar
   - Mailpit (email UI): http://localhost:8025
   - RustFS console: http://localhost:9001
   - Redis Insight: http://localhost:5540
   - Aspire Dashboard: http://localhost:18888

### Environment Variables

The project uses a **layered environment strategy**:

- **`.env`** (root) — Secrets and primitives (PostgreSQL password, JWT secret, etc.)
- **`.env.local`** (API) — Localhost connection strings assembled via `${VAR}` interpolation
- **`docker-compose.yml`** — Assembles connection strings for Docker containers

**Never commit `.env` or `.env.local` to version control.** Copy from `.env.example` and `.env.local.example`.

---

## 📂 Project Structure

```
Kakeibo.slnx                            # Solution file (.slnx format)
├── .claude/                            # AI agent configuration
│   ├── roadmap/                        # Detailed phase planning
│   ├── rules/                          # Architecture, tech stack, patterns
│   └── skills/                         # Reusable agent skills
├── .docker/                            # Docker auxiliary configs
│   └── clickhouse/                     # ClickHouse overrides
├── .github/                            # CI/CD workflows
│   └── workflows/
│       ├── quality.yml                 # PR quality gates
│       └── release.yml                 # Build + push images to Docker Hub
├── src/
│   ├── Kakeibo.Api/                    # ASP.NET Core host (composition root)
│   ├── Kakeibo.Common/                 # Shared kernel (zero project references)
│   ├── Kakeibo.Contracts/              # Inter-module contracts (events, requests, DTOs)
│   ├── Kakeibo.Infrastructure/         # Cross-cutting concerns (cache, storage, messaging)
│   └── Kakeibo.Modules.*/              # 8 business modules (Identity, Wallets, Transactions, etc.)
├── services/
│   └── Kakeibo.Email/                  # Email renderer (Bun + Hono + React Email)
├── sites/
│   └── Kakeibo.App/                    # Vue 3 PWA (frontend)
├── tests/
│   ├── Kakeibo.Modules.*.Tests/        # Module unit + integration tests
│   ├── Kakeibo.FunctionalTests/        # API-level tests (WebApplicationFactory)
│   └── Kakeibo.ArchitectureTests/      # Module boundary enforcement (NetArchTest)
├── Directory.Build.props               # Centralized MSBuild properties
├── Directory.Packages.props            # Centralized NuGet package versions (CPM)
├── docker-compose.yml                  # Infrastructure + application services
├── lefthook.yml                        # Pre-commit hooks (commitlint + oxlint + oxfmt)
├── commitlint.config.ts                # Conventional commits enforcement
└── package.json                        # Monorepo scripts (API, App, Email, Docker)
```

---

## 💻 Development

### Available Commands

All commands run from the **monorepo root** via `bun run <script>`.

#### API Commands (Backend)

| Command | Description |
|---------|-------------|
| `bun run api:restore` | Restore .NET packages |
| `bun run api:build` | Build solution in Release mode |
| `bun run api:format:check` | Verify C# formatting (CI only — user runs `dotnet format` manually) |
| `bun run api:test` | Run Identity module tests |

#### App Commands (Frontend)

| Command | Description |
|---------|-------------|
| `bun run app:install` | Install Vue app dependencies |
| `bun run app:dev` | Start Vite dev server (http://localhost:5173) |
| `bun run app:build` | Build production bundle |
| `bun run app:typecheck` | TypeScript type checking |
| `bun run app:lint` | Auto-fix lint issues (oxlint) |
| `bun run app:lint:check` | Check lint issues without fixing |
| `bun run app:format` | Auto-format code (oxfmt) |
| `bun run app:format:check` | Verify formatting without fixing |
| `bun run app:test:unit` | Run Vitest unit tests |
| `bun run app:test:e2e` | Run Playwright E2E tests |

#### Email Commands (Email Service)

| Command | Description |
|---------|-------------|
| `bun run email:install` | Install email service dependencies |
| `bun run email:typecheck` | TypeScript type checking |
| `bun run email:lint` | Auto-fix lint issues (oxlint) |
| `bun run email:format` | Auto-format code (oxfmt) |
| `bun run email:format:check` | Verify formatting without fixing |
| `bun run email:test` | Run Bun tests |

#### Docker Commands

| Command | Description |
|---------|-------------|
| `bun run docker:build:api` | Build API Docker image |
| `bun run docker:build:app` | Build App Docker image |
| `bun run docker:build:email` | Build Email Docker image |
| `bun run docker:build:all` | Build all three images |
| `docker compose up -d` | Start infrastructure services only |
| `docker compose --profile app up -d` | Start full stack (infra + API + App) |
| `docker compose down` | Stop all services |
| `docker compose logs -f` | Tail container logs |

### Development Workflow

**Option 1: Hybrid (Recommended for development)**
- Infrastructure in Docker (`docker compose up -d`)
- API via `dotnet run` (src/Kakeibo.Api/)
- App via `bun run app:dev` (Vite dev server)

**Option 2: Full Docker**
- `docker compose --profile app up -d`
- API at http://localhost:5000
- App at http://localhost:3000

### Pre-Commit Hooks

The project uses `lefthook` to enforce quality standards before every commit:

- **commit-msg:** Conventional commits format (`type(scope): description`)
- **pre-commit:** Runs `oxlint --deny-warnings` + `oxfmt --check` on staged files in `sites/Kakeibo.App/`

**Important:** Always run auto-fix commands before committing:
```bash
bun run app:format && bun run app:lint
git add <modified-files>   # Re-stage files modified by formatters
git commit -m "feat(app): add new feature"
```

Backend formatting (`dotnet format`) is **never** run by Claude — the user runs it manually (see `.claude/rules/mandatory.md` Rule 7).

---

## 📊 Roadmap

**Current Phase:** Phase 1a - Infrastructure Base (Partially Complete)

For detailed phase documentation, see [`.claude/roadmap/roadmap.md`](./.claude/roadmap/roadmap.md).

### Phase Overview

| Phase | Status | Deliverables |
|-------|--------|-------------|
| **0 - Infrastructure** | 🔨 Partial | Solution structure, Docker Compose, CI skeleton, Email service |
| **1 - Identity** | 🔨 In Progress | Registration, login, email verification, OAuth (Google, Apple) |
| **2 - Wallets + Collaboration** | ⏳ Planned | Personal/shared wallets, invitations, splits, debts, settlements |
| **3 - Transactions + Categories** | ⏳ Planned | Recording, editing, deletion, categorization, 12 system categories |
| **4 - Budgets** | ⏳ Planned | Personal + shared budgets, spending tracking, warnings |
| **5 - Goals** | ⏳ Planned | Savings targets, milestones, progress tracking |
| **6 - Recurring** | ⏳ Planned | Pattern management, auto-generation, forecasting |
| **7 - Notifications + Auditing** | ⏳ Planned | Multi-channel notifications, audit trail, activity logs |
| **8 - Dashboard + Launch** | ⏳ Planned | Onboarding flow, settings, production deployment |

### Key Features (Planned)

- ✨ **Personal Finance Tracking** — Individual wallets, transactions, budgets, goals
- ✨ **Collaborative Expenses** — Shared wallets, Splitwise-style debt tracking, settlements
- ✨ **Smart Categorization** — 12 system categories + unlimited custom categories
- ✨ **Recurring Transactions** — Automated generation with 90-day forecasting
- ✨ **Budget Management** — Real-time monitoring, warnings, projections
- ✨ **Savings Goals** — Milestone tracking, progress visualization
- ✨ **Multi-Channel Notifications** — Email, push, in-app alerts
- ✨ **Activity Audit Trail** — Immutable logging for transparency
- ✨ **PWA** — Installable web app with offline support
- ✨ **i18n** — English + Spanish (more languages planned)

---

## 🏛️ Architecture Highlights

### Module Dependency Diagram

```
                      IDENTITY
               (all modules depend on this)
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
    WALLETS                                   AUDITING ◄── All emit audit events
(incl Collaboration)
        │
        ▼
  TRANSACTIONS
 (incl Categories)
        │
        ├───────────┬─────────────┐
        ▼           ▼             ▼
    BUDGETS      GOALS       RECURRING
        │           │
        └─────┬─────┘
              ▼
      NOTIFICATIONS ◄── All emit notifications
```

### Communication Patterns

**Synchronous (IModuleClient)**
- Use when the caller needs an immediate response to proceed
- Example: Budgets queries Transactions for spending in a period

**Asynchronous (IModuleEventBus + Outbox)**
- Use when the caller doesn't need an immediate response
- Guaranteed delivery via transactional outbox + background polling
- Example: TransactionRecordedEvent triggers debt recalculation (Wallets), budget update (Budgets), goal progress (Goals)

### Outbox Pattern

1. Handler publishes integration event via `eventBus.PublishAsync()`
2. Event buffered in-memory (scoped lifetime)
3. `SaveChangesAsync()` commits entity changes + outbox messages in **one transaction** (via `OutboxInterceptor`)
4. `OutboxProcessor` background service polls outbox tables
5. Dispatches events to `IEventConsumer<T>` handlers
6. Marks messages as processed
7. Polly retry on failure (3x exponential: 1s, 5s, 15s)

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

### Conventional Commits

All commits must follow the [Conventional Commits](https://www.conventionalcommits.org/) format:

```
type(scope): description

[optional body]

[optional footer]
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`

**Scopes:** `app`, `api`, `email`, `docs`, `infra`, `deps`, `release`, `skills`, `roadmap`

Example: `feat(api): implement user registration endpoint`

### Versioning & Releases

This project uses [semantic-release](https://semantic-release.gitbook.io/) for automated versioning and release management.

**How it works:**
- Every merge to `main` triggers semantic-release
- Commits are analyzed using conventional commits format
- Version is calculated automatically based on commit types:
  - `feat`: Minor version bump (0.x.0)
  - `fix`, `perf`, `revert`, `refactor`: Patch bump (0.0.x)
  - Breaking changes (footer `BREAKING CHANGE:`): Major bump (x.0.0)
- `CHANGELOG.md` is updated automatically
- GitHub Release is created with auto-generated notes
- Version tag is pushed to repository

**What you need to know:**
- Follow conventional commits format (enforced by commitlint)
- Don't manually edit version in `package.json`
- Don't manually create tags or releases
- Don't manually edit `CHANGELOG.md`

**Example flow:**
```bash
# Feature branch
git checkout -b feat/wallets-create-endpoint
git commit -m "feat(wallets): add wallet creation endpoint"
git push origin feat/wallets-create-endpoint

# Create PR → Merge to main
# → semantic-release runs automatically
# → Version bumps from v0.1.0 to v0.2.0
# → CHANGELOG.md updated
# → GitHub Release created
```

For detailed configuration and troubleshooting, see [`.claude/rules/ci.md`](./.claude/rules/ci.md).

### Quality Gates

Before committing:

1. **Frontend:** `bun run app:format && bun run app:lint && bun run app:test:unit`
2. **Email:** `bun run email:format && bun run email:lint && bun run email:test`
3. **Backend:** Run tests via `bun run api:test`
4. **Re-stage modified files:** `git add <files>` (formatters modify files on disk)

### Pre-Commit Hooks

The project uses `lefthook` to enforce quality before every commit:
- **commit-msg:** Conventional commits validation (via commitlint)
- **pre-commit:** `oxlint --deny-warnings` + `oxfmt --check` on staged frontend files

### Documentation Requirements

When adding features or changing architecture:

- Update `.claude/rules/architecture.md` for architectural changes
- Update `.claude/rules/technical-debt.md` for new code patterns
- Update `.claude/rules/knowledge.md` for lessons learned
- Update this `README.md` for user-facing changes
- Update `.claude/roadmap/roadmap.md` for phase milestones

### Pull Request Process

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/api-amazing-feature`
3. Commit changes following Conventional Commits
4. Push to your fork: `git push origin feat/api-amazing-feature`
5. Open a Pull Request against `main`
6. CI will run quality gates (lint, format check, test, build)
7. Maintainers will review and merge

---

## 📄 License

MIT License - See [LICENSE](./LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Hani Motoko** — Creator of the original Kakeibo method (1904)
- **Splitwise** — Inspiration for debt tracking UX
- **.NET Foundation** — ASP.NET Core, Entity Framework Core
- **Vue.js Team** — Modern, progressive JavaScript framework
- **shadcn** — Accessible component system (shadcn-vue port)
- **Tailwind Labs** — Utility-first CSS framework
- **PostgreSQL Global Development Group** — World's most advanced open-source database
- **Redis Labs** — In-memory data structure store
- **Bun Team** — Fast all-in-one JavaScript runtime

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/rafitajaen/kakeibo/issues)
- **Discussions:** [GitHub Discussions](https://github.com/rafitajaen/kakeibo/discussions)

---

**Built with 💚 for mindful money management**

*Transform your relationship with money through conscious tracking and intentional spending.*
