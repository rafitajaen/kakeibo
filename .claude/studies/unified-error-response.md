# Unified Error Response with RFC 9457 Problem Details — A Study for Kakeibo

## 1. Executive Summary

The Kakeibo API currently emits **seven different error response formats** across its
endpoints. Some are RFC 7807 Problem Details, some are custom `Error` JSON, some are raw
strings, and some are empty bodies. The Vue frontend cannot parse these consistently —
any `axios.interceptors.response` that handles errors must guess the response shape based
on status code, which is fragile and breaks when endpoints change.

**Recommendation:** Converge all error responses to a single RFC 9457 Problem Details
shape. The migration has four phases (A→D) and is backward-compatible: the frontend
change is isolated to Axios interceptors, not every `catch` block in every store.

**Proposed canonical shape:**
```json
{
  "type": "https://kakeibo.local/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Wallet 'abc' was not found or you do not have access.",
  "instance": "/api/wallets/abc",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errorCode": "not_found"
}
```

**Key decisions:**
- `AddProblemDetails()` provides the serialization infrastructure
- A `Result<T>` extension method maps `Error` → `ProblemDetails` in one place
- Endpoint switch expressions become `result.ToProblemResult(httpContext)` one-liners
- `errorCode` extension field preserves the existing `Error.Code` for frontend logic

---

## 2. RFC 7807 and RFC 9457 Standard

### 2.1 History

| Version | Published | Key Change |
|---------|-----------|------------|
| RFC 7807 | 2016 | Original "Problem Details for HTTP APIs" |
| RFC 9457 | 2023 | Supersedes RFC 7807; clarifies extension fields, `type` URI semantics |

RFC 9457 is a minor update to RFC 7807. The fields are the same; the language around
`type` URIs and extension fields is clarified. Libraries built for RFC 7807 are fully
compatible with RFC 9457 responses.

### 2.2 Standard Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | URI | No (default: `about:blank`) | URI identifying the problem type. Should be a stable URL pointing to human-readable docs. |
| `title` | string | No | Short, human-readable summary of the problem type. **Must not change** between occurrences of the same type. |
| `status` | integer | No (recommended) | HTTP status code. MUST match the response status code. |
| `detail` | string | No | Human-readable explanation specific to **this occurrence**. May differ between occurrences of the same type. |
| `instance` | URI | No | URI identifying the specific occurrence. Typically the request path. |

### 2.3 Extension Fields

RFC 9457 explicitly allows additional members beyond the standard five. Extensions must
not conflict with the standard field names. Common extensions:

| Extension | Purpose |
|-----------|---------|
| `errors` | Validation errors map (`{ "fieldName": ["error1", "error2"] }`) — used by ASP.NET Core `ValidationProblemDetails` |
| `traceId` | Correlation ID for log lookup |
| `errorCode` | Machine-readable error code for client switch logic |
| `spanId` | OpenTelemetry span ID for distributed trace correlation |

### 2.4 `type` URI Semantics

RFC 9457 §3.1: The `type` URI SHOULD be dereferenceable to a human-readable document
describing the problem type. It MUST be a URI (not a URL with a valid host if using
`https://` — but `localhost` URIs are acceptable for internal APIs).

For Kakeibo:
- Production errors: `https://kakeibo.app/errors/{slug}`
- Local/test errors: `https://kakeibo.local/errors/{slug}`
- Standards-defined errors (500): `https://tools.ietf.org/html/rfc9110#section-15.6.1`

The `about:blank` default (omitting `type`) is acceptable but loses the ability to link
to documentation and makes client-side error classification harder.

### 2.5 Relationship to ASP.NET Core `ProblemDetails`

.NET's `Microsoft.AspNetCore.Mvc.ProblemDetails` class models RFC 9457 directly:

```csharp
public class ProblemDetails
{
    public string? Type { get; set; }        // → "type"
    public string? Title { get; set; }       // → "title"
    public int? Status { get; set; }         // → "status"
    public string? Detail { get; set; }      // → "detail"
    public string? Instance { get; set; }    // → "instance"
    public IDictionary<string, object?> Extensions { get; }  // → any extra fields
}
```

`ValidationProblemDetails` extends it with an `Errors` dictionary mapped to the `errors`
extension field.

---

## 3. Current Inconsistency Audit

### 3.1 The Seven Formats

