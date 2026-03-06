# Global Exception Handlers in ASP.NET Core 10 — A Study for Kakeibo

## 1. Executive Summary

The Kakeibo API currently has **zero global exception handling**. Business errors are handled
cleanly through `Result<T>` and `Error`, but any infrastructure failure — a lost database
connection, an EF Core concurrency conflict, a cancelled HTTP request — escapes the pipeline
as an unstructured response: empty body in Production, an HTML error page in Development.

**Recommendation:** adopt a three-layer defense strategy:

| Layer | Mechanism | Purpose |
|-------|-----------|---------|
| 1 | `AddProblemDetails()` + `UseProblemDetails` middleware | Formats any unhandled status-code response into RFC 9457 JSON |
| 2 | `IExceptionHandler` instances (registered, ordered) | Converts known exception types to structured Problem Details |
| 3 | Catch-all `UnhandledExceptionHandler` | Guarantees every remaining exception becomes a logged, structured 500 |

This approach requires **no changes to existing endpoint handlers** — the `Result<T>` pattern
is untouched. It adds defense-in-depth for the cases `Result<T>` was never designed to cover.

---

## 2. Current State Analysis

### 2.1 What Happens Today When an Exception Escapes

When an unhandled exception propagates past all middleware in a Minimal API application:

**Development environment (`ASPNETCORE_ENVIRONMENT=Development`):**
```
HTTP/1.1 500 Internal Server Error
Content-Type: text/html; charset=utf-8

<!DOCTYPE html>
<html lang="en">
<head>...</head>
<body>
  <h1>An unhandled exception occurred while processing the request.</h1>
  ...full stack trace rendered as HTML...
</body>
</html>
```

This is the `UseDeveloperExceptionPage()` middleware that ASP.NET Core adds automatically
in Development. It leaks stack traces, assembly paths, and connection strings.

**Production environment:**
```
HTTP/1.1 500 Internal Server Error
Content-Length: 0
```

An empty body. The HTTP status is correct but there is no body, no `Content-Type`, and no
structured data the frontend or API client can parse. The exception is logged by the Kestrel
pipeline at `LogLevel.Error` with the default host logger, but with no request context
(no request ID, no user ID, no trace correlation).

### 2.2 What `Result<T>` Covers (and Does Not Cover)

The `Result<T>` pattern handles **business logic errors** at handler boundaries:

```csharp
// This is covered — handler returns a Result, endpoint maps it
var result = await handler.HandleAsync(request, ct);
return result.IsFailure
    ? result.Error.Code switch { ... }
    : TypedResults.Ok(result.Value);
```

`Result<T>` does **not** cover:

| Exception Type | Scenario | Current Outcome |
|----------------|----------|-----------------|
| `NpgsqlException` | PostgreSQL connection dropped mid-request | 500 empty body |
| `DbUpdateConcurrencyException` | Two handlers update same row simultaneously | 500 empty body |
| `DbUpdateException` | Unique constraint violation not caught by handler | 500 empty body |
| `OperationCanceledException` | Browser tab closed, mobile app backgrounded | 500 empty body |
| `TaskCanceledException` | HTTP client timeout, cancellation propagated | 500 empty body |
| `SocketException` | Network failure during SMTP send | 500 empty body |
| `InvalidOperationException` | DI scope error, misconfigured service | 500 empty body |
| `StackOverflowException` | Circular dependency or deep recursion | Process crash |

### 2.3 Logging Gap

When an exception produces an empty-body 500, the Kestrel host logger emits:

```
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HN1...", Request id "0HN1...:00000001": An unhandled exception occurred while processing the HTTP request.
      System.NullReferenceException: Object reference not set to an instance of an object.
         at ...
```

What is **missing** from this log entry:
- Request path and method
- Authenticated user ID
- Trace/span correlation ID
- Structured properties for alerting (`{UserId}`, `{WalletId}`, `{ExceptionType}`)
- Event ID (cannot set alert thresholds without a stable event ID)

The `[LoggerMessage]` source generator pattern used throughout Kakeibo cannot be applied
to ad-hoc exceptions because there is no interception point.

---

## 3. Pros of Global Exception Handlers

### 3.1 Structured Logging Context

A dedicated handler can capture everything relevant to the request at the point the exception
escapes:

```csharp
// Inside an IExceptionHandler implementation:
logger.UnhandledException(
    exception,
    httpContext.Request.Method,
    httpContext.Request.Path,
    httpContext.TraceIdentifier
);
```

