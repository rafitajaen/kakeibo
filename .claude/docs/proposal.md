# Documentation Proposal for Kakeibo

> **Purpose**: Identify missing documentation artifacts optimized for **AI agent execution context** rather than human-only documentation. Focus on enabling autonomous task completion with minimal clarification rounds.

---

## Philosophy: Documentation for Execution

Traditional documentation answers "what does this do?" and "how does it work?".

**Agent-optimized documentation** answers:
- "What can I safely change without breaking invariants?"
- "What are the non-negotiable constraints I must preserve?"
- "What patterns should I replicate when adding new code?"
- "What are the failure modes and edge cases I must handle?"
- "What tradeoffs were already considered and rejected?"

---

## Missing Artifacts by Strategic Value

### Tier S — Critical Execution Context (Create These First)

These documents prevent the most common failure modes in autonomous code generation.

---

#### 1. `security.md` — Security Constraints & Attack Surface

**Why Claude needs this**: Financial applications have strict security requirements. Without explicit rules, agents will:
- Store JWTs in localStorage (XSS vulnerability)
- Skip input sanitization (SQL injection, XSS)
- Use weak password hashing (BCrypt with low rounds, plain SHA)
- Miss authorization checks (IDOR vulnerabilities)
- Expose sensitive data in logs

**Must contain**:

```markdown
# Security

## Non-Negotiable Rules

### Authentication
- JWT access token: 15-minute expiry, stored in memory only (Pinia ref)
- Refresh token: 7-day expiry, HttpOnly cookie (web), Capacitor Preferences (mobile)
- Password hashing: PBKDF2-SHA512, 600,000 iterations, 32-byte salt
- Session invalidation: Clear both tokens, server-side token blacklist (Redis, TTL = refresh token expiry)

### Authorization
- Every endpoint MUST verify user identity via JWT middleware
- Shared wallet operations MUST verify membership via `WalletMember` table
- Personal wallet operations MUST verify ownership via `wallet.user_id = current_user.id`
- No admin bypass — SuperAdmin role only for user management, NOT financial data access

### Input Validation
- Server-side validation REQUIRED even if client validates
- FluentValidation for all request DTOs
- SQL injection: Use parameterized queries ONLY (EF Core protects by default)
- XSS prevention: HTML encode all user-generated content before display
- Path traversal: Reject file paths with `..`, validate against whitelist

### Sensitive Data
- NEVER log passwords, tokens, credit card numbers, or SSNs
- NEVER return password hashes in API responses
- NEVER expose internal IDs in error messages (use correlation IDs)
- Database: Use a single `public` schema; all DbSets in `AppDbContext` (already enforced)

### Rate Limiting
- Authentication endpoints: 5 requests/minute per IP (brute force protection)
- Transaction creation: 100 requests/minute per user (burst protection)
- General API: 1000 requests/hour per user

### CORS
- Production: Whitelist exact origins (https://kakeibo.com, https://app.kakeibo.com)
- Development: Allow localhost:5173, localhost:3000
- Credentials: true (for cookies)

## Prohibited Patterns

❌ **NEVER**:
- Store JWT in localStorage or sessionStorage
- Use `DateTime.Now` (use NodaTime `SystemClock.Instance.GetCurrentInstant()`)
- Use `Guid.CreateVersion7()` directly (use `Guid7.NewGuid()` wrapper)
- Use BCrypt or Argon2id (use PBKDF2-SHA512)
- Hash passwords in frontend (always send plain password over HTTPS, hash server-side)
- Use magic strings for roles/permissions (use constants)
- Skip authorization checks "temporarily"
- Return stack traces to client (log them, return generic error)

## Security Checklist (for new features)

When adding a new feature, verify:

- [ ] All endpoints require authentication (except login, register, password reset)
- [ ] Authorization checks verify ownership/membership
- [ ] All inputs validated server-side with FluentValidation
- [ ] Sensitive data never logged
- [ ] Error messages don't leak internal details
- [ ] Rate limiting appropriate for endpoint sensitivity
- [ ] SQL injection impossible (parameterized queries only)
- [ ] XSS impossible (HTML encoding, Content-Security-Policy header)
- [ ] CSRF protection via SameSite cookies + CORS

## Known Vulnerabilities to Avoid

### IDOR (Insecure Direct Object Reference)
```csharp
// ❌ BAD — No authorization check
[HttpGet("/api/wallets/{id}")]
public async Task<IResult> GetWallet(Guid id) {
    var wallet = await db.Wallets.FindAsync(id);
    return TypedResults.Ok(wallet);
}

// ✅ GOOD — Verify ownership or membership
[HttpGet("/api/wallets/{id}")]
public async Task<IResult> GetWallet(Guid id, ClaimsPrincipal user) {
    var userId = Guid.Parse(user.FindFirst("sub")!.Value);
    var wallet = await db.Wallets
        .Include(w => w.Members)
        .FirstOrDefaultAsync(w => w.Id == id);

    if (wallet == null) return TypedResults.NotFound();

    // Personal wallet: user must be owner
    if (wallet.Type == WalletType.Personal && wallet.UserId != userId)
        return TypedResults.Forbid();

    // Shared wallet: user must be member
    if (wallet.Type == WalletType.Shared && !wallet.Members.Any(m => m.UserId == userId))
        return TypedResults.Forbid();

    return TypedResults.Ok(wallet);
}
```

### Mass Assignment
```csharp
// ❌ BAD — Client can set IsAdmin = true
public record CreateUserRequest(string Email, string Password, bool IsAdmin);

// ✅ GOOD — Use separate DTOs, set sensitive fields server-side
public record CreateUserRequest(string Email, string Password);
// IsAdmin set by admin endpoint only, not exposed in public registration
```

### Timing Attacks (password verification)
```csharp
// ❌ BAD — Early return leaks timing information
if (!userExists) return Error.Unauthorized("Invalid credentials");
if (!PasswordHasher.Verify(password, user.PasswordHash)) return Error.Unauthorized("Invalid credentials");

// ✅ GOOD — Constant-time comparison, same error message
var userExists = await db.Users.AnyAsync(u => u.Email == email);
var passwordValid = userExists && PasswordHasher.Verify(password, user.PasswordHash);
if (!passwordValid) return Error.Unauthorized("Invalid email or password");
```
```

