# Kakeibo API Testing Guide

This guide is the starting point for any developer who needs to write, find, or understand a test in the Kakeibo API. It explains what is tested, how to navigate the documentation, and the conventions that all tests must follow.

---

## Philosophy

The test suite is built around three principles:

**Test behavior, not implementation.** A test verifies *what* a component does, not *how* it is internally wired. Calling `HandleAsync` directly with real arguments is more useful than mocking every internal step.

**Integration-first where it matters.** Handlers that interact with the database are tested against a real PostgreSQL instance (via Testcontainers). This catches real query bugs, constraint violations, and EF Core filter behavior that in-memory stubs cannot reproduce.

**Mock only external side effects.** Services like `IEmailService`, `IWebPushService`, `IStorageService`, and `IAuditService` communicate with systems outside the API process. They are replaced with mocks in all non-dedicated tests so that no email is ever sent and no file is ever uploaded during a test run.

---

## The 6 Testable Component Types

Every file in `src/Kakeibo.Api/` belongs to one of these categories. Each category has its own document that explains exactly how to write tests for it.

| # | Component | What it does |
|---|-----------|--------------|
| 1 | **Handler** | Contains all business logic. Returns `Result<T>`. The most important component to test. |
| 2 | **Validator** | Defines FluentValidation rules for a request. Runs before the handler, via `ValidationFilter`. |
| 3 | **Endpoint** | Registers the HTTP route, applies auth and validation. Delegates all logic to the handler. |
| 4 | **Event Handler** | Reacts to a domain event published asynchronously via `IEventBus`. |
| 5 | **Background Job** | A Hangfire-scheduled task (e.g., `GenerateRecurringTransactionsJob`). |
| 6 | **Entity Configuration** | An EF Core `IEntityTypeConfiguration<T>` that maps an entity to the database schema. |

Architecture tests exist as a separate, cross-cutting concern that validates naming conventions across the entire assembly.

---

## Decision Tree — Which Document to Read

Use this tree to find the right guide for the file you are about to test.

```
What kind of file are you testing?
│
├─── Does the filename end with Handler.cs?
│         (e.g., CreateWalletHandler.cs)
│    └──► 01-handlers.md
│
├─── Does the filename end with Validator.cs?
│         (e.g., CreateWalletValidator.cs)
│    └──► 02-validators.md
│
├─── Does the filename end with Endpoint.cs?
│         (e.g., CreateWalletEndpoint.cs)
│    └──► 03-endpoints.md
│
├─── Is it an event handler?
│         (implements IEventHandler<TEvent>, usually in Features/{Domain}/Events/)
│    └──► 04-event-handlers.md
│
├─── Does the filename end with Job.cs?
│         (e.g., GenerateRecurringTransactionsJob.cs)
│    └──► 05-background-jobs.md
│
├─── Is it an infrastructure service?
│         (lives under Infrastructure/, e.g., EmailService.cs, StorageService.cs)
│    └──► 06-infrastructure-services.md
│
├─── Does the filename end with Configuration.cs?
│         (lives under Persistence/Configurations/, e.g., UserConfiguration.cs)
│    └──► 07-entity-configurations.md
│
└─── Is it an architecture rule?
          (lives under tests/Kakeibo.Tests/Architecture/)
     └──► 08-architecture-tests.md
```

If you need to understand the shared test infrastructure (TestDbContextFactory, NSubstitute, FakeClock), read **09-test-infrastructure.md** at any point.

---

## Test Type Matrix

This table summarizes what kind of test each component uses and whether Docker is required.

| Component | Test Style | Database | Docker Required? |
|-----------|-----------|----------|-----------------|
| Handler | Integration (preferred) | Real PostgreSQL via Testcontainers | Yes |
| Handler | Unit (for pure logic) | Mocked | No |
| Validator | Unit | None | No |
| Endpoint | Integration (HTTP level) | Real PostgreSQL via Testcontainers | Yes |
| Event Handler | Integration | Real PostgreSQL via Testcontainers | Yes |
| Background Job | Integration | Real PostgreSQL via Testcontainers | Yes |
| Infrastructure Service | Unit (mock in handler tests) | None | No |
| Infrastructure Service | Integration (dedicated) | External service required | Yes (+ service) |
| Entity Configuration | Integration | Real PostgreSQL via Testcontainers | Yes |
| Architecture | Static analysis | None | No |

> **When Docker is unavailable**, Testcontainers-dependent tests automatically call `Assert.Skip()` and are reported as *skipped*, never as failed. This keeps CI clean on machines without Docker.

---

## Test Naming Convention

All test methods use this format:

```
{Method}_{Scenario}_{ExpectedResult}
```

Examples:

- `HandleAsync_ValidRequest_CreatesWalletAndPublishesEvent`
- `HandleAsync_WalletNotFound_ReturnsNotFoundError`
- `HandleAsync_UserNotMember_ReturnsForbiddenError`
- `TestValidate_EmptyName_HasValidationError`
- `TestValidate_ValidRequest_HasNoErrors`

The three parts answer: *which method*, *under what condition*, *what should happen*.

---

## Where Tests Live

Tests mirror the source structure:

```
tests/Kakeibo.Tests/
├── Architecture/           ← NetArchTest naming and dependency rules
├── Features/
│   ├── Wallets/
│   │   ├── CreateWallet/
│   │   │   └── CreateWalletTests.cs
│   │   └── GetWallets/
│   │       └── GetWalletsTests.cs
│   ├── Transactions/
│   ├── Budgets/
│   └── ...
├── Integration/            ← Shared integration test utilities
├── GlobalUsings.cs         ← Global imports available in all test files
└── TestDbContextFactory.cs ← Testcontainers PostgreSQL setup
```

Each operation folder (`CreateWallet/`, `GetWallets/`, etc.) holds a single test file. Handler tests, validator tests, and event handler tests for the same operation all live in that one file.

---

## How to Run Tests

```bash
bun run api:test
```

This runs the full test suite sequentially. Do not run it in parallel with other build or format commands.

To run a single test file during development, use the dotnet CLI directly:

```bash
dotnet test tests/Kakeibo.Tests \
  --filter "FullyQualifiedName~CreateWalletTests"
```

---

## Prerequisites

- **Docker** must be running for all integration tests (handlers, event handlers, background jobs, entity configurations, endpoints).
- If Docker is not available, those tests are automatically skipped — they do not fail.
- Architecture tests and validator tests never require Docker.
- The test project already has all required NuGet packages. You do not need to install anything extra.

---

## Further Reading

| Document | What it covers |
|----------|---------------|
| [01-handlers.md](01-handlers.md) | How to test business logic handlers |
| [02-validators.md](02-validators.md) | How to test FluentValidation validators |
| [03-endpoints.md](03-endpoints.md) | How to test HTTP endpoints end-to-end |
| [04-event-handlers.md](04-event-handlers.md) | How to test async event handlers |
| [05-background-jobs.md](05-background-jobs.md) | How to test Hangfire background jobs |
| [06-infrastructure-services.md](06-infrastructure-services.md) | How to mock and test infrastructure services |
| [07-entity-configurations.md](07-entity-configurations.md) | How to test EF Core entity configurations |
| [08-architecture-tests.md](08-architecture-tests.md) | How to write and extend architecture rules |
| [09-test-infrastructure.md](09-test-infrastructure.md) | Reference for TestDbContextFactory, NSubstitute, FakeClock |
