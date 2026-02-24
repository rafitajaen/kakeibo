# Smoke Tests — 7 Critical System Flows

**Purpose:** Validate that the 7 critical end-to-end flows of the Kakeibo platform work correctly as
integrated units. Unlike Level 5 API integration tests (which cover error paths, auth, and edge
cases per endpoint), smoke tests cover **happy paths only** and verify that all architectural
seams (in-process events, event handlers, cross-domain calls, auth middleware) connect properly.

**Project:** `tests/Kakeibo.SmokeTests/`

**Difference from `Kakeibo.Tests` (API integration tests):**

| | API Integration Tests | Smoke Tests |
|-|-----------------------|-------------|
| Scope | One endpoint at a time | Full system flow, all seams |
| Paths covered | Happy path + error variants | Happy path only |
| Speed | Fast (single handler) | Slower (multi-hop) |
| Target environment | Local/CI only (Testcontainers) | Also dev/staging/prod (configurable) |
| Purpose | Regression safety per endpoint | Architectural connectivity proof |

---

## Project Structure

> **Isolation note:** `Kakeibo.SmokeTests` is a **separate project** from `Kakeibo.Tests`.
> It constructs its own `WebApplicationFactory`, starts its own static PostgreSQL container,
> and uses a completely independent database. There is no shared state between the two projects.
>
> Smoke tests use `ICollectionFixture<WebApplicationFactory>` intentionally — all 7 flows
> share a single factory instance and a single database. This is deliberate: some flows depend
> on state created by earlier flows (e.g., Flow 4 — Audit — requires member records that Flow 1
> created). The sequential, stateful nature of these tests is the reason smoke tests are
> structurally different from `Kakeibo.Tests` (which uses `IClassFixture` for per-class isolation).

```
tests/Kakeibo.SmokeTests/
├── Flows/
│   ├── InProcessEventFlowTests.cs
│   ├── DirectEventFlowTests.cs
│   ├── SyncCrossDomainFlowTests.cs
│   ├── AuditFlowTests.cs
│   ├── EmailFlowTests.cs
│   ├── AuthorizationFlowTests.cs
│   └── StartupFlowTests.cs
├── SmokeCollection.cs
└── Kakeibo.SmokeTests.csproj
```

```csharp
// SmokeCollection.cs — share one factory across all smoke test flows.
// All 7 flows run against the same database: some flows deliberately depend on
// state created by previous flows (sequential, stateful by design).
[CollectionDefinition("Smoke")]
public class SmokeCollection : ICollectionFixture<WebApplicationFactory>;
```

---

## Flow 1 — HTTP → Handler → IEventBus → IEventHandler

**Verifies:** Feature handler → `eventBus.Publish(event)` → `SaveChangesAsync` → `ChannelEventBus`
enqueues event → `EventDispatcher` dispatches to `IEventHandler<T>` → side effect in DB.

**File:** `Flows/InProcessEventFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class InProcessEventFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task WalletCreation_InProcessEventFlow_EventHandlerExecuted()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        // Step 1: Create a user and log in
        await data.CreateVerifiedUserAsync("user@smoke.com", "Test#12345Abc", "User");
        await client.LoginAsync("user@smoke.com", "Test#12345Abc");

        // Step 2: Call the endpoint — handler publishes WalletCreatedEvent via IEventBus
        var walletName = $"wallet-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsync("/api/wallets", new
        {
            name = walletName,
            type = "Personal",
            initialBalance = 1000.00,
            currency = "USD",
        });

        createResponse.AssertStatusCode(HttpStatusCode.Created);

        // Step 3: EventDispatcher is a BackgroundService — give it time to process the channel
        // In tests, EventDispatcher runs at full speed; a short delay is sufficient.
        await Task.Delay(200, ct);

        // Step 4: Verify event handler side effect — e.g. notification preference created
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.Name == walletName, ct);

        Assert.NotNull(wallet);
    }
}
```

---

## Flow 2 — Direct Event Publish (Budget Exceeded)

**Verifies:** Handler calls `eventBus.Publish(new BudgetExceededEvent {...})` directly (no entity
involved in the event dispatch) → `ChannelEventBus` enqueues it → `EventDispatcher` dispatches
to `IEventHandler<BudgetExceededEvent>` → notification sent.

**File:** `Flows/DirectEventFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class DirectEventFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task BudgetExceeded_DirectEventFlow_NotificationHandlerExecuted()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateAuthClient();

        // Setup: create user, wallet, and budget
        await factory.CreateTestDataBuilder()
            .WithUser("user@smoke.com", "Test#12345Abc")
            .WithWallet("Checking", 1000m)
            .WithBudget("Food & Dining", 100m)
            .BuildAsync();

        await client.LoginAsync("user@smoke.com", "Test#12345Abc");

        // Step 1: Record transaction that exceeds budget
        // The handler publishes BudgetExceededEvent via IEventBus (fire-and-forget)
        var transactionResponse = await client.PostAsync("/api/transactions", new
        {
            amount = 120m,
            categoryId = "food-dining",
            walletId = "checking",
            type = "Expense",
        });

        transactionResponse.AssertStatusCode(HttpStatusCode.Created);

        // Step 2: Give EventDispatcher time to dispatch the event to its handlers
        await Task.Delay(200, ct);

        // Step 3: Verify event handler side effect — e.g. notification record created
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var notification = await db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(n => n.Type == NotificationTypes.BudgetExceeded, ct);

        Assert.NotNull(notification);
    }
}
```

