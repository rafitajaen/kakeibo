# Architecture

Simple Monolith with Vertical Slices + Screaming Architecture.

---

## Tech Stack

| Component | Description |
|-----------|-------------|
| Minimal APIs | Native REPR pattern with IEndpoint interface |
| ASP.NET Core Authentication | JWT Bearer with HttpOnly cookies |
| Simple Monolith | Single project, vertical slices + screaming architecture, folder-based domain separation |
| EntityFramework | ORM with SnakeCaseConvention, NodaTime and PostgreSQL |
| FluentValidation | Model validation |
| FusionCache | Cache with Redis |
| Serilog | Structured logging |
| OpenTelemetry | Tracing, metrics and logging |
| Scalar | API documentation |
| AspNetCore.HealthChecks | Health endpoints for monitoring |
| Polly | Resilience: retries, circuit breaker, timeouts |
| System.Threading.Channels | In-memory async event bus (IEventBus, ChannelEventBus, EventDispatcher BackgroundService) |
| MailKit | SMTP client for sending emails |
| Hangfire + Hangfire.PostgreSql | Scheduled background jobs with PostgreSQL storage |
| xUnit v3 | Testing |
| Testcontainers | Docker containers for integration tests |
| Minio NuGet SDK | S3-compatible client library (used with MinIO server) |

## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| BCrypt | Use PBKDF2-SHA512 (PasswordHasher) |
| Argon2id | Use PBKDF2-SHA512 (PasswordHasher) |
| FastEndpoints | Use native Minimal APIs (IEndpoint + MapEndpoint) |
| MediatR | Use plain handler classes, no CQRS interfaces |
| Swagger | Use Scalar for API documentation |
| EF Core InMemory, SQLite in-memory | Use Testcontainers with real PostgreSQL for integration tests |
| Quartz.NET | Use Hangfire instead |
| Guid.CreateVersion7() | Little-endian byte order breaks PostgreSQL sorting. Use Guid7 wrapper (Medo.Uuid7) |
| SonarAnalyzer.CSharp | Use .editorconfig and built-in .NET analyzers instead |
| MassTransit | Use System.Threading.Channels (IEventBus + ChannelEventBus) |
| RabbitMQ | Use System.Threading.Channels (IEventBus + ChannelEventBus) |
| Keycloak | Use ASP.NET Core native JWT Bearer authentication with in-memory signing |
| Newtonsoft.Json | Use System.Text.Json (built into .NET) |
| FluentAssertions | Use xUnit v3 native Assert.* methods |
| `.WithReuse(true)` in Testcontainers | Prohibited — causes test isolation issues |
| RustFS | Abandoned alpha project (no security patches). Use MinIO server instead |

---

## Core Principles

- **Vertical Slices**: Each feature is a self-contained folder with endpoint, handler, and validator. No horizontal layers.
- **Screaming Architecture**: Folder names reflect business capabilities (`Features/Identity`, `Features/Wallets`), not technical layers.
- **Simple Monolith**: Single project, single deployment. Domain separation by folder, not by assembly.

---

## Solution Structure

```
Kakeibo.slnx
├── src/
│   ├── Kakeibo.Api/                    — Single runnable project (ASP.NET host + all domains)
│   │   ├── Common/
│   │   │   ├── Abstractions/           — Entity, Result<T>, Error, ValueObject
│   │   │   ├── Endpoints/              — IEndpoint, ValidationFilter, EndpointExtensions
│   │   │   └── Utils/                  — Guid7, PasswordHasher, DefaultSerializer, CharSets, RandomString
│   │   ├── Domain/
│   │   │   ├── Entities/               — (future) shared base entities
│   │   │   └── ValueObjects/           — (future) shared value objects
│   │   ├── Features/
│   │   │   ├── Identity/               — RegisterUser/, LoginUser/, ...
│   │   │   ├── Wallets/                — CreateWallet/, ListWallets/, InviteToWallet/, ...
│   │   │   ├── Transactions/           — RecordTransaction/, ListCategories/, ...
│   │   │   ├── Budgets/
│   │   │   ├── Goals/
│   │   │   ├── Recurring/
│   │   │   ├── Notifications/
│   │   │   └── Auditing/
│   │   ├── Infrastructure/
│   │   │   ├── Caching/                — ICacheService, FusionCacheService, CachingOptions
│   │   │   ├── Email/                  — IEmailService, EmailService, SmtpOptions, EmailRendererOptions
│   │   │   ├── Storage/                — IStorageService, StorageService, StorageOptions
│   │   │   └── Events/                 — IEvent, IEventHandler<T>, IEventBus, ChannelEventBus, EventDispatcher
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs         — Single DbContext for all domains
│   │   │   └── Configurations/         — IEntityTypeConfiguration<T> per entity
│   │   └── Program.cs
│   ├── Kakeibo.App/                    — Web app (Vue PWA)
│   └── Kakeibo.Email/                  — Email template rendering (Bun + Hono + React Email)
│
└── tests/
    └── Kakeibo.Tests/
        ├── Architecture/               — NetArchTest naming convention tests
        ├── Features/                   — Unit + integration tests per domain
        └── Integration/                — Testcontainers + real PostgreSQL
```

