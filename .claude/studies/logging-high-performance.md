# High-Performance Logging in .NET 10: A Study for Kakeibo

## 1. Executive Summary

The standard `ILogger.LogInformation(...)` extension methods (what you called "LoggerExtensions") allocate memory on **every call** — even when the log level is disabled — due to boxing of value types and a `params object[]` allocation. The `[LoggerMessage]` source generator eliminates these allocations entirely by pre-generating strongly-typed delegates at compile time. Structured properties **are fully preserved** with Serilog; the only limitation is the Serilog-specific `@` destructuring operator.

**You are correct**: `[LoggerMessage]` is strictly better than regular `ILogger.Log*()` for hot paths. It gives you zero allocations while keeping all structured data.

---

## 2. Current Logging State in Kakeibo

From codebase exploration, the current setup is:

| Aspect | Status |
|--------|--------|
| Logger | Serilog (configured via appsettings.json) |
| Sinks | Default only (console via ASP.NET Core host) — no explicit Serilog sinks configured |
| Enrichers | None configured |
| OpenTelemetry | Infrastructure declared, no active exporters |
| `[LoggerMessage]` usage | ❌ Not used anywhere |
| String interpolation | ❌ Not used (good) |
| Message templates | ✅ Used consistently (`{PropertyName}` placeholders) |
| Structured properties | ✅ Correct (UserId, WalletId, PatternId, etc.) |
| `IsEnabled` guards | ❌ Not added manually |

**Where ILogger is currently used:**
- `ClickHouseAuditService` — ClickHouse connection failures and query errors
- `EmailService` — success confirmation after each email sent
- `StorageService` — file upload/delete/bucket confirmations
- `WebPushService` — push notification failures and stale subscription cleanup
- `EventDispatcher` — background event dispatch errors
- `GenerateRecurringTransactionsJob` — job stats, per-pattern success/failure
- `BudgetExceededNotificationHandler`, `BudgetWarningNotificationHandler` — external service failures
- `InviteToWalletHandler` — fire-and-forget email failure logging

**Good news**: No string interpolation (`$"..."`) is used anywhere. All logs use correct message templates. The issue is purely about allocation overhead from the `params object[]` pattern.

---

## 3. The Three Patterns Explained

### 3.1 Regular ILogger Extension Methods (current approach)

```csharp
logger.LogInformation("Transaction {TransactionId} recorded in wallet {WalletId}", transactionId, walletId);
```

**What happens under the hood on every call:**

1. A `params object?[]` array is allocated on the heap to hold the arguments.
2. Every value type (`Guid`, `int`, `decimal`) is **boxed** into a heap-allocated `object` wrapper.
3. The message template string is passed to Serilog's `MessageTemplateCache`.
4. Serilog parses the template if it's not cached (up to 1000 templates; evicts all on overflow).
5. If `IsEnabled` is false, all of steps 1–3 **still happened** — the memory was already allocated.

**Concrete allocations per call (2 Guid parameters):**

| Allocation | ~Bytes |
|-----------|--------|
| `object[]` params array (2 elements) | 40 B |
| Boxing `Guid` (×2) | 80 B |
| **Total per call** | **~120 B** |

This is 120 bytes of heap allocation per log call, even when the log level is disabled.

**Serilog's `MessageTemplateCache`:**
- Caches up to **1,000 templates**, max 1,024 chars each.
- Uses `ReferenceEquals` for key comparison — works perfectly for string literals (interned by the compiler).
- On overflow, **clears the entire cache** (no LRU eviction).
- If you ever build templates dynamically (string concatenation/interpolation), you bypass the cache and cause GC pressure.

---

### 3.2 LoggerMessage.Define\<T\> (static factory, older pattern)

```csharp
private static readonly Action<ILogger, Guid, Guid, Exception?> _transactionRecorded =
    LoggerMessage.Define<Guid, Guid>(
        LogLevel.Information,
        new EventId(1001, "TransactionRecorded"),
        "Transaction {TransactionId} recorded in wallet {WalletId}");

public static void LogTransactionRecorded(this ILogger logger, Guid transactionId, Guid walletId)
    => _transactionRecorded(logger, transactionId, walletId, null);
```