**Alternative Structure**: Could split into `security-backend.md` and `security-frontend.md` if it gets too large.

---

#### 2. `patterns.md` — Code Patterns & Anti-Patterns

**Why Claude needs this**: Without explicit patterns, agents will:
- Mix architectural styles (CQRS in one module, anemic domain in another)
- Create inconsistent naming (GetUserById vs FetchUser vs UserById)
- Violate DRY by not knowing existing utilities
- Create circular dependencies

**Must contain**:

```markdown
# Patterns

## Architectural Patterns (Must Follow)

### Feature Slice Pattern

Every feature in `Features/{Operation}/` has:
1. `{Operation}Endpoint.cs` — IEndpoint with nested Request/Response records
2. `{Operation}Handler.cs` — Plain class with HandleAsync method
3. `{Operation}Validator.cs` — FluentValidation rules

NO:
- ICommandHandler<T,R> interfaces (use plain classes)
- MediatR (use direct DI injection)
- Separate Request/Response files (nest inside Endpoint)

### Naming Conventions

| Pattern | Example | Counter-Example |
|---------|---------|-----------------|
| Endpoint classes | `CreateWalletEndpoint` | `CreateWallet`, `WalletCreator` |
| Handler classes | `CreateWalletHandler` | `CreateWalletCommandHandler`, `WalletCreationService` |
| Validator classes | `CreateWalletValidator` | `CreateWalletRequestValidator` |
| Request records | `CreateWalletRequest` (nested in Endpoint) | `CreateWalletCommand`, `CreateWalletDto` |
| Response records | `CreateWalletResponse` (nested in Endpoint) | `CreateWalletResult`, `WalletDto` |
| DbContext | `AppDbContext` (single, shared) | `{Domain}DbContext`, `{Domain}Database` |
| Entity configurations | `{Entity}Configuration` | `{Entity}Map`, `{Entity}EntityConfig` |

### Primary Constructors (Mandatory)

```csharp
// ✅ GOOD — C# 12 primary constructor
public sealed class CreateWalletHandler(
    AppDbContext db,
    IEventBus eventBus,
    ILogger<CreateWalletHandler> logger)
{
    public async Task<Result<CreateWalletResponse>> HandleAsync(
        CreateWalletRequest request, CancellationToken ct)
    {
        // Use db, eventBus, logger directly
    }
}

// ❌ BAD — Traditional constructor
public sealed class CreateWalletHandler
{
    private readonly AppDbContext _db;
    private readonly IEventBus _eventBus;

    public CreateWalletHandler(AppDbContext db, IEventBus eventBus)
    {
        _db = db;
        _eventBus = eventBus;
    }
}
```

### Result<T> Pattern (Error Handling)

```csharp
// ✅ GOOD — Railway-oriented programming
public async Task<Result<CreateWalletResponse>> HandleAsync(...)
{
    var exists = await db.Wallets.AnyAsync(w => w.Name == request.Name, ct);
    if (exists)
        return Error.Conflict($"Wallet '{request.Name}' already exists");

    var wallet = new Wallet { ... };
    db.Wallets.Add(wallet);
    await db.SaveChangesAsync(ct);

    return new CreateWalletResponse(wallet.Id, wallet.Name);
}

// Endpoint maps Result to IResult
return result.IsSuccess
    ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
    : result.Error.Code switch
    {
        "conflict" => TypedResults.Conflict(result.Error),
        "not_found" => TypedResults.NotFound(result.Error),
        "validation" => TypedResults.BadRequest(result.Error),
        _ => TypedResults.Problem(result.Error.Message, statusCode: 500),
    };

// ❌ BAD — Throw exceptions for business logic failures
if (exists)
    throw new ConflictException($"Wallet '{request.Name}' already exists");
```

### In-Process Event Pattern

```csharp
// ✅ GOOD — Handler publishes event via IEventBus (fire-and-forget)
// Entity is a plain class inheriting Entity — no domain events list, no AddDomainEvent
public class Wallet : Entity
{
    public string Name { get; set; } = default!;
    public WalletType Type { get; set; }
    public Guid UserId { get; set; }
}

// Feature handler — publishes event before SaveChangesAsync
public sealed class CreateWalletHandler(AppDbContext db, IEventBus eventBus)
{
    public async Task<Result<CreateWalletResponse>> HandleAsync(
        CreateWalletRequest request, CancellationToken ct)
    {
        var wallet = new Wallet { Name = request.Name, Type = request.Type, UserId = request.UserId };
        db.Wallets.Add(wallet);

        // ✅ Fire-and-forget via ChannelEventBus — does not block SaveChangesAsync
        eventBus.Publish(new WalletCreatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = SystemClock.Instance.GetCurrentInstant(),
            WalletId = wallet.Id,
        });

        await db.SaveChangesAsync(ct);
        return new CreateWalletResponse(wallet.Id, wallet.Name);
    }
}

// Event handler (auto-registered via Scrutor — IEventHandler<T>)
internal sealed class WalletCreatedEventHandler : IEventHandler<WalletCreatedEvent>
{
    public async Task HandleAsync(WalletCreatedEvent @event, CancellationToken ct)
    {
        // Downstream side-effects: notifications, auditing, etc.
    }
}
```

## Anti-Patterns (Never Do This)

### ❌ Anemic Domain Model
```csharp
// BAD — All logic in service layer, entities are just data bags
public class Wallet
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Balance { get; set; }
}

public class WalletService
{
    public void Debit(Wallet wallet, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException();
        if (wallet.Balance < amount) throw new InvalidOperationException();
        wallet.Balance -= amount;
    }
}

// GOOD — Business logic in entity
public class Wallet : Entity
{
    public string Name { get; private set; } = default!;
    public decimal Balance { get; private set; }

    public Result Debit(decimal amount)
    {
        if (amount <= 0) return Error.Validation("Amount must be positive");
        if (Balance < amount) return Error.Validation("Insufficient balance");

        Balance -= amount;
        // Event published by the handler via IEventBus.Publish()
        return Result.Success();
    }
}
```

### ❌ God Classes
```csharp
// BAD — WalletService does everything
public class WalletService
{
    public Task CreateWallet(...) { }
    public Task DeleteWallet(...) { }
    public Task AddMember(...) { }
    public Task RemoveMember(...) { }
    public Task CalculateDebts(...) { }
    public Task RecordSettlement(...) { }
    // ... 20 more methods
}

