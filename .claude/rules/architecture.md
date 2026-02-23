# Architecture

Vertical Slices + Screaming Architecture + Modular Monolith.

---

## Core Principles

- **Vertical Slices**: Each feature is a self-contained folder with endpoint, handler, and validator. No horizontal layers.
- **Screaming Architecture**: Folder names reflect business capabilities (`Modules.Wallets`, `Modules.Budgets`), not technical layers.
- **Modular Monolith**: Single deployable unit with strict module boundaries. Modules communicate via contracts, never direct references.

---

## Solution Structure

```
Kakeibo.slnx
├── src/
│   ├── Kakeibo.Api/                    — Composition root (ASP.NET host)
│   ├── Kakeibo.Common/                 — Shared kernel (zero project references)
│   ├── Kakeibo.Contracts/              — Inter-module contracts (events, requests, DTOs)
│   ├── Kakeibo.Infrastructure/         — Technical cross-cutting concerns
│   │
│   ├── Kakeibo.Modules.Identity/       — Core: Authentication & users
│   ├── Kakeibo.Modules.Notifications/  — Core: Multi-channel notifications
│   ├── Kakeibo.Modules.Auditing/       — Core: Activity logs & audit trail
│   │
│   ├── Kakeibo.Modules.Wallets/        — Business: Wallets + Collaboration (merged)
│   ├── Kakeibo.Modules.Transactions/   — Business: Transactions + Categories (merged)
│   ├── Kakeibo.Modules.Budgets/        — Business: Spending limits
│   ├── Kakeibo.Modules.Goals/          — Business: Savings targets
│   └── Kakeibo.Modules.Recurring/      — Business: Pattern management
│
├── services/
│   └── Kakeibo.Email/                  — Email template rendering (Bun + Hono + React Email)
│
├── sites/
│   └── Kakeibo.App/                    — Web app (Vue PWA)
│
├── tests/
│   ├── Kakeibo.Modules.Identity.Tests/
│   ├── Kakeibo.Modules.Notifications.Tests/
│   ├── Kakeibo.Modules.Auditing.Tests/
│   ├── Kakeibo.Modules.Wallets.Tests/
│   ├── Kakeibo.Modules.Transactions.Tests/
│   ├── Kakeibo.Modules.Budgets.Tests/
│   ├── Kakeibo.Modules.Goals.Tests/
│   ├── Kakeibo.Modules.Recurring.Tests/
│   ├── Kakeibo.FunctionalTests/        — API-level tests (WebApplicationFactory)
│   └── Kakeibo.ArchitectureTests/      — Module boundary enforcement (NetArchTest)
│
├── Directory.Build.props
├── Directory.Packages.props
└── Kakeibo.slnx
```

**Total: 12 projects** (4 infrastructure + 8 modules).

**Changed from original (10 → 8 modules)**:
- ❌ Removed: `Kakeibo.Modules.Collaboration` (merged into Wallets)
- ❌ Removed: `Kakeibo.Modules.Categories` (merged into Transactions)

---

## Project Dependency Graph

```
                      Kakeibo.Api
              (Composition Root — refs ALL)
                 /        |        \
                v         v         v
        Kakeibo.Modules.*  Kakeibo.Infrastructure
              |    \         |
              v     v        v
        Kakeibo.Common  Kakeibo.Contracts
                        |
                        v
                   Kakeibo.Common
                  (zero refs)
```

| Project | Can reference | CANNOT reference |
|---------|--------------|------------------|
| `Kakeibo.Common` | NuGet packages only | Any project |
| `Kakeibo.Contracts` | `Kakeibo.Common` | Modules, Infrastructure, Api |
| `Kakeibo.Infrastructure` | `Kakeibo.Common`, `Kakeibo.Contracts` | Modules, Api |
| `Kakeibo.Modules.*` | `Kakeibo.Common`, `Kakeibo.Contracts`, `Kakeibo.Infrastructure` | Other modules, Api |
| `Kakeibo.Api` | Everything | — |

**Critical rule: No cross-module references.** Module A NEVER references Module B's project. All inter-module communication goes through `Kakeibo.Contracts` types dispatched via `IModuleClient` (sync) or `IModuleEventBus` (async). Enforced by architecture tests.

