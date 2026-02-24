---
name: kakeibo-testing
description: >
  Definitive testing reference for the Kakeibo platform monorepo. Use when writing
  tests, deciding what to test, implementing TDD, setting up test infrastructure,
  or diagnosing gaps in test coverage for any of the 3 projects: API (.NET),
  Kakeibo.App (Vue 3), Kakeibo.Email (Bun/Hono).
  Covers: entity unit, value object equality, middleware unit, feature handlers,
  event handlers (IEventHandler<T>), plain handler classes, background jobs, API
  integration, architecture, infrastructure (ChannelEventBus, EventDispatcher,
  ClickHouseAuditService), smoke tests (7 system flows), Vitest, Playwright,
  Testcontainers, NSubstitute, TDD, Pinia testing.
user-invocable: false
---

# Kakeibo Testing — Agent Reference

Covers all 3 monorepo projects: **API (.NET)**, **Kakeibo.App (Vue 3)**, and **Kakeibo.Email (Bun/Hono)**.
All code examples are inline; detailed patterns in references.

**Agent directives — non-negotiable behaviors:**

- **Never mock DbContext or DbSet.** Use `TestDbContextFactory.CreateAsync()` with real
  PostgreSQL. Every handler test must verify persistence (query the DB after the action).
- **Never use `SystemClock.Instance` in tests.** Always inject `FakeClock`. If the class
  under test does not accept `IClock`, that is a design problem — fix it before writing the test.
- **Never write all tests at once then implement.** One test → implementation → refactor.
  The red-green-refactor cycle is not optional.
- **Always verify both the Result and the database state.** A handler test that only checks
  `result.IsSuccess` without a DB query is incomplete.
- **Always review the edge-case catalog** for the component type before declaring coverage
  complete. Read [edge-cases.md](references/edge-cases.md) each time, not from memory.
- **Read the nearest existing test in the module** before writing a new one. Absorb the
  current conventions from real code.
- **Never omit the `await using` pattern** for `TestDbContextFactory.CreateAsync()`. The
  context must be disposed asynchronously to release the test database.

**When to consult this skill proactively.** This skill is not explicitly invoked — it provides
the testing knowledge base. Consult it automatically in these situations:

- **Writing any handler test:** Check the Quick Decision Table for the right level and the
  per-handler checklist in gap-detection for completeness.
- **Adding a new event handler:** Verify the idempotency and audit patterns
  from the edge-case catalog.
- **Touching authentication or authorization code:** Review the Auth & Security section of
  edge-cases.md — it has 12 scenarios that are frequently missed.
- **Creating a new module:** Verify that the architecture test in `NamingConventionTests`
  covers the new types.
- **Debugging a flaky test:** Check the flaky test table in gap-detection before investigating
  — the cause is almost always one of the 7 listed patterns.

---

## Philosophy

1. **TDD as default.** Write the test first. The red-test forces you to design the public API before implementation — producing simpler interfaces and more cohesive handlers.
2. **Behavior over implementation.** Verify what the system does, not how it does it internally. A test that breaks after a no-behavior-change refactor is a bad test.
3. **Over-testing beats under-testing.** A redundant test costs almost nothing. A missing test lets a regression reach production.
4. **Explicit over elegant.** One scenario = one descriptively named test. Don't compress "returns 200 on success and 409 on conflict" into a parameterized test when two separate tests are more readable.
5. **Exhaustive edge cases.** For every handler: exact limits, allowed vs rejected nulls, duplicates, invalid states, concurrency, external service failures, and insufficient permissions. See [edge-cases.md](references/edge-cases.md).
6. **Tests as executable contracts.** If "the last SuperAdmin cannot be deleted" is a domain invariant, it must exist as a test. Comments are forgotten; tests break the build.
7. **Total independence.** No test depends on the execution of another. Every test starts with clean state and can run in any order.

---

## Workflow

### 1. Assess (before writing a single line)

Before writing or modifying any test, answer these questions:

**What is the task?** Writing a new test, adding coverage to an existing feature,
diagnosing a gap, or debugging a flaky test? Each has a different starting point.