| # | Method Call | HTTP Status | Response Body |
|---|-------------|-------------|---------------|
| 1 | `TypedResults.BadRequest(Error)` | 400 | `{ "code": "validation", "message": "..." }` |
| 2 | `TypedResults.NotFound(Error)` | 404 | `{ "code": "not_found", "message": "..." }` |
| 3 | `TypedResults.Conflict(Error)` | 409 | `{ "code": "conflict", "message": "..." }` |
| 4 | `TypedResults.Forbidden()` | 403 | *(empty body)* |
| 5 | `Results.ValidationProblem(dict)` | 422 | RFC 7807 `ValidationProblemDetails` |
| 6 | `TypedResults.Problem(message, statusCode: 500)` | 500 | Partial ProblemDetails |
| 7 | Unhandled exception | 500 | *(empty body)* |

### 3.2 Format 1: `TypedResults.BadRequest(Error)` — Custom `Error` JSON

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "code": "validation",
  "message": "Email address is already registered."
}
```

**Problem:** Not RFC 9457. No `type`, `title`, `status`, `instance`, or `traceId`. The
`code` field is the only machine-readable identifier. Frontend must parse `error.response.data.code`.

### 3.3 Format 2: `TypedResults.NotFound(Error)` — Custom `Error` JSON

```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "code": "not_found",
  "message": "Wallet '3d7f...' was not found or you do not have access."
}
```

**Problem:** Same as Format 1. Fields `code` and `message` are not RFC 9457 fields.

### 3.4 Format 3: `TypedResults.Conflict(Error)` — Custom `Error` JSON

```http
HTTP/1.1 409 Conflict
Content-Type: application/json

{
  "code": "conflict",
  "message": "A wallet named 'Savings' already exists."
}
```

**Problem:** Same as Formats 1 and 2.

### 3.5 Format 4: `TypedResults.Forbidden()` — Empty Body

```http
HTTP/1.1 403 Forbidden
Content-Length: 0
```

**Problem:** No body at all. The frontend cannot distinguish "not authenticated" (401)
from "authenticated but not authorized" (403) if it relies on body content. Cannot be
parsed. Error handling must `if (status === 403)` with no further detail.

### 3.6 Format 5: `Results.ValidationProblem(dict)` — RFC 7807

```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "Name": ["'Name' must not be empty.", "'Name' must be at most 100 characters."],
    "Amount": ["'Amount' must be greater than 0.01."]
  }
}
```

**Relative strength:** This is the closest to RFC 9457. Has `type`, `title`, `status`,
and `errors`. Missing: `instance`, `traceId`, `errorCode`.

**Problem:** Uses `422 Unprocessable Entity`. The HTTP status for FluentValidation failures
should be `400 Bad Request` — `422` is for semantically valid but unprocessable content
(e.g., XML schema violations). However, `422` is widely used by frameworks and clients
expect it, so this is a low-priority inconsistency.

### 3.7 Format 6: `TypedResults.Problem(message, statusCode: 500)` — Partial ProblemDetails

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json

{
  "status": 500,
  "detail": "An internal error occurred."
}
```

**Problem:** Missing `type`, `title`, `instance`, and `traceId`. Without `traceId`,
support staff cannot correlate the client's reported error with server logs.

### 3.8 Format 7: Unhandled Exception — Empty Body

```http
HTTP/1.1 500 Internal Server Error
Content-Length: 0
```

**Problem:** Nothing. The client has no information. `JSON.parse` throws. Axios interceptors
crash. As discussed in `global-exception-handlers.md`, this is resolved by implementing
`IExceptionHandler`.

### 3.9 Impact on the Vue Frontend

The current Axios error interceptor must handle all seven shapes:

```typescript
// Current (fragile) — each condition added as a new format was discovered
axios.interceptors.response.use(null, (error: AxiosError) => {
  const data = error.response?.data
  if (typeof data === 'string') {
    // Format: raw string
    showError(data)
  } else if (data?.errors) {
    // Format: ValidationProblem
    const messages = Object.values(data.errors).flat()
    showError(messages.join(', '))
  } else if (data?.message) {
    // Format: Error { code, message }
    showError(data.message)
  } else if (data?.detail) {
    // Format: ProblemDetails
    showError(data.detail)
  } else {
    // Format: empty body
    showError('An unexpected error occurred.')
  }
})
```

After unification, this collapses to:

```typescript
// After unification — single shape, always
axios.interceptors.response.use(null, (error: AxiosError<ProblemDetails>) => {
  const detail = error.response?.data?.detail ?? 'An unexpected error occurred.'
  const errorCode = error.response?.data?.errorCode
  showError(detail, errorCode)
})
```

---

## 4. Why Problem Details Wins Over Alternatives

### 4.1 Alternative: Custom Envelope

