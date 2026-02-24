# Technical Debt Rules

Knowledge base of technical debt patterns to detect and fix across the codebase. Each rule has a unique ID (`TD-xxx`), severity, description, and examples. Used by the `/audit-tech-debt` command to generate automated audit reports.

**Severities:**
- **CRITICAL** — Violates a prohibited technology rule from `CLAUDE.md`. Must be fixed immediately.
- **WARNING** — Code smell that reduces maintainability, readability, or type safety. Should be fixed in current phase.
- **INFO** — Minor improvement opportunity. Fix when touching the file.

---

## Category 1: Magic Strings & Constants

### TD-001: Hardcoded Enumerator Strings (WARNING)

Strings that represent a finite set of values (roles, statuses, health check names, tags, bucket names, cache keys, audit actions) must be defined as `public const string` in a `public static class`.

**Bad:**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(tags: ["ready"])
    .AddRedis(tags: ["ready"]);
```

**Good:**
```csharp
public static class HealthCheckTags
{
    public const string Ready = "ready";
}

builder.Services.AddHealthChecks()
    .AddNpgSql(tags: [HealthCheckTags.Ready])
    .AddRedis(tags: [HealthCheckTags.Ready]);
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Repeated string literals across multiple files or within the same file
- Strings used as identifiers, tags, names, or keys (not user-facing messages)
- Health check names and tags, bucket names, queue names, cache key prefixes

---

### TD-002: Configuration Keys as Magic Strings (WARNING)

Configuration section keys accessed via `Configuration["key"]` or `GetSection("key")` must be defined as constants, ideally using the `const string SectionName` pattern in a settings class.

**Bad:**
```csharp
var connectionString = builder.Configuration["Redis:ConnectionString"];
var baseUrl = builder.Configuration.GetSection("EmailRenderer").Get<EmailRendererSettings>()!;
```

**Good:**
```csharp
public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public required string ConnectionString { get; init; }
}

public sealed class EmailRendererOptions
{
    public const string SectionName = "EmailRenderer";
    public required string BaseUrl { get; init; }
}

var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()!;
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- `Configuration["..."]` with string literals
- `GetSection("...")` with string literals
- `GetConnectionString("...")` with string literals
- Environment variable names as inline strings: `Environment.GetEnvironmentVariable("...")`

---

### TD-003: Hardcoded Content Types (INFO)

Content type strings should use `System.Net.Mime.MediaTypeNames` or a constants class rather than inline string literals.

**Bad:**
```csharp
app.MapGet("/health", () => Results.Content("OK", "text/plain"));
```

**Good:**
```csharp
using System.Net.Mime;

app.MapGet("/health", () => Results.Content("OK", MediaTypeNames.Text.Plain));
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- `"application/json"`, `"text/plain"`, `"text/html"`, `"application/octet-stream"` as inline strings

---

### TD-008: Magic Numbers (WARNING)

Numeric literals with non-obvious meaning must be assigned to a named variable or constant that describes their purpose. Numbers like `0`, `1`, `-1` in trivial contexts (loop bounds, index access, boolean-like checks) are acceptable.

**Bad:**
```csharp
if (password.Length < 8) return false;
options.MaxRetryAttempts = 3;
var delay = TimeSpan.FromSeconds(30);
builder.Services.AddFusionCache().WithDefaultEntryOptions(o => o.Duration = TimeSpan.FromMinutes(5));
```