// GOOD — One handler per operation (vertical slices)
Features/CreateWallet/CreateWalletHandler.cs
Features/DeleteWallet/DeleteWalletHandler.cs
Features/InviteMember/InviteMemberHandler.cs
...
```

### ❌ Circular Handler Dependencies
```csharp
// BAD — CreateWalletHandler depends on a handler that depends back on Wallets
// This creates a circular dependency graph.

// GOOD — Use IEventBus.Publish() for decoupled cross-domain communication.
// Domain A publishes an event → Domain B's IEventHandler<T> reacts independently.
// Direct handler injection is allowed for synchronous queries (read-only calls).
```

## Utility Patterns

### Date/Time Handling
```csharp
// ✅ ALWAYS use NodaTime
var now = SystemClock.Instance.GetCurrentInstant();
var today = now.InUtc().Date; // LocalDate

// ❌ NEVER use BCL DateTime
var now = DateTime.UtcNow; // PROHIBITED
var today = DateOnly.FromDateTime(DateTime.Now); // PROHIBITED
```

### GUID Generation
```csharp
// ✅ Use Guid7 wrapper for entity IDs (correct byte order for PostgreSQL)
var id = Guid7.NewGuid().Value;

// ❌ NEVER use Guid.CreateVersion7() directly
var id = Guid.CreateVersion7(); // PROHIBITED — wrong byte order
```

### Password Hashing
```csharp
// ✅ Use PasswordHasher utility (PBKDF2-SHA512)
var hash = PasswordHasher.HashPassword(plainPassword);
var isValid = PasswordHasher.VerifyPassword(plainPassword, hash);

// ❌ NEVER use BCrypt or Argon2id
using BCrypt.Net; // PROHIBITED
using Konscious.Security.Cryptography; // PROHIBITED (Argon2id)
```
```

**Alternative Structure**: Could be split into:
- `patterns-backend.md` (C# patterns)
- `patterns-frontend.md` (Vue patterns)
- `patterns-database.md` (EF Core patterns)

---

#### 3. `testing.md` — Testing Strategy & Patterns

**Why Claude needs this**: Without test patterns, agents will:
- Write brittle tests that break on refactoring
- Skip critical test scenarios
- Use wrong mocking approach
- Create flaky integration tests

**Must contain**:

```markdown
# Testing

## Testing Philosophy

- **Domain Unit Tests**: Pure logic, no dependencies, fast (~1ms each)
- **Handler Unit Tests**: Mocked DbContext, verify business logic
- **Integration Tests**: Real PostgreSQL (Testcontainers), verify end-to-end flow
- **Architecture Tests**: Enforce naming conventions (handlers, endpoints, validators, configurations)

## Coverage Requirements

| Level | Minimum Coverage | What to Test |
|-------|------------------|--------------|
| Domain entities | 100% | All business logic, domain events, validation |
| Handlers | 90% | Happy path + all error branches |
| Endpoints | 80% | Request/response mapping, validation |
| Integration | Critical paths | End-to-end user journeys |

## Test Naming Convention

```csharp
// Pattern: {MethodName}_{Scenario}_{ExpectedBehavior}
public class CreateWalletHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesWallet() { }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsConflictError() { }

    [Fact]
    public async Task HandleAsync_EmptyName_ReturnsValidationError() { }
}
```

## Domain Unit Tests Pattern

```csharp
// Tests for Wallet.Debit() method
public class WalletTests
{
    [Fact]
    public void Debit_ValidAmount_DecreasesBalance()
    {
        // Arrange
        var wallet = Wallet.Create("Test", WalletType.Personal, Guid.NewGuid());
        wallet.Credit(100m); // Helper to set balance

        // Act
        var result = wallet.Debit(30m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(70m, wallet.Balance);
    }

    [Fact]
    public void Debit_InsufficientBalance_ReturnsError()
    {
        // Arrange
        var wallet = Wallet.Create("Test", WalletType.Personal, Guid.NewGuid());
        wallet.Credit(50m);

        // Act
        var result = wallet.Debit(100m);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
        Assert.Contains("Insufficient balance", result.Error.Message);
        Assert.Equal(50m, wallet.Balance); // Balance unchanged
    }

    [Fact]
    public void Debit_NegativeAmount_ReturnsError()
    {
        // Arrange
        var wallet = Wallet.Create("Test", WalletType.Personal, Guid.NewGuid());

        // Act
        var result = wallet.Debit(-10m);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    // Note: Entity does not have a DomainEvents list in the Simple Monolith.
    // Test event publication by mocking IEventBus in handler tests instead.
}
```

## Handler Unit Tests Pattern

```csharp
// Use Testcontainers with real PostgreSQL — EF Core InMemory is prohibited
public class CreateWalletHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesWalletAndReturnsResponse()
    {
        // Arrange — real PostgreSQL via Testcontainers factory
        await using var db = await TestDbContextFactory.CreateAsync();
        var eventBus = Substitute.For<IEventBus>();
        var handler = new CreateWalletHandler(db, eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest("Checking", WalletType.Personal, 1000m);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Checking", result.Value.Name);
        Assert.Equal(1000m, result.Value.Balance);

        // Verify wallet persisted
        var wallet = await db.Wallets.FirstOrDefaultAsync();
        Assert.NotNull(wallet);
        Assert.Equal("Checking", wallet.Name);

        // Verify event published (IEventBus.Publish is fire-and-forget, not async)
        eventBus.Received(1).Publish(
            Arg.Is<WalletCreatedEvent>(e => e.WalletId == wallet.Id));
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsConflictError()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        db.Wallets.Add(new Wallet { Name = "Checking", Type = WalletType.Personal });
        await db.SaveChangesAsync();

        var eventBus = Substitute.For<IEventBus>();
        var handler = new CreateWalletHandler(db, eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest("Checking", WalletType.Personal, 1000m);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
        Assert.Contains("already exists", result.Error.Message);

        // Verify no event published
        eventBus.DidNotReceive().Publish(Arg.Any<IEvent>());
    }
}
```

## Integration Tests Pattern (Testcontainers)

```csharp
// Preferred: use TestDbContextFactory (static shared container, fresh DB per test)
public class WalletsIntegrationTests
{
    [Fact]
    public async Task CreateWallet_EndToEnd_PersistsToDatabase()
    {
        // Arrange — fresh AppDbContext with real PostgreSQL
        await using var db = await TestDbContextFactory.CreateAsync();
        var eventBus = Substitute.For<IEventBus>();
        var handler = new CreateWalletHandler(db, eventBus);
        var request = new CreateWalletEndpoint.CreateWalletRequest("Savings", WalletType.Personal, 5000m);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify persisted (fresh query)
        var wallet = await db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == result.Value.Id);

        Assert.NotNull(wallet);
        Assert.Equal("Savings", wallet.Name);
        Assert.Equal(5000m, wallet.Balance);
    }
}
```

## Architecture Tests Pattern

```csharp
// Single assembly — all code lives in Kakeibo.Api
public class NamingConventionTests
{
    private static readonly Assembly ApiAssembly =
        typeof(Kakeibo.Api.Persistence.AppDbContext).Assembly;

    [Fact]
    public void Endpoints_ShouldEndWith_EndpointSuffix()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEndpoint))
            .Should()
            .HaveNameEndingWith("Endpoint")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void EventHandlers_ShouldEndWith_Handler()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEventHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void EntityConfigurations_ShouldEndWith_Configuration()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .HaveNameEndingWith("Configuration")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
```

## Test Data Builders Pattern

```csharp
// Builder for complex test setup
public class WalletBuilder
{
    private string _name = "Test Wallet";
    private WalletType _type = WalletType.Personal;
    private decimal _balance = 0m;
    private Guid _userId = Guid.NewGuid();

