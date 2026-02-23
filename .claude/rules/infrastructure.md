# Infrastructure

Consolidated reference for all infrastructure decisions: Docker Compose layout, Dockerfiles, environment variable strategy, CI/CD pipelines, and target deployment architecture.

---

## Overview

The platform runs on a **single Linux server** using **Docker Compose** as the runtime orchestrator. There is no Kubernetes — the current scale does not justify that complexity.

| Dimension | Decision |
|-----------|----------|
| Runtime | Docker Compose (single file with profiles) |
| CI/CD | GitHub Actions (public runners) |
| Container registry | Docker Hub (public) |
| Deployment | Manual SSH + `docker compose up` |
| Secrets at runtime | Environment variables injected via `docker compose` |

---

## Repository & Registry

- **Git host:** GitHub.com (public repository)
- **Container registry:** Docker Hub public registry — built images are pushed here and pulled during production deployments
- **Image naming convention:** `<username>/kakeibo-api`, `<username>/kakeibo-app`, `<username>/kakeibo-email`

---

## Docker Compose

Single file for all development services. Docker Compose profiles separate infrastructure from application services:

| File | Purpose |
|------|---------|
| `docker-compose.yml` | All development services — infrastructure (always up) + application services (under `profiles: [app]`) |

```bash
docker compose up -d                  # Infrastructure only → use with dotnet run + bun dev
docker compose --profile app up -d   # Full stack (infra + kakeibo-api, kakeibo-app)
docker compose down                   # Stop everything regardless of profile
```

Production deployments are handled by GitHub Actions — images are built and pushed to Docker Hub. The server pulls and runs them directly without any compose overlay file in the repository.

### Infrastructure services (`docker-compose.yml`)

| Service | Image | Ports | Networks |
|---------|-------|-------|----------|
| `postgresdb` | `postgres:18-alpine` | `5432:5432` ⚠️ | postgres-network |
| `redis` | `redis:8-alpine` | `6379:6379` | redis-network |
| `redis-insight` | `redis/redisinsight:latest` | `5540:5540` | redis-network |
| `rustfs` | `rustfs/rustfs:1.0.0-alpha.83` | `9000:9000`, `9001:9001` | (default) |
| `clickhouse` | `clickhouse/clickhouse-server:24-alpine` | `8123:8123` | clickhouse-network |
| `mailpit` | `axllent/mailpit:latest` | `1025:1025`, `8025:8025` | (default) |
| `kakeibo-email` | Built from `services/Kakeibo.Email/Dockerfile` | `3050:3050` | (default) |
| `aspire-dashboard` | `mcr.microsoft.com/dotnet/aspire-dashboard:latest` | `18888:18888`, `18889:18889` | (default) |

> ⚠️ `postgresdb` exposes port `5432` with comment `## REMOVE ON PRODUCTION`. In production, PostgreSQL must not be reachable from outside the Docker network.

### Application services (`docker-compose.yml`, `profiles: [app]`)

Started with `docker compose --profile app up -d`. Not started by `docker compose up -d`.

| Service | Dockerfile | Port | Depends on |
|---------|------------|------|------------|
| `kakeibo-api` | `src/Kakeibo.Api/Dockerfile` | `5000:5000` | postgresdb, redis, rustfs, clickhouse, kakeibo-email |
| `kakeibo-app` | `sites/Kakeibo.App/Dockerfile` | `3000:80` | kakeibo-api |

The `kakeibo-api` container joins three named networks (`postgres-network`, `redis-network`, `clickhouse-network`) so it can reach all backing services by their container names.

### Networking strategy

Named bridge networks enforce service isolation:

- `postgres-network` — only services that need PostgreSQL
- `redis-network` — only services that need Redis
- `clickhouse-network` — only services that need ClickHouse
- `default` — `kakeibo-api`, `kakeibo-app`, and `kakeibo-email` for API ↔ frontend communication

### Volumes

All persistent data is stored in named volumes managed by Docker:

```
postgres-data      → /var/lib/postgresql/data
redis-data         → /data
redis-insight-data → /data
rustfs-data        → /data
rustfs-logs        → /logs
clickhouse-data    → /var/lib/clickhouse
clickhouse-logs    → /var/log/clickhouse-server
```

### Health checks

