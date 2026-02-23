# API Testing Pyramid — 6 Levels

Full reference for all .NET API test levels. See `SKILL.md` for the quick decision table.

---

## Level 1 — Domain Unit

**Purpose:** Verify pure domain logic: entities, value objects, validators, middleware.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Entities/`

**Dependencies:** `FakeClock` only. No database. NSubstitute for middleware tests (mocked `HttpContext`).

**Script:** `bun run api:test:unit`

**What to verify:**
- Entity invariants (a negative price value is invalid)
- Domain events added to the entity (`entity.DomainEvents`)
- Structural equality of value objects
- Domain calculations (discounts, session durations)
- Valid and invalid state transitions
- Middleware routing decisions (mocked pipeline)

```csharp
public sealed class BudgetTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 7, 15, 12, 0));

    [Fact]
    public void CalculateRemainingBudget_HalfSpent_ReturnsCorrectAmount()
    {
        var budget = new Budget
        {
            Limit = 400m,
            CategoryId = Guid7.NewGuid(),
            Period = BudgetPeriod.Month,
            IsActive = true,
            CreatedAt = _clock.GetCurrentInstant(),
        };

        var result = budget.CalculateRemaining(200m, _clock.GetCurrentInstant());

        Assert.True(result.IsSuccess);
        Assert.Equal(200m, result.Value);
    }

    [Fact]
    public void CalculateRemainingBudget_LimitExceeded_ReturnsNegative()
    {
        var budget = new Budget
        {
            Limit = 100m,
            CategoryId = Guid7.NewGuid(),
            Period = BudgetPeriod.Month,
            IsActive = true,
            CreatedAt = _clock.GetCurrentInstant(),
        };

        var result = budget.CalculateRemaining(150m, _clock.GetCurrentInstant());

        Assert.True(result.IsSuccess);
        Assert.Equal(-50m, result.Value);
    }

    [Fact]
    public void AddDomainEvent_WhenWalletCreated_EventIsQueued()
    {
        var wallet = new Wallet { Name = "Checking Account", Type = WalletType.Personal };
        wallet.Create("Checking Account", WalletType.Personal, 1000.00m, "USD");

        Assert.Single(wallet.DomainEvents);
        Assert.IsType<WalletCreatedDomainEvent>(wallet.DomainEvents[0]);
    }
}
```

### ValueObject Tests

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Entities/` (or `ValueObjects/` subfolder)

Cover the four equality contracts: `==`, `Equals`, `GetHashCode`, and component change → not equal.

```csharp
public sealed class AddressTests
{
    [Fact]
    public void Address_SameComponents_AreEqual()
    {
        var a = new Address("Calle Mayor", "1", "Cádiz", "11500");
        var b = new Address("Calle Mayor", "1", "Cádiz", "11500");

        // All three equality mechanisms must agree
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Address_DifferentPostalCode_AreNotEqual()
    {
        var a = new Address("Calle Mayor", "1", "Cádiz", "11500");
        var b = new Address("Calle Mayor", "1", "Cádiz", "11600");

        Assert.NotEqual(a, b);
        Assert.False(a == b);
    }

    [Fact]
    public void Address_EmptyVsNullComponent_AreNotEqual()
    {
        // Null and empty string are semantically different — verify they don't collapse
        var a = new Address("Calle Mayor", null, "Cádiz", "11500");
        var b = new Address("Calle Mayor", "",   "Cádiz", "11500");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Address_GetHashCode_IsConsistentAcrossCalls()
    {
        var a = new Address("Calle Mayor", "1", "Cádiz", "11500");

        // Same object, called twice — must return the same value
        Assert.Equal(a.GetHashCode(), a.GetHashCode());
    }
}
```

**Rule:** Every `ValueObject` subclass must have tests for at least: equal instances, one-component-different instance, null vs empty (if applicable), and hash code consistency.

