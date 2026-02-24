# Knowledge Base

Lessons learned and gotchas discovered during development.

---

## KB-001: vue-i18n `@` symbol must be escaped in locale files

**Discovered:** 2026-02-13
**Affects:** All `.json` files under `sites/Kakeibo.App/locales/`

vue-i18n reserves the `@` character for linked message syntax (`@:key`). Any literal `@` in locale values (email addresses, social handles, etc.) must be escaped as `{'@'}`.

| Bad | Good |
|-----|------|
| `"admin@example.com"` | `"admin{'@'}example.com"` |
| `"contact@example.com"` | `"contact{'@'}example.com"` |

**Symptom:** Console errors like `Message compilation error: Invalid linked format` or `Unexpected lexical analysis in token`.

---

## KB-002: Prefer `[MemberNotNullWhen]` over null-forgiving operators

**Discovered:** 2026-02-13
**Affects:** All `*.cs` files under `src/`

When a type has a boolean property that implies the nullability of another member (e.g., `IsSuccess` implies `Error` is non-null when `false`), annotate the boolean with `[MemberNotNullWhen]` from `System.Diagnostics.CodeAnalysis`. This lets the compiler infer nullability through flow analysis and eliminates the need for the `!` operator.

| Bad | Good |
|-----|------|
| `if (result.IsFailure) return result.Error!;` | `if (result.IsFailure) return result.Error;` |
| `result.IsSuccess ? result.Value!.Id : ...` | `result.IsSuccess ? result.Value.Id : ...` |

**Rule:** Never suppress nullable warnings with `!` when the invariant can be expressed via `[MemberNotNullWhen]`. The `!` operator hides real bugs and defeats the purpose of nullable reference types.

---

## KB-003: Use `bunx --bun` to add shadcn-vue components

**Discovered:** 2026-02-17
**Affects:** All projects under `sites/` that use shadcn-vue

The official shadcn-vue docs show `npx shadcn-vue@latest add <component>`, but this project uses Bun as the package manager. Using `npx` in a Bun monorepo can resolve the wrong registry or install with npm instead of Bun.

Always use:

```bash
bunx --bun shadcn-vue@latest add <component>
```

Run the command **from the target project folder** (not the monorepo root):

| Bad | Good |
|-----|------|
| `npx shadcn-vue@latest add tooltip` (from any dir) | `cd sites/Kakeibo.App && bunx --bun shadcn-vue@latest add tooltip` |
| `bunx shadcn-vue@latest add tooltip` (missing `--bun`) | `bunx --bun shadcn-vue@latest add tooltip` |

**Symptom without `--bun`:** Component may be installed via npm/npx even inside a Bun workspace, leading to lockfile inconsistencies.

---

## KB-004: Run auto-fix format/lint commands before committing — not the `:check` variants

**Discovered:** 2026-02-17
**Affects:** All commits touching `.ts/.tsx/.vue/.js/.jsx/.css/.json` files in any project

The pre-commit hook (lefthook) runs `oxfmt --check` and `oxlint --deny-warnings` on **staged**
files in `sites/Kakeibo.App/`. It uses check mode only — it never auto-fixes. If any staged file
is not properly formatted or has lint warnings, the commit is rejected.

The same principle applies to all other projects (Email): always run auto-fix first
so that the hook passes on the first attempt and the check scripts confirm a clean state.

### Correct pre-commit workflow

1. Make your changes
2. Run auto-fix commands for the projects you touched:
   - Frontend: `bun run app:format && bun run app:lint`
   - Email: `bun run email:format`
3. Re-stage any files modified by the formatters: `git add <modified-files>`
4. Commit — the pre-commit hook will now pass

### Quick reference

| Project | Auto-fix (run this) | Check only (do NOT rely on this to fix) |
|---------|---------------------|-----------------------------------------|
| Kakeibo.App | `bun run app:format && bun run app:lint` | `bun run app:format:check && bun run app:lint:check` |
| Kakeibo.Email | `bun run email:format` | `bun run email:format:check` |

**Note:** Backend (.NET) formatting is handled by the user manually (see mandatory.md Rule 7). Claude never runs `dotnet format`.

**Why re-stage?** The formatters modify files on disk. Git only includes the version of the
file that was staged at commit time. If you forget to re-stage, the pre-commit hook sees the
pre-fix staged version and still rejects the commit.

**Symptom:** `lefthook` rejects the commit with `oxfmt-check` or `oxlint` errors even though
you "already ran the format check". The formatters ran in check mode and the files were never
actually fixed.

---

## KB-005: Enforce behavioral contracts through tests, not just comments

**Discovered:** 2026-02-17
**Affects:** All `*.cs` files under `tests/`

