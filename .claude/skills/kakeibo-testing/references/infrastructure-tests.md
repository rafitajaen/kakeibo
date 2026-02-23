# Infrastructure Tests

Tests for cross-cutting infrastructure components that don't belong to any single module:
`OutboxInterceptor`, `OutboxProcessor`, `AuditOutboxProcessor`, and `PermissionService`.

**Project:** `tests/Kakeibo.Infrastructure.Tests/` (or co-located in `Kakeibo.Modules.Identity.Tests/`
for `PermissionService`).

**Script:** `bun run api:test:unit` (uses Testcontainers — requires Docker).

---

## OutboxInterceptor — Atomicity

**Purpose:** Verify that `OutboxInterceptor` persists entity changes AND outbox messages in the
same database transaction. A commit failure must not leave orphaned outbox messages.

**Key invariant:** `entities saved ↔ outbox rows inserted` — they are always in sync.

```csharp
public sealed class OutboxInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_WithPublishedEvent_InsertsOutboxMessage()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = new ModuleEventBus();  // real in-memory buffer, not a mock

        db.Members.Add(new Member
        {
            Email = "test@test.com",
            CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
        });

        // Buffer one integration event before SaveChanges
        await eventBus.PublishAsync(new MemberCreatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            MemberId = Guid.NewGuid(),
            Version = 1,
        }, ct);

        // Act
        await db.SaveChangesAsync(ct);  // OutboxInterceptor fires here

        // Assert — entity and outbox row persisted atomically
        var outboxMessages = await db.OutboxMessages.ToListAsync(ct);
        Assert.Single(outboxMessages);
        Assert.Equal(nameof(MemberCreatedEvent), outboxMessages[0].Type);
        Assert.Null(outboxMessages[0].ProcessedAt);  // not yet dispatched
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleEvents_InsertsAllOutboxMessages()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var eventBus = new ModuleEventBus();

        // Buffer two different integration events
        await eventBus.PublishAsync(new MemberCreatedEvent
            { Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), MemberId = Guid.NewGuid(), Version = 1 }, ct);
        await eventBus.PublishAsync(new MemberSubscribedEvent
            { Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), MemberId = Guid.NewGuid(), PlanId = "standard", Version = 1 }, ct);

        db.Members.Add(new Member { Email = "test@test.com", CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0) });
        await db.SaveChangesAsync(ct);

        var count = await db.OutboxMessages.CountAsync(ct);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task SaveChangesAsync_AfterDomainEvent_DomainEventsListIsCleared()
    {
        // DomainEvents must be cleared after SaveChanges so they are not re-dispatched
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var member = new Member { Email = "test@test.com", CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0) };
        member.Create("Ana", "García");  // adds a domain event to member.DomainEvents

        db.Members.Add(member);
        await db.SaveChangesAsync(ct);

        // After SaveChanges the list must be empty — prevents double dispatch on next SaveChanges
        Assert.Empty(member.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_NoPublishedEvents_NoOutboxMessages()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        // Save an entity without publishing any event
        db.Members.Add(new Member { Email = "test@test.com", CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0) });
        await db.SaveChangesAsync(ct);

        var count = await db.OutboxMessages.CountAsync(ct);
        Assert.Equal(0, count);
    }
}
```

---

## OutboxProcessor — Polling, Dispatch, and Retry

**Purpose:** Verify that `OutboxProcessor` picks up unprocessed outbox messages, dispatches them
to `IEventConsumer<T>` handlers, marks them as processed, and retries on transient failures.