- **Zero allocations** — strongly-typed delegate, no `params object[]`, no boxing.
- Template parsed once at field initialization, cached as `static readonly`.
- `IsEnabled` check built into `LoggerMessage.Define` internally.
- **Maximum 6 type parameters** — `Define<T1, T2, T3, T4, T5, T6>`.
- High boilerplate — must manually write the static field + extension method.
- Available since .NET Core 1.0.

---

### 3.3 [LoggerMessage] Source Generator (recommended approach)

```csharp
public static partial class RecordTransactionLogs
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Transaction {TransactionId} recorded in wallet {WalletId} by user {UserId}")]
    public static partial void TransactionRecorded(
        this ILogger logger, Guid transactionId, Guid walletId, Guid userId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Wallet {WalletId} not found for user {UserId}")]
    public static partial void WalletNotFound(
        this ILogger logger, Guid walletId, Guid userId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Failed to dispatch event {EventType} via handler {HandlerType}")]
    public static partial void EventDispatchFailed(
        this ILogger logger, string eventType, string handlerType, Exception exception);
}
```

**What the Roslyn compiler generates** (at build time, not runtime):

```csharp
private static readonly Action<ILogger, Guid, Guid, Guid, Exception?> __TransactionRecordedCallback =
    LoggerMessage.Define<Guid, Guid, Guid>(
        LogLevel.Information,
        new EventId(1001, nameof(TransactionRecorded)),
        "Transaction {TransactionId} recorded in wallet {WalletId} by user {UserId}",
        new LogDefineOptions() { SkipEnabledCheck = true });

public static partial void TransactionRecorded(this ILogger logger, Guid transactionId, Guid walletId, Guid userId)
{
    if (logger.IsEnabled(LogLevel.Information))
    {
        __TransactionRecordedCallback(logger, transactionId, walletId, userId, null);
    }
}
```