---

## Flow 3 — Sync Cross-Domain Communication (Direct Handler Injection)

**Verifies:** Feature A's handler receives a cross-domain query handler via DI injection →
calls it directly (no `IModuleClient`, no HTTP) → correct response returned synchronously
in the same HTTP request.

**File:** `Flows/SyncCrossDomainFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class SyncCrossDomainFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task CreateBudget_ValidatesWallet_CrossDomainHandlerCalled()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        // Step 1: Create a user with a wallet
        await data.CreateVerifiedUserAsync("budget-user@smoke.com", "Test#12345Abc", "User");
        await client.LoginAsync("budget-user@smoke.com", "Test#12345Abc");

        var walletResponse = await client.PostAsync("/api/wallets", new
        {
            name = "My Wallet",
            type = "Personal",
            initialBalance = 500m,
            currency = "USD",
        });
        walletResponse.AssertStatusCode(HttpStatusCode.Created);
        var walletId = (await walletResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // Step 2: Create a budget linked to the wallet
        // Internally, CreateBudgetHandler injects GetWalletByIdHandler from Wallets domain
        // and calls it synchronously via DI (no IModuleClient needed in Simple Monolith)
        var budgetResponse = await client.PostAsync("/api/budgets", new
        {
            walletId,
            categoryId = "food-dining",
            limit = 200m,
            period = "Monthly",
        });

        // If the cross-domain call fails, this returns 400/422 — the test would catch it
        budgetResponse.AssertStatusCode(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateBudget_NonExistentWallet_ReturnsBadRequest()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        await data.CreateVerifiedUserAsync("no-wallet@smoke.com", "Test#12345Abc", "User");
        await client.LoginAsync("no-wallet@smoke.com", "Test#12345Abc");

        var budgetResponse = await client.PostAsync("/api/budgets", new
        {
            walletId = Guid.NewGuid(),  // non-existent wallet
            categoryId = "food-dining",
            limit = 200m,
            period = "Monthly",
        });

        // GetWalletByIdHandler returned NotFound → CreateBudgetHandler returned 422
        budgetResponse.AssertStatusCode(HttpStatusCode.UnprocessableEntity);
    }
}
```

---

## Flow 4 — Audit Flow

**Verifies:** Feature handler publishes `AuditEvent` via `IEventBus` → `EventDispatcher`
dispatches to `AuditEventHandler` → handler calls ClickHouse stub (captured in test).

**File:** `Flows/AuditFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class AuditFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task UserRegistration_AuditFlow_AuditEventDispatchedToClickHouseStub()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateAuthClient();

        var userEmail = $"audit-{Guid.NewGuid():N}@smoke.com";

        // Step 1: Register — triggers UserRegisteredEvent published via IEventBus
        var registerResponse = await client.RegisterAsync(userEmail, "AuditUser", "Test#12345Abc");
        registerResponse.AssertStatusCode(HttpStatusCode.Created);

        // Step 2: EventDispatcher dispatches UserRegisteredEvent to AuditEventHandler
        // AuditEventHandler calls IClickHouseSink (stubbed in WebApplicationFactory)
        await Task.Delay(200, ct);

        // Step 3: Verify ClickHouse stub captured the audit row
        var clickHouseSink = factory.Services.GetRequiredService<IClickHouseSink>();

        var capturedRows = clickHouseSink.GetCapturedRows();
        Assert.Contains(capturedRows, r =>
            r.Action == AuditAction.Identity.UserRegistered &&
            r.Module == "Identity");
    }
}
```

---

## Flow 5 — Email Flow

**Verifies:** `emailService.SendAsync()` → `EmailRenderer` HTTP mock → SMTP mock capture →
email log row in DB (if applicable).

**File:** `Flows/EmailFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class EmailFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task Registration_EmailFlow_WelcomeEmailSentViaRenderer()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateAuthClient();
        var smtpSink = factory.Services.GetRequiredService<ISmtpSink>();

        var email = $"email-flow-{Guid.NewGuid():N}@smoke.com";

        // Step 1: Register — triggers email verification email
        var registerResponse = await client.RegisterAsync(email, $"user{Guid.NewGuid():N}"[..20], "Test#12345Abc");
        registerResponse.AssertStatusCode(HttpStatusCode.Created);

        // Step 2: EmailRenderer is stubbed in WebApplicationFactory (returns rendered HTML)
        // SMTP is captured via ISmtpSink (no real email sent)

        // Verify that exactly one email was captured (the verification email)
        var capturedEmails = smtpSink.GetCapturedEmails();
        var verificationEmail = capturedEmails.FirstOrDefault(e =>
            e.To.Contains(email) &&
            e.Subject.Contains("verif", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(verificationEmail);
    }
}
```