### Middleware Unit Tests

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Middleware/` (or `tests/Kakeibo.Infrastructure.Tests/Middleware/`)

Test middleware in isolation with a mocked `HttpContext` and `RequestDelegate`. No database, no real HTTP server.

#### ErrorHandlingMiddleware

```csharp
public sealed class ErrorHandlingMiddlewareTests
{
    [Theory]
    [InlineData(typeof(ArgumentNullException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized)]
    [InlineData(typeof(Exception), StatusCodes.Status500InternalServerError)]
    public async Task InvokeAsync_ThrowsException_ReturnsExpectedStatusCode(
        Type exceptionType, int expectedStatus)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // next() throws the given exception type
        var next = new RequestDelegate(_ =>
            throw (Exception)Activator.CreateInstance(exceptionType, "test")!);

        var middleware = new ErrorHandlingMiddleware(next);
        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsException_ResponseBodyIsValidJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var next = new RequestDelegate(_ => throw new Exception("Something went wrong"));
        var middleware = new ErrorHandlingMiddleware(next);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        // Must be valid JSON — not an HTML error page
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _),
            "Error response must include a traceId for diagnostics");
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var next = new RequestDelegate(_ => { nextCalled = true; return Task.CompletedTask; });

        var middleware = new ErrorHandlingMiddleware(next);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
```

#### AuditContextMiddleware

```csharp
public sealed class AuditContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithForwardedIpAndUserAgent_PopulatesAccessor()
    {
        var accessor = new AuditContextAccessor();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.1";
        context.Request.Headers["User-Agent"] = "TestAgent/1.0";

        // Authenticated user with sub claim
        var actorId = Guid.NewGuid();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, actorId.ToString())],
            "Bearer"));

        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new AuditContextMiddleware(next, accessor);

        await middleware.InvokeAsync(context);

        Assert.Equal("203.0.113.1", accessor.IpAddress);
        Assert.Equal("TestAgent/1.0", accessor.UserAgent);
        Assert.Equal(actorId, accessor.ActorId);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousRequest_ActorIdIsNull()
    {
        var accessor = new AuditContextAccessor();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(); // no claims

        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new AuditContextMiddleware(next, accessor);

        await middleware.InvokeAsync(context);

        Assert.Null(accessor.ActorId);
    }
}
```

#### JwtRevocationMiddleware

```csharp
public sealed class JwtRevocationMiddlewareTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();

    public JwtRevocationMiddlewareTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
    }

    [Fact]
    public async Task InvokeAsync_RevokedJti_Returns401WithoutCallingNext()
    {
        var jti = Guid.NewGuid().ToString();
        _db.KeyExistsAsync($"revoked:{jti}", Arg.Any<CommandFlags>()).Returns(true);

        var context = BuildAuthenticatedContext(jti);
        var nextCalled = false;
        var next = new RequestDelegate(_ => { nextCalled = true; return Task.CompletedTask; });

        var middleware = new JwtRevocationMiddleware(next, _redis);
        await middleware.InvokeAsync(context);

        Assert.False(nextCalled, "Pipeline must stop — revoked token must not reach the handler");
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidJti_CallsNext()
    {
        var jti = Guid.NewGuid().ToString();
        _db.KeyExistsAsync($"revoked:{jti}", Arg.Any<CommandFlags>()).Returns(false);

        var context = BuildAuthenticatedContext(jti);
        var nextCalled = false;
        var next = new RequestDelegate(_ => { nextCalled = true; return Task.CompletedTask; });

        var middleware = new JwtRevocationMiddleware(next, _redis);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousRequest_SkipsRevocationCheck()
    {
        // No JTI claim → no Redis lookup
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal();

        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new JwtRevocationMiddleware(next, _redis);

        await middleware.InvokeAsync(context);

        await _db.DidNotReceive().KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    private static DefaultHttpContext BuildAuthenticatedContext(string jti)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("jti", jti)],
            "Bearer"));
        return context;
    }
}
```

**Validator tests (no DB, no mocks):**

```csharp
[Fact]
public void Validate_EmptyWalletName_HasValidationError()
{
    var validator = new CreateWalletValidator();
    var result = validator.Validate(new CreateWalletEndpoint.CreateWalletRequest(
        "", WalletType.Personal, 0m, "USD"));

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "Name");
}