Every stateful service defines a `healthcheck` block. The `kakeibo-api` container uses `depends_on` with `condition: service_healthy` for PostgreSQL, Redis, RustFS, ClickHouse, and the email renderer — it will not start until all dependencies are confirmed healthy.

### Production network policy

In production, **no database, cache, analytics, or internal service port is exposed to the Internet**. Rules:

- **PostgreSQL, Redis, ClickHouse, kakeibo-email, Mailpit, Aspire Dashboard, Redis Insight** — no port bindings to the host in production. These services communicate exclusively over Docker named networks.
- **`kakeibo-api`, `kakeibo-app`** — served exclusively through a TLS reverse proxy (port 443). Their internal ports (5000 and 80) are never bound directly to the host in production.
- **RustFS API (9000)** — may be exposed if external S3 access is required. The console (9001) is always internal only.
- The `5432:5432` mapping for `postgresdb` is flagged `## REMOVE ON PRODUCTION` in `docker-compose.yml` and must be removed before any production deployment.

**Administrative access** to internal services in production is performed exclusively through an SSH tunnel:

```bash
ssh -L <local-port>:<container-name>:<container-port> user@production-server
```

Examples:
- PostgreSQL: `ssh -L 15432:postgresdb:5432 user@server` → `psql -h localhost -p 15432`
- Redis: `ssh -L 16379:redis:6379 user@server` → `redis-cli -p 16379`
- ClickHouse: `ssh -L 18123:clickhouse:8123 user@server` → `curl http://localhost:18123`
- RustFS console: `ssh -L 19001:rustfs:9001 user@server` → browser `http://localhost:19001`

---

## Dockerfiles

Three Dockerfiles, one per deployable application. All use multi-stage builds to minimize final image size. Each Dockerfile lives inside its own project directory — never at the monorepo root.

### Canonical locations

| Service | Dockerfile | dockerignore | Build context |
|---------|-----------|--------------|---------------|
| kakeibo-api | `src/Kakeibo.Api/Dockerfile` | `src/Kakeibo.Api/Dockerfile.dockerignore` | `.` (repo root) |
| kakeibo-app | `sites/Kakeibo.App/Dockerfile` | `sites/Kakeibo.App/.dockerignore` | `./sites/Kakeibo.App` |
| kakeibo-email | `services/Kakeibo.Email/Dockerfile` | `services/Kakeibo.Email/.dockerignore` | `./services/Kakeibo.Email` |

### `src/Kakeibo.Api/Dockerfile` — .NET API

```
Stage 1: mcr.microsoft.com/dotnet/sdk:10.0  (build)
  → Copy Kakeibo.slnx + Directory.Build.props + Directory.Packages.props
  → Copy .csproj files for each project (restore cache layer):
      Kakeibo.Api, Kakeibo.Common, Kakeibo.Contracts, Kakeibo.Infrastructure,
      Kakeibo.Modules.Households, Kakeibo.Modules.Accounts, Kakeibo.Modules.Transactions,
      Kakeibo.Modules.Budgets, Kakeibo.Modules.Reports
  → dotnet restore Kakeibo.slnx
  → Copy source code for each project
  → dotnet publish -c Release → artifacts/publish/Kakeibo.Api/release/

Stage 2: mcr.microsoft.com/dotnet/aspnet:10.0  (runtime)
  → apt-get install curl  (health check probe)
  → Copy /src/artifacts/publish/Kakeibo.Api/release/ from build stage
  → EXPOSE 5000
  → ASPNETCORE_URLS=http://+:5000
  → HEALTHCHECK: curl http://localhost:5000/health/live
  → adduser appuser (uid 1001, non-root)
  → USER appuser
  → ENTRYPOINT: dotnet Kakeibo.Api.dll
```

The restore layer is cached separately from the source copy so NuGet packages are only re-downloaded when `.csproj` or `Directory.Packages.props` files change.

Build context is `.` (repo root) because the Dockerfile copies from multiple `src/` subdirectories. The per-Dockerfile dockerignore is named `Dockerfile.dockerignore` and placed alongside the Dockerfile — Docker resolves it as `{context}/{dockerfile-path}.dockerignore` = `./src/Kakeibo.Api/Dockerfile.dockerignore`.

> **Adding a new module:** When a new `Kakeibo.Modules.*` project is added to the solution, its `.csproj` must be added to both the restore cache layer and the source copy layer in `src/Kakeibo.Api/Dockerfile`.