```csharp
public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessBatchAsync_UnprocessedMessage_DispatchesToConsumerAndMarksDone()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        // Seed an unprocessed outbox message
        var memberId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(MemberCreatedEvent),
            Payload = JsonSerializer.Serialize(new MemberCreatedEvent
            {
                Id = Guid.NewGuid(),
                OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0),
                MemberId = memberId,
                Version = 1,
            }, DefaultSerializer.Options),
            CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            ProcessedAt = null,
        });
        await db.SaveChangesAsync(ct);

        var consumer = Substitute.For<IEventConsumer<MemberCreatedEvent>>();
        consumer.ConsumeAsync(Arg.Any<MemberCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var processor = new OutboxProcessor(db, consumer, NullLogger<OutboxProcessor>.Instance);

        // Act
        await processor.ProcessBatchAsync(ct);

        // Assert consumer was called
        await consumer.Received(1).ConsumeAsync(
            Arg.Is<MemberCreatedEvent>(e => e.MemberId == memberId),
            Arg.Any<CancellationToken>());

        // Assert message marked as processed
        var message = await db.OutboxMessages.FirstAsync(ct);
        Assert.NotNull(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBatchAsync_AlreadyProcessedMessage_SkipsDispatch()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(MemberCreatedEvent),
            Payload = "{}",
            CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            ProcessedAt = Instant.FromUtc(2026, 1, 1, 0, 1),  // already processed
        });
        await db.SaveChangesAsync(ct);

        var consumer = Substitute.For<IEventConsumer<MemberCreatedEvent>>();
        var processor = new OutboxProcessor(db, consumer, NullLogger<OutboxProcessor>.Instance);

        await processor.ProcessBatchAsync(ct);

        // Already-processed messages must not be re-dispatched
        await consumer.DidNotReceive().ConsumeAsync(Arg.Any<MemberCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_ConsumerThrows_MessageRemainsUnprocessed()
    {
        // If the consumer throws, the message must stay unprocessed so the next poll retries it.
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var messageId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = messageId,
            Type = nameof(MemberCreatedEvent),
            Payload = JsonSerializer.Serialize(new MemberCreatedEvent
            {
                Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), MemberId = Guid.NewGuid(), Version = 1
            }, DefaultSerializer.Options),
            CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            ProcessedAt = null,
        });
        await db.SaveChangesAsync(ct);

        var consumer = Substitute.For<IEventConsumer<MemberCreatedEvent>>();
        consumer.ConsumeAsync(Arg.Any<MemberCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Consumer failure"));

        var processor = new OutboxProcessor(db, consumer, NullLogger<OutboxProcessor>.Instance);

        // Processor must not throw — it handles consumer errors internally
        await processor.ProcessBatchAsync(ct);

        // Message must still be unprocessed — will be retried on next poll
        var message = await db.OutboxMessages.FindAsync([messageId], ct);
        Assert.NotNull(message);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBatchAsync_SameMessageProcessedTwice_IsIdempotent()
    {
        // KB-005: Encode the idempotency contract as a test.
        // OutboxProcessor guarantees at-least-once delivery — the same message may be dispatched
        // twice if a crash occurs between dispatch and marking-as-processed.
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var memberId = Guid.NewGuid();
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(MemberCreatedEvent),
            Payload = JsonSerializer.Serialize(new MemberCreatedEvent
            {
                Id = Guid.NewGuid(), OccurredAt = Instant.FromUtc(2026, 1, 1, 0, 0), MemberId = memberId, Version = 1
            }, DefaultSerializer.Options),
            CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
            ProcessedAt = null,
        });
        await db.SaveChangesAsync(ct);

        var consumer = Substitute.For<IEventConsumer<MemberCreatedEvent>>();
        consumer.ConsumeAsync(Arg.Any<MemberCreatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var processor = new OutboxProcessor(db, consumer, NullLogger<OutboxProcessor>.Instance);

        // Process twice — simulating a retry after a partial failure
        await processor.ProcessBatchAsync(ct);
        await processor.ProcessBatchAsync(ct);

        // Consumer is called once (second poll sees message already processed)
        await consumer.Received(1).ConsumeAsync(Arg.Any<MemberCreatedEvent>(), Arg.Any<CancellationToken>());
    }
}
```

---

## AuditOutboxProcessor — ClickHouse Write

**Purpose:** Verify that `AuditOutboxProcessor` reads audit envelopes from the outbox, maps them
to the correct ClickHouse row format, and marks messages as processed.