**Total: 2 projects** (1 source + 1 test).

---

## Namespace Convention

All code lives under `Kakeibo.Api.*`:

| Folder | Namespace |
|--------|-----------|
| `Common/Abstractions/` | `Kakeibo.Api.Common.Abstractions` |
| `Common/Endpoints/` | `Kakeibo.Api.Common.Endpoints` |
| `Common/Utils/` | `Kakeibo.Api.Common.Utils` |
| `Infrastructure/Caching/` | `Kakeibo.Api.Infrastructure.Caching` |
| `Infrastructure/Email/` | `Kakeibo.Api.Infrastructure.Email` |
| `Infrastructure/Storage/` | `Kakeibo.Api.Infrastructure.Storage` |
| `Infrastructure/Events/` | `Kakeibo.Api.Infrastructure.Events` |
| `Features/Identity/RegisterUser/` | `Kakeibo.Api.Features.Identity.RegisterUser` |
| `Persistence/` | `Kakeibo.Api.Persistence` |

---

## Kakeibo.Api — Key Abstractions

### Common/Abstractions

| Type | Description |
|------|-------------|
| `Entity` | Base class: `Guid Id` (Guid7), `Instant CreatedAt/UpdatedAt`, `Instant? DeletedAt`, computed `bool IsDeleted` |
| `ValueObject` | Structural equality via `GetEqualityComponents()` |
| `Result<T>` | Discriminated union: `IsSuccess/IsFailure`, `Value`, `Error`. Static `Success(T)`, `Failure(Error)` |
| `Error` | Record: `Error(string Code, string Message)` with factories: `NotFound`, `Validation`, `Conflict`, `Unauthorized`, `Forbidden`, `Internal` |

### Common/Endpoints

| Type | Description |
|------|-------------|
| `IEndpoint` | `static abstract void MapEndpoint(IEndpointRouteBuilder app)` |
| `EndpointExtensions` | Assembly scanning for `IEndpoint` implementations |
| `ValidationFilter<T>` | Generic FluentValidation endpoint filter |

### Common/Utils

| Type | Description |
|------|-------------|
| `Guid7` | `NewGuid()` → `Uuid7` with correct byte order for PostgreSQL indexing |
| `PasswordHasher` | PBKDF2-SHA512, salt generation, constant-time verify |
| `DefaultSerializer` | `JsonSerializerOptions` with camelCase, null handling |
| `CharSets` | `public const string` sets for random string generation |
| `RandomString` | Cryptographically secure random string generator |

### Infrastructure/Events — In-Process Event System

Replaces the Outbox Pattern + IModuleEventBus. Async in-memory communication via `System.Threading.Channels`.

| Type | Description |
|------|-------------|
| `IEvent` | Base interface: `Guid Id`, `Instant OccurredAt` |
| `IEventHandler<TEvent>` | Handler: `Task HandleAsync(TEvent, CancellationToken)` |
| `IEventBus` | Publisher: `void Publish<TEvent>(TEvent)` — fire-and-forget |
| `ChannelEventBus` | Singleton. Writes to `Channel<IEvent>`. Used directly by feature handlers |
| `EventDispatcher` | `BackgroundService`. Reads from channel, resolves `IEventHandler<T>` via DI in a new scope |

**Usage in a feature handler:**
```csharp
// Fire-and-forget — does not block SaveChangesAsync
eventBus.Publish(new TransactionRecordedEvent
{
    Id = Guid.NewGuid(),
    OccurredAt = SystemClock.Instance.GetCurrentInstant(),
    TransactionId = transaction.Id,
    WalletId = transaction.WalletId,
    Amount = transaction.Amount
});
await db.SaveChangesAsync(ct);
```

