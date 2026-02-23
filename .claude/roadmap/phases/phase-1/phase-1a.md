# Phase 1a: Infrastructure Base

**Status**: Partially Complete
**Objective**: Establish foundational infrastructure for development and deployment

---

## Prerequisites

| Dependency | Source | Required For |
|------------|--------|-------------|
| Docker Compose | docker.com | Infrastructure services |
| GitHub account | github.com | CI/CD pipelines |
| Bun | bun.sh | Email service runtime, frontend package management |
| .NET 10 SDK | microsoft.com/dotnet | API development |

---

## Scope

### ✅ Included

- Solution structure: All 12 projects scaffolded with minimal folder structure
- Docker Compose: 8 infrastructure services (PostgreSQL, Redis, RustFS, ClickHouse, Mailpit, Redis Insight, Aspire Dashboard, Email Renderer)
- Email service: Bun + Hono + React Email microservice on port 3050 (with /health endpoint)
- Vue PWA shell: Vite + Vue 3 + TypeScript + Tailwind CSS v4 + shadcn-vue (with router + Pinia configured)
- CI pipeline: GitHub Actions quality gates (api, app, email, docker)
- Pre-commit hooks: lefthook.yml (commitlint, oxlint, oxfmt)
- Development scripts: `bun run setup`, `bun run dev:infra`, `bun run dev:all`
- Common interfaces: `IDomainEvent`, `IIntegrationEvent`, `IDomainEventHandler<T>`, `IEventConsumer<T>`, `IModuleRequest<T>`, `IModuleRequestHandler<,>`
- Empty DbContexts: Each module has `{Module}DbContext.cs` with schema constant
- Program.cs: Functional with /health endpoint and Scalar configured
- appsettings.json: Structure with all configuration sections (empty values)

### ❌ Excluded

- Any module implementation (Phase 1b onwards)
- Architecture tests with real content (will work once modules have code)
- Semantic release configuration (deferred)

---

## Deliverables

### New Files

**Root**:
- `Kakeibo.slnx` — Solution with all 12 projects
- `Directory.Build.props` — Centralized build properties
- `Directory.Packages.props` — Central Package Management
- `.editorconfig` — Code style enforcement
- `docker-compose.yml` — Infrastructure + app services
- `.docker/clickhouse/*.xml` — ClickHouse config overrides
- `lefthook.yml` — Pre-commit hooks
- `scripts/setup-local.sh` — Idempotent setup script

**Infrastructure Projects**:
- `src/Kakeibo.Api/` — ASP.NET host with Program.cs, /health endpoint
- `src/Kakeibo.Common/` — Shared kernel with all base interfaces
- `src/Kakeibo.Contracts/` — Folder structure for 8 modules (empty)
- `src/Kakeibo.Infrastructure/` — Folder structure for cross-cutting concerns

**Module Projects** (8 modules):
- `src/Kakeibo.Modules.{Module}/` — Each with:
  - `{Module}DbContext.cs` (empty, with schema constant)
  - Folder structure: `Entities/`, `Features/`, `Persistence/`, `Services/`
  - `{Module}ModuleRegistration.cs` (minimal DI registration)

**Frontend**:
- `sites/Kakeibo.App/` — Vue PWA with router configured, Pinia setup, minimal layout

**Email Service**:
- `services/Kakeibo.Email/` — Bun + Hono service with /health endpoint

**CI/CD**:
- `.github/workflows/quality.yml` — Quality gates for PR
- `.github/workflows/release.yml` — Build and push Docker images

**Tests**:
- `tests/Kakeibo.ArchitectureTests/` — NetArchTest enforcement (may have failing tests until modules have content)
- `tests/Kakeibo.Modules.{Module}.Tests/` — Empty test projects (one per module)

---

## Acceptance Criteria

- [ ] All 12 .csproj files exist and build successfully (`dotnet build Kakeibo.slnx`)
- [ ] `docker compose up -d` starts 8 infrastructure services successfully
- [ ] All infrastructure services pass health checks (PostgreSQL, Redis, RustFS, ClickHouse)
- [ ] Email renderer service responds HTTP 200 on http://localhost:3050/health
- [ ] API responds HTTP 200 on http://localhost:5000/health
- [ ] Vue PWA builds successfully (`bun run app:build`)
- [ ] GitHub Actions CI has all 4 jobs defined (api, app, email, docker)
- [ ] Pre-commit hooks are configured (commitlint + oxlint + oxfmt)
- [ ] `bun run setup` completes successfully (idempotent)
- [ ] All Common interfaces exist: `IDomainEvent`, `IIntegrationEvent`, `IDomainEventHandler<T>`, `IEventConsumer<T>`
- [ ] All module DbContexts exist with schema constant
- [ ] appsettings.json has all sections structured

---

## Definition of "Phase 1a Completed"

1. All acceptance criteria checked (12 items)
2. All services start successfully with Docker Compose
3. CI pipeline runs without errors (tests may be minimal)
4. Development environment is fully functional
5. Phase 1b (Identity Backend) can begin

---

**Next Sub-Phase:** [Phase 1b: Identity Backend](./phase-1b.md)