**Good:**
```csharp
var minPasswordLength = 8;
if (password.Length < minPasswordLength) return false;

var maxRetryAttempts = 3;
options.MaxRetryAttempts = maxRetryAttempts;

var defaultTimeoutSeconds = 30;
var delay = TimeSpan.FromSeconds(defaultTimeoutSeconds);

var cacheMinutes = 5;
builder.Services.AddFusionCache().WithDefaultEntryOptions(o => o.Duration = TimeSpan.FromMinutes(cacheMinutes));
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Numeric literals in comparisons, assignments, or method arguments where the number's meaning isn't self-evident
- `TimeSpan.FromSeconds(N)`, `TimeSpan.FromMinutes(N)` with inline literals
- Iteration counts, thresholds, sizes, ports, retry counts as bare numbers
- **Exceptions:** `0`, `1`, `-1` in trivial contexts; array/collection initializers; enum values; test assertions

---

## Category 2: Prohibited API Usage

### TD-004: DateTime/DateTimeOffset Usage (CRITICAL)

`DateTime`, `DateTimeOffset`, and `DateOnly` are prohibited. Use NodaTime types instead.

**Bad:**
```csharp
var now = DateTime.UtcNow;
var today = DateOnly.FromDateTime(DateTime.Now);
var timestamp = DateTimeOffset.UtcNow;
```

**Good:**
```csharp
var now = SystemClock.Instance.GetCurrentInstant();
var today = SystemClock.Instance.GetCurrentInstant().InUtc().Date; // LocalDate
```

**Applies to:** All `*.cs` files under `src/` (excluding `Migrations/`)

**Detection patterns:**
- `DateTime.UtcNow`, `DateTime.Now`, `DateTime.Today`
- `DateTimeOffset.UtcNow`, `DateTimeOffset.Now`
- `DateOnly.FromDateTime`
- `new DateTime(`, `new DateTimeOffset(`
- Type declarations: `DateTime `, `DateTimeOffset `, `DateOnly `

---

### TD-005: Direct Guid.CreateVersion7 Usage (CRITICAL)

`Guid.CreateVersion7()` has broken byte order for PostgreSQL indexing. Use the `Guid7` wrapper instead.

**Bad:**
```csharp
var id = Guid.CreateVersion7();
```

**Good:**
```csharp
var id = Guid7.NewGuid();
```

**Applies to:** All `*.cs` files under `src/` (excluding `Utils/Guid7.cs`)

**Detection patterns:**
- `Guid.CreateVersion7()`

---

## Category 3: Resource & Configuration Duplication

### TD-006: Duplicated Resource Names (WARNING)

Resource identifiers (bucket names, queue names, exchange names, cache key prefixes) that appear in more than one file must be consolidated into a single static constants class.

**Bad:**
```csharp
// In StorageExtensions.cs
await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket("avatars"));

// In UploadAvatarEndpoint.cs
await minioClient.PutObjectAsync(new PutObjectArgs().WithBucket("avatars")...);
```

**Good:**
```csharp
public static class BucketNames
{
    public const string Avatars = "avatars";
    public const string Documents = "documents";
}

// Both files reference BucketNames.Avatars
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Same string literal appearing in 2+ different files as a resource identifier
- Bucket names, queue/exchange names, cache key prefixes, ClickHouse table names

---

### TD-007: Inline Timeout and Duration Values (INFO)

Timeout, retry, and duration values repeated inline should be named constants or sourced from configuration.

**Bad:**
```csharp
options.Timeout = TimeSpan.FromSeconds(30);
// ... elsewhere in the codebase
policy.TimeoutAsync(TimeSpan.FromSeconds(30));
```

**Good:**
```csharp
public static class DefaultTimeouts
{
    public static readonly TimeSpan Database = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ExternalApi = TimeSpan.FromSeconds(15);
}
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- `TimeSpan.FromSeconds(`, `TimeSpan.FromMinutes(`, `TimeSpan.FromMilliseconds(` with literal numeric values
- Same duration value appearing in multiple locations

---

## Category 4: Naming Conventions

### TD-009: Configuration Models Must Use Options Suffix (WARNING)

Classes that bind to `appsettings.json` sections must be named `{Name}Options`. The `*Settings` and `*Config` suffixes are prohibited for configuration binding classes. The `const string SectionName` pattern inside the class remains unchanged.

**Bad:**
```csharp
public sealed class RedisSettings
{
    public const string SectionName = "Redis";
    public required string ConnectionString { get; init; }
}

public sealed class SmtpConfig
{
    public const string SectionName = "Smtp";
    public required string Host { get; init; }
}
```

**Good:**
```csharp
public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public required string ConnectionString { get; init; }
}

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public required string Host { get; init; }
}
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Class names ending in `Settings` or `Config` that contain a `SectionName` constant
- Classes used with `GetSection<T>()`, `Configure<T>()`, `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`

---

### TD-010: EF Core Entity Configurations Must Use Configuration Suffix (WARNING)

Classes implementing `IEntityTypeConfiguration<T>` must be named `{EntityName}Configuration`.

**Bad:**
```csharp
public class UserMap : IEntityTypeConfiguration<User> { }
public class UserEntityConfig : IEntityTypeConfiguration<User> { }
```

**Good:**
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User> { }
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Classes implementing `IEntityTypeConfiguration<T>` without `Configuration` suffix

---

### TD-011: Endpoint Classes Must Use Endpoint Suffix (WARNING)

Classes implementing `IEndpoint` must be named `{Operation}Endpoint`.

**Bad:**
```csharp
public sealed class UploadFile : IEndpoint { }
public sealed class DeleteFile : IEndpoint { }
```

**Good:**
```csharp
public sealed class UploadFileEndpoint : IEndpoint { }
public sealed class DeleteFileEndpoint : IEndpoint { }
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Classes implementing `IEndpoint` without `Endpoint` suffix

---

### TD-013: Endpoint Input/Output Must Use {Operation}Request/{Operation}Response (WARNING)

Records entering or leaving an endpoint must be named `{Operation}Request` and `{Operation}Response`. They must be `sealed record` types nested inside the endpoint class. The terms `Dto`, `DTO`, `Model`, and `ViewModel` are prohibited for endpoint input/output types. Not all endpoints need both — e.g., a GET with route params may have no request record.

**Bad:**
```csharp
public sealed class CreateUserEndpoint : IEndpoint
{
    public sealed record Request(string Email, string Name);           // Missing operation prefix
    public sealed record Response(Guid Id);                            // Missing operation prefix
}

public sealed record CreateUserDto(string Email, string Name);         // DTO prohibited + not nested
```

**Good:**
```csharp
public sealed class CreateUserEndpoint : IEndpoint
{
    public sealed record CreateUserRequest(string Email, string Name);
    public sealed record CreateUserResponse(Guid Id);

    public static void MapEndpoint(IEndpointRouteBuilder app) { ... }
    private static async Task<IResult> HandleAsync(CreateUserRequest request, ...) { ... }
}
```

**Applies to:** All `*.cs` files under `src/`

**Detection patterns:**
- Records/classes with `Dto`/`DTO`/`Model`/`ViewModel` suffix under `Features/`
- Nested records named just `Request` or `Response` without operation prefix
- Request/Response records not nested inside the endpoint class

---

## Category 5: Code Documentation

### TD-012: Non-Trivial Methods Must Be Commented (WARNING)

Non-trivial methods must have a summary comment above the method signature explaining what it does, and inline annotations within the method body for any logic that is not immediately obvious. Comments must be simple and compact — avoid verbose XML doc blocks. Trivial methods (simple getters, one-liner delegations, obvious CRUD) do not require comments.

**Bad:**
```csharp
public static string HashPassword(string password)
{
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithm, KeySize);
    var result = new byte[SaltSize + KeySize];
    salt.CopyTo(result, 0);
    hash.CopyTo(result, SaltSize);
    return Convert.ToBase64String(result);
}
```

**Good:**
```csharp
// Hashes a password using PBKDF2-SHA512 with a random salt.
public static string HashPassword(string password)
{
    var salt = RandomNumberGenerator.GetBytes(SaltSize);
    var hash = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithm, KeySize);

    // Concatenate salt + hash into a single byte array for storage
    var result = new byte[SaltSize + KeySize];
    salt.CopyTo(result, 0);
    hash.CopyTo(result, SaltSize);

    return Convert.ToBase64String(result);
}
```

**Applies to:** All `*.cs` files under `src/` (excluding `Migrations/`)

**Detection patterns:**
- Methods longer than ~5 lines with no comments at all (header or inline)
- Complex logic: bitwise operations, cryptographic routines, multi-step algorithms, LINQ chains with side effects
- Non-obvious control flow: early returns with conditions, nested loops, retry/fallback patterns
- **Exceptions:** Trivial methods (property accessors, simple delegations, one-liner lambdas, obvious CRUD operations)

---

## Category 6: Project Configuration

### TD-014: Missing InternalsVisibleTo for Test Projects (WARNING)

Every non-test `.csproj` under `src/` must include `<InternalsVisibleTo>` pointing to its corresponding test project. Test projects using NSubstitute must include `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />`.

**Bad:**
```xml
<!-- src/Kakeibo.Api/Kakeibo.Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <!-- No InternalsVisibleTo -->
</Project>
```

**Good:**
```xml
<!-- src/Kakeibo.Api/Kakeibo.Api.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <InternalsVisibleTo Include="Kakeibo.Tests" />
  </ItemGroup>