---

## Kakeibo.Common (Shared Kernel)

Zero project references. Only NuGet packages (FluentValidation, Medo.Uuid7, NodaTime, Microsoft.AspNetCore.Http.Abstractions, Microsoft.AspNetCore.Routing).

### Abstractions

| Type | Description |
|------|-------------|
| `Entity` | Base class: `Guid Id` (Guid7), `Instant CreatedAt/UpdatedAt`, `bool IsDeleted`, domain events list |
| `AggregateRoot : Entity` | Marker for aggregate roots |
| `ValueObject` | Structural equality via `GetEqualityComponents()` |
| `IDomainEvent` | Internal module event: `Guid Id`, `Instant OccurredAt` |
| `IDomainEventHandler<TEvent>` | Handler for domain events: `Task HandleAsync(TEvent, CancellationToken)`. Dispatched by `OutboxInterceptor` during `SaveChangesAsync` |
| `IIntegrationEvent` | Cross-module event (persisted in outbox): `Guid Id`, `Instant OccurredAt`, `int Version` |
| `Result<T>` | Discriminated union: `IsSuccess/IsFailure`, `Value`, `Error`. Static `Success(T)`, `Failure(Error)` |
| `Error` | Record: `Error(string Code, string Message)` with factories: `NotFound`, `Validation`, `Conflict`, `Unauthorized`, `Forbidden` |

### Endpoints

| Type | Description |
|------|-------------|
| `IEndpoint` | `static abstract void MapEndpoint(IEndpointRouteBuilder app)` |
| `EndpointExtensions` | Assembly scanning for `IEndpoint` implementations |
| `ValidationFilter<T>` | Generic FluentValidation endpoint filter |

### Modules

| Type | Description |
|------|-------------|
| `IModuleRequest<TResponse>` | Marker interface for sync inter-module requests |
| `IModuleRequestHandler<TRequest, TResponse>` | Handler: `Task<TResponse> HandleAsync(TRequest, CancellationToken)` |
| `IModuleClient` | Dispatcher: `Task<TResponse> SendAsync(IModuleRequest<TResponse>, CancellationToken)` |
| `IModuleEventBus` | Publisher: `Task PublishAsync(IIntegrationEvent, CancellationToken)` |
| `IEventConsumer<TEvent>` | Consumer: `Task ConsumeAsync(TEvent @event, CancellationToken)` — transport-agnostic interface for handling integration events |

### Persistence

| Type | Description |
|------|-------------|
| `IUnitOfWork` | Unit of work abstraction |
| `OutboxMessage` | Entity for outbox messages |

### Utils

`Guid7`, `PasswordHasher`, `DefaultSerializer`, `CharSets`, `RandomString` — same pattern as typical modular monolith utilities.

---

## Kakeibo.Contracts

Inter-module contracts organized by **publisher module** (the module that owns and defines the contract).

```
Kakeibo.Contracts/
├── Identity/
│   ├── Events/       — UserRegisteredEvent, UserDeactivatedEvent, ...
│   ├── Requests/     — GetUserByIdRequest, ValidateUserPermissionRequest, ...
│   └── Responses/    — UserDto
├── Wallets/
│   ├── Events/       — WalletCreatedEvent, WalletArchivedEvent, InvitationSentEvent,
│   │                    MemberJoinedEvent, SettlementRecordedEvent, ...
│   ├── Requests/     — GetWalletMembersRequest, GetWalletBalanceRequest,
│   │                    ValidateInvitationRequest, ...
│   └── Responses/    — WalletDto, WalletBalanceDto, InvitationStatusDto
├── Transactions/
│   ├── Events/       — TransactionRecordedEvent, TransactionUpdatedEvent, TransactionDeletedEvent, ...
│   ├── Requests/     — GetTransactionsInPeriodRequest, GetCategoryByIdRequest, ...
│   └── Responses/    — TransactionSummaryDto, CategoryDto
├── Budgets/
│   ├── Events/       — BudgetExceededEvent, BudgetWarningEvent, ...
│   └── Responses/    — BudgetStatusDto
├── Goals/
│   ├── Events/       — GoalMilestoneReachedEvent, GoalAchievedEvent, ...
│   └── Responses/    — GoalProgressDto
├── Recurring/
│   ├── Events/       — RecurringTransactionGeneratedEvent, ...
│   └── Responses/    — RecurringPatternDto
├── Notifications/
│   └── Requests/     — SendNotificationRequest, ...
└── Auditing/
    └── Requests/     — GetAuditTrailRequest, ...
```