This produces a log entry with all structured properties intact, queryable in any log
aggregator (Seq, Loki, OpenSearch). Without a handler, the host-level log has none of these.

### 3.2 Consistent Client Response

Clients (the Vue frontend, mobile apps, third-party integrations) can rely on a single
error shape. When an exception escapes today, the frontend receives either an empty body
(Production) or HTML (Development) — both crash any `JSON.parse()` call in Axios interceptors.

A global handler ensures every error response is RFC 9457 Problem Details JSON:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Use the traceId to correlate logs.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

### 3.3 Client Disconnect Handling

`OperationCanceledException` is thrown when the client disconnects or the request
`CancellationToken` fires. These are not server errors — they are normal lifecycle events.
Without handling, they appear in logs as errors and inflate error rate metrics.

A dedicated `OperationCanceledExceptionHandler` can:
1. Respond with 499 (client closed request — a de-facto standard used by Nginx/Cloudflare)
2. Log at `Information` level (not `Error`) since no server fault occurred

### 3.4 EF Core Concurrency Conflicts

`DbUpdateConcurrencyException` is an expected race condition in any multi-user system.
It means two requests modified the same row simultaneously. The correct HTTP response is
`409 Conflict`, prompting the client to retry. Without a handler, the client receives 500
and has no signal to differentiate a retry-able conflict from a fatal server error.

### 3.5 Defense-in-Depth

The `Result<T>` pattern is excellent but relies on every handler returning a `Result` rather
than throwing. New developers may `throw` in a handler. Third-party library code always throws.
Global handlers are a safety net that catches what the application layer misses.

### 3.6 Centralized Security

Without a handler, stack traces reach the client in Development and potentially leak
sensitive information (connection strings, file paths, assembly names) if the environment
variable is misconfigured in Production. A handler ensures internal details are never
serialized to the response body.

---

## 4. Cons and Tradeoffs

### 4.1 Risk of Masking Bugs

A poorly-implemented catch-all can swallow exceptions that should propagate, making bugs
harder to diagnose. Mitigation: **always log at `Error` level with the full exception** in
the catch-all handler, never just return 500 silently.

```csharp
// Bad — swallows the exception, no log
catch (Exception)
{
    return TypedResults.Problem(statusCode: 500);
}

// Good — logs everything, then returns structured response
catch (Exception ex)
{
    logger.UnhandledException(ex, method, path, traceId);
    return TypedResults.Problem(statusCode: 500);
}
```

### 4.2 Handler Ordering Matters

`IExceptionHandler` implementations are invoked in registration order. If the catch-all
(`UnhandledExceptionHandler`) is registered before a specific handler
(`DbUpdateConcurrencyExceptionHandler`), the specific handler never runs.

Rule: **Register specific handlers first, catch-all last.**

### 4.3 Exception Type Granularity

It is tempting to write many fine-grained handlers (`NpgsqlExceptionHandler`,
`SocketExceptionHandler`, `HttpRequestExceptionHandler`). This creates maintenance burden
for limited benefit — most infrastructure exceptions are not distinguishable from the
client's perspective and all map to 500.

Rule: **Handle only exception types that produce a different HTTP status or log level.**
Everything else belongs in the catch-all.

### 4.4 Interaction with Cancellation in Middleware

`OperationCanceledException` can fire during response body write (not just handler execution).
If the response has already started (headers sent), `IExceptionHandler` cannot change the
status code. The framework will reset the connection instead.

This is acceptable behavior. The `OperationCanceledExceptionHandler` should check
`httpContext.Response.HasStarted` and skip if true, letting the framework handle teardown.

### 4.5 Not a Substitute for `Result<T>`

Global handlers are for **unexpected** errors. Known business conditions (wallet not found,
duplicate email, insufficient balance) should still use `Result<T>`. Using exceptions for
flow control is an anti-pattern and degrades performance due to stack trace capture.

---

## 5. ASP.NET Core Exception Handling Mechanisms

### 5.1 `UseExceptionHandler(path)` — Legacy

```csharp
app.UseExceptionHandler("/error");
app.MapGet("/error", (HttpContext ctx) =>
    ctx.Features.Get<IExceptionHandlerFeature>()?.Error is var ex
    ? TypedResults.Problem(ex?.Message)
    : TypedResults.Problem());
```