[Fact]
public void Validate_ValidRequest_HasNoErrors()
{
    var validator = new CreateWalletValidator();
    var result = validator.Validate(new CreateWalletEndpoint.CreateWalletRequest(
        "Checking Account", WalletType.Personal, 1000.00m, "USD"));

    Assert.True(result.IsValid);
}
```

**Edge cases to cover at Level 1:** exact limits (CurrentUses == MaxTotalUses vs <),
temporal validity (expired exactly 1 second ago), entities with `IsDeleted = true`.

**Using `[Theory]` for multiple invalid inputs:**

```csharp
// Use Theory when multiple inputs test the same rule (avoids test duplication)
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void Validate_InvalidWalletName_HasValidationError(string? name)
{
    var validator = new CreateWalletValidator();
    var result = validator.Validate(new CreateWalletEndpoint.CreateWalletRequest(
        name!, WalletType.Personal, 0m, "USD"));

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "Name");
}

// Use MemberData when test data is complex or needs to be reused
public static TheoryData<string, string> InvalidWalletNameData => new()
{
    { "", "Name is required" },
    { new string('a', 101), "Name max 100 chars" },
};

[Theory]
[MemberData(nameof(InvalidNameData))]
public void Validate_InvalidFirstName_HasExpectedError(string firstName, string expectedMessage)
{
    var validator = new CreateWalletValidator();
    var result = validator.Validate(new CreateWalletEndpoint.CreateWalletRequest(
        firstName, "García", "valid@test.com", null, "standard"));

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "FirstName"
        && e.ErrorMessage.Contains(expectedMessage));
}
```

**Rule:** Prefer `[Theory]` when the same assertion applies to multiple data variants. Use two
separate `[Fact]` tests when the scenarios are meaningfully different (different error codes,
different expected behaviors).

---

## Level 2 — Feature Handler Unit

**Purpose:** Verify handler business logic with a real PostgreSQL database.
Covers feature handlers only. Consumers and request handlers have their own levels (2b and 2c).

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Features/{Operation}/`

**Dependencies:** `TestDbContextFactory` (real PostgreSQL via Testcontainers), `FakeClock`.
NSubstitute mocks for `IModuleEventBus`, `IAuditOutbox`, `INotificationService`.

**Script:** `bun run api:test:unit`

**Critical rule:** Never mock `DbContext` or `DbSet<T>`. They are internal module details.
Use `TestDbContextFactory.CreateAsync()` for a real isolated DB per test.

```csharp
public sealed class CreateWalletHandlerTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 2, 17, 12, 0));

    [Fact]
    public async Task HandleAsync_WithValidRequest_CreatesWalletAndReturnsResponse()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IModuleEventBus>();
        var handler = new CreateWalletHandler(db, eventBus, _clock);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Checking Account",
            Type: WalletType.Personal,
            InitialBalance: 1000.00m,
            Currency: "USD");

        // Act
        var result = await handler.HandleAsync(request, ct);

        // Assert result
        Assert.True(result.IsSuccess);
        Assert.Equal("Checking Account", result.Value.Name);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        // Assert persistence
        var inDb = await db.Wallets.FindAsync([result.Value.Id], ct);
        Assert.NotNull(inDb);
        Assert.Equal(1000.00m, inDb.Balance);
        Assert.Equal(WalletType.Personal, inDb.Type);
    }

    [Fact]
    public async Task HandleAsync_DuplicateWalletName_ReturnsConflictError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        db.Wallets.Add(new Wallet
        {
            Name = "Checking Account",
            Type = WalletType.Personal,
            Balance = 500m,
            CreatedAt = _clock.GetCurrentInstant(),
        });
        await db.SaveChangesAsync(ct);

        var handler = new CreateWalletHandler(db, Substitute.For<IModuleEventBus>(), _clock);
        var request = new CreateWalletEndpoint.CreateWalletRequest(
            "Checking Account", WalletType.Personal, 100m, "USD");

        var result = await handler.HandleAsync(request, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Wallet.DuplicateName", result.Error.Code);
    }
}
```

### Assert.Multiple — Verifying Result + Persistence Together

When a test must verify both the returned result AND the persisted state, use `Assert.Multiple`
to run all assertions and collect all failures rather than stopping at the first one:

