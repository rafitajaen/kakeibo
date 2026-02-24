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

- Solution structure: 2 projects (`Kakeibo.Api` + `Kakeibo.Tests`) with vertical slice folder layout
- Architecture tests: Naming convention rules in `tests/Kakeibo.Tests/Architecture/` (incremental — additional rules added per phase as new patterns are introduced)
- Docker Compose: 8 infrastructure services (PostgreSQL, Redis, RustFS, ClickHouse, Mailpit, Redis Insight, Aspire Dashboard, Email Renderer)
- Email service: Bun + Hono + React Email microservice on port 3050 (with /health endpoint)
- Vue PWA shell: Vite + Vue 3 + TypeScript + Tailwind CSS v4 + shadcn-vue (with router + Pinia configured)
- CI pipeline: GitHub Actions quality gates (api, app, email, docker)
- Pre-commit hooks: lefthook.yml (commitlint, oxlint, oxfmt)
- Development scripts: `bun run setup`, `bun run dev:infra`, `bun run dev:all`
- Common abstractions: `Entity`, `Result<T>`, `Error`, `ValueObject`, `IEndpoint`, `ValidationFilter`, `EndpointExtensions`
- Event system: `IEvent`, `IEventBus`, `IEventHandler<T>` interfaces + `ChannelEventBus` (singleton) + `EventDispatcher` (BackgroundService)
- Single `AppDbContext` with `UseSnakeCaseNamingConvention()` and `UseNodaTime()`
- Program.cs: Functional with /health endpoint and Scalar configured
- appsettings.json: Structure with all configuration sections (empty values)

### ❌ Excluded

- Any feature implementation (Phase 1b onwards)
- Semantic release configuration (deferred)

---

## Deliverables

### New Files

**Root**:
- `Kakeibo.slnx` — Solution with 2 projects
- `Directory.Build.props` — Centralized build properties
- `Directory.Packages.props` — Central Package Management
- `.editorconfig` — Code style enforcement
- `docker-compose.yml` — Infrastructure + app services
- `.docker/clickhouse/*.xml` — ClickHouse config overrides
- `lefthook.yml` — Pre-commit hooks
- `scripts/setup-local.sh` — Idempotent setup script

**API Project** (`src/Kakeibo.Api/`):
- `Program.cs` — Composition root with /health and Scalar
- `Kakeibo.Api.csproj` — Single project with all NuGet references
- `Common/Abstractions/` — `Entity.cs`, `Result.cs`, `Error.cs`, `ValueObject.cs`
- `Common/Endpoints/` — `IEndpoint.cs`, `ValidationFilter.cs`, `EndpointExtensions.cs`
- `Common/Utils/` — `Guid7.cs`, `PasswordHasher.cs`, `DefaultSerializer.cs`, `CharSets.cs`, `RandomString.cs`
- `Infrastructure/Events/` — `IEvent.cs`, `IEventBus.cs`, `IEventHandler.cs`, `ChannelEventBus.cs`, `EventDispatcher.cs`
- `Infrastructure/Email/` — `IEmailService.cs`, `EmailService.cs`, `SmtpOptions.cs`
- `Infrastructure/Caching/` — `ICacheService.cs`, `FusionCacheService.cs`, `CachingOptions.cs`
- `Infrastructure/Storage/` — `IStorageService.cs`, `StorageService.cs`, `StorageOptions.cs`
- `Persistence/AppDbContext.cs` — Single DbContext (empty DbSets, ready for entity registration)
- `Features/` — Empty folder structure for future slices

**Test Project** (`tests/Kakeibo.Tests/`):
- `Kakeibo.Tests.csproj` — xUnit v3 + Testcontainers + NSubstitute + NetArchTest
- `Architecture/` — NetArchTest rules:
  - Endpoints end in `Endpoint`
  - Validators end in `Validator`
  - Event handlers end in `Handler` and implement `IEventHandler<T>`
  - Configuration classes end in `Options` (never `Settings` or `Config`)
  - *Additional rules added incrementally as new patterns are introduced*

**Frontend**:
- `sites/Kakeibo.App/` — Vue PWA with router configured, Pinia setup, minimal layout

**Email Service**:
- `services/Kakeibo.Email/` — Bun + Hono service with /health endpoint

**CI/CD**:
- `.github/workflows/quality.yml` — Quality gates for PR
- `.github/workflows/release.yml` — Semantic-release + Docker image builds

---

## Acceptance Criteria

- [ ] 2 `.csproj` files exist and build successfully (`dotnet build Kakeibo.slnx`)
- [ ] `docker compose up -d` starts 8 infrastructure services successfully
- [ ] All infrastructure services pass health checks (PostgreSQL, Redis, RustFS, ClickHouse)
- [ ] Email renderer service responds HTTP 200 on http://localhost:3050/health
- [ ] API responds HTTP 200 on http://localhost:5000/health
- [ ] Vue PWA builds successfully (`bun run app:build`)
- [ ] GitHub Actions CI has all 4 jobs defined (api, app, email, docker)
- [ ] Pre-commit hooks are configured (commitlint + oxlint + oxfmt)
- [ ] `bun run setup` completes successfully (idempotent)
- [ ] Common abstractions exist: `Entity`, `Result<T>`, `Error`, `IEndpoint`
- [ ] Event system operational: `IEvent`, `IEventBus`, `IEventHandler<T>`, `ChannelEventBus`, `EventDispatcher`
- [ ] `AppDbContext` exists and is registered in DI
- [ ] appsettings.json has all sections structured
- [ ] Architecture tests project exists with base NetArchTest naming rules
- [ ] Architecture tests pass (all base rules green)

---

## Definition of "Phase 1a Completed"

1. All acceptance criteria checked (15 items)
2. All services start successfully with Docker Compose
3. CI pipeline runs without errors (tests may be minimal)
4. Development environment is fully functional
5. Phase 1b (Identity Backend) can begin

---

**Next Sub-Phase:** [Phase 1b: Identity Backend](./phase-1b.md)

---

*Note: The Events System (ChannelEventBus + EventDispatcher) was completed as part of Phase 1a. Phase 1c previously referred to "Events System" but has been renumbered to "Audit Logging" since the events implementation is included here.*