**Cons:**
- Re-executes a route, adding unnecessary latency
- The `/error` endpoint must be excluded from authentication, rate limiting, etc.
- Cannot easily return different status codes per exception type
- Awkward to inject services into the error handler

**Not recommended** for new Kakeibo code. Exists primarily for MVC compatibility.

### 5.2 `IExceptionHandler` (ASP.NET Core 8+) — Recommended

```csharp
public interface IExceptionHandler
{
    ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken);
}
```

Returns `true` if the handler handled the exception (stops the chain).
Returns `false` to pass to the next handler.

**Features:**
- Ordered via registration sequence
- Full DI support — inject `ILogger`, `IProblemDetailsService`, etc.
- Access to `HttpContext` — read headers, user claims, trace IDs
- Can be unit-tested independently
- Composable — specific handlers check `exception is SpecificType` and delegate otherwise

**Registration:**
```csharp
builder.Services.AddExceptionHandler<OperationCanceledExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateConcurrencyExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();
```

**Middleware activation:**
```csharp
app.UseExceptionHandler();  // No path — uses registered IExceptionHandler implementations
```

### 5.3 `AddProblemDetails()` + `IStatusCodePagesMiddleware`

`AddProblemDetails()` configures the `IProblemDetailsService` that formats `ProblemDetails`
objects. It also enables automatic Problem Details responses for status codes without bodies:

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        // Custom extensions added here
    };
});

app.UseStatusCodePages(); // Converts 404/405/etc with empty bodies to Problem Details
```

`AddProblemDetails()` does **not** handle exceptions. It only formats status codes.
Combined with `IExceptionHandler`, it provides the full solution.

### 5.4 Custom Middleware

```csharp
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception ex)
    {
        // Handle here
    }
});
```

**Cons:**
- Cannot be unit-tested in isolation
- Not composable — must handle all exception types in one block
- Does not participate in DI scoped lifetime cleanly
- Less readable than `IExceptionHandler`

**Verdict:** Use only if targeting ASP.NET Core < 8. For .NET 10, prefer `IExceptionHandler`.

### 5.5 Endpoint Filter / `EndpointFilterFactory`

Not suitable for exception handling — filters execute inside the endpoint invocation,
not outside the middleware pipeline. They catch exceptions from the handler but not from
earlier middleware.

### 5.6 Comparison Table

| Mechanism | .NET Version | DI Support | Composable | Testable | Recommended |
|-----------|-------------|------------|------------|----------|-------------|
| `UseExceptionHandler(path)` | All | Partial | No | No | Legacy only |
| Custom middleware | All | Yes | No | No | Pre-.NET 8 |
| `IExceptionHandler` | 8+ | Yes | Yes | Yes | ✅ Yes |
| `AddProblemDetails()` | 7+ | Yes | N/A | Yes | ✅ Complement |
| Endpoint filter | All | Yes | Yes | Yes | Not for exceptions |

---

## 6. Recommended Handlers for Kakeibo

### 6.1 Handler 1: `OperationCanceledExceptionHandler`

**Trigger:** Client disconnects (browser tab closed, mobile app backgrounded) or request
timeout fires. The `CancellationToken` is signalled, and downstream code throws
`OperationCanceledException` or `TaskCanceledException` (a subclass).

**HTTP Status:** 499 — "Client Closed Request" (Nginx de-facto standard, understood by
Cloudflare, AWS ALB, and observability tools).

**Log Level:** `Information` — this is normal lifecycle, not a server fault.

**Not logged as error:** Avoids polluting error rate dashboards with client behavior.

```csharp
// src/Kakeibo.Api/Infrastructure/Exceptions/OperationCanceledExceptionHandler.cs
namespace Kakeibo.Api.Infrastructure.Exceptions;

// Handles client disconnects and cancellation by returning 499 at Information severity.
internal sealed class OperationCanceledExceptionHandler(ILogger<OperationCanceledExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not OperationCanceledException)
            return false;

        // Response may have already started if cancellation fired during body write
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode = 499;
        }

        logger.ClientDisconnected(httpContext.Request.Method, httpContext.Request.Path);
        return true;
    }
}
```

```csharp
// src/Kakeibo.Api/Infrastructure/Exceptions/ExceptionHandlerLogs.cs
namespace Kakeibo.Api.Infrastructure.Exceptions;

internal static partial class ExceptionHandlerLogs
{
    [LoggerMessage(1601, LogLevel.Information,
        "Client disconnected during {Method} {Path}")]
    internal static partial void ClientDisconnected(
        this ILogger logger, string method, string path);