```json
{
  "success": false,
  "error": {
    "code": "not_found",
    "message": "Wallet was not found."
  },
  "data": null
}
```

**Pros:** Simple, already partially in use via `Error` JSON.
**Cons:** Non-standard — clients, API gateways, and load balancers do not understand it.
Doubles the response size for success cases (always `"data": {...}`, `"success": true`).
OpenAPI tooling generates incorrect schemas. Zero ecosystem support.

### 4.2 Alternative: JSON:API Error Object

```json
{
  "errors": [
    {
      "status": "404",
      "code": "not_found",
      "title": "Resource Not Found",
      "detail": "Wallet 'abc' was not found."
    }
  ]
}
```

**Pros:** Standard, supports arrays of errors.
**Cons:** Requires full JSON:API adoption (data, links, meta, relationships). Heavy overhead
for a non-JSON:API API. The `status` field is a **string** (not integer), inconsistent with
HTTP semantics. Not natively supported by ASP.NET Core.

### 4.3 Alternative: GraphQL Error Object

Only relevant if the API uses GraphQL. Not applicable to REST Minimal APIs.

### 4.4 Why RFC 9457 Problem Details

| Criterion | Problem Details | Custom Envelope | JSON:API |
|-----------|----------------|-----------------|----------|
| Industry standard | ✅ IETF RFC | ❌ Custom | ✅ jsonapi.org spec |
| ASP.NET Core native | ✅ Built-in | ❌ Requires work | ❌ Third-party |
| OpenAPI/Swagger generation | ✅ Automatic | ❌ Manual | ❌ Manual |
| Client libraries | ✅ Most HTTP clients | ❌ None | ⚠️ Some |
| Extension fields | ✅ Explicit support | ✅ Ad-hoc | ⚠️ Limited |
| Validation errors | ✅ `ValidationProblemDetails` | ❌ Custom | ✅ Supported |
| Migration cost | Low (additive) | None (current) | High |

---

## 5. Proposed Kakeibo Problem Details Shape

### 5.1 Standard Fields

```json
{
  "type": "https://kakeibo.local/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Wallet '3d7f...' was not found or you do not have access.",
  "instance": "/api/wallets/3d7f..."
}
```

### 5.2 Extension Fields

```json
{
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errorCode": "not_found"
}
```

| Extension | Source | Purpose |
|-----------|--------|---------|
| `traceId` | `Activity.Current?.TraceId` or `HttpContext.TraceIdentifier` | Log correlation |
| `errorCode` | `Error.Code` | Machine-readable code for frontend switch logic |

### 5.3 Full Shape (all fields populated)

```json
{
  "type": "https://kakeibo.local/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Wallet '3d7f...' was not found or you do not have access.",
  "instance": "/api/wallets/3d7f...",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errorCode": "not_found"
}
```

### 5.4 Validation Errors (unchanged shape, add `traceId` + `errorCode`)

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "instance": "/api/wallets",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "errorCode": "validation",
  "errors": {
    "Name": ["'Name' must not be empty."],
    "InitialBalance": ["'Initial Balance' must be greater than or equal to 0."]
  }
}
```

### 5.5 Unhandled Exception (500)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Use traceId '4bf92f...' to correlate logs.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

---

## 6. Error Code to ProblemDetails Mapping

### 6.1 Mapping Table

| `Error.Code` | HTTP Status | `type` URI slug | `title` |
|-------------|-------------|-----------------|---------|
| `not_found` | 404 | `not-found` | Resource Not Found |
| `validation` | 400 | `validation` | Validation Failed |
| `conflict` | 409 | `conflict` | Conflict |
| `unauthorized` | 401 | `unauthorized` | Unauthorized |
| `forbidden` | 403 | `forbidden` | Forbidden |
| `internal` | 500 | (uses RFC 9110 URI) | Internal Server Error |
| *(any other)* | 500 | (uses RFC 9110 URI) | Internal Server Error |

### 6.2 Type URI Constants

```csharp
// src/Kakeibo.Api/Common/Endpoints/ProblemDetailsUris.cs
namespace Kakeibo.Api.Common.Endpoints;

// Base URIs for RFC 9457 ProblemDetails type fields.
public static class ProblemDetailsUris
{
    private const string Base = "https://kakeibo.local/errors";

    public const string NotFound = $"{Base}/not-found";
    public const string Validation = $"{Base}/validation";
    public const string Conflict = $"{Base}/conflict";
    public const string Unauthorized = $"{Base}/unauthorized";
    public const string Forbidden = $"{Base}/forbidden";