### `sites/Kakeibo.App/Dockerfile` — Vue 3 PWA

```
Stage 1: oven/bun:latest  (build)
  → Copy package.json + bun.lock (dependency cache layer)
  → bun install --frozen-lockfile
  → Copy source code (context is ./sites/Kakeibo.App — no path prefix needed)
  → bun run build → /app/dist

Stage 2: nginx:alpine  (serve)
  → Copy /app/dist → /usr/share/nginx/html
  → Copy nginx.conf → /etc/nginx/conf.d/default.conf
  → EXPOSE 80
```

Build context is `./sites/Kakeibo.App` — all COPY paths are relative to that directory. The `nginx.conf` lives in the same directory as the Dockerfile and is included in the build context. The `.dockerignore` is a standard `.dockerignore` file placed in `sites/Kakeibo.App/`.

### `services/Kakeibo.Email/Dockerfile` — Email renderer

```
Stage 1: oven/bun:latest  (runtime)
  → Copy package.json + bun.lock
  → bun install --frozen-lockfile
  → Copy source code
  → EXPOSE 3050
  → CMD: bun run src/index.ts
```

Single-stage Bun container. Built directly from the `services/Kakeibo.Email/` context in `docker-compose.yml`. Runs the Hono server that renders React Email templates on port 3050.

### Per-Dockerfile `.dockerignore` pattern

Each service owns its dockerignore alongside its Dockerfile. Docker resolves the ignore file as follows:

- **Standard naming** (`Dockerfile` + `.dockerignore`): Used when the build context is the project directory (`sites/Kakeibo.App/`, `services/Kakeibo.Email/`).
- **Per-Dockerfile naming** (`Dockerfile.dockerignore`): Used when the build context is the repo root and the `--dockerfile` flag points to a subdirectory path. Docker resolves `{context}/{dockerfile-path}.dockerignore` — e.g., `./src/Kakeibo.Api/Dockerfile.dockerignore`.

| File | Scope |
|------|-------|
| `.dockerignore` | Fallback — generic exclusions for any build without a specific ignore file |
| `src/Kakeibo.Api/Dockerfile.dockerignore` | API build — excludes `sites/`, `services/`, `tests/` (only backend source needed) |
| `sites/Kakeibo.App/.dockerignore` | App build — excludes `src/`, `services/` (scoped to `sites/Kakeibo.App/` context) |

---

## Environment & Configuration

The project uses **layered env files with explicit container injection**.

### Key principle

The `.env` file is used **only for Docker Compose `${VAR}` substitution** — it is **not** passed wholesale into containers. Each container receives only the variables explicitly listed in its `environment:` block. Container isolation is enforced by the compose file, not by `.env`.

### File inventory

| File | Committed | Purpose |
|------|-----------|---------|
| `.env.example` | ✅ Yes | Primitives only — credentials and non-secret identifiers |
| `.env` | ❌ No (gitignored) | Actual secrets — copy from `.env.example` |
| `src/Kakeibo.Api/.env.local.example` | ✅ Yes | Localhost connection strings (assembled via `${VAR}` interpolation) |
| `src/Kakeibo.Api/.env.local` | ❌ No (gitignored) | Localhost overrides for `dotnet run` — copy from `.env.local.example` |
| `src/Kakeibo.Api/appsettings.json` | ✅ Yes | Empty skeleton — no real values, no secrets |
| `src/Kakeibo.Api/appsettings.Production.json` | ✅ Yes | Non-secret production overrides (log levels, batch sizes) |
| `src/Kakeibo.Api/Properties/launchSettings.json` | ✅ Yes | IDE profiles only — never contains secrets |

### .env.example — canonical template

`.env.example` is the **single source of truth** for all credentials and non-secret identifiers. It contains **only primitives** — never connection strings, never assembled values. Connection strings are assembled by `docker-compose.yml` (Docker mode) or `.env.local` (dotnet run mode) using `${VAR}` references.

```bash
# Setup for local development
cp .env.example .env
# Edit .env with your actual secrets (never commit .env)
```

**Variables defined in `.env.example`:**