```csharp
[Fact]
public async Task HandleAsync_WithValidRequest_CreatesWalletAndReturnsResponse()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;
    var handler = new CreateWalletHandler(db, Substitute.For<IModuleEventBus>(), _clock);

    var result = await handler.HandleAsync(
        new CreateWalletEndpoint.CreateWalletRequest("Checking Account", WalletType.Personal, 1000.00m, "USD"), ct);

    var inDb = result.IsSuccess
        ? await db.Wallets.FindAsync([result.Value.Id], ct)
        : null;

    // All assertions run — failures accumulate instead of short-circuiting
    Assert.Multiple(
        () => Assert.True(result.IsSuccess),
        () => Assert.Equal("Checking Account", result.Value.Name),
        () => Assert.NotEqual(Guid.Empty, result.Value.Id),
        () => Assert.NotNull(inDb),
        () => Assert.Equal(1000.00m, inDb!.Balance),
        () => Assert.Equal(WalletType.Personal, inDb!.Type)
    );
}
```

**Rule:** Use `Assert.Multiple` when you need to verify both the handler result and the
persisted DB state in a single test. Collecting all failures makes debugging faster than
fixing one assertion at a time.

### ITestOutputHelper — Diagnostic Output on Failure

Inject `ITestOutputHelper` to print diagnostic information when a test fails:

```csharp
public sealed class CreateWalletHandlerTests(ITestOutputHelper output)
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_CreatesWallet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var handler = new CreateWalletHandler(db, Substitute.For<IModuleEventBus>(), _clock);

        var result = await handler.HandleAsync(
            new CreateWalletEndpoint.CreateWalletRequest("Checking Account", WalletType.Personal, 1000.00m, "USD"), ct);

        // Only visible in output when the test fails — great for debugging DB state
        output.WriteLine($"Result: {result.IsSuccess}, Error: {result.Error?.Code}");
        output.WriteLine($"Members in DB: {await db.Wallets.CountAsync(ct)}");

        Assert.True(result.IsSuccess);
    }
}
```

**Rule:** Add `ITestOutputHelper` output for assertions that fail intermittently or when
the failure reason is not obvious from the assert message alone. Output is suppressed for passing tests.

### When to verify `eventBus.PublishAsync` at Level 2

There are two distinct flows, and the rule is different for each:

**Flow A — Handler publishes directly (entity-less events):** The handler calls `eventBus.PublishAsync()`
in its body (e.g., a failed login attempt where there is no entity to attach the event to). In this
case, verify the call at Level 2 — it is direct output of the handler under test.

```csharp
// ✅ Verify at Level 2: handler calls eventBus directly
await eventBus.Received(1).PublishAsync(
    Arg.Is<LoginFailedEvent>(e => e.Email == request.Email),
    Arg.Any<CancellationToken>());
```

**Flow B — Handler uses `entity.AddDomainEvent()`:** The event bus is called later by
`OutboxInterceptor` → `DomainEventHandler`. **Do NOT verify `eventBus` at Level 2.**
Instead, verify that the domain event was queued on the entity before `SaveChangesAsync`:

```csharp
// ✅ Verify at Level 2: domain event was added to the entity
var inDb = await db.Wallets.FindAsync([result.Value.Id], ct);
Assert.Single(inDb!.DomainEvents);
Assert.IsType<MemberCreatedDomainEvent>(inDb.DomainEvents[0]);

// ✅ Verify eventBus at Level 3 (DomainEventHandler tests) — not here
```

The full verification of what the domain event handler publishes belongs in Level 3
(`DomainEventHandlerTests`), not in the feature handler test.

### Result & Error Handling

```csharp
// Verify all error paths return Result.Failure with the correct Error.Code
// Never throws for domain errors

Assert.True(result.IsSuccess);
Assert.Equal(expectedValue, result.Value);

Assert.True(result.IsFailure);
Assert.Equal("Member.NotFound", result.Error.Code);
```

---

## Level 2b — Request Handler Unit (`IModuleRequestHandler`)