> **Note:** Collaboration contracts (invitations, splits, debts, settlements) are now under `Wallets/`. Categories contracts are now under `Transactions/`.

**Contract types:**

- **Integration events** — `sealed record` implementing `IIntegrationEvent` with `Id`, `OccurredAt`, `Version` + domain properties
- **Module requests** — `sealed record` implementing `IModuleRequest<TResponse>`
- **Shared DTOs** — Minimalist `sealed record` with only what consumers need (suffix: `Dto` — allowed only here in Contracts, NOT in endpoint types)

```csharp
namespace Kakeibo.Contracts.Wallets.Events;

public sealed record WalletCreatedEvent : IIntegrationEvent
{
    public required Guid Id { get; init; }
    public required Instant OccurredAt { get; init; }
    public int Version => 1;

    public required Guid WalletId { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required WalletType Type { get; init; }
    public required decimal InitialBalance { get; init; }
}
```

```csharp
namespace Kakeibo.Contracts.Wallets.Requests;

public sealed record GetWalletBalanceRequest(Guid WalletId) : IModuleRequest<decimal>;
```

---

## Kakeibo.Infrastructure

Technical cross-cutting concerns. References `Kakeibo.Common` and `Kakeibo.Contracts`.

```
Kakeibo.Infrastructure/
├── Email/          — IEmailService, EmailService, ReactEmailRenderer, SmtpOptions
├── Storage/        — IStorageService, StorageService, RustFsOptions
├── Caching/        — ICacheService, FusionCacheService, CachingOptions
├── Messaging/      — ModuleClient, ModuleEventBus (buffers integration events), DomainEventDispatcher (resolves IDomainEventHandler<T> via DI)
├── Audit/          — IAuditService, ClickHouseAuditService, ClickHouseOptions
├── Outbox/         — OutboxInterceptor (harvests domain events + persists outbox), OutboxProcessor (BackgroundService), OutboxOptions
├── HealthChecks/   — ClickHouseHealthCheck, RustFsHealthCheck, ...
└── Observability/  — SerilogExtensions, OpenTelemetryExtensions
```

**Key implementations:**

- `ModuleClient` — Resolves `IModuleRequestHandler<,>` from DI via reflection, dispatches sync requests in-process
- `ModuleEventBus` — Buffers integration events in-memory (scoped lifetime). Events are captured by `OutboxInterceptor` during `SaveChangesAsync`
- `DomainEventDispatcher` — Resolves all `IDomainEventHandler<T>` for a given domain event via DI (reflection on open generic), invokes each handler sequentially
- `OutboxInterceptor` — `SaveChangesInterceptor` that: (1) harvests domain events from `ChangeTracker.Entries<Entity>()`, (2) dispatches them via `DomainEventDispatcher` (handlers publish integration events + stage audit), (3) reads buffered integration events from `ModuleEventBus`, (4) writes `OutboxMessage` rows within the same database transaction
- `OutboxProcessor` — Background service that polls per-module outbox tables, dispatches to `IEventConsumer<T>` handlers, and marks messages as processed. Includes Polly retry (3x exponential: 1s, 5s, 15s)

---

## Module Anatomy

Internal structure of `Kakeibo.Modules.Wallets` (post-consolidation example):

