# Infrastructure

Single Linux server running Docker Compose. Deployment: manual SSH + `docker compose up`. Secrets injected as environment variables.

---

## Docker Compose

Single `docker-compose.yml` with profiles. Infrastructure services start by default; application services require the `app` profile.

> **Critical:** `docker-compose.yml` must declare `name: kakeibo` at the top level. Without it, Docker Compose derives the project name from the directory name — so cloning the repo into a differently named folder produces different container, network, and volume names, breaking local setups and CI reproducibility.

```bash
docker compose up -d                  # Infrastructure only → use with dotnet run + bun dev
docker compose --profile app up -d   # Full stack (infra + kakeibo-api, kakeibo-app)
docker compose down                   # Stop everything
```

### Infrastructure Services

| Service | Image | Ports |
|---------|-------|-------|
| `postgresdb` | `postgres:18-alpine` | `5432:5432` ⚠️ |
| `redis` | `redis:8-alpine` | `6379:6379` |
| `redis-insight` | `redis/redisinsight:latest` | `5540:5540` |
| `minio` | `minio/minio:latest` | `9000:9000` ⚠️, `9001:9001` ⚠️ |
| `clickhouse` | `clickhouse/clickhouse-server:24-alpine` | `8123:8123` |
| `mailpit` | `axllent/mailpit:latest` | `1025:1025`, `8025:8025` |
| `kakeibo-email` | Built from `src/Kakeibo.Email/Dockerfile` | `3050:3050` |
| `aspire-dashboard` | `mcr.microsoft.com/dotnet/aspire-dashboard:latest` | `18888:18888`, `18889:18889` |

> ⚠️ `postgresdb` exposes port 5432 — flagged `## REMOVE ON PRODUCTION` in `docker-compose.yml`.

### Application Services (`profiles: [app]`)

| Service | Dockerfile | Port |
|---------|------------|------|
| `kakeibo-api` | `src/Kakeibo.Api/Dockerfile` | `5000:5000` |
| `kakeibo-app` | `src/Kakeibo.App/Dockerfile` | `3000:80` |

---

## Dockerfiles

Each deployable app owns its Dockerfile inside its project directory — never at the monorepo root. All use multi-stage builds.

### Canonical Locations

| Service | Dockerfile | dockerignore | Build context |
|---------|-----------|--------------|---------------|
| kakeibo-api | `src/Kakeibo.Api/Dockerfile` | `src/Kakeibo.Api/Dockerfile.dockerignore` | `.` (repo root) |
| kakeibo-app | `src/Kakeibo.App/Dockerfile` | `src/Kakeibo.App/.dockerignore` | `./src/Kakeibo.App` |
| kakeibo-email | `src/Kakeibo.Email/Dockerfile` | `src/Kakeibo.Email/.dockerignore` | `./src/Kakeibo.Email` |

**Dockerignore naming:** When the build context is the repo root and the Dockerfile is in a subdirectory, Docker resolves `{context}/{dockerfile-path}.dockerignore` — hence `Dockerfile.dockerignore` for the API. When the build context is the project directory itself, standard `.dockerignore` applies.

---

## Environment & Configuration

`.env` is the single source of truth for all secrets. It contains only primitives — never assembled connection strings. Connection strings are built by `docker-compose.yml` (Docker mode) or `.env.local` (`dotnet run` mode) using `${VAR}` references.

### File Inventory

| File | Committed | Purpose |
|------|-----------|---------|
| `.env.example` | Yes | Primitives only — credentials and non-secret identifiers |
| `.env` | No | Actual secrets — copy from `.env.example` |
| `src/Kakeibo.Api/.env.local.example` | Yes | Localhost connection strings (assembled via `${VAR}`) |
| `src/Kakeibo.Api/.env.local` | No | Localhost overrides for `dotnet run` |
| `src/Kakeibo.Api/appsettings.json` | Yes | Empty skeleton — no real values |
| `src/Kakeibo.Api/appsettings.Production.json` | Yes | Non-secret production overrides |
| `src/Kakeibo.Api/Properties/launchSettings.json` | Yes | IDE profiles only — never secrets |

### Variables (`.env.example`)

| Variable | Description |
|----------|-------------|
| `POSTGRES_USER` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password (secret) |
| `POSTGRES_DB` | PostgreSQL database name |
| `REDIS_PASSWORD` | Redis password (secret) |
| `STORAGE_ACCESS_KEY` | MinIO access key |
| `STORAGE_SECRET_KEY` | MinIO secret key (secret) |
| `JWT_SECRET_KEY` | JWT signing key — min 32 chars (secret) |
| `JWT_ISSUER` | JWT issuer claim |
| `JWT_AUDIENCE` | JWT audience claim |
| `SMTP_FROM` | Default sender email address |

**`.env.example` must be kept well-documented.** Every variable must have an inline comment explaining its purpose and any constraints. When a value must be generated, the generation command must be included as a comment directly above the variable:

```bash
# Generate with: openssl rand -hex 32
JWT_SECRET_KEY=
```

### Services & Ports

> Consolidated reference for local development and production. The Docker Compose tables above show the host:container port mapping per service.

| Service | Port(s) | Type | Local URL | Prod |
|---------|---------|------|-----------|------|
| Kakeibo.Api | 5000 | .NET API | http://localhost:5000 | ✅ |
| Scalar (API docs) | 5000 | API docs | http://localhost:5000/scalar | — |
| Kakeibo.App (Vite) | 5173 | Vue PWA dev server | http://localhost:5173 | ❌ |
| Kakeibo.App (Docker) | 3000 | Vue PWA (Nginx) | http://localhost:3000 | ✅ |
| Kakeibo.Email | 3050 | Email renderer (Hono/Bun) | http://localhost:3050 | ✅ |
| PostgreSQL | 5432 | Relational DB | — | ⚠️ Remove |
| Redis | 6379 | Distributed cache | — | Internal only |
| Redis Insight | 5540 | Redis GUI | http://localhost:5540 | ❌ |
| MinIO API | 9000 | Object storage | — | Internal only |
| MinIO Console | 9001 | Object storage UI | http://localhost:9001 | ❌ |
| ClickHouse HTTP | 8123 | Analytical DB | — | Internal only |
| Mailpit SMTP | 1025 | Dev email capture | — | ❌ |
| Mailpit Web | 8025 | Dev email UI | http://localhost:8025 | ❌ |
| Aspire Dashboard | 18888 | OpenTelemetry UI | http://localhost:18888 | Optional |
| Aspire OTLP | 18889 | OTLP gRPC receiver | — | ✅ |

> Internal-only ports must never be exposed outside the Docker network. Port 5432 is flagged `## REMOVE ON PRODUCTION`. All production admin access to internal services via SSH tunnel only.

---

## Project Configuration

| Component | Description |
|-----------|-------------|
| .slnx | Solution format |
| .editorconfig | Style configuration |
| Directory.Build.props | Centralized properties |
| Directory.Packages.props | Centralized package management |
| .env / appsettings.json | Secrets and configuration |
| InternalsVisibleTo | Every src project exposes internals to its corresponding test project |
| No nested src/ in src/ | Projects under `src/` must not contain a nested `src/` subfolder. See mandatory.md Rule 10. |