**Purpose:** Verify that `IModuleRequestHandler<TRequest, TResponse>` implementations return the
correct data from the database. These handlers are the synchronous inter-module API surface.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/RequestHandlers/`

**File name:** `{Request}RequestHandlerTests.cs`

**Dependencies:** `TestDbContextFactory` (real PostgreSQL). No `IModuleEventBus` — request handlers
do not publish events.

**Script:** `bun run api:test:unit`

```csharp
public sealed class GetWalletByUserIdRequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingActiveUser_ReturnsMemberSummary()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid.NewGuid();
        db.Wallets.Add(new Member
        {
            UserId = userId,
            FirstName = "Ana",
            LastName = "García",
            Email = "ana@test.com",
            PlanId = "standard",
            Status = MemberStatusCodes.Active,
            CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
        });
        await db.SaveChangesAsync(ct);

        var handler = new GetWalletByUserIdRequestHandler(db);
        var result = await handler.HandleAsync(new GetWalletByUserIdRequest(userId), ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("Ana García", result.Value.FullName);
        Assert.Equal("standard", result.Value.PlanId);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task HandleAsync_NonExistentUser_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var handler = new GetWalletByUserIdRequestHandler(db);
        var result = await handler.HandleAsync(new GetWalletByUserIdRequest(Guid.NewGuid()), ct);

        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_DeletedMember_ReturnsNotFoundError()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid.NewGuid();
        db.Wallets.Add(new Member
        {
            UserId = userId,
            Email = "deleted@test.com",
            Status = MemberStatusCodes.Active,
            IsDeleted = true,
            DeletedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            CreatedAt = Instant.FromUtc(2025, 1, 1, 0, 0),
        });
        await db.SaveChangesAsync(ct);

        var handler = new GetWalletByUserIdRequestHandler(db);
        var result = await handler.HandleAsync(new GetWalletByUserIdRequest(userId), ct);

        // Soft-deleted members must not be returned via cross-module requests
        Assert.True(result.IsFailure);
        Assert.Equal("Member.NotFound", result.Error.Code);
    }
}
```

**What to cover per request handler:**
- Happy path: entity exists → correct DTO mapping (all fields)
- Not found: non-existent ID → `Error.NotFound`
- Soft-deleted entity: treated as not found (query filter active)
- If handler applies additional authorization/filtering: verify each condition

---

## Level 2c — Consumer Unit (`IEventConsumer<T>`)

**Purpose:** Verify that `IEventConsumer<T>` implementations correctly react to integration events:
persisting side effects, handling already-processed events idempotently, and delegating to
external services.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Consumers/`

**File name:** `{EventName}ConsumerTests.cs`

**Dependencies:** `TestDbContextFactory` (real PostgreSQL). NSubstitute for `INotificationService`
or other external services the consumer may call.

**Script:** `bun run api:test:unit`

**Critical rule — Idempotency:** Every consumer that creates or modifies state MUST have a test
that calls `ConsumeAsync` with the same event twice and verifies no duplicate data is created.
The Outbox guarantees at-least-once delivery — duplicate processing will happen.

```csharp
public sealed class UserRegisteredConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_NewUser_CreatesMemberProfile()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var consumer = new UserRegisteredConsumer(db);

        var @event = new UserRegisteredEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            UserId = Guid.NewGuid(),
            Email = "new@test.com",
            Version = 1,
        };

        await consumer.ConsumeAsync(@event, ct);

        var profile = await db.MemberProfiles
            .FirstOrDefaultAsync(p => p.UserId == @event.UserId, ct);
        Assert.NotNull(profile);
        Assert.Equal(@event.Email, profile.Email);
    }

    [Fact]
    public async Task ConsumeAsync_SameEventTwice_IsIdempotent()
    {
        // KB-005: Idempotency is a behavioral contract — encode it as a test, not just a comment.
        // At-least-once delivery from OutboxProcessor makes duplicate consumption a certainty.
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var consumer = new UserRegisteredConsumer(db);

        var @event = new UserRegisteredEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            UserId = Guid.NewGuid(),
            Email = "idempotent@test.com",
            Version = 1,
        };

        await consumer.ConsumeAsync(@event, ct);
        await consumer.ConsumeAsync(@event, ct);  // second time — must not create a duplicate

        var count = await db.MemberProfiles
            .CountAsync(p => p.UserId == @event.UserId, ct);
        Assert.Equal(1, count);  // exactly one — not zero, not two
    }

    [Fact]
    public async Task ConsumeAsync_ExternalServiceFails_DoesNotThrow()
    {
        // Consumers must handle external service failures gracefully.
        // A thrown exception causes the outbox processor to retry — which may loop forever.
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var notifications = Substitute.For<INotificationService>();
        notifications
            .SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Failure("Service unavailable"));

        var consumer = new UserRegisteredConsumer(db, notifications);

        var @event = new UserRegisteredEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            UserId = Guid.NewGuid(),
            Email = "fail@test.com",
            Version = 1,
        };

        // Must not throw — failure is handled internally
        await consumer.ConsumeAsync(@event, ct);

        // Primary side effect (DB write) must still happen even when notification fails
        var profile = await db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == @event.UserId, ct);
        Assert.NotNull(profile);
    }
}
```