```
Kakeibo.Modules.Wallets/
├── Entities/
│   ├── Wallet.cs                       — Aggregate root (personal + shared)
│   ├── WalletMember.cs                 — Membership in shared wallet
│   ├── Invitation.cs                   — Access grant for shared wallet
│   ├── Split.cs                        — Expense division configuration
│   ├── Debt.cs                         — Calculated debt between members
│   └── Settlement.cs                   — External payment record
├── ValueObjects/
│   ├── WalletType.cs                   — Personal vs Shared enum
│   └── SplitType.cs                    — Equal, Percentage, Custom
├── Errors/
│   └── WalletErrors.cs                 — Typed Error records for the module
├── Events/
│   └── WalletCreatedDomainEvent.cs     — Internal domain event (NOT in Contracts)
├── DomainEventHandlers/
│   └── WalletCreatedDomainEventHandler.cs — Publishes integration event + stages audit
├── Features/
│   ├── CreateWallet/
│   │   ├── CreateWalletEndpoint.cs     — IEndpoint + nested Request/Response
│   │   ├── CreateWalletHandler.cs      — Plain class with HandleAsync
│   │   └── CreateWalletValidator.cs    — FluentValidation rules
│   ├── GetWallet/
│   ├── ListWallets/
│   ├── ArchiveWallet/
│   ├── GetWalletBalance/
│   ├── InviteToWallet/                 — ← Collaboration feature
│   ├── AcceptInvitation/               — ← Collaboration feature
│   ├── RecordSettlement/               — ← Collaboration feature
│   ├── GetWalletDebts/                 — ← Collaboration feature
│   └── GetWalletMembers/               — ← Collaboration feature
├── Consumers/
│   └── UserRegisteredConsumer.cs        — Consumes integration event from Identity
├── Persistence/
│   ├── WalletsDbContext.cs              — Module-scoped DbContext
│   ├── Configurations/
│   │   ├── WalletConfiguration.cs      — EF Core entity mapping
│   │   ├── InvitationConfiguration.cs  — ← Collaboration entity mapping
│   │   ├── SplitConfiguration.cs       — ← Collaboration entity mapping
│   │   ├── DebtConfiguration.cs        — ← Collaboration entity mapping
│   │   └── SettlementConfiguration.cs  — ← Collaboration entity mapping
│   ├── Migrations/
│   ├── Seeders/                         — IOnboardingSeeder implementations
│   └── WalletsOutboxSource.cs           — IOutboxSource for this module
├── RequestHandlers/
│   ├── GetWalletMembersRequestHandler.cs
│   └── GetWalletBalanceRequestHandler.cs
├── Services/
│   └── DebtCalculationService.cs        — ← Collaboration service (debt minimization algorithm)
├── Authorization/                       — Permission-based authorization (Identity-specific)
├── Constants/                           — Module-scoped string constants
├── Middleware/                          — Module-specific HTTP middleware
├── WalletsModuleRegistration.cs         — DI + endpoint registration
└── Kakeibo.Modules.Wallets.csproj
```

Internal structure of `Kakeibo.Modules.Transactions` (post-consolidation example):

```
Kakeibo.Modules.Transactions/
├── Entities/
│   ├── Transaction.cs                   — Aggregate root (income, expense, transfer)
│   └── Category.cs                      — ← Categories entity (system + custom)
├── ValueObjects/
│   ├── TransactionType.cs               — Income, Expense, Transfer
│   └── SystemCategory.cs                — ← Categories (12 predefined)
├── Errors/
│   └── TransactionErrors.cs
├── Events/
│   └── TransactionRecordedDomainEvent.cs
├── DomainEventHandlers/
│   └── TransactionRecordedDomainEventHandler.cs
├── Features/
│   ├── RecordTransaction/
│   ├── UpdateTransaction/
│   ├── DeleteTransaction/
│   ├── ListTransactions/
│   ├── CreateCategory/                  — ← Categories feature
│   ├── UpdateCategory/                  — ← Categories feature
│   ├── ListCategories/                  — ← Categories feature
│   └── ArchiveCategory/                 — ← Categories feature
├── Consumers/
│   └── UserRegisteredConsumer.cs
├── Persistence/
│   ├── TransactionsDbContext.cs
│   ├── Configurations/
│   │   ├── TransactionConfiguration.cs
│   │   └── CategoryConfiguration.cs     — ← Categories entity mapping
│   ├── Migrations/
│   ├── Seeders/
│   │   └── SystemCategoriesSeeder.cs    — ← Seeds 12 system categories
│   └── TransactionsOutboxSource.cs
├── RequestHandlers/
│   └── GetTransactionsInPeriodRequestHandler.cs
├── Services/
├── Authorization/
├── Constants/
├── Middleware/
├── TransactionsModuleRegistration.cs
└── Kakeibo.Modules.Transactions.csproj
```

> Not all modules need all folders — use only what the domain requires.