</Project>
```

**Applies to:** All `.csproj` files under `src/`

**Detection patterns:**
- `.csproj` files under `src/` without any `<InternalsVisibleTo>` element
- Test projects using NSubstitute without `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />`

---

## Category 7: Monorepo Script Alignment

### TD-015: Monorepo Scripts Must Target Kakeibo.slnx (WARNING)

Scripts in `package.json` that perform solution-wide operations (build, restore, test, format) must target `Kakeibo.slnx` explicitly. Scripts in `package.json` must stay aligned with `scripts/quality-check.ts`. Every test project under `tests/` must have a corresponding `api:test:*` script.

**Bad:**
```json
{
  "api:build": "dotnet build src/Kakeibo.Api/Kakeibo.Api.csproj",
  "api:restore": "dotnet restore src/Kakeibo.Api/Kakeibo.Api.csproj",
  "api:format:check": "dotnet format --verify-no-changes",
  "api:test": "dotnet test"
}
```

**Good:**
```json
{
  "api:build": "dotnet build Kakeibo.slnx",
  "api:restore": "dotnet restore Kakeibo.slnx",
  "api:format:check": "dotnet format Kakeibo.slnx --verify-no-changes",
  "api:test": "dotnet test tests/Kakeibo.Tests/ --configuration Release"
}
```

**Applies to:** `package.json`, `scripts/quality-check.ts`

**Detection patterns:**
- `dotnet build`, `dotnet restore`, `dotnet format` without explicit `Kakeibo.slnx` target (excluding project-specific scripts like `api:run`)
- Commands in `package.json` that diverge from their equivalent in `quality-check.ts` (e.g., different target or missing flags)
- `tests/Kakeibo.Tests/` not having a corresponding `api:test` script in `package.json`
- **Exceptions:** `api:run` (runs a single project), `--no-restore`/`--no-build` flags in `quality-check.ts` (pipeline optimization)

---

## Category 8: Redundant State

### TD-016: Redundant Boolean for Nullable Timestamp (WARNING)

When a nullable timestamp already encodes a boolean condition (e.g., `DeletedAt != null` means "is deleted"), a separate `bool` field is redundant. Use only the nullable timestamp and derive the boolean as a computed property or expression-bodied member. This avoids state desynchronization bugs where one field is updated but the other is forgotten.

**Bad:**
```csharp
public bool IsDeleted { get; set; }
public Instant? DeletedAt { get; set; }
```

**Good:**
```csharp
public Instant? DeletedAt { get; set; }
public bool IsDeleted => DeletedAt is not null;
```

**Applies to:** All `*.cs` files under `src/` (excluding `Migrations/`)

**Detection patterns:**
- A `bool` property paired with a nullable timestamp where the boolean's semantics are equivalent to "timestamp is not null"
- Common pairs: `IsDeleted` + `DeletedAt`, `IsVerified` + `VerifiedAt`, `IsConfirmed` + `ConfirmedAt`, `IsLocked` + `LockedAt`, `IsCompleted` + `CompletedAt`
- The boolean and the timestamp are set independently (risk of desynchronization)

---

## Category 9: Import Conventions

### TD-017: Relative Path Imports Beyond Same Directory (WARNING)

Imports that point to a directory other than the current one must use the configured path alias (`@/`). Only same-folder relative imports are allowed (`./file`). Deep relative paths (`../`, `../../`) make refactoring harder and obscure the true source location of a dependency.

**Bad:**
```ts
import { Button } from '../../components/Button';
import { useAuthStore } from '../../../stores/auth';
import { formatDate } from '../utils/date';
```

**Good:**
```ts
import { Button } from '@/components/Button';
import { useAuthStore } from '@/stores/auth';
import { formatDate } from '@/utils/date';
// Allowed — same directory
import { helper } from './utils';
```

**Applies to:** All `*.ts`, `*.tsx`, `*.vue`, `*.js`, `*.jsx` files under `sites/` and `services/`

**Detection patterns:**
- Import paths starting with `../` (one or more levels up)
- Import paths starting with `../../` (two or more levels up)
- **Exceptions:** Same-directory imports starting with `./` are always allowed

---

## Category 10: Icon Library Consistency

### TD-018: Mixed Icon Libraries in the Same Project (INFO)

When two icon libraries are declared simultaneously (e.g., `lucide-vue-next` and `@hugeicons/vue`),
the project must document which library is canonical for application-level components and plan to
consolidate. The coexistence state should be temporary.

**Context:** shadcn-vue primitive components under `components/ui/` embed icon imports
directly in their copied source files (see KB-006). Until those primitives are migrated,
the embedded icon library must remain as a dependency even if another library is the primary choice for
new components.

**Resolution:** Migrate the affected `components/ui/` files to the canonical icon library,
then remove the legacy dependency from `package.json`.

**Applies to:** `sites/Kakeibo.App/package.json`, `sites/Kakeibo.App/components/ui/`

**Detection patterns:**
- Two icon library packages declared simultaneously in `package.json`
- Files under `components/ui/` importing from a different icon library than files under `components/`

---

## Category 11: Test Infrastructure

### TD-019: Testcontainers Tests Must Skip When Docker Is Unavailable (WARNING)

Any test that starts a Testcontainers container must wrap the startup call in a `try-catch`
that calls `Assert.Skip()`. Letting `DockerUnavailableException` propagate causes the test to
**fail** in CI environments without Docker access, breaking the pipeline. See KB-008 for full
patterns.

**Bad:**
```csharp
// TestDbContextFactory — no skip guard
private static readonly Lazy<Task> ContainerStartTask = new(() => PostgresContainer.StartAsync());