Key points:
- **Zero allocations** — no `params object[]`, no boxing.
- **`IsEnabled` guard is generated automatically** — no work done when level is disabled.
- **No maximum parameter count** — unlimited (unlike `Define<T>`'s 6-type limit).
- **Compile-time diagnostics** — `SYSLIB0014` etc. if template is malformed.
- **Stable `EventId`** — helps filter logs in backends.
- Available since .NET 6.

---

## 4. Comparison Table

| Feature | `logger.LogInformation()` | `LoggerMessage.Define<T>()` | `[LoggerMessage]` Source Gen |
|---------|--------------------------|----------------------------|-----------------------------|
| **Allocation per call (enabled)** | ~64–120 B | **0 B** | **0 B** |
| **Allocation per call (disabled)** | ~64–120 B | **0 B** | **0 B** |
| **IsEnabled guard** | ❌ Manual | ✅ Built-in | ✅ Generated |
| **CPU overhead (benchmark)** | ~98 ns | ~59 ns | ~49 ns |
| **Overhead when disabled** | ~87 ns | ~8 ns | **~7 ns** |
| **Max parameters** | Unlimited | 6 | Unlimited |
| **Structured data** | ✅ | ✅ | ✅ |
| **Serilog `@` destructuring** | ✅ Full | ✅ Full | ⚠️ Not supported |
| **Serilog template caching** | ✅ (1000 entries) | ✅ | ✅ |
| **OTel trace correlation** | ✅ | ✅ | ✅ |
| **Boilerplate** | None | High | Minimal (attribute) |
| **Compile-time errors** | ❌ | ❌ | ✅ (SYSLIB warnings) |
| **.NET version** | Any | Any | .NET 6+ |
| **Log level dynamic** | ✅ | ❌ | ✅ (omit Level from attr) |
| **Microsoft CA1848 rule** | ⚠️ Warning | ✅ | ✅ |

### Benchmark data (BenchmarkDotNet, Andrew Lock — 1,000 iterations):

| Approach | Mean latency | Total allocation |
|----------|-------------|-----------------|
| String interpolation `$"..."` | 553 ns | 664 B |
| `logger.LogInformation(template, args)` | 98 ns | 64–120 B |
| `IsEnabled` guard + `logger.Log*()` (disabled) | 7 ns | 0 B |
| `LoggerMessage.Define<T>()` | 59 ns | 0 B |
| **`[LoggerMessage]` source generator** | **49 ns** | **0 B** |

### Benchmark data (GoatReview, .NET 9 — 1,000 iterations):

| Approach | Mean | Total allocation |
|----------|------|-----------------|
| String interpolation | 74,690 ns | 120,000 B |
| Structured templates (regular) | 31,212 ns | 88,000 B |
| **`[LoggerMessage]`** | **896 ns** | **0 B** |

**~83× faster, 100% fewer allocations in tight loops.**

---

## 5. Does Serilog Still Receive Structured Properties?

**Yes, fully.** The data flow is:

```
[LoggerMessage] generated code
  → calls LoggerMessage.Define<T>() static delegate
  → produces LogValues<T> struct
    (implements IReadOnlyList<KeyValuePair<string, object?>>)
  → Microsoft.Extensions.Logging pipeline
  → Serilog.Extensions.Logging.SerilogLoggerProvider
  → Serilog extracts properties from IReadOnlyList
  → MessageTemplateCache caches the {OriginalFormat}
  → Properties emitted as LogEventProperty (typed, structured)
  → All sinks (Seq, Elasticsearch, console JSON, etc.) receive full structure
```

`WalletId`, `UserId`, `TransactionId` etc. are all preserved as typed structured properties in Serilog's `LogEvent`. Any downstream sink receives them correctly.

**The only limitation**: Serilog's `@` destructuring operator (e.g., `{@Request}`) is **not supported** by the source generator. The generator sees `@Request` as a parameter named `@Request` and emits a `SYSLIB1014` compile error. This is a known limitation tracked in [dotnet/runtime#69490](https://github.com/dotnet/runtime/issues/69490).

**Workaround for destructuring**: Keep using regular `logger.LogInformation("Received {@Request}", request)` for the rare cases where you need to destructure a complex object. These are typically cold paths (low frequency), so the allocation cost is negligible.

---

## 6. OpenTelemetry Correlation

`[LoggerMessage]` has **no negative effect** on OpenTelemetry trace correlation. The flow:

1. ASP.NET Core creates an `Activity` (OTel Span) for each HTTP request.
2. The `Activity` is stored in `Activity.Current` (via `AsyncLocal<T>`).
3. When `ILogger<T>` emits a log, `OpenTelemetryLoggerProvider` automatically reads from `Activity.Current` and attaches `TraceId`, `SpanId`, and `TraceFlags` to the `LogRecord`.

This works identically whether you use regular `ILogger.Log*()` or `[LoggerMessage]` — both produce standard `ILogger.Log<TState>(...)` calls that OTel intercepts.

**Marginal OTel benefit of `[LoggerMessage]`**: The stable `EventId` (derived from the method name hash) makes filtering OTel logs by event type more reliable in backends like Seq, Jaeger, or Grafana.

---

## 7. Microsoft's Official Position

- Microsoft publishes analyzer rule **CA1848**: *"Use the LoggerMessage delegates"*.
- The documentation states: **"Do not suppress a warning from this rule."**
- CA1848 is not enabled by default — opt in via `<AnalysisLevel>latest-recommended</AnalysisLevel>` in `Directory.Build.props`.
- Recommendation: enable as `suggestion` severity, migrate hot paths first.

---

## 8. Recommended Organization for Kakeibo

Two valid approaches — choose based on how many log calls exist per feature:

### Option A: Separate `*Logs.cs` file (preferred for features with 3+ log calls)

```
Features/
  Transactions/
    RecordTransaction/
      RecordTransactionEndpoint.cs
      RecordTransactionHandler.cs
      RecordTransactionValidator.cs
      RecordTransactionLogs.cs       ← log definitions here
```

```csharp
// RecordTransactionLogs.cs
namespace Kakeibo.Api.Features.Transactions.RecordTransaction;

internal static partial class RecordTransactionLogs
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Transaction {TransactionId} recorded in wallet {WalletId} by user {UserId}")]
    internal static partial void TransactionRecorded(
        this ILogger logger, Guid transactionId, Guid walletId, Guid userId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Wallet {WalletId} not found for user {UserId}")]
    internal static partial void WalletNotFound(
        this ILogger logger, Guid walletId, Guid userId);
}
```

### Option B: Private partial methods on the handler class (for 1–2 log calls)

```csharp
public sealed partial class EmailService(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<EmailService> logger) : IEmailService
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Verification email sent to {Email} for user {UserId}")]
    private partial void LogVerificationEmailSent(string email, Guid userId);

    public async Task SendEmailVerificationAsync(Guid userId, string email, string token, CancellationToken ct)
    {
        // ...
        LogVerificationEmailSent(email, userId); // logger is captured from primary constructor
    }
}
```

Note: When using instance partial methods, `ILogger` is captured from the primary constructor — no need to pass it explicitly.

---

## 9. Pros and Cons Summary

### `[LoggerMessage]` Source Generator

**Pros:**
- Zero heap allocations per call (no params array, no boxing).
- `IsEnabled` guard generated automatically — no work done when log level is disabled.
- Compile-time validation of message templates (SYSLIB diagnostics).
- Stable `EventId` per log call — better filtering in OTel/Seq backends.
- Full structured property preservation with Serilog.
- Unlimited parameters (unlike `LoggerMessage.Define`).
- Minimal boilerplate (attribute + partial method declaration).
- Endorsed by Microsoft (CA1848 rule).

**Cons:**
- Requires .NET 6+ (not a concern for this project on .NET 10).
- Does not support Serilog's `@` destructuring operator.
- Adds an extra file/class per feature (mitigated by Option B for small feature handlers).
- Methods must be `partial void` — slightly different calling convention.
- Cannot be used in anonymous methods or local functions.

### Regular `ILogger.LogInformation()` (current approach)

**Pros:**
- Familiar syntax, no boilerplate.
- Supports Serilog's `@` destructuring operator.
- Works with any .NET version.
- Fine for cold paths and low-traffic endpoints.

**Cons:**
- Allocates ~64–120 B per call (params array + boxing of value types).
- Allocates even when log level is disabled.
- No compile-time template validation.
- Triggers CA1848 analyzer warning on `latest-recommended` analysis level.
- ~2× slower than source generator on hot paths.

---

## 10. Migration Priority for Kakeibo

Based on actual usage in the codebase, suggested priority:

| File | Current Usage | Priority | Reason |
|------|--------------|----------|--------|
| `EventDispatcher.cs` | `LogError` in tight loop | **High** | Background service — runs continuously |
| `GenerateRecurringTransactionsJob.cs` | `LogInfo/Debug/Warning/Error` in batch loop | **High** | Processes N patterns per run |
| `EmailService.cs` | 8× `LogInformation` | Medium | Called on each email — moderate frequency |
| `ClickHouseAuditService.cs` | `LogDebug/Warning` in degraded path | Low | Only fires on connection failure |
| `WebPushService.cs` | `LogInfo/Warning` in catch blocks | Low | Error path only |
| `StorageService.cs` | 3× `LogInformation` | Low | Called infrequently |
| `InviteToWalletHandler.cs` | `LogError` in fire-and-forget | Low | Error path only |
| Notification event handlers | `LogWarning` in catch blocks | Low | Error paths only |

---

## 11. Enabling CA1848 (Optional)

To get compiler suggestions on all remaining `ILogger.Log*()` calls, add to `Directory.Build.props`:

```xml
<PropertyGroup>
  <!-- Enable latest recommended analyzers including CA1848 -->
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <!-- Or set CA1848 specifically as a suggestion (not error) -->
  <NoWarn>$(NoWarn)</NoWarn>
</PropertyGroup>
```

Or in `.editorconfig`:
```ini
dotnet_diagnostic.CA1848.severity = suggestion
```

---

## 12. References

- [Compile-time logging source generation — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- [High-performance logging — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging)
- [CA1848: Use the LoggerMessage delegates — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1848)
- [Improving logging performance with source generators — Andrew Lock](https://andrewlock.net/exploring-dotnet-6-part-8-improving-logging-performance-with-source-generators/)
- [High-Performance Logging in .NET 9 — GoatReview](https://goatreview.com/high-performance-logging-dotnet/)
- [Serilog performance optimization (MessageTemplateCache) — DeepWiki](https://deepwiki.com/serilog/serilog/6.2-performance-optimization)
- [dotnet/runtime#69490: LoggerMessage + Serilog @ destructuring](https://github.com/dotnet/runtime/issues/69490)
- [OpenTelemetry .NET Logs Correlation](https://opentelemetry.io/docs/languages/dotnet/logs/correlation/)