**Rules:**
- `Entities/`, `ValueObjects/`, `Events/`, `Errors/`, `DomainEventHandlers/` are **internal** — no other module can access them
- `DomainEventHandlers/` contains `IDomainEventHandler<T>` implementations that react to domain events by publishing integration events and staging audit entries. Dispatched automatically by `OutboxInterceptor` during `SaveChangesAsync`
- `Consumers/` handles integration events from other modules via `Kakeibo.Contracts`
- `RequestHandlers/` handles sync requests from other modules via `IModuleClient`
- `.csproj` includes `<InternalsVisibleTo Include="Kakeibo.Modules.Wallets.Tests" />`
- Every `.csproj` under `src/` must include `<InternalsVisibleTo Include="{CorrespondingTestProject}" />` to enable testing of internal types
- Module NEVER references another module's project

---

## Feature Slice Pattern

Each feature has up to 3 files: Endpoint, Handler, Validator.

### Endpoint

```csharp
namespace Kakeibo.Modules.Wallets.Features.CreateWallet;

public sealed class CreateWalletEndpoint : IEndpoint
{
    // Nested Request/Response following TD-013 naming
    public sealed record CreateWalletRequest(
        string Name, WalletType Type, decimal InitialBalance);

    public sealed record CreateWalletResponse(
        Guid Id, string Name, WalletType Type, decimal Balance);

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
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500),
            };
    }
}
```

### Handler

**Handlers are plain classes** — no `ICommandHandler<T,R>`, no MediatR, no CQRS interfaces. Just a class with a `HandleAsync` method. Scrutor auto-registers by name convention (`*Handler`).
**Use primary constructors** for injected dependencies — no explicit `private readonly` fields or constructor bodies.

```csharp
namespace Kakeibo.Modules.Wallets.Features.CreateWallet;

// Creates a new wallet and publishes a creation event.
public sealed class CreateWalletHandler(
    WalletsDbContext db, IModuleEventBus eventBus)
{
    public async Task<Result<CreateWalletEndpoint.CreateWalletResponse>> HandleAsync(
        CreateWalletEndpoint.CreateWalletRequest request, CancellationToken ct)
    {
        var exists = await db.Wallets.AnyAsync(m => m.Name == request.Name, ct);
        if (exists)
            return Error.Conflict($"A wallet with name '{request.Name}' already exists.");

        var wallet = new Wallet
        {
            Name = request.Name,
            Type = request.Type,
            Balance = request.InitialBalance,
        };

        db.Wallets.Add(wallet);

        // Publish integration event (persisted in outbox within same transaction)
        await eventBus.PublishAsync(new WalletCreatedEvent
        {
            Id = Guid7.NewGuid().Value,
            OccurredAt = SystemClock.Instance.GetCurrentInstant(),
            WalletId = wallet.Id,
            UserId = wallet.UserId,
            Name = wallet.Name,
            Type = wallet.Type,
            InitialBalance = request.InitialBalance,
        }, ct);

        await db.SaveChangesAsync(ct);

        return new CreateWalletEndpoint.CreateWalletResponse(
            wallet.Id, wallet.Name, wallet.Type, wallet.Balance);
    }
}
```

### Validator

```csharp
namespace Kakeibo.Modules.Wallets.Features.CreateWallet;

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

### Cross-cutting

Applied via **endpoint filters**, not decorator chains:
- Validation: `.WithValidation<TRequest>()` (uses `ValidationFilter<T>`)
- Authorization: `.RequireAuthorization()` or `.RequireAuthorization("policy")`
- Rate limiting: `.RequireRateLimiting("standard")`
- Logging/observability: Global middleware

---

## Inter-Module Communication

### Sync: IModuleClient

When Module A needs data from Module B **right now**.

```
Module A Handler → IModuleClient.SendAsync(request)
    → DI resolves IModuleRequestHandler from Module B
    → TResponse
```

**Caller side** (Budgets needs transaction data):
```csharp
var transactions = await moduleClient.SendAsync(
    new GetTransactionsInPeriodRequest(request.WalletId, request.CategoryId, startDate, endDate), ct);

if (transactions is null || !transactions.Any())
    return Error.NotFound("No transactions found in the specified period.");