**No Outbox Pattern.** Events are in-memory only. If guaranteed delivery is needed in the future, the `IEventBus` interface can be backed by a persistent outbox without changing call sites.

---

## Feature Slice Pattern

Each feature lives in `Features/{Domain}/{Operation}/` with up to 3 files.

### Endpoint

```csharp
namespace Kakeibo.Api.Features.Wallets.CreateWallet;

public sealed class CreateWalletEndpoint : IEndpoint
{
    // Nested Request/Response records (never *Dto outside Kakeibo.Contracts equivalent)
    public sealed record CreateWalletRequest(string Name, decimal InitialBalance);
    public sealed record CreateWalletResponse(Guid Id, string Name, decimal Balance);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<CreateWalletRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreateWalletRequest request, CreateWalletHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/wallets/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
```

### Handler

Plain class with `HandleAsync`. No MediatR, no CQRS interfaces. Scrutor auto-registers by name convention (`*Handler`).
Primary constructors required.

```csharp
namespace Kakeibo.Api.Features.Wallets.CreateWallet;

// Creates a new wallet for the authenticated user.
public sealed class CreateWalletHandler(AppDbContext db, IEventBus eventBus)
{
    public async Task<Result<CreateWalletEndpoint.CreateWalletResponse>> HandleAsync(
        CreateWalletEndpoint.CreateWalletRequest request, CancellationToken ct)
    {
        var exists = await db.Wallets.AnyAsync(w => w.Name == request.Name, ct);
        if (exists)
            return Error.Conflict($"A wallet named '{request.Name}' already exists.");

        var wallet = new Wallet { Name = request.Name };
        db.Wallets.Add(wallet);

        // Publish event before SaveChangesAsync — fire-and-forget via Channel
        eventBus.Publish(new WalletCreatedEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = SystemClock.Instance.GetCurrentInstant(),
            WalletId = wallet.Id
        });

        await db.SaveChangesAsync(ct);

        return new CreateWalletEndpoint.CreateWalletResponse(wallet.Id, wallet.Name, 0m);
    }
}
```

### Validator

```csharp
namespace Kakeibo.Api.Features.Wallets.CreateWallet;

public sealed class CreateWalletValidator
    : AbstractValidator<CreateWalletEndpoint.CreateWalletRequest>
{
    public CreateWalletValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
    }
}
```

---

## Database Strategy

**Single `AppDbContext`** for all domains. Single schema (`public`). Single migrations history table.

```csharp
namespace Kakeibo.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // All DbSets defined here
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    // ...

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.UseSnakeCaseNamingConvention();
        builder.UseNodaTime();
    }
}
```

**EF Core migrations:**
```bash
dotnet ef migrations add <Name> \
  --project src/Kakeibo.Api \
  --startup-project src/Kakeibo.Api \
  --context AppDbContext \
  --output-dir Persistence/Migrations
```

---

## DI Registration

All in `Program.cs`. No per-domain registration classes.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Infrastructure — events
builder.Services.AddSingleton<ChannelEventBus>();
builder.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<ChannelEventBus>());
builder.Services.AddHostedService<EventDispatcher>();

// Infrastructure — persistence
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseNodaTime())
           .UseSnakeCaseNamingConvention());

// Infrastructure — other services
builder.Services.AddSingleton<ICacheService, FusionCacheService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddSingleton<IStorageService, StorageService>();

// Feature handlers (auto-scan by name convention)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Handler")))
    .AsSelf()
    .WithScopedLifetime());

// Event handlers (auto-scan)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Map all IEndpoint implementations in the assembly
app.MapEndpoints(typeof(Program).Assembly);

app.MapHealthChecks("/health");
app.MapScalarApiReference();
app.Run();

