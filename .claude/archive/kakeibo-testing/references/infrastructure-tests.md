# Infrastructure Tests

Tests for cross-cutting infrastructure components that don't belong to any single feature:
`ChannelEventBus`, `EventDispatcher`, and `PermissionService`.

**Project:** `tests/Kakeibo.Tests/Integration/`

**Script:** `bun run api:test` (uses Testcontainers — requires Docker).

---

## ChannelEventBus — Throughput and Ordering

**Purpose:** Verify that `ChannelEventBus` writes events to the channel correctly, that
`Publish` is fire-and-forget (non-blocking), and that events are not dropped under load.

**Strategy:** Use a real `ChannelEventBus` with a capturing `IEventHandler<T>` stub.
Assert that all published events are eventually dispatched.

```csharp
public sealed class ChannelEventBusTests
{
    [Fact]
    public void Publish_SingleEvent_DoesNotBlock()
    {
        // Arrange
        var bus = new ChannelEventBus();

        var sw = Stopwatch.StartNew();
        bus.Publish(new WalletCreatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            WalletId = Guid7.NewGuid(),
        });
        sw.Stop();

        // Publish must return in microseconds — it only writes to a channel, never awaits a handler
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Publish took {sw.ElapsedMilliseconds}ms — it must be non-blocking");
    }

    [Fact]
    public async Task Publish_MultipleEvents_AllDispatchedByEventDispatcher()
    {
        var ct = TestContext.Current.CancellationToken;
        var capturedEvents = new ConcurrentBag<IEvent>();

        // Build a DI container with ChannelEventBus + EventDispatcher + capturing handler
        var services = new ServiceCollection();
        services.AddSingleton<ChannelEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<ChannelEventBus>());
        services.AddHostedService<EventDispatcher>();
        services.AddScoped<IEventHandler<WalletCreatedEvent>>(
            _ => new CapturingEventHandler<WalletCreatedEvent>(capturedEvents));
        services.AddLogging();

        await using var host = services.BuildServiceProvider();
        var hostedServices = host.GetServices<IHostedService>();
        foreach (var svc in hostedServices)
            await svc.StartAsync(ct);

        var bus = host.GetRequiredService<IEventBus>();
        const int eventCount = 10;
        for (var i = 0; i < eventCount; i++)
        {
            bus.Publish(new WalletCreatedEvent
            {
                Id = Guid.NewGuid(),
                OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
                WalletId = Guid7.NewGuid(),
            });
        }

        // Wait for all events to be dispatched (EventDispatcher processes asynchronously)
        await Task.Delay(500, ct);

        Assert.Equal(eventCount, capturedEvents.Count);

        foreach (var svc in hostedServices)
            await svc.StopAsync(ct);
    }
}

// Capturing handler for use in infrastructure tests
internal sealed class CapturingEventHandler<T>(ConcurrentBag<IEvent> bag) : IEventHandler<T>
    where T : IEvent
{
    public Task HandleAsync(T @event, CancellationToken ct)
    {
        bag.Add(@event);
        return Task.CompletedTask;
    }
}
```

---

## EventDispatcher — Error Isolation

**Purpose:** Verify that `EventDispatcher` catches exceptions from individual handlers and does
NOT stop processing subsequent events when one handler throws.

**Key invariant:** A failing handler must not block the channel — subsequent events continue
to be dispatched.

```csharp
public sealed class EventDispatcherTests
{
    [Fact]
    public async Task Dispatch_WhenHandlerThrows_ContinuesProcessingNextEvent()
    {
        var ct = TestContext.Current.CancellationToken;
        var successfulEvents = new ConcurrentBag<IEvent>();

        var services = new ServiceCollection();
        services.AddSingleton<ChannelEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<ChannelEventBus>());
        services.AddHostedService<EventDispatcher>();
        services.AddLogging();

        // Register two handlers: first always throws, second captures the event
        services.AddScoped<IEventHandler<WalletCreatedEvent>, ThrowingEventHandler>();
        services.AddScoped<IEventHandler<TransactionRecordedEvent>>(
            _ => new CapturingEventHandler<TransactionRecordedEvent>(successfulEvents));

        await using var host = services.BuildServiceProvider();
        var hostedServices = host.GetServices<IHostedService>();
        foreach (var svc in hostedServices)
            await svc.StartAsync(ct);

        var bus = host.GetRequiredService<IEventBus>();

        // Publish event that will throw, then event that should succeed
        bus.Publish(new WalletCreatedEvent { Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), WalletId = Guid7.NewGuid() });
        bus.Publish(new TransactionRecordedEvent { Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), TransactionId = Guid7.NewGuid() });

        await Task.Delay(500, ct);

        // The TransactionRecordedEvent must still have been dispatched despite the WalletCreatedEvent handler throwing
        Assert.Single(successfulEvents);

        foreach (var svc in hostedServices)
            await svc.StopAsync(ct);
    }
}

// Handler that always throws — for testing EventDispatcher error isolation
internal sealed class ThrowingEventHandler : IEventHandler<WalletCreatedEvent>
{
    public Task HandleAsync(WalletCreatedEvent @event, CancellationToken ct)
        => throw new InvalidOperationException("Simulated handler failure");
}
```