**Strategy:** Inject a capturer (via constructor or `virtual` method override) instead of a real
ClickHouse connection. Verify the mapped payload, not the ClickHouse driver.

```csharp
// Test subclass that captures instead of writing to ClickHouse
internal sealed class CapturingAuditOutboxProcessor(
    IdentityDbContext db,
    FakeClock clock,
    ILogger<AuditOutboxProcessor> logger)
    : AuditOutboxProcessor(db, clock, logger)
{
    public List<AuditRow> CapturedRows { get; } = [];

    // Override the virtual write method — no real ClickHouse connection needed
    protected override Task WriteBulkAsync(IEnumerable<AuditRow> rows, CancellationToken ct)
    {
        CapturedRows.AddRange(rows);
        return Task.CompletedTask;
    }
}

public sealed class AuditOutboxProcessorTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 2, 17, 12, 0));

    [Fact]
    public async Task ProcessBatchAsync_AuditEnvelope_MapsToCorrectRow()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var actorId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "AuditEventEnvelope",
            Payload = JsonSerializer.Serialize(new AuditEventEnvelope
            {
                Action = AuditAction.Members.Created,
                Module = "Members",
                EntityType = "Member",
                EntityId = entityId.ToString(),
                ActorId = actorId,
                OccurredAt = _clock.GetCurrentInstant(),
            }, DefaultSerializer.Options),
            CreatedAt = _clock.GetCurrentInstant(),
            ProcessedAt = null,
        });
        await db.SaveChangesAsync(ct);

        var processor = new CapturingAuditOutboxProcessor(
            db, _clock, NullLogger<AuditOutboxProcessor>.Instance);

        await processor.ProcessBatchAsync(ct);

        // Assert: one row captured with correct field mapping
        Assert.Single(processor.CapturedRows);
        var row = processor.CapturedRows[0];

        Assert.Multiple(
            () => Assert.Equal(AuditAction.Members.Created, row.Action),
            () => Assert.Equal("Members", row.Module),
            () => Assert.Equal("Member", row.EntityType),
            () => Assert.Equal(entityId.ToString(), row.EntityId),
            () => Assert.Equal(actorId, row.ActorId));
    }

    [Fact]
    public async Task ProcessBatchAsync_NonAuditMessage_IsSkipped()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        // A regular integration event — not an AuditEventEnvelope
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(MemberCreatedEvent),
            Payload = "{}",
            CreatedAt = _clock.GetCurrentInstant(),
            ProcessedAt = null,
        });
        await db.SaveChangesAsync(ct);

        var processor = new CapturingAuditOutboxProcessor(
            db, _clock, NullLogger<AuditOutboxProcessor>.Instance);

        await processor.ProcessBatchAsync(ct);

        // Non-audit messages must be ignored by the audit processor
        Assert.Empty(processor.CapturedRows);
    }

    [Fact]
    public async Task ProcessBatchAsync_MultiplePendingEnvelopes_BatchesAllRows()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        for (var i = 0; i < 5; i++)
        {
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "AuditEventEnvelope",
                Payload = JsonSerializer.Serialize(new AuditEventEnvelope
                {
                    Action = "test.action",
                    Module = "Test",
                    EntityType = "Entity",
                    EntityId = Guid.NewGuid().ToString(),
                    OccurredAt = _clock.GetCurrentInstant(),
                }, DefaultSerializer.Options),
                CreatedAt = _clock.GetCurrentInstant(),
                ProcessedAt = null,
            });
        }
        await db.SaveChangesAsync(ct);

        var processor = new CapturingAuditOutboxProcessor(
            db, _clock, NullLogger<AuditOutboxProcessor>.Instance);

        await processor.ProcessBatchAsync(ct);

        Assert.Equal(5, processor.CapturedRows.Count);

        // All messages marked as processed
        var unprocessed = await db.OutboxMessages.CountAsync(m => m.ProcessedAt == null, ct);
        Assert.Equal(0, unprocessed);
    }
}
```

---

## PermissionService