**What project?** API (.NET), Kakeibo.App (Vue 3), or Kakeibo.Email (Bun/Hono)? This determines
the tool, the runner, and the infrastructure patterns.

**What level in the pyramid?** Use the Quick Decision Table to choose the correct level.
Do not write an integration test when a handler unit test is sufficient. Do not write a
unit test when the bug lives in the HTTP pipeline.

**What are the boundaries?** Identify which dependencies are system boundaries (mock them)
and which are internal details (use the real implementation). When in doubt, the rule is:
if you mock to avoid a side effect, it is a boundary; if you mock to avoid complexity,
use the real thing.

### 2. Locate (find existing patterns)

Read the existing test file in the module before writing anything new. Do not rely on
memory — read the nearest test each time to absorb current conventions.

- For handler tests: find the nearest `{Operation}HandlerTests.cs` in the same module
- For domain event handler tests: find the nearest `{Event}DomainEventHandlerTests.cs`
- For frontend tests: find the nearest `{Component}.spec.ts`
- If there are no tests for this module yet, read the reference files listed at the end

### 3. Write (one test at a time)

Follow TDD when creating a new feature. Follow the per-handler checklist in gap-detection
when filling coverage gaps. One test, one scenario, one group of assertions.

### 4. Verify (before declaring the test complete)

Go through the Quality Checklist at the end of this document. Every point must pass before
the test is considered done.

---

## TDD Workflow

### Red-Green-Refactor

| Phase | Action | Exit Criterion |
|-------|--------|----------------|
| **Red** | Write the test for the simplest case. Run it. Must fail for the right reason (not a compilation error). | Test fails with the expected error message |
| **Green** | Write the minimum implementation to pass that test. No more. | Test passes. All prior tests still pass. |
| **Refactor** | Remove duplication, improve naming, improve readability. | All tests still pass. No behavior change. |

Repeat the cycle per case: happy path → not found → conflict → validation → auth.

### Anti-pattern: Horizontal Slicing

```
❌ Write all 8 tests first, then implement everything
✅ Write 1 test → implement → pass → write the next one
```

### Commands per Project

```bash
# API (.NET)
bun run api:test:unit          # Unit + handler tests (Testcontainers)
bun run api:test:arch          # Architecture tests (NetArchTest)
bun run api:test:functional    # Full HTTP pipeline (WebApplicationFactory)

# Kakeibo.App
bun run app:test:unit          # Vitest component, store, composable tests
bun run app:test:e2e           # Playwright E2E

# Kakeibo.Email
bun run email:test             # Bun test runner
```

---

## Quick Decision Table — Which Test Level?

**Use it actively, not as a passive reference.** Before writing any test, locate the
component type in the first column. If you find yourself using Testcontainers for a domain
event handler, the table tells you that is wrong — use NSubstitute, not a real DB.
When in doubt between two levels, always choose the lowest level that covers the behavior.

