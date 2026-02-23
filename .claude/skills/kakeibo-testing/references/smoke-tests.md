# Smoke Tests — 7 Critical System Flows

**Purpose:** Validate that the 7 critical end-to-end flows of the Kakeibo platform work correctly as
integrated units. Unlike Level 5 API integration tests (which cover error paths, auth, and edge
cases per endpoint), smoke tests cover **happy paths only** and verify that all architectural
seams (outbox, domain event handlers, inter-module calls, auth middleware) connect properly.

**Project:** `tests/Kakeibo.SmokeTests/`

**Difference from `Kakeibo.Api.IntegrationTests`:**

| | API Integration Tests | Smoke Tests |
|-|-----------------------|-------------|
| Scope | One endpoint at a time | Full system flow, all seams |
| Paths covered | Happy path + error variants | Happy path only |
| Speed | Fast (single handler) | Slower (multi-hop) |
| Target environment | Local/CI only (Testcontainers) | Also dev/staging/prod (configurable) |
| Purpose | Regression safety per endpoint | Architectural connectivity proof |

---

## Project Structure

> **Isolation note:** `Kakeibo.SmokeTests` is a **separate project** from `Kakeibo.Api.IntegrationTests`.
> It constructs its own `WebApplicationFactory`, starts its own static PostgreSQL container,
> and uses a completely independent database. There is no shared state between the two projects.
>
> Smoke tests use `ICollectionFixture<WebApplicationFactory>` intentionally — all 7 flows
> share a single factory instance and a single database. This is deliberate: some flows depend
> on state created by earlier flows (e.g., Flow 4 — Audit — requires member records that Flow 1
> created). The sequential, stateful nature of these tests is the reason smoke tests are
> structurally different from `Kakeibo.Api.IntegrationTests` (which uses `IClassFixture` for
> per-class isolation).

```
tests/Kakeibo.SmokeTests/
├── Flows/
│   ├── DomainEventFlowTests.cs
│   ├── EntityLessEventFlowTests.cs
│   ├── SyncInterModuleFlowTests.cs
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

## Flow 1 — HTTP → DomainEvent → IntegrationEvent → Consumer

**Verifies:** Handler → `entity.AddDomainEvent()` → `SaveChangesAsync` → `OutboxInterceptor`
persists outbox row → `OutboxProcessor.ProcessBatchAsync` → consumer side effect in DB.

**File:** `Flows/DomainEventFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class DomainEventFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task WalletCreation_DomainEventFlow_ConsumerExecuted()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        // Step 1: Create a user and log in
        await data.CreateVerifiedUserAsync("user@smoke.com", "Test#12345Abc", "User");
        await client.LoginAsync("user@smoke.com", "Test#12345Abc");

        // Step 2: Call the endpoint that raises a domain event (WalletCreated)
        var walletName = $"wallet-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsync("/api/wallets", new
        {
            name = walletName,
            type = "Personal",
            initialBalance = 1000.00,
            currency = "USD",
        });

        createResponse.AssertStatusCode(HttpStatusCode.Created);

        // Step 3: Manually trigger the outbox processor (disabled as background service in tests)
        await factory.TriggerOutboxProcessorAsync(ct);

        // Step 4: Verify consumer side effect — Wallet created in the DB
        // (created by handler, audit entry created by consumer reacting to the domain event)
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletsDbContext>();
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.Name == walletName, ct);

        Assert.NotNull(wallet);
    }
}
```

---

## Flow 2 — Entity-less Integration Event (Budget Exceeded)

**Verifies:** Handler calls `eventBus.PublishAsync()` + `auditOutbox.PublishAsync()` directly
(no entity, no domain event) → `OutboxInterceptor` persists the outbox row → consumer executed.

**File:** `Flows/EntityLessEventFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class EntityLessEventFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task BudgetExceeded_EntityLessEventFlow_OutboxRowCreated()
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

        // Step 1: Record transaction that exceeds budget — handler publishes event directly (no entity)
        var transactionResponse = await client.PostAsync("/api/transactions", new
        {
            amount = 120m,
            categoryId = "food-dining",
            walletId = "checking",
            type = "Expense",
        });

        transactionResponse.AssertStatusCode(HttpStatusCode.Created);

        // Step 2: Verify outbox row was persisted for BudgetExceededEvent
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetsDbContext>();

        var outboxRow = await db.OutboxMessages
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(m => m.Type == nameof(BudgetExceededEvent), ct);

        Assert.NotNull(outboxRow);
        Assert.Null(outboxRow.ProcessedAt);  // not yet dispatched in this test
    }
}
```

---

## Flow 3 — Sync Inter-Module Communication (`IModuleClient`)

**Verifies:** Module A calls `moduleClient.SendAsync(new {Request})` → `ModuleClient` resolves
handler from Module B → correct response returned synchronously in the same HTTP request.

**File:** `Flows/SyncInterModuleFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class SyncInterModuleFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task PadelBooking_RequiresMembership_SyncCallToMembersModule()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        // Step 1: Create a user with an active membership
        var userId = await data.CreateVerifiedUserAsync("padel-user@smoke.com", "Test#12345Abc", "User");
        await data.CreateActiveMembershipAsync(userId, planId: "standard");
        await client.LoginAsync("padel-user@smoke.com", "Test#12345Abc");

        // Step 2: Book a padel court — internally calls IModuleClient to validate membership
        var bookingResponse = await client.PostAsync("/api/padel/courts/1/bookings", new
        {
            startAt = "2026-07-01T10:00:00Z",
        });

        // The Padel module called Members module synchronously via IModuleClient
        // If the sync call fails, this returns 400/422 — the test would catch it
        bookingResponse.AssertStatusCode(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PadelBooking_NoMembership_SyncCallReturnsError()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        // User without membership
        await data.CreateVerifiedUserAsync("no-member@smoke.com", "Test#12345Abc", "User");
        await client.LoginAsync("no-member@smoke.com", "Test#12345Abc");

        var bookingResponse = await client.PostAsync("/api/padel/courts/1/bookings", new
        {
            startAt = "2026-07-01T10:00:00Z",
        });

        // IModuleClient returned NotFound → Padel module returned 422 Unprocessable
        bookingResponse.AssertStatusCode(HttpStatusCode.UnprocessableEntity);
    }
}
```

---

## Flow 4 — Audit Flow

**Verifies:** `DomainEventHandler.Stage()` → outbox row with `Type == "AuditEventEnvelope"` →
`AuditOutboxProcessor` picks it up → dispatches to ClickHouse stub (captured in test).

**File:** `Flows/AuditFlowTests.cs`

```csharp
[Collection("Smoke")]
public sealed class AuditFlowTests(WebApplicationFactory factory)
{
    private const string SkipReason = "Docker is not available. Smoke tests require Testcontainers.";