---

## PermissionService

**Purpose:** Verify the three key behaviors of `PermissionService`: Admin bypass (no DB query),
cache hit prevention of duplicate DB queries, and correct permission string matching.

**Strategy:** Use `virtual` override on `PermissionService` or inject a fake cache to control
cache hit/miss scenarios without Redis.

```csharp
public sealed class PermissionServiceTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 2, 17, 12, 0));

    [Fact]
    public async Task HasPermissionAsync_AdminUser_ReturnsTrueWithoutQueryingDb()
    {
        // Admin must bypass all permission checks — wildcard grant
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid7.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "admin@test.com",
            RoleName = RoleNames.Admin,
            CreatedAt = _clock.GetCurrentInstant(),
        });
        await db.SaveChangesAsync(ct);

        // Use a cache that always misses so we can verify DB access
        var queryCount = 0;
        var trackingDb = new QueryCountingDbContext(db, () => queryCount++);
        var service = new PermissionService(trackingDb, new NullPermissionCache());

        var result = await service.HasPermissionAsync(userId, "wallets:delete", ct);

        Assert.True(result);
        Assert.Equal(0, queryCount);  // Admin never touches the permissions table
    }

    [Fact]
    public async Task HasPermissionAsync_CachedPermissions_DoesNotQueryDbAgain()
    {
        // L1 cache hit must prevent a second DB round-trip within the same request scope
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid7.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "user@test.com", RoleName = "User", CreatedAt = _clock.GetCurrentInstant() });
        db.RolePermissions.Add(new RolePermission { RoleName = "User", Permission = "wallets:read" });
        await db.SaveChangesAsync(ct);

        var queryCount = 0;
        var trackingDb = new QueryCountingDbContext(db, () => queryCount++);
        var service = new PermissionService(trackingDb, new NullPermissionCache());

        // First call — hits DB
        await service.HasPermissionAsync(userId, "wallets:read", ct);
        var afterFirst = queryCount;

        // Second call — must use cache, not DB
        await service.HasPermissionAsync(userId, "wallets:read", ct);

        Assert.Equal(afterFirst, queryCount);  // no additional DB query
    }

    [Fact]
    public async Task HasPermissionAsync_CorrectPermission_ReturnsTrue()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid7.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "user@test.com", RoleName = "User", CreatedAt = _clock.GetCurrentInstant() });
        db.RolePermissions.Add(new RolePermission { RoleName = "User", Permission = "wallets:read" });
        await db.SaveChangesAsync(ct);

        var service = new PermissionService(db, new NullPermissionCache());

        Assert.True(await service.HasPermissionAsync(userId, "wallets:read", ct));
        Assert.False(await service.HasPermissionAsync(userId, "wallets:delete", ct));
    }

    [Fact]
    public async Task HasPermissionAsync_PermissionCaseSensitivity_MustMatch()
    {
        // Permissions are stored lowercase — "Wallets:Read" must NOT match "wallets:read"
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid7.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "user@test.com", RoleName = "User", CreatedAt = _clock.GetCurrentInstant() });
        db.RolePermissions.Add(new RolePermission { RoleName = "User", Permission = "wallets:read" });
        await db.SaveChangesAsync(ct);

        var service = new PermissionService(db, new NullPermissionCache());

        // Mixed-case should NOT match (permissions are stored lowercase)
        Assert.False(await service.HasPermissionAsync(userId, "Wallets:Read", ct));
    }
}
```

---

## Test Infrastructure Notes

### `TestDbContextFactory` for Infrastructure Tests

Infrastructure tests reuse the same `TestDbContextFactory` pattern from feature tests:

```csharp
internal static class TestDbContextFactory
{
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private static readonly Lazy<Task> ContainerStartTask = new(() => PostgresContainer.StartAsync());

    private static async Task EnsureContainerStartedAsync()
    {
        try { await ContainerStartTask.Value; }
        catch { Assert.Skip("Docker is not available. Infrastructure tests require Testcontainers."); }
    }

    public static async Task<AppDbContext> CreateAsync()
    {
        await EnsureContainerStartedAsync();

        var database = $"kakeibo_infra_test_{Guid.NewGuid():N}";
        var connString = new NpgsqlConnectionStringBuilder(PostgresContainer.GetConnectionString())
            { Database = database }.ConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
```

### `NullPermissionCache`

Simple in-memory stub used in permission tests to control cache miss/hit behavior:

```csharp
internal sealed class NullPermissionCache : IPermissionCache
{
    private readonly Dictionary<Guid, IReadOnlyList<string>> _store = new();

    public ValueTask<IReadOnlyList<string>?> GetAsync(Guid userId, CancellationToken ct)
        => ValueTask.FromResult(_store.TryGetValue(userId, out var perms) ? perms : null);

    public ValueTask SetAsync(Guid userId, IReadOnlyList<string> permissions, CancellationToken ct)
    {
        _store[userId] = permissions;
        return ValueTask.CompletedTask;
    }
}
```