When a component has a behavioral contract that a future developer could accidentally break
(idempotency, nullability, ordering, single-execution), prefer encoding that contract as a test
rather than — or in addition to — a code comment. A comment documents the intent; a test
enforces it and will catch regressions automatically.

**The pattern:** write a test that exercises the component in the exact scenario the contract
covers. If the contract is "calling this twice must be safe", call it twice and assert the
outcome is still correct. If a future change breaks the contract, the test fails immediately.

**When to apply:**
- A method or class is annotated with "idempotent", "thread-safe", "at-most-once", or similar
- The contract protects against a realistic failure mode (e.g., message broker retry, concurrent
  requests, repeated cache eviction)
- A comment alone would be the only enforcement — tests make it impossible to ignore

**When a comment is still useful:** keep the comment alongside the test to explain *why* the
contract exists (e.g., at-least-once delivery from OutboxProcessor), not just *that* it exists.
The test catches regressions; the comment explains the reasoning.

---

## KB-006: shadcn-vue primitives embed icon imports — must be migrated manually

**Discovered:** 2026-02-18
**Resolved:** 2026-02-18 (migrated to `@hugeicons/vue`)
**Affects:** `sites/Kakeibo.App/components/ui/dialog/`, `components/ui/dropdown-menu/`, `components/ui/select/`

shadcn-vue copies its components as editable files into the project (not node_modules).
When adding a new icon library, these files must be updated manually — they will not be
automatically updated by reinstalling shadcn-vue components.

Example files that may need migration when changing icon libraries:

| Component | Common Icons Used |
|-----------|------------------|
| `DialogContent.vue`, `DialogScrollContent.vue` | Close/Cancel icon |
| `DropdownMenuCheckboxItem.vue`, `SelectItem.vue` | Check/Tick icon |
| `DropdownMenuRadioItem.vue` | Circle icon |
| `DropdownMenuSubTrigger.vue` | ChevronRight/ArrowRight icon |
| `SelectTrigger.vue`, `SelectScrollDownButton.vue` | ChevronDown/ArrowDown icon |
| `SelectScrollUpButton.vue` | ChevronUp/ArrowUp icon |

**Icon type:** Icon libraries vary in their import patterns. Some use Vue components directly, others use data arrays with wrapper components. Always check the library's documentation for the correct usage pattern.

---

## KB-008: Tests that use Testcontainers must skip when Docker is unavailable

**Discovered:** 2026-02-18
**Affects:** All test files under `tests/` that start a `PostgreSqlContainer` or any other Testcontainers container

CI runners may execute jobs inside a Docker container without DinD (Docker-in-Docker) or without access to `/var/run/docker.sock`. In those environments Testcontainers throws `DockerUnavailableException` on startup, which causes tests to **fail** instead of being skipped, breaking the pipeline.

The fix is to wrap the container startup in a `try-catch` that calls `Assert.Skip()`. xUnit v3 treats `SkipException` as a skipped test (green), not a failure.

### Pattern A — `TestDbContextFactory` (shared static factory)

Extract a private helper and call it from every public method that awaits the container:

```csharp
private static readonly Lazy<Task> ContainerStartTask = new(() => PostgresContainer.StartAsync());

// Awaits container startup and skips the test if Docker is not available.
private static async Task EnsureContainerStartedAsync()
{
    try
    {
        await ContainerStartTask.Value;
    }
    catch
    {
        Assert.Skip("Docker is not available. These tests require Testcontainers (PostgreSQL).");
    }
}

public static async Task<MyDbContext> CreateAsync()
{
    await EnsureContainerStartedAsync();
    // ...
}

public static async Task<string> GetConnectionStringForAsync(string databaseName)
{
    await EnsureContainerStartedAsync();
    // ...
}
```

### Pattern B — `IAsyncLifetime.InitializeAsync` (class-level fixture)

Wrap the container start at the top of `InitializeAsync`:

```csharp
public async ValueTask InitializeAsync()
{
    try
    {
        await ContainerStartTask.Value;
    }
    catch
    {
        Assert.Skip("Docker is not available. This test requires Testcontainers (PostgreSQL).");
    }
    // rest of setup ...
}
```

### Pattern C — inline helper method (test class with ad-hoc container)

Wrap in the method that creates the context:

```csharp
private async Task<MyDbContext> CreateDbContextAsync()
{
    try
    {
        await ContainerStartTask.Value;
    }
    catch
    {
        Assert.Skip("Docker is not available. This test requires Testcontainers (PostgreSQL).");
    }
    // ...
}
```

**Key points:**
- `Assert.Skip()` in xUnit v3 throws `SkipException` — the runner catches it and marks the test as **Skipped**, not Failed
- The `Lazy<Task>` pattern ensures the container is started at most once across all tests in the class/project
- Tests run normally in local development (where Docker is available); they are silently skipped in CI
- Do **not** add `// DOCKER_REQUIRED` comments or similar markers — the skip guard is self-documenting
- This applies to **every** test file that creates a Testcontainers container, including `TestDbContextFactory`, `IAsyncLifetime` fixtures, and ad-hoc container fields in test classes

