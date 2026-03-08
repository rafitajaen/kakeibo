# Testing Handlers

Handlers are the most important component to test in the Kakeibo API. They contain all business logic, interact with the database, publish events, and return a typed `Result<T>`. Every handler should have tests that cover the happy path and the main failure paths.

---

## What Is a Handler?

A handler is a plain C# class whose name ends with `Handler`. It has a single public method called `HandleAsync` that receives a request record and a user ID, performs business logic against the database, optionally publishes events, and returns a `Result<T>`.

Handlers are auto-registered by Scrutor (assembly scanning). They do not inherit from any base class and do not implement any interface. This makes them trivially easy to instantiate in tests — you just call `new`.

**Location:** `src/Kakeibo.Api/Features/{Domain}/{Operation}/{Op}Handler.cs`

**Example signature:**

```csharp
public sealed class CreateWalletHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<CreateWalletEndpoint.CreateWalletResponse>> HandleAsync(
        CreateWalletEndpoint.CreateWalletRequest request,
        Guid userId,
        CancellationToken ct)
    { ... }
}
```

---

## Two Testing Strategies

### Strategy 1 — Integration Test (Preferred)

The handler runs against a **real PostgreSQL database** provisioned by Testcontainers. The `AppDbContext` is real. Queries, constraints, global query filters, and cascade behaviors all work as they would in production.

**Use this when:**
- The handler reads from or writes to the database (almost always).
- You need to verify that the correct rows were created, updated, or soft-deleted.
- You need to verify that global query filters (e.g., soft delete) behave correctly.

**Requires:** Docker running on the test machine.

### Strategy 2 — Unit Test (Mocked)

The handler is instantiated with a mocked `AppDbContext` via NSubstitute. No database is involved.

**Use this when:**
- The handler contains pure logic that does not depend on database query results.
- You want a fast sanity check that does not need Docker.

This strategy is rare in practice because most handlers make at least one database call. Prefer the integration approach unless the logic is genuinely data-independent.

---

## Setting Up an Integration Test

### Step 1 — Obtain a real database context

```csharp
await using var db = await TestDbContextFactory.CreateAsync();
```

This creates a fresh, isolated PostgreSQL database for this test. The schema is applied automatically. The database is destroyed when `db` is disposed.

### Step 2 — Seed required data

Insert any entities the handler needs to find in the database before calling it.

```csharp
var user = new User
{
    Id = Guid7.NewGuid().ToGuid(),
    Email = "alice@example.com",
    Username = "alice",
    PasswordHash = "hash",
    Currency = "EUR"
};
db.Users.Add(user);
await db.SaveChangesAsync(ct);
```

### Step 3 — Create mocks for side-effect dependencies

`IEventBus`, `IEmailService`, `IWebPushService`, and similar services must be mocked so no real emails are sent or events are dispatched to a real bus.

```csharp
var eventBus = Substitute.For<IEventBus>();
```

### Step 4 — Instantiate the handler directly

Use `new` with all required dependencies. No DI container needed.

```csharp
var handler = new CreateWalletHandler(db, eventBus, SystemClock.Instance);
```

### Step 5 — Call HandleAsync

```csharp
var ct = TestContext.Current.CancellationToken;
var request = new CreateWalletEndpoint.CreateWalletRequest("Checking Account", "Personal", 0m);
var result = await handler.HandleAsync(request, user.Id, ct);
```

### Step 6 — Assert the result and side effects

```csharp
Assert.Multiple(
    () => Assert.True(result.IsSuccess),
    () => Assert.Equal("Checking Account", result.Value.Name)
);

eventBus.Received(1).Publish(Arg.Is<WalletCreatedEvent>(e =>
    e.WalletId == result.Value.Id && e.UserId == user.Id));
```

---

## Asserting `Result<T>`

`Result<T>` is the return type of every handler. It represents either a success with a value or a failure with an error. The `[MemberNotNullWhen]` attribute makes both branches type-safe.

| Assertion | Meaning |
|-----------|---------|
| `Assert.True(result.IsSuccess)` | The handler succeeded |
| `Assert.False(result.IsSuccess)` | The handler failed |
| `result.Value` | The response record (only safe when `IsSuccess` is true) |
| `result.Error` | The error record (only safe when `IsSuccess` is false) |
| `Assert.Equal("not_found", result.Error.Code)` | The error code matches |
| `Assert.Equal("conflict", result.Error.Code)` | Conflict (e.g., duplicate entry) |
| `Assert.Equal("forbidden", result.Error.Code)` | User is authenticated but not authorized |

**Error codes used in the codebase:**

| Code | Meaning |
|------|---------|
| `not_found` | Entity does not exist or is soft-deleted |
| `validation` | Input failed a business rule inside the handler |
| `conflict` | Attempted to create a duplicate |
| `unauthorized` | User identity could not be established |
| `forbidden` | User is authenticated but does not have access |
| `internal` | Unexpected server error |

---

## Asserting Event Publication

Handlers publish events via `IEventBus.Publish()` before calling `SaveChangesAsync`. Use NSubstitute's `Received()` to verify the correct event was published.

**Verify that exactly one event was published:**
```csharp
eventBus.Received(1).Publish(Arg.Any<WalletCreatedEvent>());
```

