# Test Doubles

Definitions, decision matrix, and Kakeibo-specific patterns for all test double types.

---

## Definitions (Gerard Meszaros Taxonomy)

| Type | Definition | Has behavior? | Verifiable? |
|------|-----------|---------------|-------------|
| **Dummy** | Passed as argument but never used. Fills required parameter slots. | No | No |
| **Stub** | Returns hardcoded or configured values. Replaces a dependency to control the test's inputs. | Minimal | No |
| **Fake** | A working implementation that takes shortcuts (in-memory DB, test clock). Used when the real thing is too heavy. | Yes | No |
| **Spy** | A stub that also records how it was called. You can inspect call count and arguments after the fact. | Yes | Optional |
| **Mock** | Pre-programmed with expectations. Verifies that specific interactions occurred. The test fails if the interaction doesn't happen as expected. | Yes | Yes |

### Practical summary for Kakeibo

- **Use a stub** when you need to return a specific value from a dependency (e.g., `INotificationService` returning `NotificationResult.Ok()`).
- **Use a mock** when the test must verify that a specific method was called with specific arguments (e.g., asserting that `IEventBus.Publish()` was called with a specific event or that `INotificationService.SendAsync()` was called).
- **Use a fake** when you need working behavior but can't use the real thing in tests (e.g., `FakeClock` instead of `SystemClock`, `TestDbContextFactory` with real PostgreSQL instead of mocked `DbContext`).
- **Use a spy** when you need both — a working implementation plus call verification.
- **Use a dummy** only when a parameter is required but will never be used in that test path.

> In NSubstitute, all test doubles are created with `Substitute.For<T>()` regardless of whether you configure return values, verify calls, or both. The double type is determined by how you use it, not how you create it.

---

## Decision Flowchart

```
Is it a system boundary (external service, network, clock, filesystem)?
  └─ YES → Mock/Stub it
      Is the test just about return values?
        └─ YES → Stub (configure Returns())
        └─ NO (need to verify it was called) → Mock (use Received())
  └─ NO → Is it an internal module detail (DbContext, handler, validator)?
      └─ YES → Use the real implementation
          For DbContext: TestDbContextFactory (real PostgreSQL, isolated per test)
          For handlers: instantiate directly
          For validators: new Validator().Validate(request)
      └─ NO → Is it time-dependent?
          └─ YES → FakeClock (always)
          └─ NO → Use the real implementation
```

---

## Kakeibo-Specific Decision Matrix

| Dependency | Double Type | Tool | Reason |
|------------|-------------|------|--------|
| `IEventBus` | Mock | NSubstitute | In-process event bus — verify Publish calls; fire-and-forget |
| `INotificationService` | Mock / Stub | NSubstitute | External channel — verify calls + control failure |
| `IClock` | Fake | `NodaTime.Testing.FakeClock` | Time must be deterministic |
| `IEmailService` | Mock / Stub | NSubstitute | External SMTP |
| `IStorageService` | Stub | NSubstitute | RustFS/S3 — avoid real network |
| `DbContext` / `DbSet<T>` | **Real (Fake)** | `TestDbContextFactory` | Internal detail — never mock |
| `ILogger<T>` | Dummy / Stub | `NullLogger<T>.Instance` or `Substitute.For<ILogger<T>>()` | Use dummy unless log output is under test |
| Capacitor plugins | Stub / Mock | `vi.mock()` | Native APIs unavailable in Node/Vitest |
| Pinia stores in store tests | **Real** | `createPinia()` | Internal detail — use real store |
| Vue components under test | **Real** | `mount()` | Never mock what you're testing |
| Vue child components (no side effects) | **Real** | — | Only mock if they make HTTP calls or have timers |
| Axios HTTP calls in components | Stub | `vi.mock('@/lib/api', ...)` | Network boundary |

---

## NSubstitute Patterns (C#)

### Creating a substitute

```csharp
var eventBus = Substitute.For<IEventBus>();
var notifications = Substitute.For<INotificationService>();
```

### Stub: configure return values

```csharp
// Synchronous
notifications.IsAvailable().Returns(true);

// Async
notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
    .Returns(NotificationResult.Ok());

// Simulate failure
notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
    .Returns(NotificationResult.Failure("SMTP unavailable"));

// Throw exception
notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
    .Throws(new InvalidOperationException("SMTP is down"));
```

### Mock: verify calls (Received)

```csharp
// IEventBus.Publish is void (fire-and-forget) — no await needed
eventBus.Received(1).Publish(Arg.Any<WalletCreatedEvent>());

// Verify called with specific argument values
eventBus.Received(1).Publish(
    Arg.Is<WalletCreatedEvent>(e =>
        e.WalletId == expectedWalletId));

// Verify never called
eventBus.DidNotReceive().Publish(Arg.Any<WalletCreatedEvent>());

// Verify on async external service method
await notifications.Received(1).SendAsync(
    Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
```

