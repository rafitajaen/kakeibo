# Phase 0: Base Infrastructure

**Objective:** Establish foundational infrastructure for development and deployment — Docker Compose environment, CI/CD pipeline, email rendering service, Vue PWA shell, and architecture tests.

**Status:** Partially complete

**Blocks:** All subsequent phases (1-8)

**Requires:** None (foundation phase)

---

## Prerequisites

| Dependency | Source | Required For |
|------------|--------|-------------|
| Docker Compose | docker.com | Infrastructure services (PostgreSQL, Redis, RustFS, ClickHouse) |
| GitHub account | github.com | CI/CD pipelines, Docker Hub image registry |
| Bun | bun.sh | Email service runtime, frontend package management |
| .NET 10 SDK | microsoft.com/dotnet | API development |

---

## Scope

### Included

| Area | Functionality |
|------|---------------|
| **Solution structure** | All 12 projects scaffolded: Api, Common, Contracts, Infrastructure, 8 modules (Identity, Notifications, Auditing, Wallets, Transactions, Budgets, Goals, Recurring) |
| **Docker Compose** | 8 infrastructure services: PostgreSQL 18, Redis, RustFS, ClickHouse, Mailpit, Redis Insight, Aspire Dashboard, Email Renderer |
| **Email service** | Bun + Hono + React Email microservice on port 3050 |
| **Vue PWA shell** | Vite + Vue 3 + TypeScript + Tailwind CSS v4 + shadcn-vue |
| **CI pipeline** | GitHub Actions quality gates: API (lint, build, test), App (lint, build, test), Email (typecheck, lint, test), Docker (build validation) |
| **Architecture tests** | NetArchTest enforcement: no cross-module references, naming conventions, dependency direction |
| **Pre-commit hooks** | lefthook.yml: commitlint (conventional commits), oxlint + oxfmt (frontend formatting) |
| **Development scripts** | `bun run setup` (idempotent local setup), `bun run dev:infra` (infrastructure only), `bun run dev:all` (full stack) |

### Excluded (later phases)

| Functionality | Target Phase |
|---------------|-------------|
| Identity module implementation | Phase 1 |
| Any module with business logic | Phases 2-8 |
| Production deployment | Phase 8d |

---

## Module Architecture

Not applicable — Phase 0 establishes infrastructure, not module implementation.

---

## MVP Acceptance Criteria

- [ ] All 12 .csproj files exist with correct project references (Common, Contracts, Infrastructure, 8 modules)
- [ ] `docker compose up -d` starts 8 infrastructure services successfully
- [ ] All infrastructure services pass health checks (PostgreSQL, Redis, RustFS, ClickHouse)
- [ ] Email renderer service responds on http://localhost:3050/health
- [ ] Vue PWA shell builds successfully (`bun run app:build`)
- [ ] GitHub Actions CI passes all 4 quality gates (api, app, email, docker)
- [ ] Architecture tests enforce module isolation (no cross-module references)
- [ ] Pre-commit hooks block commits with formatting issues
- [ ] `bun run setup` completes successfully (idempotent)

---

## Deliverables

### New Files

| File | Layer | Purpose |
|------|-------|---------|
| `Kakeibo.slnx` | Root | Solution file with all 12 projects |
| `Directory.Build.props` | Root | Centralized build properties (TargetFramework, TreatWarningsAsErrors, nullable) |
| `Directory.Packages.props` | Root | Central Package Management (CPM) |
| `.editorconfig` | Root | Code style enforcement (primary constructors, file-scoped namespaces) |
| `docker-compose.yml` | Root | Infrastructure services + app services (under `profiles: [app]`) |
| `.docker/clickhouse/*.xml` | Config | ClickHouse config overrides (logs, ipv4-only, low-resources) |
| `services/Kakeibo.Email/` | Email | Bun + Hono + React Email service |
| `sites/Kakeibo.App/` | Frontend | Vue 3 PWA shell |
| `.github/workflows/quality.yml` | CI | Quality gates for PR |
| `.github/workflows/release.yml` | CI | Build and push Docker images to Docker Hub |
| `lefthook.yml` | Root | Pre-commit hooks (commitlint, oxlint, oxfmt) |
| `tests/Kakeibo.ArchitectureTests/` | Tests | NetArchTest architecture enforcement |
| `scripts/setup-local.sh` | Scripts | Idempotent local development setup |

### Modified Files

None (Phase 0 creates from scratch)