---

## KB-010: Migración de Modular Monolith a Simple Monolith (2026-02-24)

**Discovered:** 2026-02-24
**Affects:** All architectural decisions in `src/` and `tests/`

At ~5% implementation (infrastructure + CI only, zero business logic), the architecture was
migrated from a Modular Monolith (12 projects: Api, Common, Contracts, Infrastructure + 8 modules)
to a Simple Monolith (2 projects: Kakeibo.Api + Kakeibo.Tests). The migration was zero-risk at
this point — no business code existed to break.

### What was removed

| Component | Reason |
|-----------|--------|
| `Kakeibo.Common` project | Absorbed into `Kakeibo.Api/Common/` |
| `Kakeibo.Contracts` project | No inter-module contracts needed (single assembly) |
| `Kakeibo.Infrastructure` project | Absorbed into `Kakeibo.Api/Infrastructure/` |
| `Kakeibo.Modules.Identity` project | Features live in `Kakeibo.Api/Features/Identity/` |
| `Kakeibo.Modules.Identity.Tests` | Merged into `Kakeibo.Tests` |
| `Outbox Pattern` (OutboxInterceptor, OutboxProcessor, OutboxMessage) | Replaced by `System.Threading.Channels` |
| `IModuleClient` / `IModuleRequest` / `IModuleRequestHandler` | No cross-assembly communication needed |
| `IModuleEventBus` / `IIntegrationEvent` / `IEventConsumer<T>` | Replaced by `IEventBus` / `IEvent` / `IEventHandler<T>` |
| `IDomainEvent` / `IDomainEventHandler<T>` / `AggregateRoot` | Replaced by `IEvent` / `IEventHandler<T>` |
| `IUnitOfWork` | Replaced by direct `AppDbContext` usage |
| Per-module DbContexts | Replaced by single `AppDbContext` |
| Per-module PostgreSQL schemas | Single `public` schema |

### What was created

| Component | Location |
|-----------|----------|
| `IEvent`, `IEventHandler<T>`, `IEventBus` | `Kakeibo.Api/Infrastructure/Events/` |
| `ChannelEventBus` (singleton, fire-and-forget) | `Kakeibo.Api/Infrastructure/Events/` |
| `EventDispatcher` (BackgroundService) | `Kakeibo.Api/Infrastructure/Events/` |
| `AppDbContext` (single context) | `Kakeibo.Api/Persistence/AppDbContext.cs` |
| `Kakeibo.Tests` (single test project) | `tests/Kakeibo.Tests/` |
| Architecture tests (naming conventions only) | `tests/Kakeibo.Tests/Architecture/` |

### New namespace root

All code lives under `Kakeibo.Api.*`:
- `Kakeibo.Api.Common.Abstractions` — Entity, Result<T>, Error, ValueObject
- `Kakeibo.Api.Common.Endpoints` — IEndpoint, ValidationFilter, EndpointExtensions
- `Kakeibo.Api.Common.Utils` — Guid7, PasswordHasher, DefaultSerializer, CharSets
- `Kakeibo.Api.Infrastructure.Events` — IEvent, IEventBus, ChannelEventBus, EventDispatcher
- `Kakeibo.Api.Infrastructure.Caching` — ICacheService, FusionCacheService
- `Kakeibo.Api.Infrastructure.Email` — IEmailService, EmailService
- `Kakeibo.Api.Infrastructure.Storage` — IStorageService, StorageService
- `Kakeibo.Api.Features.Identity.*` — Identity domain features
- `Kakeibo.Api.Persistence` — AppDbContext, Configurations/

### Build result after migration

```
dotnet build Kakeibo.slnx → 0 errors, 0 warnings
dotnet test tests/Kakeibo.Tests/ → 3 tests passed (naming convention architecture tests)
```

---

## KB-009: RustFS SSE (Server-Side Encryption) is broken in alpha.83

**Discovered:** 2026-02-19
**Affects:** Any modules requiring encryption at rest for sensitive documents

RustFS alpha.83 reports SSE support via API but data is stored in plaintext on disk.
Confirmed in issues #1397, #1278, #1604 (maintainers confirmed "KMS feature is not currently
available"), and #1800.

**Current impact:** Depends on current project phase and data sensitivity requirements.

**Required action:** Re-evaluate SSE before implementing features that handle sensitive documents
requiring encryption at rest. If SSE is still broken, alternatives: client-side encryption
(AES-256-GCM before upload) or filesystem-level encryption on the Docker volume (LUKS/dm-crypt).