**What to cover per consumer:**
- Happy path: event arrives → correct DB state created/updated
- Idempotency: same event twice → same final state (no duplicates)
- External service failure: notification/email fails → consumer does not throw, primary state is persisted
- Version mismatch (if consumer checks `event.Version`): unsupported version → logged and skipped, no throw

---

## Level 3 — Domain Event Handler

**Purpose:** Verify that `IDomainEventHandler<T>` publishes the correct integration events,
stages audit entries, and handles external service failures without propagating exceptions.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/DomainEventHandlers/`

**Dependencies:** NSubstitute for `IModuleEventBus`, `IAuditOutbox`, `INotificationService`.
No database — domain event handlers MUST NOT inject `DbContext`.

```csharp
public sealed class MemberCreatedDomainEventHandlerTests
{
    private readonly IModuleEventBus _eventBus = Substitute.For<IModuleEventBus>();
    private readonly IAuditOutbox _auditOutbox = Substitute.For<IAuditOutbox>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 2, 17, 12, 0));

    [Fact]
    public async Task HandleAsync_PublishesIntegrationEventWithCorrectPayload()
    {
        var domainEvent = BuildEvent();
        _notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());

        var handler = new MemberCreatedDomainEventHandler(_eventBus, _auditOutbox, _notifications);

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<MemberCreatedEvent>(e =>
                e.MemberId == domainEvent.MemberId &&
                e.Email == domainEvent.Email),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StagesAuditEntry()
    {
        var domainEvent = BuildEvent();
        _notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());

        var handler = new MemberCreatedDomainEventHandler(_eventBus, _auditOutbox, _notifications);

        await handler.HandleAsync(domainEvent, CancellationToken.None);

        _auditOutbox.Received(1).Stage(Arg.Is<AuditEventEnvelope>(e =>
            e.Action == AuditAction.Members.Created &&
            e.EntityId == domainEvent.MemberId.ToString()));
    }

    [Fact]
    public async Task HandleAsync_WhenNotificationFails_DoesNotThrow()
    {
        var domainEvent = BuildEvent();
        _notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Failure("SMTP unavailable"));

        var handler = new MemberCreatedDomainEventHandler(_eventBus, _auditOutbox, _notifications);

        // Must not throw — notification failure is non-critical
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        // Integration event and audit MUST still execute
        await _eventBus.Received(1).PublishAsync(Arg.Any<MemberCreatedEvent>(), Arg.Any<CancellationToken>());
        _auditOutbox.Received(1).Stage(Arg.Any<AuditEventEnvelope>());
    }

    private MemberCreatedDomainEvent BuildEvent() => new(
        Id: Guid.NewGuid(),
        OccurredAt: _clock.GetCurrentInstant(),
        MemberId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Email: "test@test.com",
        MemberNumber: "CW000001");
}
```

---

## Level 4 — Background Job

**Purpose:** Verify Hangfire job logic by seeding the DB, running the job, and asserting state changes.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/BackgroundJobs/`

**Dependencies:** `TestDbContextFactory` (real PostgreSQL), `FakeClock`,
`NullLogger<T>.Instance`, NSubstitute for external services.