public static async Task<MyDbContext> CreateAsync()
{
    await ContainerStartTask.Value;   // throws DockerUnavailableException in CI → test fails
    // ...
}
```

```csharp
// IAsyncLifetime — no skip guard
public async ValueTask InitializeAsync()
{
    await ContainerStartTask.Value;   // throws → test fails
    // ...
}
```

**Good:**
```csharp
// TestDbContextFactory — skip guard in private helper
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
```

```csharp
// IAsyncLifetime — skip guard inline
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
    // ...
}
```

**Applies to:** All `*.cs` files under `tests/` that instantiate any Testcontainers container
(PostgreSQL, Redis, MinIO, ClickHouse, etc.)

**Detection patterns:**
- `await ContainerStartTask.Value` or `await PostgresContainer.StartAsync()` **not** inside a
  `try-catch` block
- `new PostgreSqlBuilder(...)`, `new RedisBuilder(...)`, or any `new *Builder(...).Build()` call
  without a corresponding skip guard on the startup await
- `IAsyncLifetime.InitializeAsync` or `IAsyncLifetime.InitializeAsync` that awaits a container
  without a surrounding `try-catch`

### TD-020: `.WithReuse(true)` Prohibited in Testcontainers (CRITICAL)

`.WithReuse(true)` on any Testcontainers builder is prohibited in test files. It causes
`Build()` to validate Docker connectivity at class load time (static constructor), which
throws `DockerUnavailableException` before the `Assert.Skip()` guard can execute,
breaking CI pipelines. See mandatory.md Rule 4 and KB-008.

**Bad:**
```csharp
private static readonly PostgreSqlContainer PostgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
    .WithReuse(true)   // ← PROHIBITED
    .Build();