**Verify the event payload:**
```csharp
eventBus.Received(1).Publish(Arg.Is<WalletCreatedEvent>(e =>
    e.WalletId == result.Value.Id &&
    e.UserId == user.Id));
```

**Verify that NO event was published (failure path):**
```csharp
eventBus.DidNotReceive().Publish(Arg.Any<WalletCreatedEvent>());
```

---

## Controlling Time with FakeClock

Handlers that use `IClock` for timestamps (e.g., to set `CreatedAt` or check expiry) can be given a fake clock so tests are deterministic.

```csharp
var fakeTime = Instant.FromUtc(2024, 6, 15, 12, 0, 0);
var clock = new FakeClock(fakeTime);
var handler = new SomeHandler(db, eventBus, clock);
```

`FakeClock` is from the `NodaTime.Testing` package, already available via `GlobalUsings.cs`.

---

## Testing Failure Paths

A good test suite always covers the main failure scenarios alongside the happy path.

### Entity Not Found

Verify the handler returns a `not_found` error when the target entity does not exist.

```csharp
[Fact]
public async Task HandleAsync_WalletDoesNotExist_ReturnsNotFoundError()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;
    var eventBus = Substitute.For<IEventBus>();
    var handler = new DeleteWalletHandler(db, eventBus, SystemClock.Instance);

    var nonExistentId = Guid.NewGuid();
    var result = await handler.HandleAsync(
        new DeleteWalletEndpoint.DeleteWalletRequest(nonExistentId),
        userId: Guid.NewGuid(),
        ct);

    Assert.False(result.IsSuccess);
    Assert.Equal("not_found", result.Error.Code);
    eventBus.DidNotReceive().Publish(Arg.Any<WalletDeletedEvent>());
}
```

### Forbidden (Wrong User)

Verify the handler rejects access when the authenticated user does not own or belong to the resource.

```csharp
[Fact]
public async Task HandleAsync_UserNotMember_ReturnsForbiddenError()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;
    var eventBus = Substitute.For<IEventBus>();
    var handler = new DeleteWalletHandler(db, eventBus, SystemClock.Instance);

    // Seed a wallet owned by Alice
    var alice = CreateUser("alice@example.com");
    var wallet = CreateWallet(alice.Id);
    db.Users.Add(alice);
    db.Wallets.Add(wallet);
    await db.SaveChangesAsync(ct);

    // Bob tries to delete it
    var bobId = Guid.NewGuid();
    var result = await handler.HandleAsync(
        new DeleteWalletEndpoint.DeleteWalletRequest(wallet.Id),
        userId: bobId,
        ct);

    Assert.False(result.IsSuccess);
    Assert.Equal("forbidden", result.Error.Code);
}
```

### Conflict (Duplicate)

Verify the handler rejects creation when a unique constraint would be violated.

```csharp
[Fact]
public async Task HandleAsync_DuplicateEmail_ReturnsConflictError()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    var ct = TestContext.Current.CancellationToken;
    var eventBus = Substitute.For<IEventBus>();
    var handler = new RegisterUserHandler(db, eventBus, SystemClock.Instance);

    // First registration succeeds
    var request = new RegisterUserEndpoint.RegisterUserRequest("alice@example.com", "password123");
    await handler.HandleAsync(request, ct);

    // Second registration with the same email should fail
    var result = await handler.HandleAsync(request, ct);

    Assert.False(result.IsSuccess);
    Assert.Equal("conflict", result.Error.Code);
}
```

---

## Complete Example — Happy Path

```csharp
public sealed class CreateWalletTests
{
    [Fact]
    public async Task HandleAsync_ValidPersonalWallet_CreatesWalletAndPublishesEvent()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var handler = new CreateWalletHandler(db, eventBus, SystemClock.Instance);

        var user = new User
        {
            Id = Guid7.NewGuid().ToGuid(),
            Email = "alice@example.com",
            Username = "alice",
            PasswordHash = "hash",
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Checking Account",
            Type: "Personal",
            InitialBalance: 500m);

        // Act
        var result = await handler.HandleAsync(request, user.Id, ct);

        // Assert
        Assert.Multiple(
            () => Assert.True(result.IsSuccess),
            () => Assert.Equal("Checking Account", result.Value.Name),
            () => Assert.Equal("Personal", result.Value.Type)
        );

        // Verify the wallet was persisted
        var saved = await db.Wallets.FindAsync([result.Value.Id], ct);
        Assert.NotNull(saved);
        Assert.Equal("Checking Account", saved.Name);

        // Verify the event was published
        eventBus.Received(1).Publish(Arg.Is<WalletCreatedEvent>(e =>
            e.WalletId == result.Value.Id && e.UserId == user.Id));
    }
}
```

---

## Checklist Before Submitting Handler Tests

- [ ] Happy path covered with a real database
- [ ] Main failure paths covered (not found, forbidden, conflict — whichever apply)
- [ ] Event publication verified (both that it fires on success and does not fire on failure)
- [ ] No real email/push/storage calls (mocked with NSubstitute)
- [ ] Test names follow `{Method}_{Scenario}_{ExpectedResult}` convention
- [ ] `Assert.Multiple()` used when checking more than one property of the same result
