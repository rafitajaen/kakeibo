# Tech Stack

## Infrastructure

| Component | Description |
|-----------|-------------|
| PostgreSQL 18 | Main relational database |
| Redis | Distributed cache (used by FusionCache) |
| RustFS | S3-compatible storage server (Apache 2.0, alpha) ⚠️ **Known limitation**: SSE (Server-Side Encryption) broken in alpha.83. Data stored in plaintext on disk. Client-side encryption or filesystem-level encryption required for sensitive documents. |
| ClickHouse | Analytical database for events and logs |

## Project Configuration

| Component | Description |
|-----------|-------------|
| .slnx | Solution format |
| .editorconfig | Style configuration |
| Directory.Build.props | Centralized properties |
| Directory.Packages.props | Centralized package management |
| .env / appsettings.json | Secrets and configuration |
| InternalsVisibleTo | Every src project exposes internals to its corresponding test project |
| Primary constructors | C# 12 primary constructors required. Enforced by .editorconfig (IDE0290:warning) |
| No nested src/ in src/ | Projects under `src/` must not contain a nested `src/` subfolder. See mandatory.md Rule 10. |

## API (.NET 10)

| Component | Description |
|-----------|-------------|
| Minimal APIs | Native REPR pattern with IEndpoint interface |
| ASP.NET Core Authentication | JWT Bearer with HttpOnly cookies |
| Simple Monolith | Single project, vertical slices + screaming architecture, folder-based domain separation |
| EntityFramework | ORM with SnakeCaseConvention, NodaTime and PostgreSQL |
| FluentValidation | Model validation |
| FusionCache | Cache with Redis |
| Serilog | Structured logging |
| OpenTelemetry | Tracing, metrics and logging |
| Scalar | API documentation |
| AspNetCore.HealthChecks | Health endpoints for monitoring |
| Polly | Resilience: retries, circuit breaker, timeouts |
| System.Threading.Channels | In-memory async event bus (IEventBus, ChannelEventBus, EventDispatcher BackgroundService) |
| MailKit | SMTP client for sending emails |
| Hangfire + Hangfire.PostgreSql | Scheduled background jobs with PostgreSQL storage |
| xUnit v3 | Testing |
| Testcontainers | Docker containers for integration tests |
| Minio NuGet SDK | S3-compatible client library (works with RustFS) |

## Email Rendering

| Component | Description |
|-----------|-------------|
| React Email | Email template rendering with React components |
| Hono | Micro HTTP server for email rendering API |
| Bun | Runtime for email renderer service |
| oxlint | Lint |
| oxfmt | Format |

## Vue (Web App — PWA)

| Component | Description |
|-----------|-------------|
| Vue.js | Framework with Composition API |
| Vite | Build tool with HMR |
| TypeScript | Strict mode required |
| Pinia | State management |
| Axios | HTTP client |
| Vue Router | Routing |
| shadcn-vue | Accessible UI components |
| Tailwind CSS v4 | Utility-first styles, no configuration file |
| @hugeicons/vue + @hugeicons/core-free-icons | Icons (4,600+ free, tree-shakeable) |
| VeeValidate + Zod | Form validation |
| date-fns | Date manipulation |
| Axios (interceptors) + Pinia | Authentication state management and automatic token refresh |
| Radix UI | Charts |
| Playwright | E2E tests |
| Vitest | Unit tests |
| i18n | Internationalization |
| Bun | Package manager |
| oxlint | Lint |
| oxfmt | Format |
| .env typed | Environment variables |

## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| Chart.js | Prefer Radix UI for charts |
| Python | Unified stack on .NET and JS/TS. Scripts must be sh or TypeScript only |
| Webpack | Use Vite instead |
| Firebase | Custom solution with RustFS, PostgreSQL and Redis |
| Supabase | Custom solution with RustFS, PostgreSQL and Redis |
| BCrypt | Use more modern algorithms (PBKDF2-SHA512) |
| Argon2id | Use more modern algorithms (PBKDF2-SHA512) |
| Kubernetes (K8s) | Unnecessary complexity for the project |
| Razor (email templates) | Use React Email for templates |
| mjml | Use React Email for templates |
| FastEndpoints | Use native Minimal APIs (IEndpoint + MapEndpoint) |
| MediatR | Do not use (plain handler classes, no CQRS interfaces) |
| Swagger | Use Scalar for API documentation |
| Biome | Use oxlint + oxfmt |
| EF Core InMemory, SQLite in-memory | Use Testcontainers with real PostgreSQL for integration tests |
| Quartz.NET | Use Hangfire instead |
| Guid.CreateVersion7() | PROHIBITED. Little-endian byte order breaks PostgreSQL sorting. Use Guid7 wrapper (Medo.Uuid7) for entity IDs. Regular Guid is allowed for non-entity purposes |
| SonarAnalyzer.CSharp | Use .editorconfig and built-in .NET analyzers instead |
| MassTransit | Use System.Threading.Channels (IEventBus + ChannelEventBus) |
| RabbitMQ | Use System.Threading.Channels (IEventBus + ChannelEventBus) |
| Keycloak | Use ASP.NET Core native JWT Bearer authentication with in-memory signing |
| dayjs | Frontend only. Use date-fns instead |
| datejs | Frontend only. Use date-fns instead |
| frappe-ui | Use shadcn-vue instead |
| MinIO (server) | Archived project, no security patches. Use RustFS (Apache 2.0, S3-compatible). The Minio NuGet SDK is still the S3 client library — only the server is prohibited |
| Newtonsoft.Json | Use System.Text.Json (built into .NET). Better performance, native integration, and actively maintained by Microsoft |
| FluentAssertions | Use xUnit v3 native Assert.* methods manually |
| `npx` | Use `bunx` (or `bunx --bun` when Bun runtime is required). See mandatory.md Rule 9 |