```csharp
public sealed class CheckExpiringSubscriptionsJobTests
{
    private static readonly FakeClock Clock = new(Instant.FromUtc(2024, 1, 15, 0, 0));

    [Fact]
    public async Task RunAsync_WithExpiringSubscriptions_SendsRenewalReminders()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var notifications = Substitute.For<INotificationService>();
        notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());

        var now = Clock.GetCurrentInstant();
        db.Wallets.AddRange(CreateWallet("alice@test.com"), CreateWallet("bob@test.com"));
        var plan = CreatePlan();
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        db.Subscriptions.AddRange(
            CreateSubscription(db.Wallets.Local[0].Id, plan.Id, now.Plus(Duration.FromDays(5))),
            CreateSubscription(db.Wallets.Local[1].Id, plan.Id, now.Plus(Duration.FromDays(3))));
        await db.SaveChangesAsync(ct);

        var job = new CheckExpiringSubscriptionsJob(db, notifications, Clock,
            NullLogger<CheckExpiringSubscriptionsJob>.Instance);

        await job.RunAsync(ct);

        await notifications.Received(2)
            .SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_NoExpiringSubscriptions_SendsNoNotifications()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var notifications = Substitute.For<INotificationService>();
        var job = new CheckExpiringSubscriptionsJob(db, notifications, Clock,
            NullLogger<CheckExpiringSubscriptionsJob>.Instance);

        await job.RunAsync(TestContext.Current.CancellationToken);

        await notifications.DidNotReceive()
            .SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    // Static factory helpers for seed data
    private static Member CreateWallet(string email) => new()
    {
        UserId = Guid.NewGuid(),
        Email = email,
        Status = MemberStatusCodes.Active,
        CreatedAt = Instant.FromUtc(2024, 1, 1, 0, 0),
    };

    private static SubscriptionPlan CreatePlan() => new()
    {
        Name = "Test Plan",
        Code = "TEST001",
        MonthlyPrice = 50m,
        IsActive = true,
        CreatedAt = Instant.FromUtc(2024, 1, 1, 0, 0),
    };

    private static Subscription CreateSubscription(Guid memberId, Guid planId, Instant endsAt) => new()
    {
        MemberId = memberId,
        PlanId = planId,
        Status = SubscriptionStatusCodes.Active,
        StartedAt = Instant.FromUtc(2024, 1, 1, 0, 0),
        EndsAt = endsAt,
        CreatedAt = Instant.FromUtc(2024, 1, 1, 0, 0),
    };
}
```

---

## Level 5 — API Integration (HTTP)

**Purpose:** Verify the full HTTP pipeline: routing, authentication, validation,
handler, and persistence via a real `HttpClient` against `WebApplicationFactory<Program>`.

**Location:** `tests/Kakeibo.Api.IntegrationTests/`

**Script:** `bun run api:test:integration`

**Class fixture pattern** (one factory per test class, each with its own isolated database):

```csharp
// Each test class gets its own WebApplicationFactory instance → its own database.
// Tests within the same class share one database — always use unique test data
// (e.g., email = $"member-{Guid.NewGuid():N}@test.com") to avoid inter-test interference.
public sealed class MemberRegistrationTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory>
{
    private const string SkipReason =
        "Docker is not available. Integration tests require Docker to run Testcontainers.";

    [Fact]
    public async Task CreateWallet_ValidData_Returns201WithLocation()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        using var client = factory.CreateAuthClient();
        var data = factory.CreateTestDataBuilder();
        await data.CreateVerifiedUserAsync("admin@test.com", "Test#12345Abc", "Admin");

        await client.LoginAsync("admin@test.com", "Test#12345Abc");

        var response = await client.PostAsync("/api/members", new
        {
            firstName = "Ana",
            lastName = "García",
            email = $"member-{Guid.NewGuid():N}@test.com",
            planId = "standard"
        });

        response.AssertStatusCode(HttpStatusCode.Created);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateWallet_WithoutPermission_Returns403()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        var data = factory.CreateTestDataBuilder();
        await data.CreateVerifiedUserAsync("member@test.com", "Test#12345Abc", "User");

        using var client = factory.CreateAuthClient();
        await client.LoginAsync("member@test.com", "Test#12345Abc");

        var response = await client.PostAsync("/api/members",
            new { firstName = "A", lastName = "B", email = "new@test.com", planId = "standard" });

        response.AssertStatusCode(HttpStatusCode.Forbidden);
    }
}
```

