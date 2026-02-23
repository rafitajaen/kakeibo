# Test Infrastructure — Canonical Implementations

All reusable test infrastructure patterns for the Kakeibo monorepo.

---

## TestDbContextFactory (API)

Creates an isolated real PostgreSQL database per test using Testcontainers.
**Never add `.WithReuse(true)`** — it breaks the CI Docker skip guard (Rule 4, KB-008).

```csharp
internal static class TestDbContextFactory
{
    // Build() does NOT start Docker — only configures the builder.
    // NEVER add .WithReuse(true) — causes Build() to call Validate() at class load time,
    // which fails before Assert.Skip() can run in CI environments without Docker.
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCommand("-c", "max_connections=500")
            .Build();

    // Lazy<Task> guarantees a single container startup per assembly
    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    // Skip guard: in CI without Docker, the test is skipped (not failed)
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

    // Creates an isolated DB per test. Always use `await using`.
    public static async Task<MyModuleDbContext> CreateAsync()
    {
        await EnsureContainerStartedAsync();

        var databaseName = $"kakeibo_test_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(PostgresContainer.GetConnectionString())
        {
            Database = databaseName
        };

        var options = new DbContextOptionsBuilder<MyModuleDbContext>()
            .UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        var context = new MyModuleDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
```

**Usage:**

```csharp
// Always await using — disposes context and cleans up resources
await using var db = await TestDbContextFactory.CreateAsync();
var ct = TestContext.Current.CancellationToken;
```

---

## FakeClock

`NodaTime.Testing.FakeClock` is the canonical time fake. Inject it wherever `IClock` is needed.
Never use `SystemClock.Instance` in test code — time must be deterministic.

```csharp
// Declare in test class
private readonly FakeClock _clock = new(Instant.FromUtc(2026, 7, 15, 12, 0));

// Inject into handler/job under test
var handler = new CreateWalletHandler(db, eventBus, _clock);
var job = new CheckExpiringSubscriptionsJob(db, notifications, _clock, NullLogger<...>.Instance);

// Advance time in multi-step tests
_clock.AdvanceMinutes(5);
_clock.AdvanceHours(24);
_clock.AdvanceDays(7);

// Read current instant (same API as SystemClock)
var now = _clock.GetCurrentInstant();
```