**Purpose:** Verify the three key behaviors of `PermissionService`: SuperAdmin bypass (no DB query),
cache hit prevention of duplicate DB queries, and correct permission string matching.

**Strategy:** Use `virtual` override on `PermissionService` or inject a fake cache to control
cache hit/miss scenarios without Redis.

```csharp
public sealed class PermissionServiceTests
{
    private readonly FakeClock _clock = new(Instant.FromUtc(2026, 2, 17, 12, 0));

    [Fact]
    public async Task HasPermissionAsync_SuperAdmin_ReturnsTrueWithoutQueryingDb()
    {
        // SuperAdmin must bypass all permission checks — wildcard grant
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "superadmin@test.com",
            RoleName = RoleNames.SuperAdmin,
            CreatedAt = _clock.GetCurrentInstant(),
        });
        await db.SaveChangesAsync(ct);

        // Use a cache that always misses so we can verify DB access
        var queryCount = 0;
        var trackingDb = new QueryCountingDbContext(db, () => queryCount++);
        var service = new PermissionService(trackingDb, new NullPermissionCache());

        var result = await service.HasPermissionAsync(userId, "members:delete", ct);

        Assert.True(result);
        Assert.Equal(0, queryCount);  // SuperAdmin never touches the permissions table
    }

    [Fact]
    public async Task HasPermissionAsync_CachedPermissions_DoesNotQueryDbAgain()
    {
        // L1 cache hit must prevent a second DB round-trip within the same request scope
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "emp@test.com", RoleName = "Employee", CreatedAt = _clock.GetCurrentInstant() });
        db.RolePermissions.Add(new RolePermission { RoleName = "Employee", Permission = "members:read" });
        await db.SaveChangesAsync(ct);

        var queryCount = 0;
        var trackingDb = new QueryCountingDbContext(db, () => queryCount++);
        var service = new PermissionService(trackingDb, new NullPermissionCache());

        // First call — hits DB
        await service.HasPermissionAsync(userId, "members:read", ct);
        var afterFirst = queryCount;

        // Second call — must use cache, not DB
        await service.HasPermissionAsync(userId, "members:read", ct);

        Assert.Equal(afterFirst, queryCount);  // no additional DB query
    }

    [Fact]
    public async Task HasPermissionAsync_CorrectPermission_ReturnsTrue()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "emp@test.com", RoleName = "Employee", CreatedAt = _clock.GetCurrentInstant() });
        db.RolePermissions.Add(new RolePermission { RoleName = "Employee", Permission = "members:read" });
        await db.SaveChangesAsync(ct);

        var service = new PermissionService(db, new NullPermissionCache());

        Assert.True(await service.HasPermissionAsync(userId, "members:read", ct));
        Assert.False(await service.HasPermissionAsync(userId, "members:delete", ct));
    }

    [Fact]
    public async Task HasPermissionAsync_PermissionCaseSensitivity_MustMatch()
    {
        // Permissions are stored lowercase — "Members:Read" must NOT match "members:read"
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = "emp@test.com", RoleName = "Employee", CreatedAt = _clock.GetCurrentInstant() });
        db.RolePermissions.Add(new RolePermission { RoleName = "Employee", Permission = "members:read" });
        await db.SaveChangesAsync(ct);

        var service = new PermissionService(db, new NullPermissionCache());

        // Mixed-case should NOT match (permissions are stored lowercase)
        Assert.False(await service.HasPermissionAsync(userId, "Members:Read", ct));
    }
}
```

---

## Test Infrastructure Notes

### `TestDbContextFactory` for Infrastructure Tests

Infrastructure test projects reuse the same `TestDbContextFactory` pattern from module tests:

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

    public static async Task<IdentityDbContext> CreateAsync()
    {
        await EnsureContainerStartedAsync();

        var database = $"kakeibo_infra_test_{Guid.NewGuid():N}";
        var connString = new NpgsqlConnectionStringBuilder(PostgresContainer.GetConnectionString())
            { Database = database }.ConnectionString;

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        var context = new IdentityDbContext(options);
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