| Variable | Description |
|----------|-------------|
| `POSTGRES_USER` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password (secret) |
| `POSTGRES_DB` | PostgreSQL database name |
| `REDIS_PASSWORD` | Redis password (secret) |
| `STORAGE_ACCESS_KEY` | RustFS access key |
| `STORAGE_SECRET_KEY` | RustFS secret key (secret) |
| `JWT_SECRET_KEY` | JWT signing key — min 32 chars (secret) |
| `JWT_ISSUER` | JWT issuer claim (e.g. `kakeibo-api`) |
| `JWT_AUDIENCE` | JWT audience claim (e.g. `kakeibo-app`) |
| `SMTP_FROM` | Default sender email address |

**To change a password:** update it in `.env` only. Both Docker Compose and `dotnet run` read from this single source.

### .env.local — localhost overrides for `dotnet run`

When running the API outside Docker (`dotnet run` or IDE), Docker service hostnames (`postgresdb`, `redis`, `rustfs`) resolve to nothing. `.env.local` assembles connection strings using `localhost` and `${VAR}` interpolation from root `.env`.

```bash
cp src/Kakeibo.Api/.env.local.example src/Kakeibo.Api/.env.local
# Edit .env.local only if your local ports differ from defaults
```

DotNetEnv loads root `.env` first, setting `POSTGRES_PASSWORD`, `JWT_SECRET_KEY`, etc. into the process environment. Then it loads `.env.local`, expanding `${VAR}` references automatically. This means `.env.local` never contains raw credentials — only `${VAR}` placeholders.

### Load order in the API

`DotNetEnv` loads files in this order (later files take precedence):

1. `../../.env` — root `.env` primitives (not overwritten if OS env is already set)
2. `.env.local` — localhost connection strings with `${VAR}` interpolation (always wins over `.env`)
3. `AddEnvironmentVariables()` — OS-level env vars injected by Docker Compose (highest priority)

This means the same binary works in all environments: Docker Compose injects assembled connection strings at the OS level (step 3), overriding anything in `.env` or `.env.local`.

### GitHub Actions Secrets (production secrets)

In production, these variables are stored as **GitHub Actions Secrets** (encrypted at rest, masked in logs). The deployment script generates `.env` on the server from these variables at deploy time.

| Variable | Secret | Value |
|----------|--------|-------|
| `POSTGRES_PASSWORD` | ✅ Yes | Strong random password |
| `REDIS_PASSWORD` | ✅ Yes | Strong random password |
| `STORAGE_SECRET_KEY` | ✅ Yes | Strong random password |
| `JWT_SECRET_KEY` | ✅ Yes | `openssl rand -hex 32` |
| `POSTGRES_USER` | No | `postgres` |
| `POSTGRES_DB` | No | `kakeibo` |
| `STORAGE_ACCESS_KEY` | No | `kakeibo` |
| `JWT_ISSUER` | No | `kakeibo-api` |
| `JWT_AUDIENCE` | No | `kakeibo-app` |
| `SMTP_FROM` | No | `noreply@example.com` |
| `DOCKER_HUB_USERNAME` | No | Docker Hub username |
| `DOCKER_HUB_TOKEN` | ✅ Yes | Docker Hub access token |

No secrets are duplicated in GitHub or stored in the repository. The server generates `.env` on the fly, and `docker-compose.yml` assembles connection strings automatically.

### appsettings.json — empty skeleton

`appsettings.json` defines the **structure** of configuration but contains **no real values**. Sensitive fields are empty strings. Non-sensitive defaults (e.g., `ClickHouse__Port: 8123`, `Serilog` sinks) are safe to commit.

```json
{
  "ConnectionStrings": {
    "PostgreSQL": ""        // Populated from env at runtime
  },
  "Jwt": {
    "SecretKey": ""         // NEVER put a real key here
  }
}
```

### appsettings.Production.json — non-secret overrides

Only contains values that are safe to commit and differ between development and production:

| Setting | Dev | Production |
|---------|-----|-----------|
| Serilog minimum level | Information | Warning |
| Outbox polling interval | 10s | 5s |
| Outbox batch size | 100 | 200 |
| AuditOutbox polling interval | 15s | 10s |
| AuditOutbox batch size | 500 | 1000 |

### launchSettings.json — IDE only

Visual Studio / Rider launch profiles. Sets `ASPNETCORE_ENVIRONMENT=Development`. Never used in Docker or CI. Never contains secrets.

---

## CI Pipeline (`.github/workflows/`)

### Workflow rules

Pipelines run in exactly two situations:
1. **Pull Request** — all quality gate jobs
2. **Push to `main`** — build and push images to Docker Hub

This prevents duplicate pipelines when a branch has an open PR.