```

**Handler side** (Transactions exposes data):
```csharp
namespace Kakeibo.Modules.Transactions.RequestHandlers;

public sealed class GetTransactionsInPeriodRequestHandler(TransactionsDbContext db)
    : IModuleRequestHandler<GetTransactionsInPeriodRequest, List<TransactionSummaryDto>>
{
    public async Task<List<TransactionSummaryDto>?> HandleAsync(
        GetTransactionsInPeriodRequest request, CancellationToken ct)
    {
        return await db.Transactions
            .Where(t => t.WalletId == request.WalletId
                && t.CategoryId == request.CategoryId
                && t.Date >= request.StartDate
                && t.Date <= request.EndDate
                && !t.IsDeleted)
            .Select(t => new TransactionSummaryDto(
                t.Id, t.WalletId, t.Type, t.Amount, t.CategoryId, t.Date))
            .ToListAsync(ct);
    }
}
```

### Async: IModuleEventBus + Outbox

Fire-and-forget with guaranteed delivery via transactional outbox.

```
Handler → entity.AddDomainEvent(event) or eventBus.PublishAsync(event)
    → Handler calls db.SaveChangesAsync()
    → OutboxInterceptor:
        1. Harvest domain events from ChangeTracker.Entries<Entity>()
        2. Dispatch to IDomainEventHandler<T> via DomainEventDispatcher
           → Handlers call eventBus.PublishAsync() + auditOutbox.Stage()
        3. Capture buffered integration events from ModuleEventBus
        4. INSERT outbox_messages (same DB tx)
    → Transaction committed (atomic)
    → OutboxProcessor (background) polls → IEventConsumer<T>.ConsumeAsync()
```

**Consumer** (Collaboration reacts to transaction):
```csharp
namespace Kakeibo.Modules.Collaboration.Consumers;

public sealed class TransactionRecordedConsumer(CollaborationDbContext db)
    : IEventConsumer<TransactionRecordedEvent>
{
    public async Task ConsumeAsync(TransactionRecordedEvent @event, CancellationToken ct)
    {
        // Recalculate debts for the shared wallet
        var wallet = await db.SharedWallets
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == @event.WalletId, ct);

        if (wallet is null) return; // Personal wallet, no debt calculation needed

        // Debt recalculation logic...
        await db.SaveChangesAsync(ct);
    }
}
```

### Decision Table

| Criteria | IModuleClient (Sync) | IModuleEventBus (Async) |
|----------|---------------------|------------------------|
| Caller needs response | Yes | No |
| Failure should block operation | Yes | No |
| Consistency | Request-response in same request | Eventual via outbox |
| Coupling | Caller knows request type | Publisher doesn't know consumers |
| Retries | Caller handles failure | OutboxProcessor built-in retry (3x exponential: 1s, 5s, 15s) |

---

## Database Schema Strategy

**One PostgreSQL schema per module.** All modules share the same connection string — separation is logical (schemas), not physical.

```sql
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS notifications;
CREATE SCHEMA IF NOT EXISTS auditing;
CREATE SCHEMA IF NOT EXISTS wallets;        -- includes collaboration features
CREATE SCHEMA IF NOT EXISTS transactions;   -- includes categories
CREATE SCHEMA IF NOT EXISTS budgets;
CREATE SCHEMA IF NOT EXISTS goals;
CREATE SCHEMA IF NOT EXISTS recurring;
```

### Per-module DbContext

```csharp
namespace Kakeibo.Modules.Wallets.Persistence;

public sealed class WalletsDbContext(DbContextOptions<WalletsDbContext> options)
    : DbContext(options), IOutboxSource
{
    public const string SchemaName = "wallets";

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletsDbContext).Assembly);
    }
}
```

### Per-module DbContext registration

```csharp
services.AddDbContext<WalletsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.UseNodaTime();
        npgsql.MigrationsHistoryTable("__ef_migrations_history", WalletsDbContext.SchemaName);
    })
    .UseSnakeCaseNamingConvention());
```

### Per-module migrations

```bash
dotnet ef migrations add InitialCreate \
  --project src/Kakeibo.Modules.Wallets \
  --startup-project src/Kakeibo.Api \
  --context WalletsDbContext \
  --output-dir Persistence/Migrations