### Snapshot Testing at Level 5

When an integration response has many fields (> 5 nested properties), use `Verify` instead
of individual `Assert.Equal` calls:

```csharp
// Instead of 8 individual Assert.Equal calls:
await Verify(response);  // generates a .verified.json snapshot file

// Parameterized snapshot per plan type:
await Verify(response).UseParameters(planCode);
```

See [snapshot-testing.md](snapshot-testing.md) for full setup, scrubbing NodaTime/GUIDs,
email template snapshots, and the acceptance workflow.

**Note:** Outbox pattern testing (OutboxInterceptor atomicity, OutboxProcessor polling, retry)
is covered in [infrastructure-tests.md](infrastructure-tests.md). Level 5 focuses on the HTTP
pipeline, not the persistence infrastructure.

---

## Level 6 — Architecture

**Purpose:** Enforce naming conventions and module boundary rules at the type level.

**Location:** `tests/Kakeibo.ArchitectureTests/`

**Script:** `bun run api:test:arch`

```csharp
public sealed class NamingConventionTests
{
    private static readonly Assembly[] SourceAssemblies =
    [
        typeof(IEndpoint).Assembly,
        typeof(MembersModuleRegistration).Assembly,
        // ... all module assemblies
    ];

    [Fact]
    public void EndpointImplementations_ShouldEndWithEndpoint()
    {
        var result = Types.InAssemblies(SourceAssemblies)
            .That().ImplementInterface(typeof(IEndpoint)).And().AreNotAbstract()
            .Should().HaveNameEndingWith("Endpoint")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"IEndpoint implementations must end with 'Endpoint'. Offending: {Format(result)}");
    }

    [Fact]
    public void Consumers_ShouldEndWithConsumer()
    {
        var consumerInterface = typeof(IEventConsumer<>);
        var offending = SourceAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface
                && t.GetInterfaces().Any(i => i.IsGenericType
                    && i.GetGenericTypeDefinition() == consumerInterface)
                && !t.Name.EndsWith("Consumer"))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void EndpointNestedTypes_ShouldHaveOperationPrefix()
    {
        // Nested records named just "Request" or "Response" violate TD-013
        var endpointInterface = typeof(IEndpoint);
        var offending = SourceAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && endpointInterface.IsAssignableFrom(t))
            .SelectMany(t => t.GetNestedTypes())
            .Where(n => n.Name is "Request" or "Response")
            .Select(n => $"{n.DeclaringType?.Name}.{n.Name}")
            .ToList();

        Assert.Empty(offending);
    }
}

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Modules_ShouldNotReferenceOtherModules()
    {
        var moduleNamespaces = new[] { "Kakeibo.Modules.Identity", "Kakeibo.Modules.Members" };

        foreach (var moduleAssembly in ModuleAssemblies)
        {
            foreach (var otherModule in moduleNamespaces.Where(n => n != moduleAssembly.GetName().Name))
            {
                var result = Types.InAssembly(moduleAssembly)
                    .ShouldNot().HaveDependencyOn(otherModule)
                    .GetResult();

                Assert.True(result.IsSuccessful,
                    $"{moduleAssembly.GetName().Name} must not reference {otherModule}");
            }
        }
    }

    [Fact]
    public void Common_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(CommonAssembly)
            .ShouldNot().HaveDependencyOn("Kakeibo.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Kakeibo.Common must not reference Kakeibo.Infrastructure");
    }
}
```

---

## Factory Helpers Pattern

When the same entity creation code appears in 3+ tests in the same class, extract static factory helpers:

```csharp
private static Member CreateActiveMember(string email = "test@test.com") => new()
{
    UserId = Guid.NewGuid(),
    Email = email,
    Status = MemberStatusCodes.Active,
    CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
};
```

Use static private methods, never mutable shared state.