    // For standard HTTP 5xx errors, point to the RFC
    public const string InternalServerError = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
    public const string ValidationProblem = "https://tools.ietf.org/html/rfc4918#section-11.2";
}
```

---

## 7. Implementation Strategy

### 7.1 Central `Result<T>` Extension Method

The key insight: all seven inconsistent formats originate from endpoint switch expressions
that map `Error.Code` to `TypedResults.*`. If we centralize that mapping in one extension
method, fixing the format means changing one file.

```csharp
// src/Kakeibo.Api/Common/Abstractions/ResultExtensions.cs
namespace Kakeibo.Api.Common.Abstractions;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

// Extension methods for mapping Result<T> failures to RFC 9457 Problem Details responses.
public static class ResultExtensions
{
    // Maps a failed Result<T> to an IResult with Problem Details JSON.
    // The caller is responsible for ensuring result.IsFailure before calling this.
    public static IResult ToProblemResult(this Result result, HttpContext httpContext) =>
        result.Error.ToProblemResult(httpContext);

    // Overload for Result<T> — same mapping applied to the error.
    public static IResult ToProblemResult<T>(this Result<T> result, HttpContext httpContext) =>
        result.Error.ToProblemResult(httpContext);

    // Maps an Error directly to an IResult with Problem Details JSON.
    public static IResult ToProblemResult(this Error error, HttpContext httpContext)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        var (statusCode, type, title) = error.Code switch
        {
            "not_found"    => (StatusCodes.Status404NotFound,             ProblemDetailsUris.NotFound,    "Resource Not Found"),
            "validation"   => (StatusCodes.Status400BadRequest,           ProblemDetailsUris.Validation,  "Validation Failed"),
            "conflict"     => (StatusCodes.Status409Conflict,             ProblemDetailsUris.Conflict,    "Conflict"),
            "unauthorized" => (StatusCodes.Status401Unauthorized,         ProblemDetailsUris.Unauthorized,"Unauthorized"),
            "forbidden"    => (StatusCodes.Status403Forbidden,            ProblemDetailsUris.Forbidden,   "Forbidden"),
            _              => (StatusCodes.Status500InternalServerError,  ProblemDetailsUris.InternalServerError, "Internal Server Error")
        };

        return TypedResults.Problem(new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = error.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId,
                ["errorCode"] = error.Code
            }
        });
    }
}
```

### 7.2 ValidationFilter Integration

The `ValidationFilter<T>` currently calls `Results.ValidationProblem(dict)`. Update it
to include the `traceId` and `errorCode` extensions:

```csharp
// Updated ValidationFilter.cs (excerpt)
private static IResult BuildValidationProblem(
    Dictionary<string, string[]> errors,
    HttpContext httpContext)
{
    var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

    var problemDetails = new HttpValidationProblemDetails(errors)
    {
        Type = ProblemDetailsUris.ValidationProblem,
        Instance = httpContext.Request.Path
    };
    problemDetails.Extensions["traceId"] = traceId;
    problemDetails.Extensions["errorCode"] = "validation";

    return TypedResults.ValidationProblem(errors, problemDetails.Instance, problemDetails.Type);
}
```

**Note:** `TypedResults.ValidationProblem` returns a `ValidationProblem` result that
ASP.NET Core serializes as `ValidationProblemDetails`. The `CustomizeProblemDetails`
callback registered in `AddProblemDetails()` can add `traceId` globally:

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.TraceId.ToString()
            ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions.TryAdd("traceId", traceId);
        // instance is set per-endpoint via ResultExtensions or inline
        ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
    };
});
```

With `CustomizeProblemDetails` as the global fallback, `traceId` and `instance` are
automatically added even if `ResultExtensions` does not set them.

### 7.3 Before/After: Endpoint Switch Expression

**Before (current pattern — seven exit points, inconsistent shapes):**

```csharp
private static async Task<IResult> HandleAsync(
    CreateWalletRequest request,
    CreateWalletHandler handler,
    CancellationToken ct)
{
    var result = await handler.HandleAsync(request, ct);

    return result.IsSuccess
        ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
        : result.Error.Code switch
        {
            "conflict"  => TypedResults.Conflict(result.Error),
            "not_found" => TypedResults.NotFound(result.Error),
            _           => TypedResults.Problem(result.Error.Message, statusCode: 500)
        };
}
```

**After (single exit point for failures — uniform Problem Details):**