```

### Per-module outbox table

Each schema has its own `outbox_messages` table with a filtered index on unprocessed messages.

---

## Testing Strategy

| Level | What | Where | Dependencies |
|-------|------|-------|-------------|
| **1. Domain Unit** | Entity behavior, value object validation, domain events | `Kakeibo.Modules.{X}.Tests/Entities/` | None (pure domain logic) |
| **2. Handler Unit** | Business logic with mocked DbContext and dependencies | `Kakeibo.Modules.{X}.Tests/Features/{Op}/` | NSubstitute for mocks |
| **3. Module Integration** | Full module with real PostgreSQL via Testcontainers | `Kakeibo.Modules.{X}.Tests/Integration/` | `PostgreSqlContainer`, real DbContext, external modules mocked |
| **4. API Functional** | Full HTTP pipeline via `WebApplicationFactory<Program>` | `Kakeibo.FunctionalTests/` | Testcontainers for all infrastructure |
| **5. Architecture** | Module boundary enforcement, naming conventions, dependency direction | `Kakeibo.ArchitectureTests/` | NetArchTest |

**Architecture test examples:**
- No module can reference another module's assembly
- `Kakeibo.Common` cannot reference any module
- `Kakeibo.Contracts` cannot reference `Kakeibo.Infrastructure`
- Endpoints end in `Endpoint`, Validators in `Validator`, Consumers in `Consumer`
- Consumers implement `IEventConsumer<T>` and end in `Consumer`
- Domain event handlers implement `IDomainEventHandler<T>` and end in `DomainEventHandler`
- Types in `DomainEventHandlers` namespace must implement `IDomainEventHandler<T>`
- Configuration classes never end in `Settings`

---

## DI Registration Pattern

Each module exposes a static `{Module}ModuleRegistration` class with two extension methods.

```csharp
namespace Kakeibo.Modules.Wallets;

public static class WalletsModuleRegistration
{
    // Registers all module services in DI
    public static WebApplicationBuilder AddWalletsModule(this WebApplicationBuilder builder)
    {
        // DbContext with module schema
        builder.Services.AddDbContext<WalletsDbContext>(options => /* ... */);

        // Outbox source
        builder.Services.AddScoped<IOutboxSource, WalletsOutboxSource>();

        // Feature handlers (auto-scan by name convention)
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<WalletsModuleRegistration>()
            .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Handler")))
            .AsSelf()
            .WithScopedLifetime());