### Arg matchers

```csharp
Arg.Any<T>()                    // any value of type T
Arg.Is<T>(x => condition)       // value matching predicate
Arg.Is("exact-string")          // exact value match
```

### Sequence / ordered calls

```csharp
// Verify order within a single substitute
Received.InOrder(() =>
{
    eventBus.Publish(Arg.Any<WalletCreatedEvent>());
    notifications.SendAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
});
```

---

## vi.mock Patterns (TypeScript / Vitest)

### Module-level mock

```typescript
// Mock entire module at the top of the test file (hoisted automatically)
vi.mock('@/lib/api', () => ({
    membersApi: {
        getAll: vi.fn(),
        create: vi.fn(),
        delete: vi.fn(),
    }
}))

// Import after vi.mock to get the mocked version
import { membersApi } from '@/lib/api'
```

### Stub: configure return values

```typescript
// Resolve with value
vi.mocked(membersApi.getAll).mockResolvedValue([
    { id: '1', name: 'Ana García', email: 'ana@test.com' }
])

// Reject with error
vi.mocked(membersApi.getAll).mockRejectedValue(new Error('Network error'))

// Return once, then fall through to default
vi.mocked(membersApi.getAll)
    .mockResolvedValueOnce([{ id: '1', name: 'Ana' }])  // first call
    .mockResolvedValue([])                               // subsequent calls
```

### Spy: verify calls

```typescript
expect(membersApi.create).toHaveBeenCalledOnce()
expect(membersApi.create).toHaveBeenCalledWith({
    firstName: 'Ana',
    lastName: 'García',
    email: 'ana@test.com',
})
expect(membersApi.delete).not.toHaveBeenCalled()
```

### Mock cleanup

```typescript
beforeEach(() => {
    vi.clearAllMocks()   // clears call counts and return values; keeps mock implementation
    // vi.resetAllMocks()  // also resets return values to undefined
    // vi.restoreAllMocks() // restores spied methods to their original implementation
})
```

### Partial module mock

```typescript
// Only mock specific exports, use real implementation for the rest
vi.mock('@/utils/date', async (importOriginal) => {
    const original = await importOriginal<typeof import('@/utils/date')>()
    return {
        ...original,
        formatDate: vi.fn().mockReturnValue('2026-02-17'),
    }
})
```

### Capacitor plugin mock (in setup file)

```typescript
// vitest.setup.mobile.ts
vi.mock('@capacitor/preferences', () => ({
    Preferences: {
        get: vi.fn().mockResolvedValue({ value: null }),
        set: vi.fn().mockResolvedValue(undefined),
        remove: vi.fn().mockResolvedValue(undefined),
        clear: vi.fn().mockResolvedValue(undefined),
    }
}))
```

---

## FakeClock — The Time Fake

`NodaTime.Testing.FakeClock` is the canonical fake for time. It implements `IClock` so it can be
injected wherever `IClock` is used. Never use `SystemClock.Instance` in test code.

```csharp
// Declare in test class
private readonly FakeClock _clock = new(Instant.FromUtc(2026, 7, 15, 12, 0));

// Inject into handler/job/service under test
var handler = new CreateWalletHandler(db, eventBus, _clock);

// Advance time in multi-step tests
_clock.AdvanceMinutes(5);
_clock.AdvanceHours(24);
_clock.AdvanceDays(7);

// Read current instant (same API as SystemClock)
var now = _clock.GetCurrentInstant();
```

Why `FakeClock` is a fake, not a mock:
- It has a working implementation (tracks an instant that can be advanced)
- It does not verify that `GetCurrentInstant()` was called
- It is injectable and deterministic, which is all time-dependent tests need

---

## When to Use Real Implementations vs Doubles

The key question: **is this a system boundary?**

System boundaries that should be doubled:
- External services (SMTP, RustFS, WhatsApp, ClickHouse)
- Time (`IClock` → `FakeClock`)
- Native device APIs (Capacitor plugins)
- In-process event bus (`IEventBus`) when verifying that events are published

Internal details that should use real implementations:
- `DbContext` and all EF Core queries — use `TestDbContextFactory`
- Domain entities, value objects, services — instantiate directly
- Validators — instantiate and call `.Validate()`
- Pinia stores under test — use `createPinia()`
- Vue components — use `mount()`
- Handlers and event handlers — instantiate with real dependencies + mocked boundaries

> The rule of thumb: if you're mocking something to avoid a side effect (network, disk, clock),
> that's a legitimate boundary to mock. If you're mocking something to avoid understanding how
> it works, that's a test smell — use the real thing.
