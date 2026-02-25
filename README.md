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

**Current Status:** ✅ **Phase 6 (Recurring Transactions) — Complete**

### What Works Now
- ✅ Solution structure: 2 projects (Kakeibo.Api + Kakeibo.Tests)
- ✅ Docker Compose environment (8 infrastructure services)
- ✅ CI/CD pipeline (GitHub Actions + semantic-release)
- ✅ Development tooling (lefthook, commitlint, oxlint, oxfmt)
- ✅ Email rendering service (Bun + Hono + React Email) — `WalletInvitation` template
- ✅ Core abstractions: Entity, Result&lt;T&gt;, Error, IEndpoint, IEventBus, ChannelEventBus, EventDispatcher
- ✅ Architecture tests: naming convention enforcement (Kakeibo.Tests)
- ✅ **Phase 1 — Identity:** Registration, login, JWT tokens, sessions, password recovery, audit logging, full auth frontend
- ✅ **Phase 2 — Wallets + Collaboration:** Personal wallets, shared wallets, invitations, member management, splits, debt calculation, settlements
- ✅ **Phase 3 — Transactions + Categories:** Recording income/expense/transfer, 12 system categories + custom categories, balance tracking
- ✅ **Phase 4 — Budgets:** Spending limits, budget monitoring (on track / warning / exceeded), multi-wallet support, alerts
- ✅ **Phase 5 — Goals:** Savings targets, 3 tracking modes (wallet-linked/cross-wallet/manual), milestones, projected completion
- ✅ **Phase 6 — Recurring Transactions:** Pattern CRUD, recurrence rules (daily/weekly/biweekly/monthly/yearly), Hangfire auto-generation job, 30/90-day forecast

### What's Coming
- 🔨 **Phase 7 (Next):** Notifications + Auditing — multi-channel notifications, in-app notification center, audit trail UI
- ⏳ **Phase 8 (Planned):** Dashboard + Onboarding + Settings + Production launch

For detailed roadmap, see [`.claude/roadmap/roadmap.md`](./.claude/roadmap/roadmap.md).

---

## 🏗️ Architecture

### Simple Monolith

Kakeibo uses a **simple monolith** with vertical slices and screaming architecture:

- **2 projects:** `src/Kakeibo.Api/` (single runnable host) + `tests/Kakeibo.Tests/`
- **Domain separation by folder**, not by assembly — `Features/Identity/`, `Features/Wallets/`, etc.
- **Single `AppDbContext`** for all domains — one schema, one migrations history
- **In-memory events** via `System.Threading.Channels` — fire-and-forget with no external queue

### Domain Structure (8 Business Domains in `Features/`)

| Tier | Domain | Status | Description |
|------|--------|--------|-------------|
| **Platform Core** | Identity | ✅ Done | Authentication, user accounts, sessions, password recovery |
| **Platform Core** | Notifications | ⏳ Planned | Multi-channel notifications (email, push, in-app) |
| **Platform Core** | Auditing | ✅ Done (Phase 1c) | Activity logs, audit trail, immutable event recording |
| **Financial Core** | Wallets | ✅ Done | Personal + shared wallets, invitations, splits, debts, settlements |
| **Financial Core** | Transactions | ✅ Done | Income, expense, transfer recording + categorization |
| **Planning** | Budgets | ✅ Done | Spending limits, budget monitoring, alerts |
| **Planning** | Goals | ✅ Done | Savings targets, progress tracking, milestones |
| **Planning** | Recurring | ✅ Done | Pattern management, automatic transaction generation |

**Note:** Collaboration features live in `Features/Wallets/`. Categories live in `Features/Transactions/`.

### Communication Patterns

- **In-process events:** `IEventBus` + `ChannelEventBus` + `EventDispatcher` BackgroundService
- **No message broker, no outbox, no cross-assembly contracts**

### Key Architectural Decisions

- **Vertical Slices:** Each feature lives in `Features/{Domain}/{Operation}/` with endpoint, handler, and validator
- **Screaming Architecture:** Folder names reflect business capabilities, not technical layers
- **Single deployment unit:** One API project, one test project, no per-module projects

---

## 🛠️ Tech Stack

### Backend (.NET 10)

- **Runtime:** .NET 10 (LTS)
- **API:** ASP.NET Core Minimal APIs with native REPR pattern (IEndpoint interface)
- **Database:** PostgreSQL 18 with Entity Framework Core 10.0 (single AppDbContext)
- **Validation:** FluentValidation 12.1
- **Caching:** FusionCache + Redis
- **Background Jobs:** Hangfire + PostgreSQL storage
- **Observability:** Serilog + OpenTelemetry + Aspire Dashboard
- **Resilience:** Polly (retries, circuit breaker, timeouts)
- **Event Bus:** System.Threading.Channels (IEventBus, ChannelEventBus, EventDispatcher)
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