Static clock for class-level sharing (safe when tests don't mutate time):

```csharp
private static readonly FakeClock Clock = new(Instant.FromUtc(2024, 1, 15, 0, 0));
```

---

## WebApplicationFactory (Level 5 Integration Tests)

Full `WebApplicationFactory<Program>` for API integration tests. Each test **class** gets its own
factory instance with a unique isolated database. The PostgreSQL container is shared across all
instances in the assembly (started once, never reused per Mandatory Rule 4).

```csharp
public sealed class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestJwtSecretKey = "test-secret-key-for-integration-tests-min-32-chars!!";
    public const string TestJwtIssuer = "kakeibo-api";
    public const string TestJwtAudience = "kakeibo-app";

    // Static container shared across all factory instances in the assembly.
    // Started at most once — each class gets its own database, not its own container.
    // NEVER add .WithReuse(true) — see Mandatory Rule 4, KB-008.
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    internal ConcurrentDictionary<string, string> RedisStore { get; } = new();
    public bool IsDockerAvailable { get; private set; }

    // Each factory instance (= each test class) gets its own isolated database.
    private readonly string _databaseName = $"kakeibo_integration_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = _connectionString,
                ["Jwt:SecretKey"] = TestJwtSecretKey,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
            }));

        builder.ConfigureServices(services =>
        {
            // Real PostgreSQL via PostgreSqlContainer (postgres:18-alpine)
            ReplaceDbContextsWithTestContainers(services);

            // Redis → NSubstitute mock backed by ConcurrentDictionary<string, string>
            StubRedisWithConcurrentDictionary(services);

            // FusionCache → memory-only (no distributed layer in tests)
            ConfigureFusionCacheMemoryOnly(services);

            // Remove all IHostedService (OutboxProcessor, Hangfire scheduler)
            DisableBackgroundServices(services);

            // External HTTP clients stubbed (email renderer, ClickHouse, RustFS)
            StubExternalHttpClients(services);
        });
    }

    public async ValueTask InitializeAsync()
    {
        try
        {
            await ContainerStartTask.Value;

            // Build a connection string pointing to this class's isolated database
            var builder = new NpgsqlConnectionStringBuilder(PostgresContainer.GetConnectionString())
            {
                Database = _databaseName
            };
            _connectionString = builder.ConnectionString;

            IsDockerAvailable = true;
        }
        catch
        {
            IsDockerAvailable = false;
        }
    }

    public new async ValueTask DisposeAsync() => await base.DisposeAsync();

    public AuthTestClient CreateAuthClient() => new(this);
    public TestDataBuilder CreateTestDataBuilder() => new(Services);
}
```

### Class fixture (one factory = one isolated database per test class)

```csharp
// Each test class implements IClassFixture — gets its own factory instance and its own database.
// Tests within the same class share one database: use unique data (Guid-suffixed emails, etc.)
// to avoid interference between tests in the same class.
public sealed class MemberRegistrationTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory>
{
    private const string SkipReason =
        "Docker is not available. Integration tests require Docker to run Testcontainers.";

    [Fact]
    public async Task MyTest()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);
        // ...
    }
}
```

> **Smoke tests are the exception:** `Kakeibo.SmokeTests` keeps `ICollectionFixture<WebApplicationFactory>`
> because its 7 flows are sequential and some depend on state created in earlier flows
> (e.g., the audit flow requires member records created by the domain event flow to already exist).
> See [smoke-tests.md](smoke-tests.md) for details.

---

## Base Test Classes

### BaseIntegrationTest (Level 5 — `Kakeibo.Api.IntegrationTests`)

Abstract base class that centralizes the skip guard and DI resolution for all integration test classes.
Place in `tests/Kakeibo.Api.IntegrationTests/BaseIntegrationTest.cs`.

```csharp
// Each concrete test class inherits this and receives the factory via IClassFixture<>.
[Collection("Integration")]
public abstract class BaseIntegrationTest(WebApplicationFactory factory)
{
    private const string SkipReason =
        "Docker is not available. Integration tests require Docker to run Testcontainers.";

    // Call at the top of every [Fact] — skips test if Docker is unavailable.
    protected void SkipIfDockerUnavailable()
    {
        if (!factory.IsDockerAvailable)
            Assert.Skip(SkipReason);
    }

    // Resolves a service from DI in a new scope.
    // Caller is responsible for disposing the scope when finished.
    protected T GetService<T>() where T : notnull
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    protected AuthTestClient CreateAuthClient() => factory.CreateAuthClient();
    protected TestDataBuilder CreateTestDataBuilder() => factory.CreateTestDataBuilder();
}
```

Usage:

```csharp
public sealed class MemberRegistrationTests(WebApplicationFactory factory)
    : BaseIntegrationTest(factory), IClassFixture<WebApplicationFactory>
{
    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        SkipIfDockerUnavailable();

        using var client = CreateAuthClient();
        // ...
    }
}
```

### Architecture test assembly references

`Kakeibo.ArchitectureTests` does not use a base class — each test class declares its own assembly references directly
via `typeof(XxxModuleRegistration).Assembly`. This avoids a static initializer that could fail if an assembly
is missing or renamed.

```csharp
public sealed class DependencyDirectionTests
{
    // Each test class declares exactly the assemblies it needs — no shared base.
    private static readonly Assembly CommonAssembly =
        typeof(IEndpoint).Assembly;                              // Kakeibo.Common

    private static readonly Assembly ContractsAssembly =
        typeof(MemberCreatedEvent).Assembly;                     // Kakeibo.Contracts

    private static readonly Assembly InfrastructureAssembly =
        typeof(OutboxProcessor).Assembly;                        // Kakeibo.Infrastructure

    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(IdentityModuleRegistration).Assembly,
        typeof(MembersModuleRegistration).Assembly,
        typeof(NotificationsModuleRegistration).Assembly,
        // ... all modules
    ];

    // Tests use these assembly references directly — no base class needed.
}
```

---

## AuthTestClient

Wraps `HttpClient` and tracks JWT access tokens and refresh cookies across requests.

```csharp
public sealed class AuthTestClient : IDisposable
{
    private readonly HttpClient _client;
    public string? AccessToken { get; set; }
    public string? RefreshTokenCookie { get; set; }

    public AuthTestClient(WebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    // Auto-attaches Bearer token and refresh cookie on every request
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (AccessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        if (RefreshTokenCookie is not null)
            request.Headers.Add("Cookie", $"refreshToken={RefreshTokenCookie}");

        return await _client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password, rememberMe });
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            AccessToken = body.GetProperty("accessToken").GetString();
        }
        return response;
    }

    public Task<HttpResponseMessage> RegisterAsync(string email, string username, string password) =>
        _client.PostAsJsonAsync("/api/auth/register", new { email, username, password });

    public Task<HttpResponseMessage> GetProfileAsync() =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/users/me/profile"));

    public void Dispose() => _client.Dispose();
}
```

---

## TestDataBuilder

Seeds the database directly via `IServiceProvider` — bypasses HTTP. Use for preconditions
that are out of scope for the test being written.

```csharp
public sealed class TestDataBuilder(IServiceProvider services)
{
    // Creates a verified user with the given role (bypasses email verification flow)
    public async Task<Guid> CreateVerifiedUserAsync(string email, string password, string roleName = "Employee")
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var role = await db.Roles.FirstAsync(r => r.Name == roleName);

        var user = new User
        {
            Email = email,
            Username = email.Split('@')[0],
            PasswordHash = hasher.Hash(password),
            EmailVerifiedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            RoleId = role.Id,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<Guid> CreateSuperAdminAsync(string email, string password) =>
        await CreateVerifiedUserAsync(email, password, "Admin");

    public async Task<(Guid UserId, string VerifyToken)> CreateUnverifiedUserAsync(string email, string password)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var user = new User { Email = email };
        var token = "test-verify-token";
        // ... seed unverified user with token
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, token);
    }

    public async Task LockUserAsync(Guid userId, int lockoutMinutes = 60)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await db.Users.FindAsync(userId);
        user!.LockoutEnd = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(lockoutMinutes));
        await db.SaveChangesAsync();
    }
}
```

---

## Vitest Global Setup with i18n

```typescript
// vitest.setup.ts (Kakeibo.App and Kakeibo.Mobile)
import { config } from '@vue/test-utils'
import { createI18n } from 'vue-i18n'
import en from '@/locales/en.json'
import es from '@/locales/es.json'

// Export for use in individual tests that need to inject the plugin manually
export const i18n = createI18n({
    legacy: false,
    locale: 'es',
    messages: { en, es },
    missingWarn: true,   // warns on missing keys — catches i18n gaps
    fallbackWarn: true,
})

// Apply globally to all mounted components
config.global.plugins = [i18n]
```

In `vitest.config.ts`:

```typescript
export default defineConfig({
    test: {
        environment: 'jsdom',
        setupFiles: ['./vitest.setup.ts'],
        globals: true,
    },
})
```

---

## Capacitor Plugin Mocking (Mobile Vitest)

```typescript
// vitest.setup.mobile.ts
vi.mock('@capacitor/network', () => ({
    Network: {
        getStatus: vi.fn().mockResolvedValue({ connected: true, connectionType: 'wifi' }),
        addListener: vi.fn().mockResolvedValue({ remove: vi.fn() }),
    }
}))

vi.mock('@capacitor/preferences', () => ({
    Preferences: {
        get: vi.fn().mockResolvedValue({ value: null }),
        set: vi.fn().mockResolvedValue(undefined),
        remove: vi.fn().mockResolvedValue(undefined),
        clear: vi.fn().mockResolvedValue(undefined),
    }
}))

vi.mock('@capacitor/camera', () => ({
    Camera: {
        getPhoto: vi.fn().mockResolvedValue({
            base64String: 'fake-base64',
            format: 'jpeg',
        }),
    }
}))

vi.mock('@codetrix-studio/capacitor-google-auth', () => ({
    GoogleAuth: {
        signIn: vi.fn().mockResolvedValue({
            email: 'test@gmail.com',
            idToken: 'fake-google-id-token',
        }),
        signOut: vi.fn().mockResolvedValue(undefined),
    }
}))
```

Per-test override (to test specific behaviors):

```typescript
beforeEach(() => {
    // Reset to default "online" state
    vi.mocked(Network.getStatus).mockResolvedValue({ connected: true, connectionType: 'wifi' })
})

it('handles offline state', async () => {
    vi.mocked(Network.getStatus).mockResolvedValue({ connected: false, connectionType: 'none' })
    // ...
})
```

---

## Playwright Config (Full Reference)

```typescript
// playwright.config.ts
import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
    testDir: './e2e',
    globalSetup: './e2e/global-setup.ts',
    globalTeardown: './e2e/global-teardown.ts',
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: process.env.CI ? 1 : undefined,
    reporter: 'html',
    use: {
        baseURL: 'http://localhost:5173',
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
    },
    projects: [
        // Auth setup runs first
        { name: 'setup', testMatch: /.*\.setup\.ts/ },
        // Authenticated admin
        {
            name: 'admin',
            use: {
                ...devices['Desktop Chrome'],
                storageState: 'playwright/.auth/admin.json',
            },
            dependencies: ['setup'],
        },
        // Authenticated member
        {
            name: 'member',
            use: {
                ...devices['Desktop Chrome'],
                storageState: 'playwright/.auth/member.json',
            },
            dependencies: ['setup'],
        },
        // No auth (login, register pages)
        {
            name: 'unauthenticated',
            use: devices['Desktop Chrome'],
            testMatch: /e2e\/auth\/(login|register)\.spec\.ts/,
        },
    ],
    webServer: {
        command: 'bun run app:dev',
        url: 'http://localhost:5173',
        reuseExistingServer: !process.env.CI,
    },
})
```

---

## xUnit Parallelism Configuration

Tests within the same assembly run in parallel by default in xUnit v3. Each Level 2 handler
test gets its own isolated DB, so there are no conflicts. Level 5 integration tests share one
`WebApplicationFactory` via `[Collection("Integration")]`, which serializes them.

Configure parallelism in `xunit.runner.json` (place in the test project root):

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

**Rules:**
- Level 2 (Handler Unit): parallel by default — each test has its own DB via `TestDbContextFactory.CreateAsync()`
- Level 5 (Integration): parallel across classes — each class has its own factory + isolated DB via `IClassFixture`;
  tests within one class run sequentially (xUnit default for a single class)
- Level 3 (Domain Event Handler): parallel — no DB, pure NSubstitute mocks
- Level 4 (Background Job): parallel — each test has its own DB via `TestDbContextFactory.CreateAsync()`

```csharp
// IClassFixture: xUnit constructs one factory per test class and injects it via the constructor.
// Tests within the class run sequentially; test CLASSES run in parallel (each with its own DB).
public sealed class MemberRegistrationTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory> { ... }

public sealed class MemberDeletionTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory> { ... }
// ↑ MemberRegistrationTests and MemberDeletionTests run in parallel — different DBs, no interference.
```

---

## FakeClock vs TimeProvider

**Kakeibo uses NodaTime for all domain and application time logic.** The standard clock abstraction is:

| | FakeClock (NodaTime.Testing) | TimeProvider (.NET 8+) |
|-|------------------------------|------------------------|
| **When to use** | Any code accepting `IClock` (handlers, jobs, domain services, validators) | Infrastructure code using `DateTime` from a third-party library |
| **In Kakeibo** | ✅ Standard for all tests | ❌ Not needed — we don't use `DateTime`/`DateTimeOffset` anywhere |
| **API** | `_clock.AdvanceMinutes(5)`, `_clock.GetCurrentInstant()` | `TimeProvider.GetUtcNow()` |

```csharp
// ✅ Always use FakeClock in Kakeibo tests
private readonly FakeClock _clock = new(Instant.FromUtc(2026, 2, 17, 12, 0));
var handler = new CreateWalletHandler(db, eventBus, _clock);

// ❌ Don't use TimeProvider in Kakeibo domain tests
var clock = TimeProvider.System;  // ← no NodaTime integration

// ❌ Never use SystemClock.Instance in tests (non-deterministic)
var now = SystemClock.Instance.GetCurrentInstant();
```

**Decision:** If you encounter infrastructure code that uses `DateTime` (e.g., a third-party
library), inject `TimeProvider` from DI for that code only. For all domain and application
layer code: inject `IClock` and use `FakeClock` in tests.

---

## AssertStatusCode Extension

```csharp
// HttpResponseMessageExtensions.cs (test project)
public static class HttpResponseMessageExtensions
{
    public static void AssertStatusCode(
        this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new XunitException(
                $"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}.\nBody: {body}");
        }
    }
}
```

---

## EditorConfig Overrides for Test Projects

Add these overrides to the root `.editorconfig` to suppress false-positive analyzer warnings in test code.
The `tests/**/*.cs` glob applies to every file in any folder under `tests/`.

```editorconfig
[tests/**/*.cs]
# xUnit test method names use underscores by convention — Method_Scenario_Result
dotnet_diagnostic.CA1707.severity = none

# Test parameters don't need null guards — the test framework controls input
dotnet_diagnostic.CA1062.severity = none

# ConfigureAwait is irrelevant in tests — there's no synchronization context to capture
dotnet_diagnostic.CA2007.severity = none

# Assigning to a variable without reading it is acceptable in the Arrange phase
dotnet_diagnostic.IDE0059.severity = none
```

**Why these suppressions matter:**
- Without `CA1707`: the analyzer flags every `HandleAsync_DuplicateName_ReturnsError` as a style violation
- Without `CA2007`: it demands `ConfigureAwait(false)` on every `await` inside test methods, which adds noise without benefit
- Without `IDE0059`: assigning seed entities to variables for readability (`var member = CreateWallet(...)`) triggers warnings even when the variable is only used in `db.Members.Add(member)`