### Workflow files

| File | Trigger | Purpose |
|------|---------|---------|
| `.github/workflows/quality.yml` | `pull_request` | Quality gates (lint, test, build, format check) |
| `.github/workflows/release.yml` | `push` to `main` | Build and push Docker images to Docker Hub |

### Quality gates (`quality.yml`, PR only)

| Job | Image | Steps |
|-----|-------|-------|
| `quality-api` | `mcr.microsoft.com/dotnet/sdk:10.0` | `dotnet restore` → format check → build Release → unit tests |
| `quality-app` | `oven/bun:latest` | `bun install --frozen-lockfile` → lint → unit tests → build |
| `quality-email` | `oven/bun:latest` | `bun install --frozen-lockfile` → typecheck → lint → tests |
| `quality-docker` | `docker:latest` + dind | Build all 3 Dockerfiles (kakeibo-api, kakeibo-app, kakeibo-email) — no push, validates correctness |

> **Known limitation:** `oxfmt --check` is disabled in `quality-app` due to a `DataCloneError` in Bun CI (tracked in Bun #25610, oxc #17801). Formatting is still enforced locally via the pre-commit hook.
>
> **Performance note:** `quality-docker` is the slowest job (~4–6 min, dominated by the .NET SDK download and build). It runs last among the quality gates to avoid delaying faster checks.

### Caching strategy

Each job uses GitHub Actions cache for its dependency directory:

```yaml
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

This means subsequent runs reuse cached dependencies, avoiding a cold restore.

### Release stage (`release.yml`, `main` only)

Runs after quality gates pass and PR is merged to `main`. Builds and pushes Docker images to Docker Hub.

| Job | Dockerfile | Image tag(s) |
|-----|-----------|--------------|
| `build-push-api` | `src/Kakeibo.Api/Dockerfile` | `latest`, `sha-{git-sha}` |
| `build-push-app` | `sites/Kakeibo.App/Dockerfile` | `latest`, `sha-{git-sha}` |
| `build-push-email` | `services/Kakeibo.Email/Dockerfile` | `latest`, `sha-{git-sha}` |

Uses `docker/build-push-action` with buildx for multi-platform support (linux/amd64, linux/arm64).

### Required GitHub Secrets

Settings → Secrets and variables → Actions → Repository secrets:

| Secret | Description |
|--------|-------------|
| `DOCKER_HUB_USERNAME` | Docker Hub username |
| `DOCKER_HUB_TOKEN` | Docker Hub access token (generated from Docker Hub account settings) |

### Image flow

```
PR → quality gates (api, app, email, docker) → merge to main
  → release.yml → build + push images → Docker Hub
```

---

## Auxiliary Docker Configuration

### `.docker/clickhouse/`

Three XML override files mounted into the ClickHouse container as read-only config overrides:

| File | Purpose |
|------|---------|
| `logs.xml` | Sets log level to `warning`; enables query log with 30-day TTL; disables noisy internal logs (metric_log, query_thread_log, text_log, trace_log, etc.) |
| `ipv4-only.xml` | Forces `listen_host: 0.0.0.0` — fixes "Address family not supported" warnings caused by Docker bridge networks not enabling IPv6 by default |
| `low-resources.xml` | Limits resource usage for development: `max_threads: 1`, `max_block_size: 8192`, disables parallel parsing/formatting, caps mark cache at 500MB |

The `ulimits.nofile` is set to 262144 (soft and hard) as required by ClickHouse.

---

## Nginx

`nginx.conf` is copied into the `kakeibo-app` container at `/etc/nginx/conf.d/default.conf`.

| Rule | Behaviour |
|------|-----------|
| `location /` | `try_files $uri $uri/ /index.html` — SPA client-side routing fallback |
| `location ~* \.(js\|css\|png\|...)$` | `expires 1y`, `Cache-Control: public, immutable` — long-lived cache for hashed assets |
| `location = /index.html` | `Cache-Control: no-cache` — always fetches latest shell |

---

## Pre-commit Hooks (`lefthook.yml`)

Two hook types, both run from the repo root via `lefthook`:

### `commit-msg`

Runs `commitlint` to enforce conventional commit format (`type(scope): description`). Rejects commits that do not follow the pattern.

### `pre-commit` (parallel)

Runs two checks in parallel on staged files in `sites/Kakeibo.App/`:

| Command | Glob | What it checks |
|---------|------|----------------|
| `oxlint --deny-warnings` | `*.{ts,tsx,vue,js,jsx}` | Lint errors and warnings |
| `oxfmt --check` | `*.{ts,tsx,vue,js,jsx,css,json}` | Code formatting |

Both run in parallel. If either fails, the commit is rejected. The hook uses check mode only — it never auto-fixes. Always run `bun run app:format && bun run app:lint` and re-stage modified files before committing.

---

## Local Development

### First-time setup

```bash
cp .env.example .env
cp src/Kakeibo.Api/.env.local.example src/Kakeibo.Api/.env.local
bun run setup          # runs scripts/setup-local.sh
```

`scripts/setup-local.sh` is idempotent — safe to run multiple times. It:
1. Verifies `.env` exists (creates from `.env.example` if missing)
2. Runs `bun install` for root and all sub-projects
3. Runs `dotnet restore Kakeibo.slnx`
4. Starts Docker Compose infrastructure services
5. Waits for health checks (PostgreSQL, RustFS, Mailpit, Redis Insight)
6. Prints service URLs

### Common commands

| Command | What it does |
|---------|-------------|
| `bun run dev:infra` | Start only infrastructure containers (PostgreSQL, Redis, etc.) |
| `bun run dev:all` | Start all containers including the API and app |
| `bun run api:run` | Run the API locally via `dotnet run` (needs `.env.local`) |
| `bun run app:dev` | Start the Vite dev server on port 5173 |
| `bun run docker:up` | Start full stack: `docker compose --profile app up -d` |
| `bun run docker:down` | Stop all containers |
| `bun run docker:logs` | Tail container logs |

### Service URLs

| Service | URL |
|---------|-----|
| API | http://localhost:5000 |
| Scalar (API docs) | http://localhost:5000/scalar |
| App (Docker) | http://localhost:3000 |
| App (Vite dev) | http://localhost:5173 |
| Email renderer | http://localhost:3050 |
| Mailpit (email UI) | http://localhost:8025 |
| RustFS console | http://localhost:9001 |
| Redis Insight | http://localhost:5540 |
| Aspire Dashboard | http://localhost:18888 |

---

## Known Limitations - MVP

### No Backup Strategy

**User Decision**: Backups not implemented in MVP phase.

**Risks**:
- Data loss in case of server failure
- No point-in-time recovery
- No disaster recovery plan

**Mitigation** (when needed):
1. Implement daily PostgreSQL backups (pg_dump)
2. Off-site backup storage (AWS S3, Backblaze B2)
3. Retention: 30 daily, 12 monthly, 7 yearly
4. Monthly restore tests
5. Documented DR procedure

**Recommended for**: Production deployments with real user data

---

## Port Reference

| Service | Port(s) | Type | Dev | Prod |
|---------|---------|------|-----|------|
| Kakeibo.Api | 5000 | .NET API | ✅ | ✅ |
| Kakeibo.App (Vite) | 5173 | Vue PWA dev server | ✅ | ❌ |
| Kakeibo.App (Docker) | 3000 | Vue PWA (Nginx) | ✅ | ✅ |
| Kakeibo.Email | 3050 | Email renderer (Hono/Bun) | ✅ | ✅ |
| PostgreSQL | 5432 | Relational DB | ✅ | ⚠️ Remove |
| Redis | 6379 | Distributed cache | ✅ | Internal only |
| Redis Insight | 5540 | Redis GUI | ✅ | ❌ |
| RustFS API | 9000 | Object storage | ✅ | ✅ |
| RustFS Console | 9001 | Object storage UI | ✅ | Optional |
| ClickHouse HTTP | 8123 | Analytical DB | ✅ | Internal only |
| Mailpit SMTP | 1025 | Dev email capture | ✅ | ❌ |
| Mailpit Web | 8025 | Dev email UI | ✅ | ❌ |
| Aspire Dashboard | 18888 | OpenTelemetry UI | ✅ | Optional |
| Aspire OTLP | 18889 | OTLP gRPC receiver | ✅ | ✅ |

> **Production note:** Ports marked "Internal only" must not be exposed outside the Docker network. Port 5432 is explicitly flagged `## REMOVE ON PRODUCTION` in `docker-compose.yml`. All direct administrative access to internal services in production must go through an SSH tunnel — never expose these ports to the Internet.