```csharp
private static async Task<IResult> HandleAsync(
    CreateWalletRequest request,
    CreateWalletHandler handler,
    HttpContext httpContext,
    CancellationToken ct)
{
    var result = await handler.HandleAsync(request, ct);

    return result.IsSuccess
        ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
        : result.ToProblemResult(httpContext);
}
```

**Change summary:**
- Added `HttpContext httpContext` parameter (auto-injected by Minimal APIs)
- Replaced the `switch` expression with `result.ToProblemResult(httpContext)`
- HTTP status, `type`, `title`, `traceId`, and `instance` are all handled centrally

### 7.4 Before/After: `TypedResults.Forbidden()`

**Before:**
```csharp
if (!isOwner)
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
```

**After:**
```csharp
if (!isOwner)
    return Error.Forbidden("You do not have permission to perform this action.")
                .ToProblemResult(httpContext);
```

This requires `Error.Forbidden` factory method to exist in the `Error` record. If it does
not yet exist, add it:

```csharp
// In Common/Abstractions/Error.cs
public static Error Forbidden(string message) => new("forbidden", message);
```

### 7.5 Before/After: Unhandled Exception (500)

**Before:** Empty body (no handler installed).

**After (via `UnhandledExceptionHandler` from `global-exception-handlers.md`):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Use traceId '4bf92f...' to correlate logs.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

This is handled by the exception handler infrastructure, not by `ResultExtensions`.

---

## 8. `AddProblemDetails` Configuration

### 8.1 Full Configuration in `Program.cs`

```csharp
using System.Diagnostics;

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // 1. Always include traceId (prefer OTel TraceId for correlation)
        var traceId = Activity.Current?.TraceId.ToString()
            ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions.TryAdd("traceId", traceId);

        // 2. Always include request path as instance
        ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;

        // 3. Remove the "type": "about:blank" default for standard status codes
        //    (only applies when type was not explicitly set by the handler)
        if (ctx.ProblemDetails.Type == "about:blank")
            ctx.ProblemDetails.Type = null;
    };
});
```

### 8.2 What `CustomizeProblemDetails` Covers

`CustomizeProblemDetails` is called for **every** `ProblemDetails` response that goes
through the `IProblemDetailsService`. This includes:
- Responses generated by `IExceptionHandler` implementations that call `problemDetailsService.WriteAsync`
- Responses generated by `TypedResults.Problem(...)` in endpoint handlers
- Responses generated by `Results.ValidationProblem(...)` in `ValidationFilter<T>`

It does **not** cover:
- Direct `TypedResults.BadRequest(Error)` — these serialize `Error` directly, not ProblemDetails
- Direct `TypedResults.NotFound(Error)` — same issue

This is why the migration to `result.ToProblemResult(httpContext)` is necessary — it ensures
all failures go through `TypedResults.Problem(ProblemDetails)`, which triggers `CustomizeProblemDetails`.

---

## 9. Frontend Impact

### 9.1 TypeScript Type for Problem Details

```typescript
// src/Kakeibo.App/types/problem-details.ts
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errorCode?: string
  errors?: Record<string, string[]>  // ValidationProblemDetails
}
```

### 9.2 Updated Axios Interceptor

```typescript
// src/Kakeibo.App/lib/axios.ts (interceptor section)
import type { ProblemDetails } from '@/types/problem-details'
import type { AxiosError } from 'axios'

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    const problem = error.response?.data

    // Validation errors — show per-field messages
    if (problem?.errors && Object.keys(problem.errors).length > 0) {
      const messages = Object.values(problem.errors).flat()
      useNotificationsStore().showError(messages.join(' '))
      return Promise.reject(error)
    }

    // All other errors — show the detail message
    const message = problem?.detail ?? problem?.title ?? 'An unexpected error occurred.'
    useNotificationsStore().showError(message)

    return Promise.reject(error)
  }
)
```

### 9.3 Store Error Handling After Migration

Before migration, each store's `catch` block parsed different shapes:

```typescript
// Before — store must handle multiple shapes
try {
  const response = await api.get('/api/wallets')
  wallets.value = response.data
} catch (error: unknown) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data
    // Which shape is it? code+message? detail? empty?
    errorMessage.value = data?.message ?? data?.detail ?? 'Unknown error'
  }
}
```

After migration, the Axios interceptor handles display. Stores only need to handle
state updates:

```typescript
// After — interceptor shows the error, store just resets state
try {
  const response = await api.get('/api/wallets')
  wallets.value = response.data
} catch {
  // Interceptor already showed the error message
  // Store only cleans up optimistic state if needed
}
```

### 9.4 Error Code Routing

The `errorCode` extension field enables client-side routing on specific error types
without depending on HTTP status codes:

```typescript
// React to specific error codes in stores or components
} catch (error: unknown) {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const errorCode = error.response?.data?.errorCode
    if (errorCode === 'not_found') {
      router.push('/404')
      return
    }
    if (errorCode === 'forbidden') {
      router.push('/403')
      return
    }
  }
}
```

---

## 10. Phased Migration (A → D)

### Phase A: Infrastructure (No Breaking Changes)

**What:** Add `AddProblemDetails()` + `CustomizeProblemDetails` + `UseStatusCodePages()`.
Install `global-exception-handlers.md` infrastructure.

**Breaking changes:** None. Existing endpoints keep their current response format.
Unhandled exceptions now return structured JSON instead of empty body (improvement).

**Files changed:**
- `Program.cs` — add `AddProblemDetails`, `AddExceptionHandler` registrations, `UseExceptionHandler()`, `UseStatusCodePages()`
- Add `Infrastructure/Exceptions/` folder with 4 files

### Phase B: Central Mapping

**What:** Add `ResultExtensions.cs` with `ToProblemResult()`.
Add `ProblemDetailsUris.cs` constants.

**Breaking changes:** None. The extension method exists but no endpoints call it yet.

**Files changed:**
- `Common/Abstractions/ResultExtensions.cs` (new)
- `Common/Endpoints/ProblemDetailsUris.cs` (new)
- Add `Error.Forbidden` factory if missing

### Phase C: Endpoint Migration (Gradual)

**What:** Migrate endpoints from `switch (result.Error.Code)` to `result.ToProblemResult(httpContext)`.
Can be done domain by domain, or feature by feature.

**Breaking changes:**
- Error response body changes from `{ "code": "...", "message": "..." }` to RFC 9457 shape
- Frontend Axios interceptor must be updated **before** or **simultaneously with** the first endpoint migration

**Migration priority:** Start with endpoints used by the frontend dashboard, then work outward.

**Per-endpoint diff:**

```diff
 private static async Task<IResult> HandleAsync(
     CreateWalletRequest request,
     CreateWalletHandler handler,
+    HttpContext httpContext,
     CancellationToken ct)
 {
     var result = await handler.HandleAsync(request, ct);

     return result.IsSuccess
         ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
-        : result.Error.Code switch
-        {
-            "conflict"  => TypedResults.Conflict(result.Error),
-            "not_found" => TypedResults.NotFound(result.Error),
-            _           => TypedResults.Problem(result.Error.Message, statusCode: 500)
-        };
+        : result.ToProblemResult(httpContext);
 }
```

### Phase D: Validation Errors Unification

**What:** Update `ValidationFilter<T>` to include `traceId` and `errorCode` in
`ValidationProblemDetails`. At this point, all seven formats are unified.

**Note:** The `errors` dictionary field shape does not change — this maintains backward
compatibility for clients already parsing `errors` for field-level validation display.

**Breaking changes:** Minor — adds `traceId` and `errorCode` fields to validation responses.
These are additive (not removing existing fields).

---

## 11. Complete Reference Implementation

### 11.1 File Structure

```
src/Kakeibo.Api/
├── Common/
│   ├── Abstractions/
│   │   └── ResultExtensions.cs          ← New: ToProblemResult extension
│   └── Endpoints/
│       └── ProblemDetailsUris.cs        ← New: Type URI constants
└── Infrastructure/
    └── Exceptions/
        ├── ExceptionHandlerLogs.cs      ← New: [LoggerMessage] delegates
        ├── OperationCanceledExceptionHandler.cs
        ├── DbUpdateConcurrencyExceptionHandler.cs
        └── UnhandledExceptionHandler.cs
```

### 11.2 `ProblemDetailsUris.cs`

```csharp
namespace Kakeibo.Api.Common.Endpoints;

// Stable URI constants for RFC 9457 ProblemDetails.type fields.
// These URIs identify the problem type — they should eventually be dereferenceable.
public static class ProblemDetailsUris
{
    private const string Base = "https://kakeibo.local/errors";

    public const string NotFound = $"{Base}/not-found";
    public const string Validation = $"{Base}/validation";
    public const string Conflict = $"{Base}/conflict";
    public const string Unauthorized = $"{Base}/unauthorized";
    public const string Forbidden = $"{Base}/forbidden";

    // RFC 9110 §15.6.1 — standard reference for 500 Internal Server Error
    public const string InternalServerError =
        "https://tools.ietf.org/html/rfc9110#section-15.6.1";

    // RFC 4918 §11.2 — standard reference for validation/unprocessable entity
    public const string ValidationProblem =
        "https://tools.ietf.org/html/rfc4918#section-11.2";
}
```