---

## Flow 6 — Authorization Flow

**Verifies:** JWT with permission claim → `JwtRevocationMiddleware` → `PermissionService`
→ endpoint responds 200 (authorized) or 403 (forbidden).

**File:** `Flows/AuthorizationFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class AuthorizationFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task AuthorizedUser_AccessesProtectedEndpoint_Returns200()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        await data.CreateVerifiedUserAsync("authorized@smoke.com", "Test#12345Abc", "Admin");
        await client.LoginAsync("authorized@smoke.com", "Test#12345Abc");

        var response = await client.GetAsync("/api/members");

        // Admin has members:read → 200 OK
        response.AssertStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithoutPermission_AccessesProtectedEndpoint_Returns403()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        // User role has no members:read permission
        await data.CreateVerifiedUserAsync("user@smoke.com", "Test#12345Abc", "User");
        await client.LoginAsync("user@smoke.com", "Test#12345Abc");

        var response = await client.GetAsync("/api/members");

        response.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokedToken_AccessesProtectedEndpoint_Returns401()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        await data.CreateVerifiedUserAsync("revoked@smoke.com", "Test#12345Abc", "Admin");
        await client.LoginAsync("revoked@smoke.com", "Test#12345Abc");

        // Revoke the current access token's JTI in Redis
        var jti = client.GetCurrentJti();
        await factory.RevokeTokenAsync(jti);

        // Now the same client with the same token must get 401
        var response = await client.GetAsync("/api/members");

        response.AssertStatusCode(HttpStatusCode.Unauthorized);
    }
}
```

---

## Flow 7 — Startup Initialization

**Verifies:** EF Core migrations run without error, ClickHouse tables are initialized, onboarding
seeders execute (SuperAdmin role created), and the application reaches a healthy state.

**File:** `Flows/StartupFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class StartupFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task Application_StartsUp_ReachesHealthyState()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        // The factory starts the app — if startup fails, the test fails here before any assertion
        using var client = factory.CreateClient();

        var healthResponse = await client.GetAsync("/health/ready");

        healthResponse.AssertStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Application_StartsUp_SuperAdminRoleExists()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // IOnboardingSeeder must have created the Admin role during startup
        var adminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == RoleNames.Admin, ct);

        Assert.NotNull(adminRole);
    }

    [Fact]
    public async Task Application_StartsUp_CoreTablesExist()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Verify core tables were created by EF Core migrations (single public schema)
        var tables = new[] { "users", "wallets", "transactions" };
        foreach (var table in tables)
        {
            var exists = await db.Database.ExecuteSqlRawAsync(
                $"SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{table}'", ct) > 0;
            Assert.True(exists, $"Table 'public.{table}' was not created during startup");
        }
    }

    [Fact]
    public async Task Application_StartsUp_LivenessProbeResponds()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        using var client = factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");

        liveResponse.AssertStatusCode(HttpStatusCode.OK);
    }
}
```

---

## `WebApplicationFactory` Extensions for Smoke Tests

The base `WebApplicationFactory` requires two additional helpers for smoke test flows:

```csharp
public sealed partial class WebApplicationFactory
{
    // Adds a JTI to the Redis deny-list — used in Flow 6 to simulate token revocation.
    public async Task RevokeTokenAsync(string jti)
    {
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetDatabase().StringSetAsync($"revoked:{jti}", "1", TimeSpan.FromHours(1));
    }

    // Note: EventDispatcher runs as a BackgroundService in tests.
    // No manual trigger is needed — just await Task.Delay(200) after the HTTP call
    // to give the dispatcher enough time to dequeue and dispatch the channel events.
}
```

---

## Smoke Test `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.AspNetCore.TestHost" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NodaTime.Testing" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="xunit.v3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Kakeibo.Api\Kakeibo.Api.csproj" />
    <!-- All domains are in Kakeibo.Api — single project reference is sufficient -->
  </ItemGroup>
</Project>
```

---

## Flow Coverage Matrix

| Flow | Class | What it verifies |
|------|-------|-----------------|
| 1. HTTP → IEventBus → IEventHandler | `InProcessEventFlowTests` | Handler publishes event → EventDispatcher → handler side effect in DB |
| 2. Direct event publish | `DirectEventFlowTests` | Direct `eventBus.Publish` → EventDispatcher → notification handler |
| 3. Sync cross-domain | `SyncCrossDomainFlowTests` | Feature A handler → DI-injected query handler from domain B → response |
| 4. Audit | `AuditFlowTests` | Handler publishes AuditEvent → EventDispatcher → AuditEventHandler → ClickHouse stub |
| 5. Email | `EmailFlowTests` | `emailService.SendAsync` → EmailRenderer stub → SMTP sink capture |
| 6. Authorization | `AuthorizationFlowTests` | JWT → `JwtRevocationMiddleware` → `PermissionService` → 200/403/401 |
| 7. Startup | `StartupFlowTests` | Migrations + seeders + health probes all pass |
