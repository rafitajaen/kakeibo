# Test Infrastructure Reference

This document is a reference for the shared infrastructure used across all integration tests: how the PostgreSQL container is managed, how to use NSubstitute for mocking, and how to control time with FakeClock. Read this alongside the component-specific guides.

---

## `TestDbContextFactory`

**Location:** `tests/Kakeibo.Tests/TestDbContextFactory.cs`

This factory provides isolated real PostgreSQL databases for integration tests using Testcontainers.

### How it works

```
One shared PostgreSQL container (Docker)
    ├── Test A → database kakeibo_test_<guid-A>
    ├── Test B → database kakeibo_test_<guid-B>
    └── Test C → database kakeibo_test_<guid-C>
```

- A **single Docker container** is started once for the entire test run (via `Lazy<Task>`).
- Each call to `CreateAsync()` creates a **new isolated database** inside that container.
- The schema is applied automatically via `context.Database.EnsureCreatedAsync()`.
- The database is destroyed when the `AppDbContext` is disposed (`await using`).

### Usage

```csharp
// Create a fresh, isolated database for this test
await using var db = await TestDbContextFactory.CreateAsync();
```

Always use `await using` (not `using`) so the async disposal runs correctly.

### `CreateSecondContext` — for concurrency tests

When you need two separate `AppDbContext` instances pointing to the same database (to test optimistic concurrency or transaction isolation), use:

```csharp
await using var db1 = await TestDbContextFactory.CreateAsync();
await using var db2 = TestDbContextFactory.CreateSecondContext(db1);

// db1 and db2 share the same database, but are independent EF Core change trackers
```

---

## The Docker Skip Guard

If Docker is not running on the machine (e.g., a developer who has not started Docker Desktop, or a CI agent without Docker access), Testcontainers will fail to start the container. Instead of causing a test failure — which would be a false negative — the factory catches this and calls `Assert.Skip()`.

```csharp
// Inside TestDbContextFactory
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
```

**What this means for you:**
- If Docker is running: tests execute normally.
- If Docker is not running: tests are reported as **Skipped**, not **Failed**.
- A skipped test is acceptable in CI if Docker is unavailable. Architecture tests and validator tests are unaffected since they do not use Docker.

---

## `GlobalUsings.cs`

**Location:** `tests/Kakeibo.Tests/GlobalUsings.cs`

This file contains `global using` directives that are automatically available in every test file. You do not need to add any `using` statements for these namespaces.

| Available without import | Source |
|--------------------------|--------|
| `NSubstitute` | `global using NSubstitute;` |
| `NSubstitute.ExceptionExtensions` | `global using NSubstitute.ExceptionExtensions;` |
| `Xunit` | `global using Xunit;` |
| `NodaTime` | `global using NodaTime;` |
| `NodaTime.Testing` | `global using NodaTime.Testing;` |
| `FluentValidation.TestHelper` | `global using FluentValidation.TestHelper;` |
| `Kakeibo.Api.Common.Utils` | Includes `Guid7` |
| `Kakeibo.Api.Domain.Entities` | All entity types |
| `Kakeibo.Api.Features.*` | All feature namespaces |

If a type is not resolving, check `GlobalUsings.cs` before adding a new `using` statement.

---

## NSubstitute Cheat Sheet

NSubstitute is the mocking library used for all test doubles (mocks, stubs, fakes).

### Creating a mock

```csharp
var eventBus = Substitute.For<IEventBus>();
var emailService = Substitute.For<IEmailService>();
```

### Making a synchronous method return a value

```csharp
someService.GetValue().Returns("hello");
```

### Making an async method return a value

```csharp
someService.GetValueAsync(Arg.Any<CancellationToken>()).Returns("hello");
// or
someService.GetValueAsync(Arg.Any<CancellationToken>()).ReturnsAsync("hello");
```

### Making a method throw an exception

```csharp
someService.DoSomething().Throws(new InvalidOperationException("boom"));
// For async:
someService.DoSomethingAsync().ThrowsAsync(new HttpRequestException("network error"));
```

### Verifying a method was called exactly once

```csharp
someService.Received(1).DoSomething();
// For async:
await someService.Received(1).DoSomethingAsync(Arg.Any<CancellationToken>());
```

### Verifying a method was called any number of times

```csharp
someService.ReceivedWithAnyArgs().DoSomething();
```

### Verifying a method was NOT called

```csharp
someService.DidNotReceive().DoSomething();
await someService.DidNotReceive().DoSomethingAsync(Arg.Any<CancellationToken>());
```

### Argument matchers

| Matcher | Matches |
|---------|---------|
| `Arg.Any<T>()` | Any value of type `T` |
| `Arg.Is<T>(x => condition)` | Values of type `T` where `condition` is true |
| `Arg.Is("exact")` | Exact value |

```csharp
// Verify the email was sent to a specific address, with any token
await emailService.Received(1).SendVerificationEmailAsync(
    Arg.Is<string>(email => email == "alice@example.com"),
    Arg.Any<string>()
);
```