| Component | Level | Tool | Infrastructure |
|-----------|-------|------|----------------|
| Entity invariants, domain calculations | 1 — Domain Unit | xUnit | None |
| ValueObject equality / invariants | 1 — Domain Unit | xUnit | None |
| Middleware (`ErrorHandling`, `AuditContext`, `JwtRevocation`) | 1 — Domain Unit | xUnit + NSubstitute | None (mocked `HttpContext`) |
| FluentValidation validators | 1 — Domain Unit | xUnit | None |
| Feature handler (`*Handler`) | 2 — Feature Handler | xUnit + Testcontainers | Real PostgreSQL |
| Cross-domain sync query handler (direct DI injection) | 2b — Query Handler | xUnit + Testcontainers | Real PostgreSQL |
| Event handler (`IEventHandler<T>`) | 2c — Event Handler | xUnit + Testcontainers | Real PostgreSQL |
| Event handler with no DB side effects | 3 — Event Handler Unit | xUnit + NSubstitute | None (mocked) |
| Hangfire background job | 4 — Background Job | xUnit + Testcontainers | Real PostgreSQL |
| Full HTTP pipeline (routing → handler → DB) | 5 — API Integration | xUnit + WAF | Real PostgreSQL, Docker |
| Naming conventions | 6 — Architecture | NetArchTest | None |
| `ChannelEventBus`, `EventDispatcher`, `ClickHouseAuditService` | Infra | xUnit + Testcontainers | Real PostgreSQL + ClickHouse stub |
| Critical system flows (7 end-to-end flows) | Smoke | xUnit + WAF | Full stack |
| Vue component, shadcn-vue primitive | Component | Vitest + Vue Test Utils | None |
| Pinia store logic | Store | Vitest | None (vi.mock API) |
| Composable side effects, cleanup | Composable | Vitest | None |
| Form validation (VeeValidate + Zod) | Form | Vitest | None |
| Router guard behavior | Router | Vitest + vue-router | None |
| Axios interceptors (auth, refresh) | Interceptor | Vitest + MockAdapter | None |
| User flow end-to-end | E2E | Playwright | Full stack or page.route() |
| API response > 5 fields, email template | Snapshot | Verify (xUnit) | None |

---

## Mocking Rules Summary

The question that determines every mocking decision: **is this a system boundary?**
A system boundary is anything that produces a side effect the test cannot control: network
calls, disk writes, clock reads, native device APIs. Mock that. Everything else — the
database via Testcontainers, your own handlers, your own validators — use the real
implementation. When you mock an internal detail, you are testing your assumptions about
how that detail works, not whether the real code works.

For the complete taxonomy (stub vs mock vs fake vs spy) and NSubstitute/vi.mock code patterns,
see [test-doubles.md](references/test-doubles.md).

### Always mock (system boundary)

| Interface | Reason |
|-----------|--------|
| `IEventBus` | In-process event bus — fire-and-forget, async channel dispatch |
| `INotificationService` | External channel (SMTP, WhatsApp, push) |
| `IClock` → `FakeClock` | Time must be deterministic |
| `IEmailService` | External SMTP |
| `IStorageService` | RustFS/S3, external service |
| `axios` HTTP calls in components | Network boundary, side effects |

### Never mock (use the real implementation)

| Do not mock | Alternative |
|-------------|-------------|
| `DbContext` / `DbSet<T>` | `TestDbContextFactory` with real PostgreSQL |
| Concrete handler classes | Instantiate and test directly |
| FluentValidation validators | `new Validator().Validate(request)` |
| Pinia stores in store tests | `createPinia()` per test |
| Vue components under test | Mount with `mount()`, never mock |

### Mock cleanup

```typescript
// Vitest: clear mocks between tests
afterEach(() => vi.clearAllMocks())

// xUnit: each test gets an isolated DB — no manual cleanup needed
// await using var db = await TestDbContextFactory.CreateAsync()  ← auto-disposed
```

---

## Naming Conventions

### API (C#)

```
Test class:          {ClassUnderTest}Tests
Test method:         {Method}_{Scenario}_{ExpectedResult}
Handler tests:       {Operation}HandlerTests.cs
Event handler tests: {EventName}EventHandlerTests.cs
Background jobs:     {JobName}Tests.cs
Integration tests:   {Feature}Tests.cs  (under WebApplicationFactory collection)
Architecture tests:  NamingConventionTests.cs

Examples:
  CreateWalletHandlerTests
  UserRegisteredEventHandlerTests
  WalletCreatedEventHandlerTests
  CheckExpiredBudgetsJobTests
  HandleAsync_DuplicateWalletName_ReturnsConflictError
  HandleAsync_SameEventTwice_IsIdempotent
```

### Frontend (TypeScript / Playwright)

```
Component tests:   {ComponentName}.spec.ts   (in test/components/)
Store tests:       use{StoreName}.spec.ts     (in test/stores/)
Composable tests:  use{Name}.spec.ts          (in test/composables/)
E2E tests:         {feature}.spec.ts          (in e2e/{module}/)

Pattern:
  describe('{ComponentName}', () => {
    it('{action} {context}, {expected result}', ...)
  })

Examples:
  WalletCard.spec.ts
  useWalletsStore.spec.ts
  'shows empty state when wallet list is empty'
  'redirects to login when user is not authenticated'
```