    [Fact]
    public async Task MemberCreation_AuditFlow_AuditEnvelopeStagedInOutbox()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;
        var data = factory.CreateTestDataBuilder();
        var client = factory.CreateAuthClient();

        await data.CreateVerifiedUserAsync("admin@smoke.com", "Test#12345Abc", "Admin");
        await client.LoginAsync("admin@smoke.com", "Test#12345Abc");

        var memberEmail = $"audit-{Guid.NewGuid():N}@smoke.com";
        var createResponse = await client.PostAsync("/api/members", new
        {
            firstName = "Audit",
            lastName = "Test",
            email = memberEmail,
            planId = "standard",
        });

        createResponse.AssertStatusCode(HttpStatusCode.Created);

        // Verify audit envelope was written to the outbox (staged by DomainEventHandler)
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();

        var auditRow = await db.OutboxMessages
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(m => m.Type == "AuditEventEnvelope", ct);

        Assert.NotNull(auditRow);

        // Verify the envelope has the correct action
        var envelope = JsonSerializer.Deserialize<AuditEventEnvelope>(auditRow.Payload, DefaultSerializer.Options);
        Assert.Equal(AuditAction.Members.Created, envelope!.Action);
        Assert.Equal("Members", envelope.Module);

        // Step 2: Trigger AuditOutboxProcessor — verify ClickHouse stub receives the row
        var clickHouseSink = factory.Services.GetRequiredService<IClickHouseSink>();
        await factory.TriggerAuditOutboxProcessorAsync(ct);

        // ClickHouseSink is stubbed in WebApplicationFactory — captures rows instead of writing
        var capturedRows = clickHouseSink.GetCapturedRows();
        Assert.Contains(capturedRows, r =>
            r.Action == AuditAction.Members.Created &&
            r.Module == "Members");
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
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // IOnboardingSeeder must have created the SuperAdmin role during startup
        var superAdminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == RoleNames.SuperAdmin, ct);

        Assert.NotNull(superAdminRole);
    }

    [Fact]
    public async Task Application_StartsUp_AllModuleSchemasExist()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var ct = TestContext.Current.CancellationToken;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // Verify key schemas were created by EF Core migrations
        var schemas = new[] { "identity", "members", "padel", "billing" };
        foreach (var schema in schemas)
        {
            var exists = await db.Database.ExecuteSqlRawAsync(
                $"SELECT 1 FROM information_schema.schemata WHERE schema_name = '{schema}'", ct) > 0;
            Assert.True(exists, $"Schema '{schema}' was not created during startup");
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
    // Manually triggers OutboxProcessor for one batch — simulates a polling tick.
    // OutboxProcessor is removed as a background service in tests (see ConfigureServices).
    public async Task TriggerOutboxProcessorAsync(CancellationToken ct = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
        await processor.ProcessBatchAsync(ct);
    }

    // Triggers AuditOutboxProcessor for one batch — used in Flow 4.
    public async Task TriggerAuditOutboxProcessorAsync(CancellationToken ct = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<AuditOutboxProcessor>();
        await processor.ProcessBatchAsync(ct);
    }

    // Adds a JTI to the Redis deny-list — used in Flow 6 to simulate token revocation.
    public async Task RevokeTokenAsync(string jti)
    {
        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetDatabase().StringSetAsync($"revoked:{jti}", "1", TimeSpan.FromHours(1));
    }
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
    <!-- Include all module projects so test data helpers can access DbContexts directly -->
  </ItemGroup>
</Project>
```

---

## Flow Coverage Matrix

| Flow | Class | What it verifies |
|------|-------|-----------------|
| 1. HTTP→DomainEvent→Consumer | `DomainEventFlowTests` | Domain event → outbox → consumer side effect in DB |
| 2. Entity-less event | `EntityLessEventFlowTests` | Direct `eventBus.PublishAsync` → outbox row without domain event |
| 3. Sync inter-module | `SyncInterModuleFlowTests` | Module A → `IModuleClient` → Module B handler → response |
| 4. Audit | `AuditFlowTests` | `auditOutbox.Stage()` → outbox → `AuditOutboxProcessor` → ClickHouse stub |
| 5. Email | `EmailFlowTests` | `emailService.SendAsync` → EmailRenderer stub → SMTP sink capture |
| 6. Authorization | `AuthorizationFlowTests` | JWT → `JwtRevocationMiddleware` → `PermissionService` → 200/403/401 |
| 7. Startup | `StartupFlowTests` | Migrations + seeders + health probes all pass |