    [LoggerMessage(1602, LogLevel.Warning,
        "EF Core concurrency conflict during {Method} {Path} — returning 409")]
    internal static partial void ConcurrencyConflict(
        this ILogger logger, string method, string path, Exception exception);

    [LoggerMessage(1603, LogLevel.Error,
        "Unhandled exception during {Method} {Path} — TraceId: {TraceId}")]
    internal static partial void UnhandledException(
        this ILogger logger, string method, string path, string traceId, Exception exception);
}
```

### 6.2 Handler 2: `DbUpdateConcurrencyExceptionHandler`

**Trigger:** Two requests attempt to update the same entity row simultaneously, and EF Core
concurrency tokens (e.g., `xmin` in PostgreSQL or an explicit `RowVersion` column) detect
the conflict.

**HTTP Status:** 409 Conflict — the client can retry with fresh data.

**Log Level:** `Warning` — expected race condition, not a server fault, but worth monitoring
for frequency spikes that indicate design issues.

**Detail message:** Should not expose internal entity names. Generic message pointing client
to retry.

```csharp
// src/Kakeibo.Api/Infrastructure/Exceptions/DbUpdateConcurrencyExceptionHandler.cs
namespace Kakeibo.Api.Infrastructure.Exceptions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Handles EF Core concurrency conflicts by returning 409 Conflict with a retry hint.
internal sealed class DbUpdateConcurrencyExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DbUpdateConcurrencyExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
            return false;

        logger.ConcurrencyConflict(
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrency Conflict",
            Detail = "The resource was modified by another request. Please refresh and retry.",
            Type = "https://kakeibo.local/errors/concurrency-conflict"
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
```

### 6.3 Handler 3: `UnhandledExceptionHandler` (Catch-All)

**Trigger:** Any exception not handled by earlier `IExceptionHandler` implementations.

**HTTP Status:** 500 Internal Server Error.

**Log Level:** `Error` — full exception with stack trace, structured properties for correlation.

**Response body:** Generic message referencing the `traceId`. Never expose internal details.

**Registration:** Must be last in the DI registration order.

```csharp
// src/Kakeibo.Api/Infrastructure/Exceptions/UnhandledExceptionHandler.cs
namespace Kakeibo.Api.Infrastructure.Exceptions;

using Microsoft.AspNetCore.Mvc;

// Catch-all handler: logs every unhandled exception at Error level and returns 500.
// Must be registered last so specific handlers run first.
internal sealed class UnhandledExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<UnhandledExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Always log with full exception context for correlation
        logger.UnhandledException(
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier,
            exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = $"An unexpected error occurred. Use traceId '{httpContext.TraceIdentifier}' to correlate logs.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
```

---

## 7. Integration with `[LoggerMessage]` Pattern

### 7.1 EventId Range

Per `logs.md`, the range **1600–1699** is reserved for exception handlers:

| EventId | Method | Level | Description |
|---------|--------|-------|-------------|
| 1601 | `ClientDisconnected` | Information | Client closed request (499) |
| 1602 | `ConcurrencyConflict` | Warning | EF concurrency conflict (409) |
| 1603 | `UnhandledException` | Error | Any unhandled exception (500) |

### 7.2 Complete `ExceptionHandlerLogs.cs`

```csharp
// src/Kakeibo.Api/Infrastructure/Exceptions/ExceptionHandlerLogs.cs
namespace Kakeibo.Api.Infrastructure.Exceptions;

// [LoggerMessage] source-generated delegates for exception handler logging.
// EventId range: 1600–1699
internal static partial class ExceptionHandlerLogs
{
    // 1601: Client disconnected — not a server fault, log at Information
    [LoggerMessage(1601, LogLevel.Information,
        "Client disconnected during {Method} {Path}")]
    internal static partial void ClientDisconnected(
        this ILogger logger,
        string method,
        string path);

    // 1602: EF Core concurrency conflict — expected race, log at Warning with exception
    [LoggerMessage(1602, LogLevel.Warning,
        "EF Core concurrency conflict during {Method} {Path}")]
    internal static partial void ConcurrencyConflict(
        this ILogger logger,
        string method,
        string path,
        Exception exception);

    // 1603: Unhandled exception — server fault, log at Error with full context
    [LoggerMessage(1603, LogLevel.Error,
        "Unhandled exception during {Method} {Path} — TraceId: {TraceId}")]
    internal static partial void UnhandledException(
        this ILogger logger,
        string method,
        string path,
        string traceId,
        Exception exception);
}
```

### 7.3 Why Extension Method Syntax for `ILogger`

The `this ILogger logger` pattern (extension method) enables the fluent call style:
```csharp
logger.UnhandledException(method, path, traceId, exception);
```
rather than the static call:
```csharp
ExceptionHandlerLogs.UnhandledException(logger, method, path, traceId, exception);
```

The generated code is identical — only the call site readability differs. This is the
Kakeibo convention established in `logs.md`.

### 7.4 Why `Exception` as Last Parameter

When `Exception exception` appears as the last parameter in a `[LoggerMessage]` method,
the source generator attaches it as a first-class exception to the log entry. This means:
- Serilog captures `.ExceptionDetail` with full stack trace
- OpenTelemetry records it as a span exception event
- Log aggregators can group by exception type automatically

If `exception` is not the last parameter, it is treated as a regular structured property
(only `.ToString()` is captured, losing stack trace and inner exceptions).

---

## 8. Integration with OpenTelemetry

### 8.1 Exception Handlers and Trace Spans

When a request enters the ASP.NET Core pipeline, OpenTelemetry's `HttpInListener`
(from `OpenTelemetry.Instrumentation.AspNetCore`) creates an activity (span). If the
request fails with an unhandled exception before the `IExceptionHandler` intercepts it,
the activity is marked as faulted by the framework automatically.

**However**, when `IExceptionHandler` intercepts and returns a structured response, the
framework considers the exception "handled" — but the span status may not reflect the
HTTP 500 status correctly depending on the OTel instrumentation version.

**Best practice:** Manually set span status inside exception handlers:

```csharp
using System.Diagnostics;

// Inside UnhandledExceptionHandler.TryHandleAsync:
Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
Activity.Current?.RecordException(exception);
```

### 8.2 `RecordException` vs Manual Span Events

`Activity.Current?.RecordException(exception)` records an OpenTelemetry exception event
following the [semantic conventions](https://opentelemetry.io/docs/specs/semconv/exceptions/):

```
event: "exception"
  exception.type: "System.NullReferenceException"
  exception.message: "Object reference not set..."
  exception.stacktrace: "..."
  exception.escaped: true
```

This is the correct approach when the exception is being handled (escaped = true means
the exception propagated beyond the current span's scope before being caught).

### 8.3 TraceId Correlation

The `httpContext.TraceIdentifier` used in the `UnhandledExceptionHandler` is the ASP.NET
Core request trace identifier, **not** the OpenTelemetry `TraceId`. For full correlation,
include the OTel TraceId in the response:

```csharp
// Get the OTel TraceId (W3C format: 32-char hex)
var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
var spanId = Activity.Current?.SpanId.ToString();

problemDetails.Extensions["traceId"] = traceId;
problemDetails.Extensions["spanId"] = spanId;
```

The OTel `TraceId` correlates directly with traces in Jaeger, Tempo, and the Aspire
Dashboard (`http://localhost:18888`), while `httpContext.TraceIdentifier` only correlates
with ASP.NET Core host logs.

**Recommended:** Use OTel `TraceId` when available, fall back to `httpContext.TraceIdentifier`.

### 8.4 Updated `UnhandledExceptionHandler` with Full OTel Integration

```csharp
// src/Kakeibo.Api/Infrastructure/Exceptions/UnhandledExceptionHandler.cs
namespace Kakeibo.Api.Infrastructure.Exceptions;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

// Catch-all handler: logs every unhandled exception, sets OTel span status, returns 500.
internal sealed class UnhandledExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<UnhandledExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Prefer OTel TraceId for correlation with distributed traces
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        // Mark the OTel span as faulted and attach the exception event
        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity.Current?.RecordException(exception);

        logger.UnhandledException(
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId,
            exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = $"An unexpected error occurred. Use traceId '{traceId}' to correlate logs.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
```

---

## 9. Migration Strategy

### 9.1 Overview

The migration is **non-breaking**: no existing endpoints change, no `Result<T>` patterns
are modified. The changes are purely additive — new infrastructure registrations and new
handler files.

**Steps:**
1. Register `AddProblemDetails()` and exception handlers in `Program.cs`
2. Add the `UseExceptionHandler()` middleware
3. Create the three handler files and `ExceptionHandlerLogs.cs`

### 9.2 Step 1 — Service Registration

Locate the infrastructure services block in `Program.cs` and add:

```csharp
// Infrastructure — problem details + exception handlers
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // Ensure every problem details response includes the trace ID
        var traceId = Activity.Current?.TraceId.ToString()
            ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions.TryAdd("traceId", traceId);
    };
});

// Register in order: specific handlers first, catch-all last
builder.Services.AddExceptionHandler<OperationCanceledExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateConcurrencyExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();
```

### 9.3 Step 2 — Middleware Registration

Middleware order in `Program.cs` matters. `UseExceptionHandler()` must appear **early**
in the pipeline — before authentication, authorization, and routing — so it can intercept
exceptions from any subsequent middleware:

```csharp
var app = builder.Build();

// Exception handling must be outermost (before all other middleware)
app.UseExceptionHandler();
app.UseStatusCodePages(); // Converts empty 404/405 etc to Problem Details JSON

// ... rest of middleware (UseAuthentication, UseAuthorization, MapEndpoints, etc.)
```

### 9.4 Step 3 — Create Handler Files

Create the folder and files:

```
src/Kakeibo.Api/Infrastructure/Exceptions/
├── ExceptionHandlerLogs.cs
├── OperationCanceledExceptionHandler.cs
├── DbUpdateConcurrencyExceptionHandler.cs
└── UnhandledExceptionHandler.cs
```

No changes to existing files under `Features/` or `Common/`.

### 9.5 Verification Checklist

After implementation, verify:

- [ ] `bun run api:build` — compiles without warnings (`TreatWarningsAsErrors` enabled)
- [ ] `bun run api:test` — existing tests pass, no regressions
- [ ] Manual test: kill the PostgreSQL container mid-request → confirm 500 with JSON body and `traceId`
- [ ] Manual test: cancel request from browser → confirm 499 response code and `Information` log
- [ ] Manual test: trigger a concurrency conflict → confirm 409 with `"type": "concurrency-conflict"`
- [ ] Aspire Dashboard (`http://localhost:18888`): spans for 500 responses show `Error` status with exception event

---

## 10. Complete Reference Implementation

The following represents the complete, compilable implementation following all Kakeibo
conventions (primary constructors, file-scoped namespaces, `internal sealed`, `[LoggerMessage]`).

### 10.1 Directory Structure

```
src/Kakeibo.Api/
└── Infrastructure/
    └── Exceptions/
        ├── ExceptionHandlerLogs.cs
        ├── OperationCanceledExceptionHandler.cs
        ├── DbUpdateConcurrencyExceptionHandler.cs
        └── UnhandledExceptionHandler.cs
```

### 10.2 `ExceptionHandlerLogs.cs`

```csharp
namespace Kakeibo.Api.Infrastructure.Exceptions;

// [LoggerMessage] source-generated logging delegates for all exception handlers.
// EventId range: 1600–1699 (reserved in logs.md)
internal static partial class ExceptionHandlerLogs
{
    // 1601: Client disconnected — not an error, suppress from error metrics
    [LoggerMessage(1601, LogLevel.Information,
        "Client disconnected during {Method} {Path}")]
    internal static partial void ClientDisconnected(
        this ILogger logger,
        string method,
        string path);

    // 1602: EF Core concurrency conflict — expected race condition
    [LoggerMessage(1602, LogLevel.Warning,
        "EF Core concurrency conflict during {Method} {Path}")]
    internal static partial void ConcurrencyConflict(
        this ILogger logger,
        string method,
        string path,
        Exception exception);

    // 1603: Unhandled exception — full context for post-incident correlation
    [LoggerMessage(1603, LogLevel.Error,
        "Unhandled exception during {Method} {Path} — TraceId: {TraceId}")]
    internal static partial void UnhandledException(
        this ILogger logger,
        string method,
        string path,
        string traceId,
        Exception exception);
}
```

### 10.3 `OperationCanceledExceptionHandler.cs`

```csharp
namespace Kakeibo.Api.Infrastructure.Exceptions;

// Returns 499 (client closed request) for OperationCanceledException.
// Logged at Information level — not a server fault.
internal sealed class OperationCanceledExceptionHandler(
    ILogger<OperationCanceledExceptionHandler> logger)
    : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not OperationCanceledException)
            return ValueTask.FromResult(false);

        // Cannot modify response if headers were already sent (body streaming in progress)
        if (!httpContext.Response.HasStarted)
            httpContext.Response.StatusCode = 499;

        logger.ClientDisconnected(httpContext.Request.Method, httpContext.Request.Path);
        return ValueTask.FromResult(true);
    }
}
```

### 10.4 `DbUpdateConcurrencyExceptionHandler.cs`

```csharp
namespace Kakeibo.Api.Infrastructure.Exceptions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Returns 409 Conflict for EF Core concurrency conflicts.
// Logged at Warning level — expected race condition, not a fatal error.
internal sealed class DbUpdateConcurrencyExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DbUpdateConcurrencyExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
            return false;

        logger.ConcurrencyConflict(
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception);

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrency Conflict",
            Detail = "The resource was modified by another request. Refresh the resource and retry.",
            Type = "https://kakeibo.local/errors/concurrency-conflict"
        };
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
```

### 10.5 `UnhandledExceptionHandler.cs`

```csharp
namespace Kakeibo.Api.Infrastructure.Exceptions;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

// Catch-all handler for any exception not handled by earlier IExceptionHandler implementations.
// Logs at Error level with full OTel trace correlation. Must be registered last.
internal sealed class UnhandledExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<UnhandledExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Prefer OTel TraceId — correlates with Aspire Dashboard, Jaeger, Tempo
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        // Mark OTel span as faulted and attach the exception event
        Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
        Activity.Current?.RecordException(exception);

        logger.UnhandledException(
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId,
            exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = $"An unexpected error occurred. Use traceId '{traceId}' to correlate logs.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };
        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
```

### 10.6 `Program.cs` Changes (excerpt)

```csharp
// ─── Infrastructure — Problem Details + Exception Handlers ────────────────────
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.TraceId.ToString()
            ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions.TryAdd("traceId", traceId);
    };
});

// Register specific handlers first — catch-all must be last
builder.Services.AddExceptionHandler<OperationCanceledExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateConcurrencyExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

// ─── Build + Middleware ────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();   // Activates IExceptionHandler implementations
app.UseStatusCodePages();    // Converts empty 404/405/etc to Problem Details JSON

// ... remainder of middleware
```

---

## 11. FAQ and Edge Cases

### Q: Does `UseStatusCodePages()` interfere with existing endpoint responses?

No. `UseStatusCodePages()` only activates when the response has **no body** and a status
code in the 4xx/5xx range. Endpoints that return `TypedResults.NotFound(Error)` already
have a body (the `Error` JSON), so `UseStatusCodePages()` is skipped.

### Q: What happens if a handler throws inside `TryHandleAsync`?

ASP.NET Core's exception handler middleware has a safety net: if an `IExceptionHandler`
itself throws, the framework catches that exception and moves to the next handler. If all
handlers throw, the framework emits an empty 500. This is acceptable since the original
exception would already have been logged by any handler that partially executed.

To be safe, handlers should not throw. All fallible operations (like `WriteAsync`) should
be awaited inside the handler's own try/catch if needed.

### Q: Should `OperationCanceledException` suppress all logging?

Only downgrade to `Information`. Never suppress entirely — a spike in client disconnects
can indicate a performance problem (slow endpoints forcing timeouts) and should be
discoverable via log volume metrics.

### Q: Can I add more specific handlers later (e.g., `NpgsqlExceptionHandler`)?

Yes. Register before `UnhandledExceptionHandler`. Since the catch-all always returns `true`,
it must remain last. New specific handlers inserted anywhere before it will be evaluated first.

### Q: Does this conflict with Serilog's `UseSerilogRequestLogging()`?

No. `UseSerilogRequestLogging()` is a request pipeline middleware that logs completed
requests. `IExceptionHandler` runs earlier and handles the exception before the request
completes. Serilog will still log the request completion (with status 500 or 409) via
its request logging enricher. Both mechanisms are complementary.

---

## 12. References

- [ASP.NET Core exception handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [IExceptionHandler interface (ASP.NET Core 8+)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.diagnostics.iexceptionhandler)
- [Problem Details for HTTP APIs — RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)
- [AddProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.problemdetailsservicecollectionextensions.addproblemdetails)
- [OpenTelemetry semantic conventions — exceptions](https://opentelemetry.io/docs/specs/semconv/exceptions/)
- [Nginx 499 status code](https://nginx.org/en/docs/http/ngx_http_upstream_module.html)
- [LoggerMessage source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