        // Module request handlers (for IModuleClient)
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<WalletsModuleRegistration>()
            .AddClasses(classes => classes.AssignableTo(typeof(IModuleRequestHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Integration event consumers (for IEventConsumer<T>)
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<WalletsModuleRegistration>()
            .AddClasses(classes => classes.AssignableTo(typeof(IEventConsumer<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Domain event handlers (for IDomainEventHandler<T>)
        builder.Services.Scan(scan => scan
            .FromAssemblyOf<WalletsModuleRegistration>()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)),
                publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // FluentValidation validators
        builder.Services.AddValidatorsFromAssemblyContaining<WalletsModuleRegistration>();

        return builder;
    }

    // Maps all IEndpoint implementations from this module's assembly
    public static IEndpointRouteBuilder MapWalletsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapEndpoints(typeof(WalletsModuleRegistration).Assembly);
        return app;
    }
}
```

### Program.cs (Composition Root)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.AddSerilog();
builder.AddOpenTelemetry();
builder.AddCaching();
builder.AddStorage();
builder.AddEmail();
builder.AddMessaging();
builder.AddAudit();
builder.AddHealthChecks();

// Core Modules
builder.AddIdentityModule();
builder.AddNotificationsModule();
builder.AddAuditingModule();

// Business Modules
builder.AddWalletsModule();        // includes Collaboration features
builder.AddTransactionsModule();   // includes Categories
builder.AddBudgetsModule();
builder.AddGoalsModule();
builder.AddRecurringModule();

var app = builder.Build();

// Middleware
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapIdentityEndpoints();
app.MapNotificationsEndpoints();
app.MapAuditingEndpoints();
app.MapWalletsEndpoints();         // includes Collaboration endpoints
app.MapTransactionsEndpoints();    // includes Categories endpoints
app.MapBudgetsEndpoints();
app.MapGoalsEndpoints();
app.MapRecurringEndpoints();

app.MapHealthChecks("/health");
app.MapScalarApiReference();
app.Run();

public partial class Program; // Required for WebApplicationFactory<Program>
```

---

## Quick Reference

### Add a new feature to an existing module

1. Create folder: `src/Kakeibo.Modules.{Module}/Features/{Operation}/`
2. Create `{Op}Endpoint.cs` with nested `{Op}Request`/`{Op}Response` records
3. Create `{Op}Handler.cs` — plain class with `HandleAsync` method
4. Create `{Op}Validator.cs` — `AbstractValidator<{Op}Endpoint.{Op}Request>`
5. Handler is auto-registered by Scrutor (`*Handler` convention)

### Add a new module

1. Create `src/Kakeibo.Modules.{Name}/` with `.csproj` referencing `Kakeibo.Common`, `Kakeibo.Contracts`, `Kakeibo.Infrastructure`
2. Add `<InternalsVisibleTo Include="Kakeibo.Modules.{Name}.Tests" />`
3. Create `Persistence/{Name}DbContext.cs` with `const string SchemaName` implementing `IOutboxSource`
4. Create `{Name}ModuleRegistration.cs` with `Add{Name}Module()` and `Map{Name}Endpoints()`
5. Register in `Program.cs`: `builder.Add{Name}Module()` + `app.Map{Name}Endpoints()`
6. Create test project: `tests/Kakeibo.Modules.{Name}.Tests/`

### Raise a domain event (preferred)

1. Define in `src/Kakeibo.Modules.{Module}/Events/`: `sealed record {Event}(...) : IDomainEvent` (internal)
2. Create handler in `src/Kakeibo.Modules.{Module}/DomainEventHandlers/{Event}Handler.cs` implementing `IDomainEventHandler<{Event}>`
3. In the handler, publish integration events via `eventBus.PublishAsync()` and stage audit entries via `auditOutbox.Stage()`
4. In the feature handler, call `entity.AddDomainEvent(new {Event}(...))` **before** `SaveChangesAsync`
5. `OutboxInterceptor` harvests domain events → dispatches to `IDomainEventHandler<T>` → persists outbox messages (atomic)
6. Handler is auto-registered by Scrutor (`.AssignableTo(typeof(IDomainEventHandler<>))`, `publicOnly: false`)

**Edge case:** Entity-less events (e.g., failed login attempts) that don't originate from an `Entity` should keep the manual pattern: `eventBus.PublishAsync()` + `auditOutbox.PublishAsync()` directly in the feature handler.

### Publish an integration event (manual, for entity-less flows)

1. Define in `Kakeibo.Contracts/{Module}/Events/`: `sealed record {Event} : IIntegrationEvent`
2. In handler, **before** `SaveChangesAsync`: `await eventBus.PublishAsync(new {Event} { ... }, ct);`
3. `SaveChangesAsync` commits entity changes + outbox message in one transaction (via `OutboxInterceptor`)
4. `OutboxProcessor` picks up and dispatches to `IEventConsumer<T>`

### Handle an integration event from another module

1. Create folder: `src/Kakeibo.Modules.{Module}/Consumers/`
2. Create `{EventName}Consumer.cs` implementing `IEventConsumer<{Event}>`
3. Implement `ConsumeAsync({Event} @event, CancellationToken ct)` with business logic
4. Consumer is auto-registered by Scrutor (`.AssignableTo(typeof(IEventConsumer<>))`)

### Handle a cross-module request

1. Define in `Kakeibo.Contracts/{Module}/Requests/`: `sealed record {Request} : IModuleRequest<{ResponseDto}>`
2. Define response in `Kakeibo.Contracts/{Module}/Responses/`: `sealed record {ResponseDto}(...)`
3. Implement handler in owning module: `RequestHandlers/{Request}Handler : IModuleRequestHandler<{Request}, {ResponseDto}>`
4. Call from another module: `var result = await moduleClient.SendAsync(new {Request}(...), ct);`

---

*Kakeibo is a personal finance platform balancing individual tracking with collaborative expense management. The platform honors traditional Japanese budgeting wisdom while adapting to contemporary digital life and collaborative financial responsibilities.*