```

**Good:**
```csharp
private static readonly PostgreSqlContainer PostgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .WithCommand("-c", "max_connections=500")
    .Build();
```

**Applies to:** All `*.cs` files under `tests/`

**Detection patterns:**
- `.WithReuse(true)` on any Testcontainers builder (`PostgreSqlBuilder`, `RedisBuilder`, etc.)

---

## Category 12: .NET Modernization

### TD-021: .NET Code Must Be Checked Against the dotnet-modernize Skill (WARNING)

When reviewing or auditing C# files under `src/`, always invoke the `/dotnet-modernize`
skill to identify modernization opportunities. The skill covers target frameworks, deprecated
packages, superseded API patterns, missing build infrastructure, and outdated C# language
patterns.

**Applies to:** All `*.cs`, `*.csproj`, `Directory.Build.props`, `Directory.Packages.props`
files under `src/`

**When to apply:**
- During any `/audit-tech-debt` run on backend code
- When adding a new module or project
- When touching an existing `.csproj` or `Directory.Build.props`
- When the compiler emits an `[Obsolete]` or deprecation warning on a dependency

**Key areas checked by the skill (summary):**

| Area | What to look for |
|------|-----------------|
| Target Framework | `net9.0` or lower → upgrade to `net10.0` LTS |
| Deprecated packages | `Microsoft.Extensions.Http.Polly`, `Newtonsoft.Json` in new projects, `Swashbuckle` (`.NET 9+`), `System.Data.SqlClient` |
| API patterns | Legacy `Startup.cs`, synchronous I/O (`File.ReadAllText`, non-async stream reads), non-generic collections (`ArrayList`, `Hashtable`) |
| Language patterns | `null != x` → `x is not null`; `new ClassName()` → `new()`; block-scoped namespaces → file-scoped; manual constructors → primary constructors (also enforced by Rule 8 in mandatory.md) |
| Build infrastructure | Missing `Directory.Build.props`, `.editorconfig`, `global.json`, `NuGetAudit`, nullable reference types, `.slnx` format |
| Security | `dotnet list package --vulnerable`, `--deprecated`, `--outdated` |

**Detection patterns:**
- `<TargetFramework>net9.0` or lower in any `.csproj` or `Directory.Build.props`
- `PackageReference` or `PackageVersion` for deprecated packages listed in the skill
- `class Startup` anywhere in `src/`
- Synchronous file/stream I/O calls without `Async` suffix
- `ArrayList`, `Hashtable`, or other non-generic collection types
- Null checks with `!= null` / `== null` instead of `is not null` / `is null`
- Block-scoped `namespace Foo.Bar { }` instead of file-scoped `namespace Foo.Bar;`

---

## Adding New Rules

To add a new rule:

1. Choose the next available `TD-xxx` ID within the appropriate category
2. Follow the template: ID, title, severity, description, bad example, good example, applies to, detection patterns
3. The `/audit-tech-debt` command reads this document dynamically — new rules are automatically included in the next audit run