### 11.3 `ResultExtensions.cs`

```csharp
namespace Kakeibo.Api.Common.Abstractions;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Kakeibo.Api.Common.Endpoints;

// Extension methods for converting Result<T> failures to RFC 9457 Problem Details responses.
public static class ResultExtensions
{
    // Maps a failed Result to Problem Details. Call only when result.IsFailure is true.
    public static IResult ToProblemResult(this Result result, HttpContext httpContext) =>
        result.Error.ToProblemResult(httpContext);

    // Maps a failed Result<T> to Problem Details. Call only when result.IsFailure is true.
    public static IResult ToProblemResult<T>(this Result<T> result, HttpContext httpContext) =>
        result.Error.ToProblemResult(httpContext);

    // Core mapping: Error → (status, type, title) → RFC 9457 ProblemDetails IResult.
    public static IResult ToProblemResult(this Error error, HttpContext httpContext)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        // Map error code to HTTP status, type URI, and human-readable title
        var (statusCode, type, title) = error.Code switch
        {
            "not_found"    => (StatusCodes.Status404NotFound,
                               ProblemDetailsUris.NotFound,
                               "Resource Not Found"),
            "validation"   => (StatusCodes.Status400BadRequest,
                               ProblemDetailsUris.Validation,
                               "Validation Failed"),
            "conflict"     => (StatusCodes.Status409Conflict,
                               ProblemDetailsUris.Conflict,
                               "Conflict"),
            "unauthorized" => (StatusCodes.Status401Unauthorized,
                               ProblemDetailsUris.Unauthorized,
                               "Unauthorized"),
            "forbidden"    => (StatusCodes.Status403Forbidden,
                               ProblemDetailsUris.Forbidden,
                               "Forbidden"),
            _              => (StatusCodes.Status500InternalServerError,
                               ProblemDetailsUris.InternalServerError,
                               "Internal Server Error")
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = error.Message,
            Instance = httpContext.Request.Path
        };

        // Extension fields: traceId for log correlation, errorCode for client logic
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["errorCode"] = error.Code;

        return TypedResults.Problem(problemDetails);
    }
}
```

### 11.4 TypeScript `ProblemDetails` Type

```typescript
// src/Kakeibo.App/types/problem-details.ts

// RFC 9457 Problem Details shape returned by all Kakeibo API error responses.
export interface ProblemDetails {
  /** URI identifying the problem type (e.g., "https://kakeibo.local/errors/not-found") */
  type?: string
  /** Short, stable summary of the problem type */
  title?: string
  /** HTTP status code */
  status?: number
  /** Human-readable explanation for this specific occurrence */
  detail?: string
  /** Request URI where the problem occurred */
  instance?: string
  /** OpenTelemetry TraceId for log correlation */
  traceId?: string
  /** Machine-readable error code for client-side switch logic */
  errorCode?: string
  /** Validation field errors (only present on 422 responses) */
  errors?: Record<string, string[]>
}
```

### 11.5 Axios Interceptor Update

```typescript
// src/Kakeibo.App/lib/axios.ts

import axios from 'axios'
import type { ProblemDetails } from '@/types/problem-details'
import type { AxiosError } from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true
})

// Response interceptor — handles all RFC 9457 error responses uniformly
api.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    // Skip interceptor for 401 — auth store handles token refresh
    if (error.response?.status === 401) {
      return Promise.reject(error)
    }

    const problem = error.response?.data

    // Validation errors (422) — concatenate field-level messages
    if (problem?.errors && Object.keys(problem.errors).length > 0) {
      const messages = Object.values(problem.errors).flat()
      // Notify user with field-level messages
      console.error('[API Validation]', messages)
      return Promise.reject(error)
    }

    // All other errors — use detail > title > fallback
    const message =
      problem?.detail ??
      problem?.title ??
      'An unexpected error occurred.'

    console.error('[API Error]', {
      message,
      errorCode: problem?.errorCode,
      traceId: problem?.traceId,
      status: error.response?.status
    })

    return Promise.reject(error)
  }
)

export { api }
```

### 11.6 `Program.cs` Changes (Full Excerpt)