---

## Reference Files

| File | Content |
|------|---------|
| [references/api-pyramid.md](references/api-pyramid.md) | All API levels: Level 1 (entities, value objects, middleware), Level 2 (feature handlers), 2b (query handlers), 2c (event handlers), Level 3–6 |
| [references/frontend-pyramid.md](references/frontend-pyramid.md) | Vitest (components, stores, composables, forms, router, Axios) + Playwright E2E + Email |
| [references/test-doubles.md](references/test-doubles.md) | Mock vs stub vs fake vs spy: definitions, decision matrix, NSubstitute and vi.mock patterns |
| [references/infrastructure.md](references/infrastructure.md) | TestDbContextFactory, FakeClock, WebApplicationFactory, AuthTestClient, TestDataBuilder, Playwright config, Vitest i18n setup |
| [references/infrastructure-tests.md](references/infrastructure-tests.md) | ChannelEventBus throughput, EventDispatcher dispatch, ClickHouseAuditService integration |
| [references/smoke-tests.md](references/smoke-tests.md) | 7 critical system flows: in-process event, entity-less event, sync cross-domain, audit, email, authorization, startup |
| [references/edge-cases.md](references/edge-cases.md) | Complete edge case catalog: value objects, middleware, infrastructure, auth, DB, events, validation, external services, pagination, frontend states |
| [references/gap-detection.md](references/gap-detection.md) | Coverlet, Stryker.NET, CRAP score, flaky test management, per-handler checklist, P1/P2/P3 priorities, missing architecture tests |
| [references/snapshot-testing.md](references/snapshot-testing.md) | Verify library: setup, scrubbing NodaTime/GUIDs, API response snapshots, email template snapshots, workflow |
| [prompts/tdd.md](prompts/tdd.md) | /tdd skill: red-green-refactor loop, tracer bullet, interface design for testability, refactor candidates |

---

## Test Project Setup (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>       <!-- Required for xUnit v3 -->
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Required for NSubstitute to mock internal types -->
    <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NodaTime.Testing" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="xunit.v3" />
  </ItemGroup>
</Project>
```

`GlobalUsings.cs`:

```csharp
global using NodaTime;
global using NodaTime.Testing;
global using NSubstitute;
global using Xunit;
```

---

## Quality Checklist — Before Declaring a Test Complete

Go through each point. If any fails, the test is not done.

- **The name test.** Read only the test method name. Can you understand exactly what scenario
  is verified and what the expected result is, without reading the body?
  `HandleAsync_DuplicateEmail_ReturnsConflictError` passes. `TestCreateMember` fails.

- **The persistence test.** *(Handler tests only)* After the Act, does the test query the DB
  to verify the data was actually persisted? `Assert.True(result.IsSuccess)` alone is not
  enough — add `var inDb = await db.Entity.FindAsync(...)` and assert against it.

- **The boundary test.** Are all system boundaries mocked and all internal details real? If
  you see `Substitute.For<AppDbContext>()`, stop. If you see `SystemClock.Instance`, stop.

- **The independence test.** Can this test run in any order, after any other test, and produce
  the same result? If it shares mutable state, fix it.

- **The failure test.** Does the test fail for the right reason when the implementation is
  wrong? Delete the line of code under test — does it fail with a meaningful assertion error,
  or does it pass silently?

- **The edge case test.** Open [edge-cases.md](references/edge-cases.md), locate the section
  for this component type. Are the relevant edge cases covered in this test or in a sibling
  test in the same class?

- **The compilation test.** *(TDD Red phase only)* Did the test fail with an assertion failure,
  not a compilation error? A test that does not compile has proven nothing.

- **The cleanup test.** *(Frontend only)* Does every `vi.mock()` have `vi.clearAllMocks()` in
  `afterEach`? Is every mounted component unmounted by the framework?