### Database

Not applicable — Phase 0 only starts PostgreSQL container, no schema migrations yet.

---

## Technical Detail

### Solution Structure

```
Kakeibo.slnx
├── src/
│   ├── Kakeibo.Api/                    — Composition root (ASP.NET host)
│   ├── Kakeibo.Common/                 — Shared kernel (zero project references)
│   ├── Kakeibo.Contracts/              — Inter-module contracts (events, requests, DTOs)
│   ├── Kakeibo.Infrastructure/         — Technical cross-cutting concerns
│   │
│   ├── Kakeibo.Modules.Identity/       — Core: Authentication & users
│   ├── Kakeibo.Modules.Notifications/  — Core: Multi-channel notifications
│   ├── Kakeibo.Modules.Auditing/       — Core: Activity logs & audit trail
│   │
│   ├── Kakeibo.Modules.Wallets/        — Business: Wallets + Collaboration
│   ├── Kakeibo.Modules.Transactions/   — Business: Transactions + Categories
│   ├── Kakeibo.Modules.Budgets/        — Business: Spending limits
│   ├── Kakeibo.Modules.Goals/          — Business: Savings targets
│   └── Kakeibo.Modules.Recurring/      — Business: Pattern management
│
├── services/
│   └── Kakeibo.Email/                  — Email template rendering (Bun + Hono + React Email)
│
├── sites/
│   └── Kakeibo.App/                    — Web app (Vue PWA)
│
├── tests/
│   ├── Kakeibo.Modules.Identity.Tests/
│   ├── ... (one test project per module)
│   ├── Kakeibo.FunctionalTests/        — API-level tests
│   └── Kakeibo.ArchitectureTests/      — Module boundary enforcement
```

### Docker Compose Services

```yaml
services:
  postgresdb:
    image: postgres:18-alpine
    ports: ["5432:5432"]  # ⚠️ REMOVE ON PRODUCTION
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:8-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD}
    ports: ["6379:6379"]
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "--raw", "incr", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

  rustfs:
    image: rustfs/rustfs:1.0.0-alpha.83
    ports: ["9000:9000", "9001:9001"]
    environment:
      MINIO_ROOT_USER: ${STORAGE_ACCESS_KEY}
      MINIO_ROOT_PASSWORD: ${STORAGE_SECRET_KEY}
    volumes:
      - rustfs-data:/data
      - rustfs-logs:/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
      interval: 30s
      timeout: 20s
      retries: 3

  clickhouse:
    image: clickhouse/clickhouse-server:24-alpine
    ports: ["8123:8123"]
    volumes:
      - clickhouse-data:/var/lib/clickhouse
      - clickhouse-logs:/var/log/clickhouse-server
      - ./.docker/clickhouse/logs.xml:/etc/clickhouse-server/config.d/logs.xml:ro
      - ./.docker/clickhouse/ipv4-only.xml:/etc/clickhouse-server/config.d/ipv4-only.xml:ro
      - ./.docker/clickhouse/low-resources.xml:/etc/clickhouse-server/config.d/low-resources.xml:ro
    ulimits:
      nofile:
        soft: 262144
        hard: 262144
    healthcheck:
      test: ["CMD", "wget", "--spider", "-q", "http://localhost:8123/ping"]
      interval: 30s
      timeout: 5s
      retries: 3

  mailpit:
    image: axllent/mailpit:latest
    ports: ["1025:1025", "8025:8025"]

  redis-insight:
    image: redis/redisinsight:latest
    ports: ["5540:5540"]
    volumes:
      - redis-insight-data:/data

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    ports: ["18888:18888", "18889:18889"]
    environment:
      DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS: "true"

  kakeibo-email:
    build:
      context: ./services/Kakeibo.Email
      dockerfile: Dockerfile
    ports: ["3050:3050"]
    environment:
      NODE_ENV: development
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3050/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Application services (profiles: [app])
  kakeibo-api:
    profiles: [app]
    build:
      context: .
      dockerfile: src/Kakeibo.Api/Dockerfile
    ports: ["5000:5000"]
    depends_on:
      postgresdb: { condition: service_healthy }
      redis: { condition: service_healthy }
      rustfs: { condition: service_healthy }
      clickhouse: { condition: service_healthy }
      kakeibo-email: { condition: service_healthy }
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__PostgreSQL: "Host=postgresdb;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Redis__ConnectionString: "redis:6379,password=${REDIS_PASSWORD}"
      RustFS__Endpoint: "http://rustfs:9000"
      RustFS__AccessKey: ${STORAGE_ACCESS_KEY}
      RustFS__SecretKey: ${STORAGE_SECRET_KEY}
      ClickHouse__Host: clickhouse
      ClickHouse__Port: 8123
      EmailRenderer__BaseUrl: "http://kakeibo-email:3050"
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}

  kakeibo-app:
    profiles: [app]
    build:
      context: ./sites/Kakeibo.App
      dockerfile: Dockerfile
    ports: ["3000:80"]
    depends_on:
      - kakeibo-api
```