### Verifying event publication with `IEventBus`

`IEventBus.Publish` is a void synchronous method. Use `Received()` without `await`.

```csharp
eventBus.Received(1).Publish(Arg.Is<WalletCreatedEvent>(e =>
    e.WalletId == result.Value.Id && e.UserId == user.Id));

eventBus.DidNotReceive().Publish(Arg.Any<WalletCreatedEvent>());
```

---

## `Assert.Multiple` — Group Assertions

Use `Assert.Multiple()` when you want to check several properties of the same result and see all failures at once (instead of stopping at the first one).

```csharp
Assert.Multiple(
    () => Assert.True(result.IsSuccess),
    () => Assert.Equal("Checking Account", result.Value.Name),
    () => Assert.Equal("Personal", result.Value.Type),
    () => Assert.NotEqual(Guid.Empty, result.Value.Id)
);
```

Without `Assert.Multiple`, if the first assertion fails, the rest never execute — making it harder to diagnose the full problem.

**Rule of thumb:** Use `Assert.Multiple` when asserting more than two properties of the same object.

---

## Controlling Time with `FakeClock`

`FakeClock` is from the `NodaTime.Testing` package. It implements `IClock` and returns a fixed instant regardless of wall-clock time. Use it whenever a handler or service uses `IClock` to get the current time.

### Creating a FakeClock

```csharp
// Fixed to June 15, 2024 at 12:00 UTC
var clock = new FakeClock(Instant.FromUtc(2024, 6, 15, 12, 0, 0));
```

### Injecting into a handler

```csharp
var handler = new CreateGoalHandler(db, eventBus, clock);
```

### Advancing the clock in a test

```csharp
var clock = new FakeClock(Instant.FromUtc(2024, 6, 15, 12, 0, 0));

// ... do some work ...

clock.Advance(Duration.FromDays(7));    // now it's June 22

// ... do more work that depends on the current time ...
```

### When to use `FakeClock` vs `SystemClock.Instance`

| Situation | Use |
|-----------|-----|
| Handler sets `CreatedAt`, `ExpiresAt`, or similar | `FakeClock` — makes the timestamp deterministic |
| Handler compares two timestamps and takes a different branch | `FakeClock` — lets you control both sides |
| Timestamp is irrelevant to the assertion | `SystemClock.Instance` — simpler, no setup needed |

---

## `TestContext.Current.CancellationToken`

xUnit v3 provides a `CancellationToken` per test via `TestContext.Current.CancellationToken`. Always pass this token to `async` methods inside tests instead of `CancellationToken.None`.

```csharp
var ct = TestContext.Current.CancellationToken;
var result = await handler.HandleAsync(request, userId, ct);
```

This token is cancelled if the test times out, which helps prevent stuck tests from blocking the entire test run.

---

## Seed Helper Pattern

Many tests need the same basic entities (a user, a wallet, a category) before they can test anything domain-specific. Instead of repeating setup code in every test, define private seed helpers in the test class.

```csharp
public sealed class CreateTransactionTests
{
    private static async Task<User> SeedUserAsync(AppDbContext db, CancellationToken ct)
    {
        var user = new User
        {
            Id = Guid7.NewGuid().ToGuid(),
            Email = $"user_{Guid.NewGuid():N}@example.com",    // unique per call
            Username = "testuser",
            PasswordHash = "hash",
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Wallet> SeedWalletAsync(AppDbContext db, Guid userId, CancellationToken ct)
    {
        var wallet = new Wallet
        {
            Id = Guid7.NewGuid().ToGuid(),
            UserId = userId,
            Name = "Test Wallet",
            Type = WalletType.Personal,
            Currency = "EUR"
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);
        return wallet;
    }
}
```

**Guidelines for seed helpers:**
- Use `Guid.NewGuid()` in email addresses to ensure uniqueness across parallel test runs.
- Keep helpers private and local to the test class — do not create shared global helpers that obscure what data a test depends on.
- Only seed what the test actually needs. Do not build a complete entity graph for a test that only needs a user.

---

## Quick Reference Card

```
TestDbContextFactory.CreateAsync()      → Fresh isolated PostgreSQL DB
TestDbContextFactory.CreateSecondContext(db) → Second context, same DB

Substitute.For<T>()                     → Create mock
mock.Method().Returns(value)            → Stub return value
mock.Method().ReturnsAsync(value)       → Stub async return
mock.Method().Throws(exception)         → Stub exception

mock.Received(1).Method(args)           → Assert called once
mock.DidNotReceive().Method(args)       → Assert never called
Arg.Any<T>()                            → Match any argument
Arg.Is<T>(x => condition)              → Match conditional argument

new FakeClock(Instant.FromUtc(...))     → Fixed clock
clock.Advance(Duration.FromDays(n))    → Move clock forward

TestContext.Current.CancellationToken  → Per-test cancellation token
Assert.Multiple(() => ..., () => ...)  → All assertions run even on failure
```