- **Backend:** xUnit v3 + NSubstitute + Testcontainers (PostgreSQL) — all in `tests/Kakeibo.Tests/`
- **Frontend:** Vitest + Playwright
- **Architecture:** NetArchTest.Rules (naming convention enforcement)
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
Kakeibo.slnx                            # Solution file (.slnx format) — 2 projects
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
│   ├── Kakeibo.Api/                    # Single runnable project (ASP.NET host + all domains)
│   │   ├── Common/                     # Abstractions, Endpoints, Utils
│   │   ├── Features/                   # Identity/, Wallets/, Transactions/, Budgets/, Goals/, Recurring/, Notifications/, Auditing/
│   │   ├── Infrastructure/             # Caching/, Email/, Storage/, Events/
│   │   ├── Persistence/                # AppDbContext.cs, Configurations/, Migrations/
│   │   └── Program.cs
│   ├── Kakeibo.Email/                  # Email renderer (Bun + Hono + React Email)
│   └── Kakeibo.App/                    # Vue 3 PWA (frontend)
├── tests/
│   └── Kakeibo.Tests/                  # All tests: unit, integration, architecture (NetArchTest)
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
| `bun run api:test` | Run all tests |

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
- **pre-commit:** Runs `oxlint --deny-warnings` + `oxfmt --check` on staged files in `src/Kakeibo.App/`

**Important:** Always run auto-fix commands before committing:
```bash
bun run app:format && bun run app:lint
git add <modified-files>   # Re-stage files modified by formatters
git commit -m "feat(app): add new feature"
```

Backend formatting (`dotnet format`) is **never** run by Claude — the user runs it manually (see `.claude/rules/mandatory.md` Rule 7).

---

## 📊 Roadmap

**Current Phase:** Phase 6 - Recurring Transactions (Complete) — Phase 7 next

For detailed phase documentation, see [`.claude/roadmap/roadmap.md`](./.claude/roadmap/roadmap.md).

### Phase Overview

| Phase | Status | Deliverables |
|-------|--------|-------------|
| **1a - Infrastructure Base** | ✅ Done | Solution structure, Docker Compose, CI pipeline, Email service, core abstractions, ChannelEventBus |
| **1b - Identity Backend** | ✅ Done | Registration, login, JWT tokens, sessions, password recovery |
| **1c - Audit Logging** | ✅ Done | ClickHouse integration, IEventHandler implementations |
| **1d - Identity Frontend** | ✅ Done | Login/register screens, token refresh, route guards |
| **2a - Personal Wallets** | ✅ Done | Personal wallet CRUD, archive, events, frontend store + views |
| **2b - Shared Wallets + Invitations** | ✅ Done | Shared wallets, member management, invitation flow, email template |
| **2c - Splits + Debts + Settlements** | ✅ Done | Transaction splits, debt calculation, settlement recording |
| **3 - Transactions + Categories** | ✅ Done | Recording, editing, deletion, categorization, 12 system categories |
| **4 - Budgets** | ✅ Done | Personal + shared budgets, spending tracking, warnings |
| **5 - Goals** | ✅ Done | Savings targets, milestones, progress tracking |
| **6 - Recurring** | ✅ Done | Pattern management, auto-generation, forecasting |
| **7 - Notifications + Auditing** | 🔨 Next | Multi-channel notifications, audit trail, activity logs |
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

### Domain Dependency Order

```
Identity (foundation)
    │
    ▼
Wallets (incl Collaboration)
    │
    ▼
Transactions (incl Categories)
    │
    ├───────────┬─────────────┐
    ▼           ▼             ▼
Budgets      Goals       Recurring
    │           │
    └─────┬─────┘
          ▼
    Notifications ◄── All domains emit
    Auditing      ◄── All domains log
```

### Event System (System.Threading.Channels)

Replaces the Outbox Pattern. Async in-memory communication with no external queue:

```csharp
// Fire-and-forget — does not block SaveChangesAsync
eventBus.Publish(new TransactionRecordedEvent { ... });
await db.SaveChangesAsync(ct);
```

1. `IEventBus.Publish()` writes event to `Channel<IEvent>` (non-blocking)
2. `EventDispatcher` (BackgroundService) reads from channel continuously
3. Resolves `IEventHandler<T>` via DI in a new scope
4. Invokes each handler, logs errors (handler failures are isolated)

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