### GitHub Actions Quality Gates

```yaml
# .github/workflows/quality.yml
name: Quality Gates

on:
  pull_request:
    branches: [main]

jobs:
  quality-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0'
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props') }}
      - run: dotnet restore Kakeibo.slnx
      - run: dotnet format Kakeibo.slnx --verify-no-changes
      - run: dotnet build Kakeibo.slnx --configuration Release --no-restore
      - run: dotnet test Kakeibo.slnx --configuration Release --no-build

  quality-app:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - run: cd sites/Kakeibo.App && bun install --frozen-lockfile
      - run: cd sites/Kakeibo.App && bun run lint
      - run: cd sites/Kakeibo.App && bun run test:unit
      - run: cd sites/Kakeibo.App && bun run build

  quality-email:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - run: cd services/Kakeibo.Email && bun install --frozen-lockfile
      - run: cd services/Kakeibo.Email && bun run typecheck
      - run: cd services/Kakeibo.Email && bun run lint
      - run: cd services/Kakeibo.Email && bun run test

  quality-docker:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - name: Build API image (validation only)
        uses: docker/build-push-action@v5
        with:
          context: .
          file: src/Kakeibo.Api/Dockerfile
          push: false
      - name: Build App image (validation only)
        uses: docker/build-push-action@v5
        with:
          context: ./sites/Kakeibo.App
          file: sites/Kakeibo.App/Dockerfile
          push: false
      - name: Build Email image (validation only)
        uses: docker/build-push-action@v5
        with:
          context: ./services/Kakeibo.Email
          file: services/Kakeibo.Email/Dockerfile
          push: false
```

### Architecture Tests Example

```csharp
[Fact]
public void Modules_Should_Not_Reference_Other_Modules()
{
    var result = Types
        .InAssembly(typeof(WalletsModuleRegistration).Assembly)
        .Should()
        .NotHaveDependencyOnAny(
            "Kakeibo.Modules.Identity",
            "Kakeibo.Modules.Transactions",
            "Kakeibo.Modules.Budgets",
            "Kakeibo.Modules.Goals",
            "Kakeibo.Modules.Recurring",
            "Kakeibo.Modules.Notifications",
            "Kakeibo.Modules.Auditing")
        .GetResult();

    Assert.True(result.IsSuccessful, "Modules must not reference other modules directly");
}

[Fact]
public void Endpoints_Should_End_With_Endpoint_Suffix()
{
    var result = Types
        .InAssembly(typeof(WalletsModuleRegistration).Assembly)
        .That()
        .ImplementInterface(typeof(IEndpoint))
        .Should()
        .HaveNameEndingWith("Endpoint")
        .GetResult();

    Assert.True(result.IsSuccessful, "All IEndpoint implementations must end with 'Endpoint'");
}
```

---

## Definition of "Phase 0 Completed"

1. All 12 projects exist and build successfully (`dotnet build Kakeibo.slnx`)
2. Docker Compose infrastructure services start and pass health checks
3. Email renderer service responds with HTTP 200 on `/health`
4. Vue PWA shell builds successfully (`bun run app:build`)
5. GitHub Actions CI passes all 4 quality gates
6. Architecture tests pass (no cross-module references)
7. Pre-commit hooks block commits with format issues
8. `bun run setup` completes successfully
9. All services accessible:
   - PostgreSQL: `localhost:5432`
   - Redis: `localhost:6379`
   - RustFS console: `http://localhost:9001`
   - ClickHouse: `http://localhost:8123`
   - Mailpit: `http://localhost:8025`
   - Email renderer: `http://localhost:3050`
   - Redis Insight: `http://localhost:5540`
   - Aspire Dashboard: `http://localhost:18888`

---

**Next Phase:** [Phase 1: Identity](../phase-1/phase-1.md)
