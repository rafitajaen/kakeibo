# Testing Infrastructure Services

Infrastructure services are implementations that communicate with external systems: SMTP servers, push notification endpoints, object storage, analytics databases, and distributed caches. They are never tested directly in handler or event handler tests — instead, they are replaced with mocks.

---

## What Are Infrastructure Services?

These are classes under `src/Kakeibo.Api/Infrastructure/` that implement an interface used by handlers or event handlers. They have real-world side effects: sending emails, uploading files, writing to ClickHouse, and so on.

| Interface | Implementation | What it does |
|-----------|---------------|--------------|
| `IEmailService` | `EmailService` | Calls the kakeibo-email microservice to render and send emails via SMTP |
| `IWebPushService` | `WebPushService` | Sends Web Push notifications to browser clients |
| `IStorageService` | `StorageService` | Uploads and downloads files from MinIO (S3-compatible) |
| `IAuditService` | `ClickHouseAuditService` | Writes activity log entries to ClickHouse |
| `IFusionCache` | (FusionCache library) | Read/write distributed cache backed by Redis |

---

## The Rule: Mock in All Handler and Event Handler Tests

When a handler or event handler depends on an infrastructure service, always replace it with a NSubstitute mock. Do not use the real implementation in those tests.

**Why:**
- Real implementations require running external services (SMTP server, MinIO, ClickHouse, Redis).
- Tests must not send real emails or upload real files.
- Mocks are instantaneous; real service calls add network latency.

---

## How to Mock an Infrastructure Service

### Creating the mock

```csharp
var emailService = Substitute.For<IEmailService>();
var storageService = Substitute.For<IStorageService>();
var auditService = Substitute.For<IAuditService>();
```

### Injecting it into the handler

```csharp
var handler = new RegisterUserHandler(db, eventBus, emailService, clock);
```

### Verifying the mock was called (most important assertion pattern)

After running the handler, verify that the infrastructure service was called with the correct arguments.

```csharp
// Verify that a verification email was sent to the registered user's address
await emailService.Received(1).SendVerificationEmailAsync(
    Arg.Is<string>(email => email == "alice@example.com"),
    Arg.Any<string>()    // verification token — don't care about exact value
);
```

```csharp
// Verify that NO email was sent when registration fails
await emailService.DidNotReceive().SendVerificationEmailAsync(
    Arg.Any<string>(),
    Arg.Any<string>()
);
```

### Making the mock return a value

Some infrastructure calls return a value the handler uses. Use `.Returns()` or `.ReturnsAsync()`.

```csharp
storageService
    .UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
    .Returns("https://storage.example.com/avatar/alice.jpg");
```

### Making the mock throw an exception (failure path)

```csharp
emailService
    .SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>())
    .Throws(new HttpRequestException("Email service unavailable"));
```

---

## Complete Example — Mocking `IEmailService` in a Registration Test

```csharp
public sealed class RegisterUserTests
{
    [Fact]
    public async Task HandleAsync_ValidRegistration_SendsVerificationEmail()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var handler = new RegisterUserHandler(db, eventBus, emailService, SystemClock.Instance);

        var request = new RegisterUserEndpoint.RegisterUserRequest(
            Email: "alice@example.com",
            Password: "StrongP@ssw0rd",
            Username: "alice");

        // Act
        var result = await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        // Email must have been sent exactly once, to the correct address
        await emailService.Received(1).SendVerificationEmailAsync(
            Arg.Is<string>(e => e == "alice@example.com"),
            Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_DoesNotSendEmail()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = Substitute.For<IEventBus>();
        var emailService = Substitute.For<IEmailService>();
        var handler = new RegisterUserHandler(db, eventBus, emailService, SystemClock.Instance);

        // Seed an existing user
        db.Users.Add(new User { Email = "alice@example.com", /* ... */ });
        await db.SaveChangesAsync(ct);

        var request = new RegisterUserEndpoint.RegisterUserRequest(
            Email: "alice@example.com",
            Password: "StrongP@ssw0rd",
            Username: "alice2");

        // Act
        var result = await handler.HandleAsync(request, ct);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("conflict", result.Error.Code);

        // No email sent on failure
        await emailService.DidNotReceive().SendVerificationEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>());
    }
}
```

---

## When to Test Infrastructure Services Directly

Direct tests for infrastructure service implementations are rare and should be kept separate from the main test suite. They require external services to be running and are inherently slower.

| Service | When to test directly | How to mark |
|---------|----------------------|-------------|
| `EmailService` | Verifying the HTTP call to kakeibo-email produces a valid response | `[Trait("Category", "External")]` |
| `StorageService` | Verifying MinIO upload/download roundtrip | `[Trait("Category", "External")]` |
| `ClickHouseAuditService` | Verifying ClickHouse INSERT and SELECT | `[Trait("Category", "External")]` |
| `WebPushService` | Verifying VAPID header construction | May be unit-testable without external service |

Mark these tests with `[Trait("Category", "External")]` so the CI pipeline can exclude them with:

```bash
dotnet test --filter "Category!=External"
```

---

## FusionCache — Mocking the Cache

`IFusionCache` is used for short-lived caching (e.g., platform settings, maintenance mode flag). When testing handlers that read from cache, mock it to return specific values.

```csharp
var cache = Substitute.For<IFusionCache>();

// Simulate cache hit returning maintenance mode = true
cache.GetOrSetAsync<bool>(
    Arg.Is<string>(key => key == "platform:maintenance_mode"),
    Arg.Any<Func<FusionCacheFactoryExecutionContext<bool>, CancellationToken, ValueTask<bool>>>(),
    Arg.Any<FusionCacheEntryOptions>(),
    Arg.Any<CancellationToken>())
    .Returns(true);
```

In most cases, you do not need to mock the cache unless the handler's behavior changes based on the cached value (e.g., the maintenance mode middleware).

---

## Checklist Before Submitting Tests That Involve Infrastructure Services

- [ ] All infrastructure services are mocked with `Substitute.For<T>()` in handler tests
- [ ] Successful paths verify the service was called with `Received(1)` and correct argument matchers
- [ ] Failure paths verify the service was NOT called with `DidNotReceive()`
- [ ] No real HTTP calls, emails, file uploads, or ClickHouse writes happen during the test run
- [ ] Dedicated direct service tests (if any) are marked `[Trait("Category", "External")]`