```csharp
using System.Diagnostics;

// ─── Infrastructure — Problem Details ─────────────────────────────────────────
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // Inject traceId into every ProblemDetails response (global fallback)
        var traceId = Activity.Current?.TraceId.ToString()
            ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions.TryAdd("traceId", traceId);

        // Inject instance (request path) if not already set
        ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
    };
});

// ─── Infrastructure — Exception Handlers (order matters: specific first) ──────
builder.Services.AddExceptionHandler<OperationCanceledExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateConcurrencyExceptionHandler>();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

// ─── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Exception handler must be outermost middleware
app.UseExceptionHandler();

// Converts empty 404/405/etc responses to Problem Details JSON
app.UseStatusCodePages();

// ... (UseAuthentication, UseAuthorization, MapEndpoints, etc.)
```

---

## 12. Backward Compatibility Notes

### 12.1 Impact on Existing Tests

Integration tests that assert `response.StatusCode == HttpStatusCode.NotFound` continue
to pass — the HTTP status does not change. Tests that assert the response body contains
`"code": "not_found"` will fail after Phase C migration of that endpoint.

**Mitigation:** Update the failing assertions to assert the Problem Details shape:

```csharp
// Before
var error = await response.Content.ReadFromJsonAsync<Error>();
Assert.Equal("not_found", error!.Code);

// After
var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
Assert.Equal(404, problem!.Status);
Assert.Equal("not_found", problem.Extensions["errorCode"]?.ToString());
Assert.Equal("https://kakeibo.local/errors/not-found", problem.Type);
```

### 12.2 Content-Type Header

`TypedResults.BadRequest(Error)` sends `Content-Type: application/json`.
`TypedResults.Problem(ProblemDetails)` sends `Content-Type: application/problem+json`.

The MIME type changes. Clients that specifically check `Content-Type: application/json`
will need updating. Axios and most HTTP clients do not check MIME type by default.

### 12.3 `code` Field Removal

The existing `Error` JSON has a `code` field that some stores use for client-side logic:
```typescript
if (error.response?.data?.code === 'not_found') { ... }
```

After migration, this becomes:
```typescript
if (error.response?.data?.errorCode === 'not_found') { ... }
```

The `errorCode` extension field preserves the same values, just under a different key.
Audit stores and components for `?.code` usage before completing Phase C.

---

## 13. FAQ

### Q: Can I keep returning `TypedResults.BadRequest(Error)` for some endpoints?

Technically yes, but it undermines the goal of a uniform frontend parsing strategy.
The Axios interceptor must then handle both shapes again. Recommend migrating all
endpoints in Phase C.

### Q: What about the `TraceId` in `HttpContext` vs OTel `TraceId`?

`httpContext.TraceIdentifier` is a short Kestrel-specific ID (e.g., `0HN1ABCDEF:00000001`).
`Activity.Current?.TraceId` is a 32-hex-char W3C Trace Context ID (e.g., `4bf92f3577b34da6a3ce929d0e0e4736`).

The OTel `TraceId` is preferred because it correlates with Aspire Dashboard, Jaeger, and Tempo.
Use `Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier` as the fallback pattern.

### Q: Does `UseStatusCodePages()` affect successful responses?

No. `UseStatusCodePages()` only activates when:
1. The response status code is 400–599
2. The response body is empty (no content written yet)
3. The response has not started streaming

A successful `200 OK` response is never affected.

### Q: Should `403 Forbidden` include the reason in `detail`?

Yes — but be careful not to reveal authorization logic. Good:
`"You do not have permission to access this wallet."` Bad:
`"Permission denied: WalletId 'abc' is owned by UserId 'xyz' which does not match your UserId 'def'."` (leaks IDs).

### Q: Is `422 Unprocessable Entity` vs `400 Bad Request` for validation important?

Both are semantically acceptable for validation failures. ASP.NET Core's
`ValidationProblemDetails` defaults to `422`. RFC 9457 does not mandate one or the other.
The important part is consistency — pick one and use it everywhere. Kakeibo currently uses
`422` for `ValidationFilter<T>` and `400` for handler-returned validation errors. After
Phase D, `ValidationFilter<T>` is the only source of validation errors, so `422` is used
consistently.

---

## 14. References

- [RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457)
- [RFC 7807 — Problem Details for HTTP APIs (superseded)](https://www.rfc-editor.org/rfc/rfc7807)
- [ASP.NET Core Problem Details](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling#problem-details)
- [ProblemDetails class (.NET)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails)
- [AddProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.problemdetailsservicecollectionextensions.addproblemdetails)
- [TypedResults.Problem](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.typedresults.problem)
- [W3C Trace Context — TraceId](https://www.w3.org/TR/trace-context/#trace-id)
- [OpenTelemetry Semantic Conventions — HTTP](https://opentelemetry.io/docs/specs/semconv/http/)
