# API Contracts

Exhaustive specification for all HTTP API contracts in the Kakeibo platform. This document defines the standard patterns for requests, responses, errors, pagination, filtering, idempotency, versioning, and naming conventions. Both backend (C# / .NET 10) and frontend (TypeScript / Vue 3 / Axios) implementations are covered with complete code examples.

**Related Documents**:
- Architecture: [architecture.md](./architecture.md) -- Module structure, feature slice pattern, Result\<T\>
- Platform: [platform.md](./platform.md) -- Module catalog, event catalog, communication patterns
- Constraints: [constraints.md](./constraints.md) -- Pagination limits, rate limits, amount ranges
- Tech Stack: [tech-stack.md](./tech-stack.md) -- .NET 10, NodaTime, FluentValidation, Axios, Pinia

---

## Table of Contents

1. [HTTP Status Codes](#1-http-status-codes)
2. [Error Response Format](#2-error-response-format)
3. [Success Response Format](#3-success-response-format)
4. [Pagination Standards](#4-pagination-standards)
5. [Filtering, Sorting, Searching](#5-filtering-sorting-searching)
6. [Idempotency](#6-idempotency)
7. [Versioning Strategy](#7-versioning-strategy)
8. [Endpoint Naming Conventions](#8-endpoint-naming-conventions)
9. [Request/Response Examples](#9-requestresponse-examples)
10. [Date/Time Format](#10-datetime-format)
11. [Common Headers](#11-common-headers)

---

## 1. HTTP Status Codes

### 1.1 Standard Mapping

Every endpoint returns one of the following status codes. The mapping is deterministic -- a given `Result<T>` outcome always produces the same HTTP status.

| Status Code | Meaning | When Used | `Error.Code` |
|-------------|---------|-----------|--------------|
| `200 OK` | Request succeeded, resource returned | GET, PATCH, PUT that returns data | -- |
| `201 Created` | Resource created successfully | POST that creates a new entity | -- |
| `204 No Content` | Request succeeded, no body | DELETE, PUT/PATCH with no return value | -- |
| `400 Bad Request` | Validation failed or business rule violated | FluentValidation errors, domain rule violations | `validation` |
| `401 Unauthorized` | Missing or invalid authentication | No JWT, expired JWT, invalid JWT | `unauthorized` |
| `403 Forbidden` | Authenticated but not authorized | User lacks permission for the resource | `forbidden` |
| `404 Not Found` | Resource does not exist | Entity ID not found, soft-deleted entity | `not_found` |
| `409 Conflict` | State conflict | Duplicate email, duplicate wallet name, concurrent edit | `conflict` |
| `422 Unprocessable Entity` | Semantically invalid input | Valid JSON but violates business constraint (e.g., split sum mismatch) | `validation` |
| `429 Too Many Requests` | Rate limit exceeded | Exceeds 1000/hr authenticated, 100/hr unauthenticated, or 100/min transaction burst | -- |
| `500 Internal Server Error` | Unexpected server failure | Unhandled exception, infrastructure failure | -- |

### 1.2 Backend Mapping (C#)

The endpoint maps `Result<T>.Error.Code` to the appropriate HTTP status using a `switch` expression. This pattern is consistent across all endpoints.

```csharp
namespace Kakeibo.Modules.Wallets.Features.CreateWallet;

public sealed class CreateWalletEndpoint : IEndpoint
{
    public sealed record CreateWalletRequest(
        string Name, string? Description, string? Icon, string? Color,
        decimal InitialBalance, string Currency);

    public sealed record CreateWalletResponse(
        Guid Id, string Name, string? Description, string? Icon,
        string? Color, decimal Balance, string Currency, string CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<CreateWalletRequest>();
    }

    // Maps Result<T> to HTTP responses with consistent status codes.
    private static async Task<IResult> HandleAsync(
        CreateWalletRequest request, CreateWalletHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);

        return result.IsSuccess
            ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.UnprocessableEntity(result.Error),
                "not_found" => TypedResults.NotFound(result.Error),
                "conflict" => TypedResults.Conflict(result.Error),
                "unauthorized" => TypedResults.Unauthorized(),
                "forbidden" => TypedResults.Problem(
                    result.Error.Message, statusCode: 403),
                _ => TypedResults.Problem(
                    result.Error.Message, statusCode: 500),
            };
    }
}
```

### 1.3 Frontend Handling (TypeScript / Axios)

The Axios interceptor translates HTTP status codes into typed error objects that Vue components can handle uniformly.

```typescript
// src/lib/api/error-handler.ts
import type { AxiosError, AxiosResponse } from 'axios';

export interface ApiError {
  code: string;
  message: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

// Maps HTTP status codes to user-facing error handling strategies.
export function handleApiError(error: AxiosError<ApiError>): never {
  const status = error.response?.status;
  const data = error.response?.data;

  switch (status) {
    case 400:
    case 422:
      throw new ValidationError(data?.message ?? 'Validation failed', data?.errors);
    case 401:
      // Trigger token refresh or redirect to login
      useAuthStore().handleUnauthorized();
      throw new AuthError('Session expired');
    case 403:
      throw new ForbiddenError(data?.message ?? 'Access denied');
    case 404:
      throw new NotFoundError(data?.message ?? 'Resource not found');
    case 409:
      throw new ConflictError(data?.message ?? 'Resource conflict');
    case 429:
      throw new RateLimitError('Too many requests. Please try again later.');
    default:
      throw new ServerError(data?.message ?? 'An unexpected error occurred');
  }
}

export class ValidationError extends Error {
  constructor(message: string, public readonly fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = 'ValidationError';
  }
}

export class AuthError extends Error {
  constructor(message: string) { super(message); this.name = 'AuthError'; }
}

export class ForbiddenError extends Error {
  constructor(message: string) { super(message); this.name = 'ForbiddenError'; }
}

export class NotFoundError extends Error {
  constructor(message: string) { super(message); this.name = 'NotFoundError'; }
}

export class ConflictError extends Error {
  constructor(message: string) { super(message); this.name = 'ConflictError'; }
}

export class RateLimitError extends Error {
  constructor(message: string) { super(message); this.name = 'RateLimitError'; }
}

export class ServerError extends Error {
  constructor(message: string) { super(message); this.name = 'ServerError'; }
}
```

---

## 2. Error Response Format

### 2.1 Error Envelope

All error responses follow a single envelope format. Every error includes a `code` (machine-readable) and a `message` (human-readable). Validation errors include a `errors` object mapping field names to error arrays.

```json
{
  "code": "validation",
  "message": "One or more validation errors occurred.",
  "traceId": "0HN8Q4V2L0001:00000001",
  "errors": {
    "Name": ["Wallet name is required.", "Wallet name must be 100 characters or fewer."],
    "InitialBalance": ["Initial balance must be greater than or equal to 0."]
  }
}
```

### 2.2 Error Code Namespace

Error codes use dot-separated namespaces following the pattern `{Module}.{Entity}.{Rule}`. The top-level `code` field on the envelope is always one of the five standard codes. The `message` field contains the domain-specific error.

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `validation` | 400 / 422 | Input validation failure or business constraint violation |
| `not_found` | 404 | Requested entity does not exist or is soft-deleted |
| `conflict` | 409 | Duplicate or state conflict |
| `unauthorized` | 401 | Authentication required or invalid |
| `forbidden` | 403 | Authenticated but lacks permission |

### 2.3 Backend Error Construction (C#)

Errors are constructed using the `Error` record factories defined in `Kakeibo.Common`.

```csharp
namespace Kakeibo.Common;

// Discriminated error type with factory methods for each HTTP error category.
public sealed record Error(string Code, string Message)
{
    public static Error NotFound(string message) => new("not_found", message);
    public static Error Validation(string message) => new("validation", message);
    public static Error Conflict(string message) => new("conflict", message);
    public static Error Unauthorized(string message) => new("unauthorized", message);
    public static Error Forbidden(string message) => new("forbidden", message);
}
```

Module-specific error constants group related errors by entity.

```csharp
namespace Kakeibo.Modules.Wallets.Errors;

// Typed error constants for the Wallets module.
public static class WalletErrors
{
    public static Error NotFound(Guid walletId) =>
        Error.NotFound($"Wallet with ID '{walletId}' was not found.");

    public static Error DuplicateName(string name) =>
        Error.Conflict($"A wallet with name '{name}' already exists.");

    public static Error CannotDeleteWithTransactions(Guid walletId) =>
        Error.Validation($"Wallet '{walletId}' has transactions and cannot be deleted. Archive it instead.");

    public static Error NotMember(Guid walletId) =>
        Error.Forbidden($"You are not a member of wallet '{walletId}'.");
}
```

### 2.4 FluentValidation Error Response

The `ValidationFilter<T>` endpoint filter intercepts requests before they reach the handler. When validation fails, it returns a `400 Bad Request` with field-level errors.

```csharp
namespace Kakeibo.Common;

// Endpoint filter that runs FluentValidation before the handler executes.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return TypedResults.BadRequest(Error.Validation("Request body is required."));

        var validationResult = await validator.ValidateAsync(argument);
        if (!validationResult.IsValid)
        {
            // Group validation failures by property name
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}
```

### 2.5 Frontend Error Display (Vue Composable)

A composable extracts field-level errors from validation responses and exposes them to form components.

```typescript
// src/composables/useApiErrors.ts
import { ref, type Ref } from 'vue';
import { ValidationError } from '@/lib/api/error-handler';

interface UseApiErrors {
  fieldErrors: Ref<Record<string, string[]>>;
  generalError: Ref<string | null>;
  clearErrors: () => void;
  handleError: (error: unknown) => void;
}

// Composable that extracts field-level and general errors from API responses.
export function useApiErrors(): UseApiErrors {
  const fieldErrors = ref<Record<string, string[]>>({});
  const generalError = ref<string | null>(null);

  function clearErrors(): void {
    fieldErrors.value = {};
    generalError.value = null;
  }

  function handleError(error: unknown): void {
    clearErrors();

    if (error instanceof ValidationError) {
      if (error.fieldErrors) {
        fieldErrors.value = error.fieldErrors;
      }
      generalError.value = error.message;
    } else if (error instanceof Error) {
      generalError.value = error.message;
    } else {
      generalError.value = 'An unexpected error occurred.';
    }
  }

  return { fieldErrors, generalError, clearErrors, handleError };
}
```

### 2.6 TraceId Propagation

Every error response includes a `traceId` field from OpenTelemetry. This allows correlating frontend errors with backend logs. The Axios response interceptor extracts it automatically.

```typescript
// src/lib/api/client.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true, // Send HttpOnly cookies for refresh token
});

// Response interceptor: extract traceId and propagate errors.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const traceId = error.response?.data?.traceId;
    if (traceId) {
      console.error(`[API Error] traceId: ${traceId}`);
    }
    return Promise.reject(error);
  },
);

export { apiClient };
```

---

## 3. Success Response Format

### 3.1 Single Resource

Returned by GET (by ID), POST (create), and PATCH/PUT (update) endpoints. The response body is the resource itself -- no wrapping envelope for single resources.

```json
{
  "id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "name": "Checking Account",
  "description": "Primary bank account",
  "icon": "wallet",
  "color": "#3B82F6",
  "balance": 2450.75,
  "currency": "USD",
  "isDefault": true,
  "isArchived": false,
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-02-01T14:22:00Z"
}
```

### 3.2 Collection (Paginated)

Returned by list endpoints. The response wraps items in a `data` array alongside pagination metadata.

```json
{
  "data": [
    {
      "id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
      "concept": "Morning coffee at Cafe Luna",
      "amount": 4.50,
      "date": "2026-02-15",
      "categoryId": "sys-cat-food",
      "categoryName": "Food & Dining",
      "walletId": "01926f4e-8b3a-7d20-9a15-aaa8e4f5a6b7",
      "walletName": "Checking Account",
      "createdAt": "2026-02-15T08:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 342,
    "totalPages": 7,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

### 3.3 Empty Collection

An empty collection is a valid success response. The `data` array is empty and pagination reflects zero items.

```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 0,
    "totalPages": 0,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

### 3.4 No Content

Returned by DELETE and certain update operations. The response has no body (HTTP 204).

```
HTTP/1.1 204 No Content
```

### 3.5 Created

Returned by POST endpoints that create a resource. Includes a `Location` header pointing to the newly created resource.

```
HTTP/1.1 201 Created
Location: /api/wallets/01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7
Content-Type: application/json

{
  "id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "name": "Checking Account",
  ...
}
```

### 3.6 Backend Paginated Response (C#)

A generic paginated response record used across all list endpoints.

```csharp
namespace Kakeibo.Common;

// Generic paginated response wrapper for collection endpoints.
public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Data,
    PaginationMetadata Pagination);

public sealed record PaginationMetadata(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

// Extension method for building paginated responses from EF Core queries.
public static class PaginationExtensions
{
    public static async Task<PaginatedResponse<T>> ToPaginatedAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var totalItems = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var pagination = new PaginationMetadata(
            Page: page,
            PageSize: pageSize,
            TotalItems: totalItems,
            TotalPages: totalPages,
            HasNextPage: page < totalPages,
            HasPreviousPage: page > 1);

        return new PaginatedResponse<T>(data, pagination);
    }
}
```

### 3.7 Frontend Type Definitions (TypeScript)

```typescript
// src/types/api.ts

export interface PaginationMetadata {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: PaginationMetadata;
}
```

### 3.8 Timestamp Format in Responses

All timestamps in JSON responses are serialized as ISO 8601 UTC strings (trailing `Z`). The backend uses NodaTime `Instant` internally. See [Section 10](#10-datetime-format) for complete details.

```
"createdAt": "2026-02-15T10:30:00Z"
"updatedAt": "2026-02-15T14:22:00Z"
```

### 3.9 ID Format in Responses

All entity IDs are UUID v7 (via `Guid7` / Medo.Uuid7), serialized as lowercase hyphenated strings.

```
"id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7"
```

---

## 4. Pagination Standards

### 4.1 Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | `1` | Page number (1-based) |
| `pageSize` | int | Varies | Items per page (see defaults below) |

### 4.2 Default Page Sizes

| Resource | Default `pageSize` | Maximum `pageSize` |
|----------|-------------------|-------------------|
| Transactions | 50 | 100 |
| Wallets | 50 | 100 |
| Categories | 100 | 200 |
| Audit logs / Activities | 100 | 200 |
| Notifications | 50 | 100 |
| Budgets | 50 | 100 |
| Goals | 50 | 100 |
| Recurring patterns | 50 | 100 |
| Shared wallet members | 20 | 50 |
| Invitations | 50 | 100 |

### 4.3 Pagination Metadata

Every paginated response includes a `pagination` object alongside the `data` array.

```json
{
  "data": [...],
  "pagination": {
    "page": 2,
    "pageSize": 50,
    "totalItems": 342,
    "totalPages": 7,
    "hasNextPage": true,
    "hasPreviousPage": true
  }
}
```

### 4.4 Backend Implementation (C#)

Pagination parameters are bound from the query string. The `ToPaginatedAsync` extension method (from Section 3.6) handles the database query.

```csharp
namespace Kakeibo.Modules.Transactions.Features.ListTransactions;

public sealed class ListTransactionsEndpoint : IEndpoint
{
    public sealed record ListTransactionsResponse(
        Guid Id, Guid WalletId, string Concept, decimal Amount,
        string Date, Guid CategoryId, string CategoryName, string CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets/{walletId:guid}/transactions", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    // Lists transactions for a wallet with offset-based pagination.
    private static async Task<IResult> HandleAsync(
        Guid walletId,
        [AsParameters] PaginationQuery pagination,
        ListTransactionsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(walletId, pagination, ct);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Problem(
                    result.Error.Message, statusCode: 403),
                _ => TypedResults.Problem(
                    result.Error.Message, statusCode: 500),
            };
    }
}

// Shared query parameter binding for pagination across all list endpoints.
public sealed record PaginationQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
```

### 4.5 Frontend Pagination Composable (TypeScript)

```typescript
// src/composables/usePagination.ts
import { ref, computed, type Ref } from 'vue';
import type { PaginationMetadata } from '@/types/api';

interface UsePagination {
  page: Ref<number>;
  pageSize: Ref<number>;
  pagination: Ref<PaginationMetadata | null>;
  hasNextPage: Ref<boolean>;
  hasPreviousPage: Ref<boolean>;
  totalItems: Ref<number>;
  nextPage: () => void;
  previousPage: () => void;
  goToPage: (targetPage: number) => void;
  updatePagination: (meta: PaginationMetadata) => void;
}

// Composable for managing pagination state across list views.
export function usePagination(defaultPageSize = 50): UsePagination {
  const page = ref(1);
  const pageSize = ref(defaultPageSize);
  const pagination = ref<PaginationMetadata | null>(null);

  const hasNextPage = computed(() => pagination.value?.hasNextPage ?? false);
  const hasPreviousPage = computed(() => pagination.value?.hasPreviousPage ?? false);
  const totalItems = computed(() => pagination.value?.totalItems ?? 0);

  function nextPage(): void {
    if (hasNextPage.value) page.value++;
  }

  function previousPage(): void {
    if (hasPreviousPage.value) page.value--;
  }

  function goToPage(targetPage: number): void {
    const maxPage = pagination.value?.totalPages ?? 1;
    page.value = Math.max(1, Math.min(targetPage, maxPage));
  }

  function updatePagination(meta: PaginationMetadata): void {
    pagination.value = meta;
  }

  return {
    page, pageSize, pagination, hasNextPage, hasPreviousPage,
    totalItems, nextPage, previousPage, goToPage, updatePagination,
  };
}
```

### 4.6 Cursor-Based Pagination (Future Consideration)

For high-volume transaction feeds (Phase 2+), cursor-based pagination may supplement offset-based pagination. The cursor is an opaque, base64-encoded string containing the last item's sort key.

```
GET /api/wallets/{walletId}/transactions?cursor=eyJkYXRlIjoiMjAyNi0wMi0xNSIsImlkIjoiMDE5MjZmNGUtOGIzYS03ZDIwIn0&pageSize=50
```

Response includes `nextCursor` instead of page numbers:

```json
{
  "data": [...],
  "pagination": {
    "pageSize": 50,
    "totalItems": 342,
    "hasNextPage": true,
    "nextCursor": "eyJkYXRlIjoiMjAyNi0wMi0xNCIsImlkIjoiMDE5MjZmNGUtOGIzYS03ZDIxIn0"
  }
}
```

This is not implemented in MVP. Offset-based pagination is used for all endpoints in Phase 1.

---

## 5. Filtering, Sorting, Searching

### 5.1 Filtering

Filters are passed as query string parameters. Each parameter name matches the field being filtered.

#### Equality Filters

```
GET /api/wallets/{walletId}/transactions?categoryId=sys-cat-food
GET /api/wallets/{walletId}/transactions?transactionType=normal
```

#### Range Filters

Date and numeric ranges use `From` and `To` suffixes.

```
GET /api/wallets/{walletId}/transactions?dateFrom=2026-01-01&dateTo=2026-01-31
GET /api/wallets/{walletId}/transactions?amountFrom=10.00&amountTo=100.00
```

#### Boolean Filters

```
GET /api/wallets?isArchived=false
GET /api/categories?isSystem=true
```

#### Multiple Value Filters

Comma-separated values for OR logic on the same field.

```
GET /api/wallets/{walletId}/transactions?categoryId=sys-cat-food,sys-cat-transport
```

### 5.2 Sorting

Sorting uses `sortBy` and `sortDirection` query parameters. Default sort varies by endpoint.

| Parameter | Values | Default |
|-----------|--------|---------|
| `sortBy` | Field name (e.g., `date`, `amount`, `name`, `createdAt`) | Varies by endpoint |
| `sortDirection` | `asc`, `desc` | `desc` for dates, `asc` for names |

```
GET /api/wallets/{walletId}/transactions?sortBy=date&sortDirection=desc
GET /api/categories?sortBy=name&sortDirection=asc
```

Default sort order by endpoint:

| Endpoint | Default `sortBy` | Default `sortDirection` |
|----------|-----------------|----------------------|
| Transactions | `date` | `desc` |
| Wallets | `createdAt` | `desc` |
| Categories | `name` | `asc` |
| Budgets | `createdAt` | `desc` |
| Goals | `createdAt` | `desc` |
| Recurring patterns | `createdAt` | `desc` |
| Activities | `timestamp` | `desc` |
| Notifications | `createdAt` | `desc` |

### 5.3 Full-Text Search

The `search` parameter performs case-insensitive partial matching against relevant text fields.

```
GET /api/wallets/{walletId}/transactions?search=coffee
GET /api/categories?search=food
```

| Endpoint | Fields Searched |
|----------|----------------|
| Transactions | `concept`, `notes` |
| Wallets | `name`, `description` |
| Categories | `name`, `description` |
| Goals | `name` |
| Recurring patterns | `concept`, `notes` |

### 5.4 Backend Filter Implementation (C#)

Filters are bound from query parameters into a strongly-typed record and applied to the EF Core query.

```csharp
namespace Kakeibo.Modules.Transactions.Features.ListTransactions;

// Query parameters for filtering and sorting transactions.
public sealed record ListTransactionsQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public string? TransactionType { get; init; }
    public string? DateFrom { get; init; }
    public string? DateTo { get; init; }
    public decimal? AmountFrom { get; init; }
    public decimal? AmountTo { get; init; }
    public string SortBy { get; init; } = "date";
    public string SortDirection { get; init; } = "desc";
}

// Applies filters, sorting, and pagination to the transactions query.
public sealed class ListTransactionsHandler(TransactionsDbContext db)
{
    public async Task<Result<PaginatedResponse<ListTransactionsEndpoint.ListTransactionsResponse>>>
        HandleAsync(Guid walletId, ListTransactionsQuery query, CancellationToken ct)
    {
        var baseQuery = db.Transactions
            .Where(t => t.WalletId == walletId && !t.IsDeleted);

        // Apply equality filters
        if (query.CategoryId.HasValue)
            baseQuery = baseQuery.Where(t => t.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrEmpty(query.TransactionType))
            baseQuery = baseQuery.Where(t => t.TransactionType == query.TransactionType);

        // Apply range filters
        if (!string.IsNullOrEmpty(query.DateFrom))
        {
            var fromDate = LocalDate.FromIso8601(query.DateFrom);
            baseQuery = baseQuery.Where(t => t.Date >= fromDate);
        }

        if (!string.IsNullOrEmpty(query.DateTo))
        {
            var toDate = LocalDate.FromIso8601(query.DateTo);
            baseQuery = baseQuery.Where(t => t.Date <= toDate);
        }

        if (query.AmountFrom.HasValue)
            baseQuery = baseQuery.Where(t => t.Amount >= query.AmountFrom.Value);

        if (query.AmountTo.HasValue)
            baseQuery = baseQuery.Where(t => t.Amount <= query.AmountTo.Value);

        // Apply full-text search
        if (!string.IsNullOrEmpty(query.Search))
        {
            var searchTerm = query.Search.ToLowerInvariant();
            baseQuery = baseQuery.Where(t =>
                t.Concept.ToLower().Contains(searchTerm) ||
                (t.Notes != null && t.Notes.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        baseQuery = query.SortBy.ToLowerInvariant() switch
        {
            "amount" => query.SortDirection == "asc"
                ? baseQuery.OrderBy(t => t.Amount)
                : baseQuery.OrderByDescending(t => t.Amount),
            "createdat" => query.SortDirection == "asc"
                ? baseQuery.OrderBy(t => t.CreatedAt)
                : baseQuery.OrderByDescending(t => t.CreatedAt),
            _ => query.SortDirection == "asc"
                ? baseQuery.OrderBy(t => t.Date)
                : baseQuery.OrderByDescending(t => t.Date),
        };

        // Project and paginate
        var projected = baseQuery.Select(t => new ListTransactionsEndpoint.ListTransactionsResponse(
            t.Id, t.WalletId, t.Concept, t.Amount,
            t.Date.ToString(), t.CategoryId, t.Category.Name,
            t.CreatedAt.ToString()));

        return await projected.ToPaginatedAsync(query.Page, query.PageSize, ct);
    }
}
```

### 5.5 Frontend Filter Composable (TypeScript)

```typescript
// src/composables/useTransactionFilters.ts
import { ref, watch, type Ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

interface TransactionFilters {
  search: string | undefined;
  categoryId: string | undefined;
  transactionType: string | undefined;
  dateFrom: string | undefined;
  dateTo: string | undefined;
  amountFrom: number | undefined;
  amountTo: number | undefined;
  sortBy: string;
  sortDirection: string;
}

// Composable that syncs filter state with URL query parameters.
export function useTransactionFilters() {
  const route = useRoute();
  const router = useRouter();

  const filters = ref<TransactionFilters>({
    search: (route.query.search as string) ?? undefined,
    categoryId: (route.query.categoryId as string) ?? undefined,
    transactionType: (route.query.transactionType as string) ?? undefined,
    dateFrom: (route.query.dateFrom as string) ?? undefined,
    dateTo: (route.query.dateTo as string) ?? undefined,
    amountFrom: route.query.amountFrom ? Number(route.query.amountFrom) : undefined,
    amountTo: route.query.amountTo ? Number(route.query.amountTo) : undefined,
    sortBy: (route.query.sortBy as string) ?? 'date',
    sortDirection: (route.query.sortDirection as string) ?? 'desc',
  });

  // Sync filters to URL query parameters for shareable/bookmarkable filter state.
  watch(filters, (newFilters) => {
    const query: Record<string, string> = {};
    for (const [key, value] of Object.entries(newFilters)) {
      if (value !== undefined && value !== '') {
        query[key] = String(value);
      }
    }
    router.replace({ query });
  }, { deep: true });

  function toQueryParams(): Record<string, string | number> {
    const params: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters.value)) {
      if (value !== undefined && value !== '') {
        params[key] = value;
      }
    }
    return params;
  }

  function resetFilters(): void {
    filters.value = {
      search: undefined,
      categoryId: undefined,
      transactionType: undefined,
      dateFrom: undefined,
      dateTo: undefined,
      amountFrom: undefined,
      amountTo: undefined,
      sortBy: 'date',
      sortDirection: 'desc',
    };
  }

  return { filters, toQueryParams, resetFilters };
}
```

---

## 6. Idempotency

### 6.1 Strategy

All mutating endpoints (POST, PATCH, PUT) support idempotency via the `Idempotency-Key` header. This prevents duplicate resource creation when clients retry failed requests (network timeouts, mobile connectivity issues).

### 6.2 Header Format

```
Idempotency-Key: <UUID v4>
```

The key is a client-generated UUID v4 string. The client must generate a new key for each unique operation and reuse the same key when retrying the same operation.

### 6.3 Backend Implementation (C#)

Idempotency keys are stored in Redis with a TTL. When a duplicate key is detected, the cached response is returned instead of processing the request again.

```csharp
namespace Kakeibo.Infrastructure.Idempotency;

// Middleware that enforces idempotency for mutating HTTP methods using Redis.
public sealed class IdempotencyMiddleware(
    ICacheService cache, ILogger<IdempotencyMiddleware> logger) : IMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Only apply to mutating methods
        if (!IsMutatingMethod(context.Request.Method))
        {
            await next(context);
            return;
        }

        // Check for idempotency key
        if (!context.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            await next(context);
            return;
        }

        var idempotencyKey = keyValues.First()!;
        var cacheKey = $"idempotency:{idempotencyKey}";

        // Check if this key was already processed
        var cached = await cache.GetAsync<CachedResponse>(cacheKey);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached response for idempotency key {Key}", idempotencyKey);
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            if (cached.Body is not null)
                await context.Response.WriteAsync(cached.Body);
            return;
        }

        // Capture the response
        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await next(context);

        // Cache the response
        memoryStream.Position = 0;
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

        var cachedResponse = new CachedResponse(
            context.Response.StatusCode,
            context.Response.ContentType ?? "application/json",
            responseBody);

        await cache.SetAsync(cacheKey, cachedResponse, CacheTtl);

        // Write to original stream
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }

    private static bool IsMutatingMethod(string method) =>
        method is "POST" or "PUT" or "PATCH";
}

internal sealed record CachedResponse(int StatusCode, string ContentType, string? Body);
```

### 6.4 Frontend Usage (TypeScript)

```typescript
// src/lib/api/idempotency.ts
import { v4 as uuidv4 } from 'uuid';
import { apiClient } from '@/lib/api/client';
import type { AxiosRequestConfig } from 'axios';

// Generates a unique idempotency key for each mutating request.
export function withIdempotency(config: AxiosRequestConfig = {}): AxiosRequestConfig {
  return {
    ...config,
    headers: {
      ...config.headers,
      'Idempotency-Key': uuidv4(),
    },
  };
}

// Usage in a wallet service:
export async function createWallet(data: CreateWalletRequest) {
  const response = await apiClient.post('/api/wallets', data, withIdempotency());
  return response.data;
}

// Retry with same key (idempotent):
const idempotencyKey = uuidv4();
export async function createWalletWithRetry(
  data: CreateWalletRequest,
  maxRetries = 3,
) {
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      const response = await apiClient.post('/api/wallets', data, {
        headers: { 'Idempotency-Key': idempotencyKey },
      });
      return response.data;
    } catch (error) {
      if (attempt === maxRetries - 1) throw error;
      // Wait before retrying (exponential backoff)
      await new Promise((resolve) => setTimeout(resolve, 1000 * 2 ** attempt));
    }
  }
}
```

### 6.5 Idempotency Rules

| Rule | Description |
|------|-------------|
| Key uniqueness | Each unique business operation gets a unique key |
| Key reuse on retry | The same key is used when retrying the same operation |
| TTL | Keys expire after 24 hours |
| Storage | Redis via FusionCache |
| Scope | Per-user (key is scoped to the authenticated user) |
| GET/DELETE | GET is inherently idempotent; DELETE returns 204 regardless of existence |
| Response | Cached response is returned with same status code and body |

---

## 7. Versioning Strategy

### 7.1 MVP Approach (Phase 1)

No explicit API versioning in the MVP. All endpoints use the `/api/{resource}` pattern without version prefixes. Breaking changes are avoided through additive-only modifications.

**Additive changes (non-breaking)**:
- Adding new optional fields to request bodies
- Adding new fields to response bodies
- Adding new endpoints
- Adding new query parameters with defaults

**Breaking changes (avoided in MVP)**:
- Removing or renaming fields
- Changing field types
- Changing response structure
- Removing endpoints

### 7.2 Post-MVP Strategy (Phase 2+)

When breaking changes become necessary, URL-based versioning is introduced.

```
/api/v1/wallets          -- Original
/api/v2/wallets          -- Breaking change
```

### 7.3 Version Header (Informational)

Even without URL versioning, every response includes an informational version header.

```
X-Api-Version: 1.0
```

### 7.4 Deprecation Policy

When a new version is introduced:

1. The old version continues working for 6 months
2. Deprecated endpoints return a `Sunset` header
3. The `Deprecation` header indicates when the endpoint was deprecated
4. API documentation (Scalar) marks deprecated endpoints

```
Sunset: Sat, 01 Jan 2027 00:00:00 GMT
Deprecation: Sat, 01 Jul 2026 00:00:00 GMT
Link: </api/v2/wallets>; rel="successor-version"
```

### 7.5 Contract Evolution Rules

| Change Type | Allowed in MVP? | Requires New Version? |
|-------------|-----------------|----------------------|
| Add optional request field | Yes | No |
| Add response field | Yes | No |
| Add new endpoint | Yes | No |
| Add query parameter (with default) | Yes | No |
| Remove request field | No | Yes |
| Rename field | No | Yes |
| Change field type | No | Yes |
| Change response structure | No | Yes |
| Remove endpoint | No | Yes |

---

## 8. Endpoint Naming Conventions

### 8.1 URL Structure

All endpoints follow RESTful naming conventions with plural nouns and nested resources.

```
/api/{resource}                         -- Collection
/api/{resource}/{id}                    -- Single resource
/api/{resource}/{id}/{sub-resource}     -- Nested collection
/api/{resource}/{id}/{sub-resource}/{subId}  -- Nested single resource
/api/{resource}/{id}/{action}           -- Resource action (non-CRUD)
```

### 8.2 HTTP Method Mapping

| Operation | HTTP Method | URL Pattern | Returns |
|-----------|-------------|-------------|---------|
| List | `GET` | `/api/{resource}` | `200` + paginated collection |
| Get by ID | `GET` | `/api/{resource}/{id}` | `200` + single resource |
| Create | `POST` | `/api/{resource}` | `201` + created resource + `Location` header |
| Full update | `PUT` | `/api/{resource}/{id}` | `200` + updated resource |
| Partial update | `PATCH` | `/api/{resource}/{id}` | `200` + updated resource |
| Delete | `DELETE` | `/api/{resource}/{id}` | `204` no content |
| Action | `POST` | `/api/{resource}/{id}/{action}` | Varies |

### 8.3 Complete Endpoint Reference

#### Identity Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `POST` | `/api/auth/register` | Register new user | No |
| `POST` | `/api/auth/login` | Authenticate user, return JWT | No |
| `POST` | `/api/auth/refresh` | Refresh access token | Cookie or Body |
| `POST` | `/api/auth/logout` | Invalidate refresh token | Yes |
| `POST` | `/api/auth/forgot-password` | Request password reset email | No |
| `POST` | `/api/auth/reset-password` | Reset password with token | No |
| `POST` | `/api/auth/verify-email` | Verify email with token | No |
| `GET` | `/api/users/me` | Get current user profile | Yes |
| `PATCH` | `/api/users/me` | Update current user profile | Yes |
| `PATCH` | `/api/users/me/password` | Change password | Yes |

#### Wallets Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/wallets` | List user's personal wallets | Yes |
| `POST` | `/api/wallets` | Create personal wallet | Yes |
| `GET` | `/api/wallets/{id}` | Get wallet details | Yes |
| `PATCH` | `/api/wallets/{id}` | Update wallet | Yes |
| `DELETE` | `/api/wallets/{id}` | Delete wallet (or archive if has transactions) | Yes |
| `POST` | `/api/wallets/{id}/archive` | Archive wallet | Yes |
| `POST` | `/api/wallets/{id}/restore` | Restore archived wallet | Yes |
| `POST` | `/api/wallets/{id}/set-default` | Set as default wallet | Yes |
| `GET` | `/api/shared-wallets` | List user's shared wallets | Yes |
| `POST` | `/api/shared-wallets` | Create shared wallet | Yes |
| `GET` | `/api/shared-wallets/{id}` | Get shared wallet details | Yes |
| `PATCH` | `/api/shared-wallets/{id}` | Update shared wallet | Yes |
| `POST` | `/api/shared-wallets/{id}/archive` | Archive shared wallet | Yes |
| `GET` | `/api/shared-wallets/{id}/members` | List shared wallet members | Yes |
| `DELETE` | `/api/shared-wallets/{id}/members/{userId}` | Remove member (or leave) | Yes |
| `GET` | `/api/shared-wallets/{id}/debts` | Get debt summary for shared wallet | Yes |
| `GET` | `/api/shared-wallets/{id}/invitations` | List invitations for wallet | Yes |
| `POST` | `/api/shared-wallets/{id}/invitations` | Send invitation to join | Yes |
| `POST` | `/api/invitations/{token}/accept` | Accept invitation | Yes |
| `POST` | `/api/invitations/{token}/decline` | Decline invitation | Yes |
| `POST` | `/api/shared-wallets/{id}/settlements` | Record settlement | Yes |

#### Transactions Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/wallets/{walletId}/transactions` | List transactions in personal wallet | Yes |
| `GET` | `/api/shared-wallets/{walletId}/transactions` | List transactions in shared wallet | Yes |
| `POST` | `/api/wallets/{walletId}/transactions` | Create transaction in personal wallet | Yes |
| `POST` | `/api/shared-wallets/{walletId}/transactions` | Create transaction in shared wallet (with splits) | Yes |
| `GET` | `/api/transactions/{id}` | Get transaction details | Yes |
| `PATCH` | `/api/transactions/{id}` | Update transaction | Yes |
| `DELETE` | `/api/transactions/{id}` | Soft-delete transaction | Yes |
| `POST` | `/api/transactions/{id}/confirm` | Confirm forecasted transaction | Yes |
| `POST` | `/api/transactions/{id}/skip` | Skip forecasted transaction | Yes |
| `GET` | `/api/transactions/{id}/splits` | Get splits for shared transaction | Yes |
| `POST` | `/api/transactions/{id}/splits/{splitId}/settle` | Mark split as settled | Yes |
| `GET` | `/api/categories` | List all categories (system + custom) | Yes |
| `POST` | `/api/categories` | Create custom category | Yes |
| `PATCH` | `/api/categories/{id}` | Update custom category | Yes |
| `POST` | `/api/categories/{id}/archive` | Archive custom category | Yes |
| `POST` | `/api/categories/{id}/restore` | Restore archived category | Yes |
| `GET` | `/api/categories/{id}/subcategories` | List subcategories | Yes |
| `POST` | `/api/categories/{id}/subcategories` | Create subcategory | Yes |
| `PATCH` | `/api/subcategories/{id}` | Update subcategory | Yes |
| `POST` | `/api/subcategories/{id}/archive` | Archive subcategory | Yes |

#### Budgets Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/budgets` | List user's personal budgets | Yes |
| `POST` | `/api/budgets` | Create personal budget | Yes |
| `GET` | `/api/budgets/{id}` | Get budget details with spending | Yes |
| `PATCH` | `/api/budgets/{id}` | Update budget | Yes |
| `DELETE` | `/api/budgets/{id}` | Delete budget | Yes |
| `GET` | `/api/shared-wallets/{walletId}/budgets` | List shared wallet budgets | Yes |
| `POST` | `/api/shared-wallets/{walletId}/budgets` | Create shared wallet budget | Yes |

#### Goals Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/goals` | List user's savings goals | Yes |
| `POST` | `/api/goals` | Create savings goal | Yes |
| `GET` | `/api/goals/{id}` | Get goal details with progress | Yes |
| `PATCH` | `/api/goals/{id}` | Update goal | Yes |
| `DELETE` | `/api/goals/{id}` | Delete goal | Yes |
| `PATCH` | `/api/goals/{id}/progress` | Update manual progress | Yes |

#### Recurring Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/recurring-patterns` | List recurring patterns | Yes |
| `POST` | `/api/recurring-patterns` | Create recurring pattern | Yes |
| `GET` | `/api/recurring-patterns/{id}` | Get pattern details | Yes |
| `PATCH` | `/api/recurring-patterns/{id}` | Update pattern (future only) | Yes |
| `DELETE` | `/api/recurring-patterns/{id}` | Delete pattern + future forecasted | Yes |
| `POST` | `/api/recurring-patterns/{id}/pause` | Pause pattern | Yes |
| `POST` | `/api/recurring-patterns/{id}/resume` | Resume pattern | Yes |
| `GET` | `/api/recurring-patterns/{id}/forecast` | Get upcoming occurrences | Yes |

#### Notifications Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/notifications` | List user's notifications | Yes |
| `GET` | `/api/notifications/unread-count` | Get unread notification count | Yes |
| `PATCH` | `/api/notifications/{id}/read` | Mark notification as read | Yes |
| `POST` | `/api/notifications/read-all` | Mark all notifications as read | Yes |

#### Auditing Module

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/api/activities` | List user's activity log | Yes |

#### Health & Documentation

| Method | URL | Description | Auth |
|--------|-----|-------------|------|
| `GET` | `/health` | Health check (live) | No |
| `GET` | `/health/ready` | Readiness check (dependencies) | No |
| `GET` | `/scalar` | API documentation (Scalar UI) | No |

---

## 9. Request/Response Examples

### 9.1 User Registration

**Request**:
```
POST /api/auth/register
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecureP@ssw0rd!",
  "name": "Alice Johnson",
  "language": "en",
  "timezone": "America/New_York",
  "currency": "USD"
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "email": "alice@example.com",
  "name": "Alice Johnson",
  "language": "en",
  "timezone": "America/New_York",
  "currency": "USD",
  "isVerified": false,
  "createdAt": "2026-02-15T10:30:00Z"
}
```

**Validation Error (400)**:
```json
{
  "code": "validation",
  "message": "One or more validation errors occurred.",
  "errors": {
    "Email": ["Email address is already registered."],
    "Password": ["Password must be at least 8 characters.", "Password must contain at least one uppercase letter."]
  }
}
```

**Backend (C#)**:
```csharp
namespace Kakeibo.Modules.Identity.Features.Register;

public sealed class RegisterEndpoint : IEndpoint
{
    public sealed record RegisterRequest(
        string Email, string Password, string Name,
        string Language, string Timezone, string Currency);

    public sealed record RegisterResponse(
        Guid Id, string Email, string Name, string Language,
        string Timezone, string Currency, bool IsVerified, string CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", HandleAsync)
            .WithTags("Identity")
            .WithValidation<RegisterRequest>();
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request, RegisterHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);

        return result.IsSuccess
            ? TypedResults.Created($"/api/users/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "conflict" => TypedResults.Conflict(result.Error),
                "validation" => TypedResults.UnprocessableEntity(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500),
            };
    }
}

public sealed class RegisterValidator : AbstractValidator<RegisterEndpoint.RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Language).NotEmpty().Must(l => l is "en" or "es" or "ja");
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
```

**Frontend (TypeScript)**:
```typescript
// src/services/auth.ts
import { apiClient } from '@/lib/api/client';

interface RegisterRequest {
  email: string;
  password: string;
  name: string;
  language: string;
  timezone: string;
  currency: string;
}

interface RegisterResponse {
  id: string;
  email: string;
  name: string;
  language: string;
  timezone: string;
  currency: string;
  isVerified: boolean;
  createdAt: string;
}

export async function register(data: RegisterRequest): Promise<RegisterResponse> {
  const response = await apiClient.post<RegisterResponse>('/api/auth/register', data);
  return response.data;
}
```

### 9.2 Login

**Request**:
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecureP@ssw0rd!"
}
```

**Success Response (200)**:
```json
{
  "accessToken": "eyJhbGciOiJIUzUxMiIs...",
  "expiresIn": 900,
  "user": {
    "id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
    "email": "alice@example.com",
    "name": "Alice Johnson"
  }
}
```

The refresh token is set as an HttpOnly cookie (web) or returned in the response body (mobile, see KB-007).

```
Set-Cookie: refreshToken=eyJ...; HttpOnly; Secure; SameSite=Strict; Path=/api/auth; Max-Age=604800
```

### 9.3 Create Personal Wallet

**Request**:
```
POST /api/wallets
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{
  "name": "Checking Account",
  "description": "Primary bank account",
  "icon": "wallet",
  "color": "#3B82F6",
  "initialBalance": 2450.75,
  "currency": "USD"
}
```

**Success Response (201)**:
```
HTTP/1.1 201 Created
Location: /api/wallets/01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7
Content-Type: application/json

{
  "id": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "name": "Checking Account",
  "description": "Primary bank account",
  "icon": "wallet",
  "color": "#3B82F6",
  "balance": 2450.75,
  "currency": "USD",
  "isDefault": true,
  "isArchived": false,
  "createdAt": "2026-02-15T10:30:00Z",
  "updatedAt": "2026-02-15T10:30:00Z"
}
```

**Conflict Error (409)**:
```json
{
  "code": "conflict",
  "message": "A wallet with name 'Checking Account' already exists."
}
```

### 9.4 Record Transaction in Personal Wallet

**Request**:
```
POST /api/wallets/01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7/transactions
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...
Idempotency-Key: 660e8400-e29b-41d4-a716-446655440001

{
  "concept": "Morning coffee at Cafe Luna",
  "amount": 4.50,
  "date": "2026-02-15",
  "categoryId": "sys-cat-food",
  "subcategoryId": null,
  "notes": null
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-9c4b-7d20-9a15-d4e9f5a6b7c8",
  "walletId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "concept": "Morning coffee at Cafe Luna",
  "amount": 4.50,
  "date": "2026-02-15",
  "categoryId": "sys-cat-food",
  "categoryName": "Food & Dining",
  "subcategoryId": null,
  "subcategoryName": null,
  "transactionType": "normal",
  "isForecast": false,
  "notes": null,
  "createdAt": "2026-02-15T08:30:00Z",
  "updatedAt": "2026-02-15T08:30:00Z"
}
```

### 9.5 Record Transaction in Shared Wallet (with Splits)

**Request**:
```
POST /api/shared-wallets/01926f4e-aaaa-7d20-9a15-c3d8e4f5a6b7/transactions
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...
Idempotency-Key: 770e8400-e29b-41d4-a716-446655440002

{
  "concept": "Apartment rent",
  "amount": 1200.00,
  "date": "2026-02-01",
  "categoryId": "sys-cat-housing",
  "payerUserId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "splitType": "equal",
  "splits": [
    { "userId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7" },
    { "userId": "01926f4e-bbbb-7d20-9a15-d4e9f5a6b7c8" }
  ]
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-cccc-7d20-9a15-e5f0a6b7c8d9",
  "sharedWalletId": "01926f4e-aaaa-7d20-9a15-c3d8e4f5a6b7",
  "concept": "Apartment rent",
  "amount": 1200.00,
  "date": "2026-02-01",
  "categoryId": "sys-cat-housing",
  "categoryName": "Housing & Utilities",
  "transactionType": "normal",
  "splits": [
    {
      "id": "01926f4e-dddd-7d20-9a15-f6a1b7c8d9e0",
      "userId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
      "userName": "Alice Johnson",
      "amount": 600.00,
      "splitType": "equal",
      "status": "settled",
      "isPayer": true,
      "owedToUserId": null
    },
    {
      "id": "01926f4e-eeee-7d20-9a15-a7b2c8d9e0f1",
      "userId": "01926f4e-bbbb-7d20-9a15-d4e9f5a6b7c8",
      "userName": "Bob Smith",
      "amount": 600.00,
      "splitType": "equal",
      "status": "pending",
      "isPayer": false,
      "owedToUserId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7"
    }
  ],
  "createdAt": "2026-02-01T12:00:00Z"
}
```

### 9.6 Percentage Split Example

**Request** (70/30 split):
```json
{
  "concept": "Apartment rent",
  "amount": 1000.00,
  "date": "2026-02-01",
  "categoryId": "sys-cat-housing",
  "payerUserId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "splitType": "percentage",
  "splits": [
    { "userId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7", "percentage": 60 },
    { "userId": "01926f4e-bbbb-7d20-9a15-d4e9f5a6b7c8", "percentage": 40 }
  ]
}
```

**Validation Error (422)** -- percentages do not sum to 100:
```json
{
  "code": "validation",
  "message": "Split percentages must sum to exactly 100%.",
  "errors": {
    "Splits": ["Split percentages sum to 90%. They must total exactly 100%."]
  }
}
```

### 9.7 Custom Amount Split Example

**Request**:
```json
{
  "concept": "Weekend grocery shopping",
  "amount": 75.00,
  "date": "2026-02-15",
  "categoryId": "sys-cat-food",
  "payerUserId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "splitType": "custom_amount",
  "splits": [
    { "userId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7", "amount": 45.00 },
    { "userId": "01926f4e-bbbb-7d20-9a15-d4e9f5a6b7c8", "amount": 30.00 }
  ]
}
```

### 9.8 Create Budget

**Request**:
```
POST /api/budgets
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...

{
  "categoryId": "sys-cat-food",
  "periodYear": 2026,
  "periodMonth": 2,
  "amount": 400.00,
  "alertThreshold": 80
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-ffff-7d20-9a15-b8c3d9e0f1a2",
  "categoryId": "sys-cat-food",
  "categoryName": "Food & Dining",
  "periodYear": 2026,
  "periodMonth": 2,
  "amount": 400.00,
  "spent": 0.00,
  "remaining": 400.00,
  "percentageUsed": 0.0,
  "alertThreshold": 80,
  "createdAt": "2026-02-01T00:00:00Z"
}
```

### 9.9 Create Savings Goal

**Request**:
```
POST /api/goals
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...

{
  "name": "Europe Vacation",
  "targetAmount": 5000.00,
  "targetDate": "2026-12-31",
  "linkedWalletId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "currentAmount": 500.00
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-1111-7d20-9a15-c9d4e0f1a2b3",
  "name": "Europe Vacation",
  "targetAmount": 5000.00,
  "currentAmount": 500.00,
  "targetDate": "2026-12-31",
  "linkedWalletId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "linkedWalletName": "Checking Account",
  "progressPercentage": 10.0,
  "isActive": true,
  "isAchieved": false,
  "daysRemaining": 319,
  "dailyNeeded": 14.11,
  "createdAt": "2026-02-15T10:00:00Z"
}
```

### 9.10 Create Recurring Pattern

**Request**:
```
POST /api/recurring-patterns
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...

{
  "walletId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "concept": "Monthly rent",
  "amount": 1200.00,
  "categoryId": "sys-cat-housing",
  "frequency": "monthly",
  "dayOfMonth": 1,
  "startDate": "2026-03-01",
  "recurrenceEndDate": null
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-2222-7d20-9a15-dae5f1a2b3c4",
  "walletId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "walletName": "Checking Account",
  "concept": "Monthly rent",
  "amount": 1200.00,
  "categoryId": "sys-cat-housing",
  "categoryName": "Housing & Utilities",
  "frequency": "monthly",
  "dayOfMonth": 1,
  "startDate": "2026-03-01",
  "recurrenceEndDate": null,
  "isActive": true,
  "nextOccurrence": "2026-03-01",
  "createdAt": "2026-02-15T10:00:00Z"
}
```

### 9.11 Send Shared Wallet Invitation

**Request**:
```
POST /api/shared-wallets/01926f4e-aaaa-7d20-9a15-c3d8e4f5a6b7/invitations
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...

{
  "email": "bob@example.com"
}
```

**Success Response (201)**:
```json
{
  "id": "01926f4e-3333-7d20-9a15-ebf6a2b3c4d5",
  "sharedWalletId": "01926f4e-aaaa-7d20-9a15-c3d8e4f5a6b7",
  "sharedWalletName": "Apartment Expenses",
  "inviteeEmail": "bob@example.com",
  "status": "pending",
  "expiresAt": "2026-02-22T10:00:00Z",
  "createdAt": "2026-02-15T10:00:00Z"
}
```

### 9.12 Record Settlement

**Request**:
```
POST /api/shared-wallets/01926f4e-aaaa-7d20-9a15-c3d8e4f5a6b7/settlements
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzUxMiIs...

{
  "fromUserId": "01926f4e-bbbb-7d20-9a15-d4e9f5a6b7c8",
  "toUserId": "01926f4e-8b3a-7d20-9a15-c3d8e4f5a6b7",
  "amount": 525.00,
  "notes": "Paid via bank transfer"
}
```

**Success Response (201)**:
```json
{
  "settledSplits": 3,
  "totalSettled": 525.00,
  "remainingDebt": 0.00,
  "settlementDate": "2026-02-15T14:00:00Z"
}
```

### 9.13 Frontend API Service Pattern (TypeScript)

```typescript
// src/services/wallets.ts
import { apiClient } from '@/lib/api/client';
import { withIdempotency } from '@/lib/api/idempotency';
import type { PaginatedResponse } from '@/types/api';

// --- Types ---

export interface Wallet {
  id: string;
  name: string;
  description: string | null;
  icon: string | null;
  color: string | null;
  balance: number;
  currency: string;
  isDefault: boolean;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWalletRequest {
  name: string;
  description?: string;
  icon?: string;
  color?: string;
  initialBalance: number;
  currency: string;
}

// --- API Calls ---

export async function listWallets(
  params?: { isArchived?: boolean; page?: number; pageSize?: number },
): Promise<PaginatedResponse<Wallet>> {
  const response = await apiClient.get<PaginatedResponse<Wallet>>('/api/wallets', { params });
  return response.data;
}

export async function getWallet(id: string): Promise<Wallet> {
  const response = await apiClient.get<Wallet>(`/api/wallets/${id}`);
  return response.data;
}

export async function createWallet(data: CreateWalletRequest): Promise<Wallet> {
  const response = await apiClient.post<Wallet>('/api/wallets', data, withIdempotency());
  return response.data;
}

export async function updateWallet(
  id: string, data: Partial<CreateWalletRequest>,
): Promise<Wallet> {
  const response = await apiClient.patch<Wallet>(`/api/wallets/${id}`, data);
  return response.data;
}

export async function deleteWallet(id: string): Promise<void> {
  await apiClient.delete(`/api/wallets/${id}`);
}

export async function archiveWallet(id: string): Promise<void> {
  await apiClient.post(`/api/wallets/${id}/archive`);
}

export async function restoreWallet(id: string): Promise<void> {
  await apiClient.post(`/api/wallets/${id}/restore`);
}

export async function setDefaultWallet(id: string): Promise<void> {
  await apiClient.post(`/api/wallets/${id}/set-default`);
}
```

---

## 10. Date/Time Format

### 10.1 Storage and Serialization

| Context | Type | Format | Example |
|---------|------|--------|---------|
| Database (PostgreSQL) | `timestamptz` | UTC | `2026-02-15 10:30:00+00` |
| Backend (C#) | `NodaTime.Instant` | UTC | `Instant.FromUtc(2026, 2, 15, 10, 30)` |
| Backend (C#, dates only) | `NodaTime.LocalDate` | Date only | `new LocalDate(2026, 2, 15)` |
| JSON serialization | ISO 8601 string | UTC with `Z` suffix | `"2026-02-15T10:30:00Z"` |
| JSON serialization (date only) | ISO 8601 date | Date only | `"2026-02-15"` |
| Frontend (TypeScript) | `string` | ISO 8601 | `"2026-02-15T10:30:00Z"` |
| Frontend display | Formatted | User's timezone | `"Feb 15, 2026, 5:30 AM"` |

### 10.2 Backend: NodaTime Configuration

NodaTime is the sole date/time library. `DateTime`, `DateTimeOffset`, and `DateOnly` are prohibited (see tech-stack.md).

```csharp
// Configured in Program.cs via NpgsqlDataSourceBuilder
builder.Services.AddDbContext<WalletsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.UseNodaTime(); // Maps NodaTime types to PostgreSQL timestamptz/date
        npgsql.MigrationsHistoryTable("__ef_migrations_history", WalletsDbContext.SchemaName);
    })
    .UseSnakeCaseNamingConvention());

// JSON serialization configured in Program.cs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});
```

### 10.3 Backend: Getting Current Time

```csharp
// Always use SystemClock.Instance, never DateTime.UtcNow
var now = SystemClock.Instance.GetCurrentInstant();        // Instant (timestamp)
var today = now.InUtc().Date;                               // LocalDate (date only)
var zoned = now.InZone(DateTimeZoneProviders.Tzdb["America/New_York"]); // ZonedDateTime
```

### 10.4 Frontend: Displaying Dates with date-fns

The frontend receives ISO 8601 UTC strings from the API and formats them in the user's local timezone using `date-fns`.

```typescript
// src/utils/date.ts
import { format, formatDistanceToNow, parseISO } from 'date-fns';
import { toZonedTime } from 'date-fns-tz';
import { useUserStore } from '@/stores/user';

// Formats an ISO 8601 UTC string to the user's local timezone.
export function formatDateTime(isoString: string): string {
  const userStore = useUserStore();
  const timezone = userStore.timezone ?? 'UTC';
  const utcDate = parseISO(isoString);
  const zonedDate = toZonedTime(utcDate, timezone);
  return format(zonedDate, 'MMM d, yyyy, h:mm a');
}

// Formats a date-only string (no timezone conversion needed).
export function formatDate(dateString: string): string {
  const date = parseISO(dateString);
  return format(date, 'MMM d, yyyy');
}

// Formats a relative time string (e.g., "2 hours ago").
export function formatRelative(isoString: string): string {
  const date = parseISO(isoString);
  return formatDistanceToNow(date, { addSuffix: true });
}

// Formats a date for API requests (date-only, no timezone).
export function toApiDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

// Formats currency amounts.
export function formatCurrency(amount: number, currency: string): string {
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}
```

### 10.5 Timezone Handling Rules

| Rule | Description |
|------|-------------|
| Storage | All timestamps stored as UTC (`Instant`) |
| Transmission | All timestamps transmitted as ISO 8601 UTC strings |
| Display | Converted to user's timezone preference in the frontend |
| Date-only fields | Transmitted as `YYYY-MM-DD` strings (no timezone component) |
| Shared wallets | Each user sees times in their own timezone |
| Recurring "day" boundary | Uses wallet creator's timezone as canonical for determining when a "day" starts/ends |
| Transaction date | `LocalDate` -- represents the calendar date the transaction occurred, not a timestamp |

### 10.6 User Timezone Preference

The user's timezone is stored in their profile and sent to the frontend on login. All display formatting uses this timezone.

```typescript
// src/stores/user.ts
import { defineStore } from 'pinia';

export const useUserStore = defineStore('user', () => {
  const timezone = ref<string>('UTC');
  const language = ref<string>('en');
  const currency = ref<string>('USD');

  function setProfile(profile: { timezone: string; language: string; currency: string }) {
    timezone.value = profile.timezone;
    language.value = profile.language;
    currency.value = profile.currency;
  }

  return { timezone, language, currency, setProfile };
});
```

---

## 11. Common Headers

### 11.1 Request Headers

| Header | Required | Description | Example |
|--------|----------|-------------|---------|
| `Content-Type` | Yes (for POST/PATCH/PUT) | Request body format | `application/json` |
| `Authorization` | Yes (authenticated endpoints) | JWT Bearer token | `Bearer eyJhbGciOi...` |
| `Idempotency-Key` | Recommended (POST/PATCH/PUT) | UUID v4 for idempotency | `550e8400-e29b-41d4-a716-446655440000` |
| `Accept-Language` | Optional | Preferred language for error messages | `en`, `es`, `ja` |
| `X-Request-Id` | Optional | Client-generated request ID for tracing | `req-01926f4e-8b3a` |

### 11.2 Response Headers

| Header | Always Present | Description | Example |
|--------|---------------|-------------|---------|
| `Content-Type` | Yes (with body) | Response body format | `application/json; charset=utf-8` |
| `Location` | On `201 Created` | URL of the newly created resource | `/api/wallets/01926f4e-8b3a-7d20` |
| `X-Api-Version` | Yes | Current API version | `1.0` |
| `X-Request-Id` | Yes | Server-side request ID for tracing | `0HN8Q4V2L0001:00000001` |
| `X-RateLimit-Limit` | Yes (authenticated) | Maximum requests per window | `1000` |
| `X-RateLimit-Remaining` | Yes (authenticated) | Remaining requests in current window | `995` |
| `X-RateLimit-Reset` | Yes (authenticated) | Seconds until rate limit window resets | `3600` |
| `Retry-After` | On `429` only | Seconds to wait before retrying | `60` |
| `Cache-Control` | Yes | Caching directive | `no-cache, no-store` (for API), `public, max-age=31536000, immutable` (for static assets) |

### 11.3 Rate Limit Headers

Rate limits are enforced per-user for authenticated requests and per-IP for unauthenticated requests.

| Tier | Limit | Window | Scope |
|------|-------|--------|-------|
| Authenticated | 1000 requests | 1 hour | Per user |
| Unauthenticated | 100 requests | 1 hour | Per IP |
| Transaction burst | 100 requests | 1 minute | Per user |

When the rate limit is exceeded, the response includes:

```
HTTP/1.1 429 Too Many Requests
Retry-After: 60
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 3540
Content-Type: application/json

{
  "code": "rate_limit_exceeded",
  "message": "Too many requests. Please retry after 60 seconds."
}
```

### 11.4 CORS Headers

For the Vue PWA running on a different origin during development:

| Header | Value |
|--------|-------|
| `Access-Control-Allow-Origin` | `http://localhost:5173` (dev), production domain (prod) |
| `Access-Control-Allow-Methods` | `GET, POST, PATCH, PUT, DELETE, OPTIONS` |
| `Access-Control-Allow-Headers` | `Content-Type, Authorization, Idempotency-Key, X-Request-Id` |
| `Access-Control-Allow-Credentials` | `true` (for HttpOnly cookies) |
| `Access-Control-Max-Age` | `86400` (24 hours preflight cache) |

### 11.5 Security Headers

Applied globally via middleware:

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing |
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `X-XSS-Protection` | `0` | Disable legacy XSS filter (CSP is preferred) |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Enforce HTTPS (production only) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limit referrer information |
| `Content-Security-Policy` | `default-src 'self'` | Restrict content sources |

### 11.6 Axios Client Configuration

Complete Axios client setup with authentication, error handling, and token refresh.

```typescript
// src/lib/api/client.ts
import axios from 'axios';
import { useAuthStore } from '@/stores/auth';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true, // Send HttpOnly refresh token cookie
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: attach access token to every authenticated request.
apiClient.interceptors.request.use((config) => {
  const authStore = useAuthStore();
  if (authStore.accessToken) {
    config.headers.Authorization = `Bearer ${authStore.accessToken}`;
  }
  return config;
});

// Response interceptor: handle 401 by refreshing the access token.
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null): void {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Queue requests while refreshing
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const authStore = useAuthStore();
        const newToken = await authStore.refreshAccessToken();
        processQueue(null, newToken);
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        const authStore = useAuthStore();
        authStore.logout();
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  },
);

export { apiClient };
```

---

## Appendix A: Amount and Currency Rules

| Rule | Specification |
|------|---------------|
| Minimum amount | `0.01` |
| Maximum amount | `999,999,999.99` |
| Decimal places | Exactly 2 for all monetary values |
| Currency | Single currency per user (selected at registration) |
| Currency format | ISO 4217 (3 characters): `USD`, `EUR`, `GBP`, `JPY`, etc. |
| Amount in API | Always positive decimal; transaction type (income/expense) determines direction |
| Amount in JSON | Number type (not string) |

## Appendix B: Field Length Limits

| Field | Max Length | Notes |
|-------|-----------|-------|
| User email | 255 | RFC 5321 |
| User name | 100 | |
| User password | 128 | Before hashing |
| Wallet name | 100 | constraints.md specifies 100 |
| Category name | 50 | |
| Subcategory name | 50 | |
| Transaction concept | 200 | |
| Transaction description/notes | 500 | |
| Goal name | 100 | |
| Invitation token | 128 | Secure random |
| Color code | 7 | `#RRGGBB` |
| Currency code | 3 | ISO 4217 |
| Language code | 10 | IETF BCP 47 |
| Timezone | 50 | IANA timezone identifier |
| Settlement notes | unlimited | TEXT |

## Appendix C: Enumeration Values

| Field | Valid Values |
|-------|-------------|
| Category type | `income`, `expense` |
| Transaction type | `normal`, `forecasted` |
| Split type | `equal`, `percentage`, `custom_amount` |
| Split status | `pending`, `settled` |
| Invitation status | `pending`, `accepted`, `declined`, `expired` |
| Recurrence frequency | `daily`, `weekly`, `biweekly`, `monthly`, `yearly` |
| Notification type | `shared_wallet_invitation`, `shared_expense_created`, `debt_reminder`, `recurring_due`, `budget_alert` |
| Supported languages | `en`, `es`, `ja` |

## Appendix D: Supported Currencies (MVP)

| Code | Name |
|------|------|
| USD | US Dollar |
| EUR | Euro |
| GBP | British Pound |
| JPY | Japanese Yen |
| CAD | Canadian Dollar |
| AUD | Australian Dollar |
| CHF | Swiss Franc |
| CNY | Chinese Yuan |
| INR | Indian Rupee |
| BRL | Brazilian Real |
| MXN | Mexican Peso |

---

*This document defines the exhaustive API contract specification for the Kakeibo platform. All endpoints, request/response formats, error handling, pagination, and conventions are derived from the architecture, platform, constraints, and tech-stack documentation. Backend implementations follow the feature slice pattern with Result\<T\> error handling. Frontend implementations use Axios with typed composables and Pinia stores.*