    public WalletBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WalletBuilder WithBalance(decimal balance)
    {
        _balance = balance;
        return this;
    }

    public WalletBuilder AsShared()
    {
        _type = WalletType.Shared;
        return this;
    }

    public Wallet Build()
    {
        var wallet = Wallet.Create(_name, _type, _userId);
        if (_balance > 0)
            wallet.Credit(_balance);
        return wallet;
    }
}

// Usage
var wallet = new WalletBuilder()
    .WithName("Vacation Fund")
    .WithBalance(1000m)
    .Build();
```

## What NOT to Test

❌ Don't test:
- EF Core internals (trust the framework)
- Third-party library behavior
- Auto-generated code (EF migrations)
- Simple property getters/setters
- Constructor parameter assignment

✅ Do test:
- Business logic and domain rules
- Validation logic
- Error handling paths
- Integration event publication
- Database queries (via integration tests)
```

**Alternative Structure**: Could be one file with ToC, or split by level:
- `testing-unit.md`
- `testing-integration.md`
- `testing-e2e.md`

---

### Tier A — Execution Accelerators (Create These Next)

#### 4. `api-contracts.md` — API Design Standards

**Why Claude needs this**: Consistent API prevents:
- Mixed status code usage (returning 200 for errors)
- Inconsistent error formats
- Pagination drift (offset in one endpoint, cursor in another)
- Versioning chaos

**Must contain**:

```markdown
# API Contracts

## HTTP Status Codes (Standard Mapping)

| Status | When to Use | Example |
|--------|-------------|---------|
| 200 OK | Successful GET, PATCH (no content change) | Get wallet, Update settings |
| 201 Created | Successful POST that creates a resource | Create wallet, Record transaction |
| 204 No Content | Successful DELETE or operation with no response body | Archive wallet, Delete transaction |
| 400 Bad Request | Validation error (client fault) | Invalid amount, Missing required field |
| 401 Unauthorized | Missing or invalid authentication token | JWT expired, No token provided |
| 403 Forbidden | Authenticated but not authorized | Accessing other user's wallet |
| 404 Not Found | Resource doesn't exist | Wallet ID not found |
| 409 Conflict | Resource conflict (duplicate, constraint violation) | Duplicate wallet name |
| 422 Unprocessable Entity | Semantic error (valid syntax, invalid semantics) | Insufficient balance for debit |
| 429 Too Many Requests | Rate limit exceeded | > 1000 requests/hour |
| 500 Internal Server Error | Unexpected server error (log and investigate) | Database connection failed |

## Error Response Format (Standard Envelope)

```json
{
  "error": {
    "code": "wallet.insufficient_balance",
    "message": "Insufficient balance to complete the transaction",
    "details": {
      "required": 100.00,
      "available": 50.00
    },
    "traceId": "0HMVFE8A2N0BK:00000001"
  }
}
```

**Rules**:
- `code`: Machine-readable error code (snake_case, namespaced by module)
- `message`: Human-readable description (English, sentence case)
- `details`: Optional object with context (amounts, IDs, etc.)
- `traceId`: Correlation ID for log lookup (from Activity.Current.Id)

## Success Response Format

### Single Resource
```json
{
  "id": "018f5e7a-5c3b-7890-abcd-ef1234567890",
  "name": "Checking Account",
  "type": "personal",
  "balance": 1234.56,
  "createdAt": "2025-01-15T10:30:00Z"
}
```

### Collection (Paginated)
```json
{
  "items": [
    { "id": "...", "name": "Wallet 1" },
    { "id": "...", "name": "Wallet 2" }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 123,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## Pagination Standards

**Query parameters**:
- `page`: 1-indexed page number (default: 1)
- `pageSize`: Items per page (default: 50, max: 100)

**Example**:
```
GET /api/transactions?page=2&pageSize=50
```

**Alternative (cursor-based for real-time feeds)**:
```
GET /api/transactions?after=018f5e7a-5c3b-7890-abcd-ef1234567890&limit=50
```

## Filtering, Sorting, Searching

**Filtering** (equality):
```
GET /api/transactions?walletId=abc&type=expense
```

**Sorting**:
```
GET /api/transactions?sortBy=date&sortOrder=desc
```

**Searching** (full-text):
```
GET /api/transactions?search=coffee
```

## Idempotency

**POST requests** creating resources should support idempotency keys:

```
POST /api/transactions
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{
  "amount": 50.00,
  "description": "Coffee"
}
```

- Server stores key + response for 24 hours
- Duplicate request with same key returns cached response (201 or 409)

## Versioning Strategy

**MVP**: No versioning (single version)

**Post-MVP**: URL versioning when breaking changes needed:
```
/api/v1/wallets
/api/v2/wallets
```

**Rules**:
- v1 remains supported for 6 months after v2 release
- Add deprecation warnings to v1 responses
- Never change v1 behavior (create v2 instead)

## Endpoint Naming Conventions

| Operation | Method | Path | Example |
|-----------|--------|------|---------|
| List collection | GET | `/api/{resource}` | `GET /api/wallets` |
| Get single | GET | `/api/{resource}/{id}` | `GET /api/wallets/abc` |
| Create | POST | `/api/{resource}` | `POST /api/wallets` |
| Update (full) | PUT | `/api/{resource}/{id}` | `PUT /api/wallets/abc` |
| Update (partial) | PATCH | `/api/{resource}/{id}` | `PATCH /api/wallets/abc` |
| Delete | DELETE | `/api/{resource}/{id}` | `DELETE /api/wallets/abc` |
| Action | POST | `/api/{resource}/{id}/{action}` | `POST /api/wallets/abc/archive` |

**Nested resources**:
```
GET /api/wallets/{walletId}/transactions
POST /api/wallets/{walletId}/members/{userId}/remove
```
```

---

#### 5. `performance.md` — Performance Budgets & Optimization

**Why Claude needs this**: Without performance rules, agents will:
- Create N+1 queries
- Fetch entire collections into memory
- Skip database indexes
- Use inefficient LINQ patterns

**Must contain**:

```markdown
# Performance

## Performance Budgets

| Metric | Target | Maximum | Measurement |
|--------|--------|---------|-------------|
| API response time (p50) | < 100ms | < 200ms | OpenTelemetry |
| API response time (p95) | < 300ms | < 500ms | OpenTelemetry |
| Page load (FCP) | < 1.5s | < 2.5s | Lighthouse |
| Page load (LCP) | < 2.5s | < 4.0s | Lighthouse |
| Bundle size (initial) | < 200 KB | < 300 KB | Vite build stats |

## Database Query Optimization

### Required Indexes

Every module must define indexes for:
- Foreign keys (already indexed by EF Core)
- Commonly filtered columns (user_id, wallet_id, date, type)
- Sorting columns (created_at desc)

```csharp
// WalletConfiguration.cs
public void Configure(EntityTypeBuilder<Wallet> builder)
{
    builder.HasIndex(w => w.UserId);
    builder.HasIndex(w => w.Type);
    builder.HasIndex(w => new { w.UserId, w.IsDeleted }); // Composite for filtered queries
}
```

### N+1 Query Prevention

❌ **BAD — N+1 queries**:
```csharp
var wallets = await db.Wallets.ToListAsync();
foreach (var wallet in wallets)
{
    // N queries (one per wallet)
    var memberCount = await db.WalletMembers.CountAsync(m => m.WalletId == wallet.Id);
}
```

✅ **GOOD — Single query with Include**:
```csharp
var wallets = await db.Wallets
    .Include(w => w.Members)
    .ToListAsync();

var walletsWithCount = wallets.Select(w => new
{
    Wallet = w,
    MemberCount = w.Members.Count
});
```

✅ **GOOD — Projection with GroupBy**:
```csharp
var walletsWithCount = await db.Wallets
    .GroupJoin(
        db.WalletMembers,
        w => w.Id,
        m => m.WalletId,
        (wallet, members) => new
        {
            Wallet = wallet,
            MemberCount = members.Count()
        })
    .ToListAsync();
```

### Pagination (Always)

❌ **NEVER** load entire collection:
```csharp
var transactions = await db.Transactions.ToListAsync(); // Memory explosion
```

✅ **ALWAYS** paginate:
```csharp
var transactions = await db.Transactions
    .OrderByDescending(t => t.Date)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### Asynchronous I/O

✅ **ALWAYS** use async methods:
```csharp
var wallet = await db.Wallets.FindAsync(id);
await db.SaveChangesAsync();
```

❌ **NEVER** use synchronous methods in async context:
```csharp
var wallet = db.Wallets.Find(id); // Blocks thread
db.SaveChanges(); // Blocks thread
```

### Select Only Needed Columns

❌ **BAD — Select all columns**:
```csharp
var wallets = await db.Wallets
    .Select(w => w) // Fetches all columns
    .ToListAsync();
```

✅ **GOOD — Project to DTO**:
```csharp
var wallets = await db.Wallets
    .Select(w => new WalletSummaryDto(w.Id, w.Name, w.Balance))
    .ToListAsync();
```

## Caching Strategy

### Redis (via FusionCache)

**Cache these**:
- User profile (TTL: 15 min)
- Wallet list per user (TTL: 5 min)
- System categories (TTL: 1 hour)
- Budget status (TTL: 1 min)

**Don't cache these**:
- Transaction details (real-time accuracy required)
- Balance calculations (consistency critical)
- Debt calculations (consistency critical)

```csharp
var wallets = await cache.GetOrSetAsync(
    $"user:{userId}:wallets",
    async _ => await db.Wallets.Where(w => w.UserId == userId).ToListAsync(),
    options => options.SetDuration(TimeSpan.FromMinutes(5))
);
```

## Frontend Optimization

### Code Splitting

```typescript
// ✅ GOOD — Lazy load routes
const routes = [
  {
    path: '/wallets',
    component: () => import('@/views/WalletsView.vue') // Chunk: wallets.*.js
  },
  {
    path: '/transactions',
    component: () => import('@/views/TransactionsView.vue') // Chunk: transactions.*.js
  }
]
```

### Image Optimization

```vue
<!-- ✅ GOOD — Responsive images with WebP -->
<picture>
  <source srcset="/img/logo.webp" type="image/webp">
  <img src="/img/logo.png" alt="Kakeibo" width="200" height="100">
</picture>
```

### Virtual Scrolling (for long lists)

```vue
<!-- Use vue-virtual-scroller for 100+ items -->
<RecycleScroller
  :items="transactions"
  :item-size="80"
  key-field="id"
>
  <template #default="{ item }">
    <TransactionCard :transaction="item" />
  </template>
</RecycleScroller>
```
```

---

#### 6. `git-workflow.md` — Development Process

**Why Claude needs this**: Prevents:
- Inconsistent commit messages (breaks semantic-release)
- Wrong branch strategy
- Unclear PR requirements

**Must contain**:

```markdown
# Git Workflow

## Branching Strategy

**GitHub Flow** (simple, trunk-based):

```
main (always deployable)
  ├── feature/wallet-archiving
  ├── fix/transaction-validation
  └── refactor/debt-calculation
```

**Rules**:
- `main` is always deployable (protected branch)
- Create feature branches from `main`
- Name: `{type}/{short-description}` (kebab-case)
- Types: `feature`, `fix`, `refactor`, `docs`, `test`, `chore`

## Commit Message Convention

**Conventional Commits** (enforced by commitlint):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Examples**:
```
feat(wallets): add wallet archiving endpoint
fix(transactions): validate amount precision to 2 decimals
refactor(budgets): extract spending calculation to service
docs(readme): update installation instructions
test(goals): add integration tests for goal progress
chore(deps): update EF Core to 10.0.1
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code change (no feature/fix)
- `docs`: Documentation only
- `style`: Code style (formatting, whitespace)
- `test`: Add/update tests
- `chore`: Maintenance (deps, config)
- `perf`: Performance improvement
- `ci`: CI/CD changes

**Scopes** (must match module names):
```
api, app, email, docs,
wallets, transactions, budgets, goals, recurring,
identity, notifications, auditing
```

**Breaking changes**:
```
feat(api)!: remove deprecated wallet endpoints

BREAKING CHANGE: Removed GET /api/wallets/legacy. Use GET /api/wallets instead.
```

## Pull Request Process

1. **Create PR** from feature branch to `main`
2. **Title**: Same as commit message (for squash merge)
3. **Description**: Use template:

```markdown
## Summary
Brief description of changes

## Changes
- Added wallet archiving endpoint
- Updated wallet list to filter archived wallets
- Added integration tests

## Testing
- [x] Unit tests pass
- [x] Integration tests pass
- [x] Manual testing completed

## Screenshots (if UI changes)
[Attach screenshots]

## Related Issues
Closes #123
```

4. **Quality gates** must pass:
   - Lint (oxlint, dotnet format)
   - Tests (unit, integration)
   - Build (Docker images)

5. **Code review** (required):
   - At least 1 approval
   - No unresolved comments
   - All checks green

6. **Merge**: Squash and merge (single commit to main)

## Release Process

**Automatic** (on push to `main`):
1. `semantic-release` analyzes commits
2. Determines version bump (patch, minor, major)
3. Generates CHANGELOG.md
4. Creates GitHub release
5. Tags commit
6. Builds and pushes Docker images

**Manual** (hotfix):
```bash
git checkout main
git pull
git checkout -b hotfix/critical-bug
# Fix bug
git commit -m "fix(api): resolve critical authentication bypass"
git push -u origin hotfix/critical-bug
# Create PR, merge to main
# Automatic release triggers
```
```

---

### Tier B — Domain Intelligence (Create for Complex Modules)

#### 7. `business-rules.md` — Domain Invariants & Edge Cases

**Why Claude needs this**: Prevents:
- Breaking business invariants (e.g., negative balance in cash wallet)
- Missing edge cases (e.g., settling debt when no debt exists)
- Inconsistent validation logic

**Must contain**:

```markdown
# Business Rules

## Critical Invariants (NEVER Violate)

### Wallet Invariants

1. **Balance accuracy**:
   ```
   wallet.balance = SUM(transactions affecting wallet)
   ```
   - Balance MUST equal sum of transaction impacts
   - Enforced at database level (calculated column) + application checks

2. **Ownership**:
   - Personal wallet: Exactly 1 owner (wallet.user_id)
   - Shared wallet: 1+ members (wallet_members table)
   - Cannot change wallet type after creation

3. **Archiving**:
   - Archived wallet: No new transactions
   - Existing transactions remain visible
   - Can unarchive if no conflicting wallet name

### Transaction Invariants

1. **Amount**:
   - Must be positive (> 0)
   - Max precision: 2 decimals
   - Range: 0.01 to 999,999,999.99

2. **Date**:
   - Cannot be more than 1 year in future
   - Can be in past (backdated transactions allowed)

3. **Transfer atomicity**:
   - Transfer affects exactly 2 wallets (source, destination)
   - Both balance updates succeed or both fail (database transaction)

4. **Categorization**:
   - Exactly 1 category per transaction
   - Cannot delete category if transactions reference it (archive instead)

### Debt Invariants

1. **Calculation**:
   ```
   debt(A → B) = SUM(amount A should pay) - SUM(amount A paid)
   ```
   - Debts calculated from transaction history, NEVER set manually
   - Simplified to minimum number of debts (graph reduction)

2. **Settlements**:
   - Settlement amount ≤ current debt
   - Settlement DOES NOT affect wallet balances (external payment)
   - Settlement creates audit record but no transaction

### Budget Invariants

1. **Period**:
   - Start date ≤ end date
   - Duration: 1 day to 5 years
   - Cannot overlap budgets for same category + wallet

2. **Spending calculation**:
   ```
   spent = SUM(expenses in category, wallet, period)
   ```
   - Only expense transactions count (not income, not transfers)

## Edge Cases & How to Handle

### Wallet Operations

**Creating wallet with existing name**:
```
Request: POST /api/wallets { name: "Checking" }
Existing: Wallet { name: "Checking", is_deleted: false }
Result: 409 Conflict "Wallet 'Checking' already exists"
```

**Archiving wallet with pending transactions**:
```
Allowed: Yes (archiving doesn't delete transactions)
Effect: Wallet hidden from list, transactions remain visible in history
```

**Deleting last member from shared wallet**:
```
Allowed: Yes
Effect: Wallet becomes orphaned (no members)
UI: Show warning "You are the last member. Wallet will be inaccessible to everyone."
```

### Transaction Operations

**Recording transaction with future date**:
```
Date: 2026-12-31
Today: 2026-01-15
Max future: 2027-01-15 (1 year)
Result: 200 OK (allowed, within range)

Date: 2027-06-01
Result: 400 Bad Request "Date cannot be more than 1 year in future"
```

**Editing transaction that created debt**:
```
Original: $100 expense, equal split (A pays, B owes $50)
Edit: Change to $200
Effect: Debt recalculates (B now owes $100)
Notification: Sent to both A and B
```

**Deleting transaction (soft delete)**:
```
Effect: is_deleted = true, balance reversed
Audit: DeletedAt timestamp, deleted_by user_id
Visibility: Hidden from list, visible in audit trail
Recovery: Can undelete within 30 days
```

### Debt & Settlement Operations

**Settling more than owed**:
```
Debt: A owes B $50
Settlement: A records $100 settlement
Result: 400 Bad Request "Settlement amount ($100) exceeds debt ($50)"
```

**Settling when no debt exists**:
```
Debt: A owes B $0
Settlement: A records $50 settlement
Result: 400 Bad Request "No debt exists between users"
```

**Multiple debts simplified**:
```
Before: A owes B $100, B owes C $100
After simplification: A owes C $100 (B eliminated)
Algorithm: Graph reduction (minimum debts)
```

### Budget Operations

**Budget exceeded mid-period**:
```
Budget: $500 for Food & Dining (Jan 1-31)
Spent: $520 on Jan 20
Effect: BudgetExceededEvent published
Notification: Sent to user
Action: User decides (ignore, reduce spending, adjust budget)
```

**Overlapping budgets**:
```
Budget 1: $500 for Food & Dining, Wallet A, Jan 1-31
Budget 2: $300 for Food & Dining, Wallet A, Jan 15-31
Result: 400 Bad Request "Overlapping budget exists"
```

## Validation Rules Summary

| Field | Min | Max | Precision | Example |
|-------|-----|-----|-----------|---------|
| Transaction amount | 0.01 | 999,999,999.99 | 2 decimals | 1234.56 |
| Wallet name | 1 char | 100 chars | — | "Checking Account" |
| Transaction description | 0 chars | 500 chars | — | "Coffee at Starbucks" |
| Category name | 1 char | 50 chars | — | "Dining Out" |
| Budget period | 1 day | 5 years | — | 2026-01-01 to 2026-12-31 |
```

---

#### 8. `observability.md` — Monitoring & Debugging

**Why Claude needs this**: Enables:
- Adding appropriate logging when implementing features
- Understanding what metrics to track
- Debugging production issues

**Must contain**:

```markdown
# Observability

## Structured Logging (Serilog)

### Log Levels

| Level | When to Use | Example |
|-------|-------------|---------|
| Trace | Detailed diagnostic (dev only) | Loop iterations, variable values |
| Debug | Debugging information (dev only) | Method entry/exit, parameter values |
| Information | Normal flow (production) | User login, transaction created |
| Warning | Unexpected but handled | Rate limit approached, retry attempt |
| Error | Error handled by application | Validation failure, business rule violation |
| Critical | Unhandled exception, system failure | Database connection lost, OutOfMemory |

### Logging Patterns

```csharp
// ✅ GOOD — Structured logging with context
logger.LogInformation(
    "Wallet created {WalletId} by user {UserId} with initial balance {Balance}",
    wallet.Id, userId, wallet.Balance
);

// ✅ GOOD — Error logging with exception
try
{
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateException ex)
{
    logger.LogError(ex,
        "Failed to save wallet {WalletId} for user {UserId}",
        wallet.Id, userId
    );
    throw;
}

// ❌ BAD — String interpolation (not structured)
logger.LogInformation($"Wallet {wallet.Id} created"); // Don't index individual fields

// ❌ BAD — Logging sensitive data
logger.LogInformation("User password: {Password}", password); // NEVER
```

### What to Log

✅ **DO log**:
- User actions (login, create wallet, record transaction)
- Integration events published/consumed
- External API calls (with duration)
- Background job execution
- Rate limit hits
- Validation failures
- Business rule violations

❌ **DO NOT log**:
- Passwords (plain or hashed)
- JWT tokens
- Credit card numbers
- SSNs
- Full request/response bodies (may contain secrets)

## Metrics (OpenTelemetry)

### Key Metrics

| Metric | Type | Labels | Purpose |
|--------|------|--------|---------|
| `api.request.duration` | Histogram | endpoint, status | Track API performance |
| `db.query.duration` | Histogram | query_type | Detect slow queries |
| `outbox.messages_processed` | Counter | status | Monitor outbox health |
| `cache.hits` | Counter | cache_name | Cache effectiveness |
| `cache.misses` | Counter | cache_name | Cache effectiveness |
| `wallets.created` | Counter | type | Business KPI |
| `transactions.recorded` | Counter | type | Business KPI |
| `budgets.exceeded` | Counter | — | Business alert |

### Adding Metrics

```csharp
// Counter
private static readonly Counter<long> WalletsCreated = Meter.CreateCounter<long>(
    "wallets.created",
    description: "Number of wallets created"
);

// Usage
WalletsCreated.Add(1, new KeyValuePair<string, object?>("type", wallet.Type.ToString()));

// Histogram
private static readonly Histogram<double> QueryDuration = Meter.CreateHistogram<double>(
    "db.query.duration",
    unit: "ms",
    description: "Database query duration"
);

// Usage
var stopwatch = Stopwatch.StartNew();
var wallets = await db.Wallets.ToListAsync();
stopwatch.Stop();
QueryDuration.Record(stopwatch.ElapsedMilliseconds,
    new KeyValuePair<string, object?>("query_type", "wallets.list"));
```

## Distributed Tracing

### Automatic Spans

- HTTP requests (ASP.NET Core)
- Database queries (EF Core)
- Redis operations (FusionCache)
- External HTTP calls (HttpClient)

### Custom Spans

```csharp
using var activity = Activity.StartActivity("CalculateDebts");
activity?.SetTag("wallet.id", walletId);
activity?.SetTag("member.count", members.Count);

// Complex operation here
var debts = CalculateDebtsInternal(members, transactions);

activity?.SetTag("debts.count", debts.Count);
```

## Health Checks

### Endpoint: `GET /health`

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "postgres": { "status": "Healthy", "duration": "00:00:00.0123" },
    "redis": { "status": "Healthy", "duration": "00:00:00.0045" },
    "rustfs": { "status": "Healthy", "duration": "00:00:00.0234" },
    "clickhouse": { "status": "Healthy", "duration": "00:00:00.0567" }
  }
}
```

### Adding Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"])
    .AddUrlGroup(new Uri("http://rustfs:9000/minio/health/live"), name: "rustfs", tags: ["ready"]);
```

## Dashboards

### Aspire Dashboard (Development)

- Real-time traces, metrics, logs
- URL: http://localhost:18888
- OTLP endpoint: http://localhost:18889

### Production (Grafana + Prometheus)

**Key dashboards**:
1. API Performance (p50, p95, p99 latency per endpoint)
2. Database Health (connection pool, query duration, slow queries)
3. Business Metrics (wallets created, transactions/day, active users)
4. Error Rate (4xx, 5xx by endpoint)
5. Outbox Processing (messages/sec, queue depth, retry rate)

## Alerting Rules

| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| API p95 > 500ms | 5 min sustained | Warning | Investigate slow queries |
| API p95 > 1s | 5 min sustained | Critical | Page on-call |
| Error rate > 1% | 5 min sustained | Warning | Check logs |
| Error rate > 5% | 5 min sustained | Critical | Page on-call |
| DB connection pool > 80% | 5 min sustained | Warning | Scale up |
| Outbox queue depth > 1000 | 10 min sustained | Warning | Check processor |
```

---

## Alternative Organizational Structures

### Option A: Single "agent-context.md" Mega-Document

Combine all execution-critical rules into one 10,000-line document with excellent ToC navigation.

**Pros**:
- Single source of truth
- Easy to grep/search
- One file to pass to Claude

**Cons**:
- Overwhelming length
- Hard to maintain
- Mix of concerns

### Option B: Modular Files by Domain (Recommended)

Keep separate files (security.md, patterns.md, testing.md, etc.) with strong cross-references.

**Pros**:
- Clear separation
- Easy to update single concern
- Can be composed as needed

**Cons**:
- Need to read multiple files
- Risk of duplication

### Option C: Hybrid — Core + Extensions

Create `core-rules.md` (10 critical rules, 1 page) + detailed files for deep dives.

**Example core-rules.md**:
```markdown
1. NEVER use DateTime — use NodaTime
2. NEVER use Guid.CreateVersion7() — use Guid7.NewGuid()
3. NEVER store JWT in localStorage — use memory + HttpOnly cookie
4. ALWAYS use primary constructors
5. ALWAYS return Result<T> from handlers
6. ALWAYS validate server-side (FluentValidation)
7. ALWAYS use async/await
8. ALWAYS paginate collections
9. ALWAYS log structured (Serilog with {Context})
10. NEVER cross-reference modules (use Contracts only)

→ See security.md for auth details
→ See patterns.md for code examples
→ See testing.md for test patterns
```

---

## Meta-Documentation: What Claude Really Needs

Beyond technical docs, Claude benefits from:

### 1. Decision Log (ADRs)

`adr/001-why-simple-monolith.md`:
```markdown
# ADR 001: Simple Monolith over Modular Monolith or Microservices

**Status**: Accepted

**Context**: Need to balance modularity with deployment simplicity for MVP.

**Decision**: Use a Simple Monolith (2 projects: Kakeibo.Api + Kakeibo.Tests) with vertical slices
and screaming architecture. Domain separation is enforced by folder structure and naming conventions,
not by assembly boundaries.

**Consequences**:
- ✅ Single deployment artifact (simpler ops)
- ✅ In-process communication (lower latency)
- ✅ Single `AppDbContext` (full ACID transactions across all domains)
- ✅ Maximum developer velocity for a single-developer MVP
- ❌ Cannot scale domains independently (acceptable at MVP stage)

**Alternatives Considered**:
- Microservices: Rejected (over-engineering for MVP)
- Modular Monolith (12 projects): Rejected at ~5% implementation — added complexity with no
  tangible benefit at current scale. Migrated to Simple Monolith (see KB-010).
```

### 2. Glossary (Ubiquitous Language)

`glossary.md`:
```markdown
# Glossary

| Term | Definition | Module | Related Concepts |
|------|------------|--------|------------------|
| Wallet | Financial container holding money and organizing transactions | Wallets | Account, Envelope |
| Transaction | Financial event that changes wallet balance | Transactions | Income, Expense, Transfer |
| Split | Mechanism dividing shared expense among members | Wallets | Equal, Percentage, Custom |
| Debt | Calculated amount owed between users based on transaction history | Wallets | Settlement, Balance |
| Settlement | Record of external payment to settle debt (does not affect wallet balance) | Wallets | Debt |
| IEventBus | In-process fire-and-forget event bus backed by System.Threading.Channels | Infrastructure | IEvent, IEventHandler, EventDispatcher |
```

### 3. Migration Guides

`migrations/bcrypt-to-pbkdf2.md`:
```markdown
# Migration: BCrypt → PBKDF2-SHA512

**Status**: Planned for Phase 1

**Reason**: BCrypt limited to 72-byte passwords, lower iterations than PBKDF2.

**Steps**:
1. Add `password_hash_version` column (default: 1 = BCrypt)
2. On login success, check version:
   - If version 1: Re-hash with PBKDF2, update hash + version to 2
3. After 90 days, enforce version 2 only
4. Remove BCrypt library

**Timeline**:
- Week 1: Deploy version column
- Week 2-12: Gradual rehashing on login
- Week 13: Enforce PBKDF2 only
```

---

## Recommended Creation Order

Based on highest impact for autonomous execution:

1. **security.md** (prevents critical vulnerabilities)
2. **patterns.md** (ensures consistency)
3. **testing.md** (enables verification)
4. **api-contracts.md** (frontend/backend alignment)
5. **business-rules.md** (domain correctness)
6. **performance.md** (scalability)
7. **git-workflow.md** (process clarity)
8. **observability.md** (production debugging)
9. **glossary.md** (shared vocabulary)
10. **adr/** (context for decisions)

---

## Final Recommendation

**Create Tier S documents first** (security, patterns, testing) with **Option B structure** (modular files).

These three files will:
- Prevent 80% of autonomous execution errors
- Enable high-quality code generation
- Reduce clarification rounds from ~5 to ~1 per task

The remaining files (Tier A, B) can be added incrementally as the codebase grows.

Would you like me to generate the full content for any specific document?