public partial class Program;
```

---

## Naming Conventions

| Type | Convention | Namespace |
|------|-----------|-----------|
| Feature folder | `Features/{Domain}/{Operation}/` | `Kakeibo.Api.Features.{Domain}.{Operation}` |
| Endpoint | `{Op}Endpoint : IEndpoint` | same namespace |
| Handler | `{Op}Handler` (Scrutor scan) | same namespace |
| Validator | `{Op}Validator : AbstractValidator<>` | same namespace |
| Event record | `{Name}Event : IEvent` | `Kakeibo.Api.Features.{Domain}.Events` |
| Event handler | `{EventName}Handler : IEventHandler<{Event}>` | consuming domain |
| Entity | inherits `Entity` | `Kakeibo.Api.Domain.Entities` (shared) or `Features/{Domain}/` (domain-specific) |
| EF Core config | `{Entity}Configuration : IEntityTypeConfiguration<T>` | `Kakeibo.Api.Persistence.Configurations` |
| Options class | `{Name}Options` with `const string SectionName` | `Kakeibo.Api.Infrastructure.*` |
| Logs class | `{Name}Logs` — `internal static partial class` | same namespace as consumer |

---

## Logging

All logging uses the `[LoggerMessage]` source generator. Direct `logger.Log*()` calls are prohibited (CA1848 = `error` in `.editorconfig`).

- **File**: `{Name}Logs.cs` — never inline in a handler or service.
- **Class**: `internal static partial class {Name}Logs` in the same namespace as the consumer.
- **Methods**: `internal static partial void`, first param `this ILogger logger` (extension method syntax).

```csharp
// WalletHandlerLogs.cs
namespace Kakeibo.Api.Features.Wallets.CreateWallet;

internal static partial class WalletHandlerLogs
{
    [LoggerMessage(3001, LogLevel.Information, "Wallet {WalletId} created by user {UserId}")]
    internal static partial void WalletCreated(this ILogger logger, Guid walletId, Guid userId);
}

// Call site — no class prefix, no logger arg
logger.WalletCreated(wallet.Id, userId);
```

**EventId ranges:**

| Range | Location |
|-------|----------|
| 1100–1199 | Infrastructure/Audit |
| 1200–1299 | Infrastructure/Email |
| 1300–1399 | Infrastructure/Storage |
| 1400–1499 | Infrastructure/WebPush |
| 1500–1599 | Infrastructure/Events |
| 2100–2199 | Features/Identity/Jobs |
| 2200–2299 | Features/Recurring/Jobs |
| 2300–2399 | Features/Identity/ImportData + ExportData |
| 3000–3099 | Features/Wallets |
| 3100–3199 | Features/Notifications/Events |
| 3200–3299 | Features/Friends |

---

## Testing Strategy

| Level | What | Where | Dependencies |
|-------|------|-------|-------------|
| **1. Unit** | Entity behavior, handler logic with mocks | `tests/Kakeibo.Tests/Features/{Domain}/` | NSubstitute for mocks |
| **2. Integration** | Full feature with real PostgreSQL | `tests/Kakeibo.Tests/Integration/` | `PostgreSqlContainer`, real DbContext |
| **3. Architecture** | Naming conventions | `tests/Kakeibo.Tests/Architecture/` | NetArchTest |

**Architecture tests (naming only — no cross-assembly boundary checks needed):**
- Endpoints implement `IEndpoint` and end in `Endpoint`
- Validators inherit `AbstractValidator<>` and end in `Validator`
- Event handlers implement `IEventHandler<T>` and end in `Handler`

---

## Quick Reference

### Add a new feature

1. Create folder: `src/Kakeibo.Api/Features/{Domain}/{Operation}/`
2. Create `{Op}Endpoint.cs` with nested `{Op}Request`/`{Op}Response` records
3. Create `{Op}Handler.cs` — plain class, Scrutor scans it automatically
4. Create `{Op}Validator.cs` — `AbstractValidator<{Op}Endpoint.{Op}Request>`
5. Create test: `tests/Kakeibo.Tests/Features/{Domain}/{Op}/{Op}Tests.cs`

### Add an event

1. Define in `Features/{Domain}/Events/`: `sealed record {Name}Event : IEvent`
2. In handler, call `eventBus.Publish(new {Name}Event { ... })` before `SaveChangesAsync`
3. Create handler in consuming domain: `{EventName}Handler : IEventHandler<{Event}>`
4. Handler is auto-registered by Scrutor (`.AssignableTo(typeof(IEventHandler<>))`)

### Add an entity

1. Create in `Domain/Entities/` (if shared) or `Features/{Domain}/` (if domain-specific), inheriting `Entity`
2. Create `{Entity}Configuration : IEntityTypeConfiguration<{Entity}>` in `Persistence/Configurations/`
3. `AppDbContext` picks up configurations via `ApplyConfigurationsFromAssembly`

---

*Kakeibo is a personal finance platform. The simple monolith architecture prioritizes developer velocity and maintainability for a single-developer MVP over theoretical modularity boundaries.*
