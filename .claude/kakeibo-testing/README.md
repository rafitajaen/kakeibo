# kakeibo-testing skill

Definitive testing reference for the Kakeibo platform monorepo. Covers all 3 projects:
**API (.NET)**, **Kakeibo.App (Vue 3)**, and **Kakeibo.Email (Bun/Hono)**.

---

## What this skill does

When Claude loads this skill, it gains:

- A comprehensive testing philosophy (TDD, behavior-over-implementation, edge-case exhaustiveness)
- A quick decision table: which test level to use for each component type
- Naming conventions for both API and frontend tests
- Mocking rules (what to always mock vs what to never mock)
- Links to all reference documents with detailed patterns and code examples

The skill is loaded automatically when you ask Claude to write tests, review test coverage,
set up test infrastructure, or apply TDD to any part of the Kakeibo codebase.

---

## Slash command

This skill is **not user-invocable** (`user-invocable: false`). It is loaded automatically
by Claude when the context matches testing work. The `/tdd` skill (in `prompts/tdd.md`)
handles the TDD workflow loop explicitly.

---

## File structure

```
.claude/skills/kakeibo-testing/
├── SKILL.md                        ← Agent instructions, quick decision table, red flags
├── README.md                       ← This file
├── references/
│   ├── api-pyramid.md              ← 6 API test levels: Theory/InlineData, Assert.Multiple, ITestOutputHelper, snapshot note
│   ├── frontend-pyramid.md         ← Vitest + Playwright (locators, page.route, debugger, CI caching) + Email
│   ├── test-doubles.md             ← Mock vs stub vs fake vs spy, decision matrix
│   ├── infrastructure.md           ← TestDbContextFactory, FakeClock vs TimeProvider, xUnit parallelism, WebApplicationFactory
│   ├── infrastructure-tests.md     ← ChannelEventBus, EventDispatcher, ClickHouseAuditService
│   ├── smoke-tests.md              ← 7 critical system flows end-to-end
│   ├── edge-cases.md               ← Complete edge case catalog + snapshot regression
│   ├── gap-detection.md            ← P1/P2/P3 priorities, CRAP score, flaky test management, missing arch tests, Stryker, Coverlet
│   └── snapshot-testing.md         ← Verify library: setup, scrubbing NodaTime/GUIDs, API snapshots, email templates, workflow
└── prompts/
    └── tdd.md                      ← /tdd skill: red-green-refactor, interface design for testability, refactor candidates
```

---

## Reference files at a glance

### `references/api-pyramid.md`
All 6 test levels for the .NET API:
- Level 1: Domain Unit — entities, value objects, validators
- Level 2: Handler Unit — feature handlers, event handlers, cross-domain query handlers with real PostgreSQL
- Level 3: Event Handler Unit — `IEventHandler<T>` with NSubstitute mocks (no DB side effects)
- Level 4: Background Job — Hangfire jobs with real PostgreSQL and FakeClock
- Level 5: API Integration — full HTTP pipeline via `WebApplicationFactory`
- Level 6: Architecture — naming conventions via NetArchTest

Also covers: event handler testing, authorization testing, Result/Error handling.

### `references/frontend-pyramid.md`
Vitest tests for:
- Components (shadcn-vue + custom), Views/Pages, Composables
- Pinia stores, Forms (VeeValidate + Zod), Router guards, Axios interceptors
- Email renderer (Bun test)

Playwright E2E:
- Real API vs mocked API decision
- Storage state authentication (no manual login per test)
- Auth fixtures for multiple roles
- E2E folder organization

### `references/test-doubles.md`
- Definitions: stub, mock, fake, spy, dummy (Gerard Meszaros taxonomy)
- Decision flowchart: system boundary → mock; internal detail → use real; time → FakeClock
- Kakeibo-specific decision matrix
- NSubstitute patterns: `Received()`, `Arg.Is()`, `Returns()`
- `vi.mock` patterns: `vi.fn()`, `mockResolvedValue()`, `mockRejectedValue()`

### `references/infrastructure.md`
Canonical implementations:
- `TestDbContextFactory` with Docker skip guard (KB-008, Rule 4)
- `FakeClock` injection and time advancement
- `WebApplicationFactory` full configuration
- `AuthTestClient` and `TestDataBuilder`
- Vitest global setup with real i18n
- Playwright `playwright.config.ts` with storage state projects

### `references/edge-cases.md`
Complete catalog organized by category:
- Auth & Security (JWT, concurrent login, role-based access)
- Database & Persistence (soft delete, concurrency, NodaTime)
- Events & Idempotence (fire-and-forget delivery, ChannelEventBus, EventDispatcher, handler isolation)
- Validation & Types (exact limits, whitespace, enum values)
- External Services (notification failure, SMTP failure, RustFS unavailable)
- Pagination & Lists (empty result, out-of-range page, large dataset)
- Frontend UI States (loading, error, empty, optimistic updates)
- E2E Network (throttling, API down, session timeout)

### `references/gap-detection.md`
Tools and strategies to find untested code:
- **P1/P2/P3 priorities** — what must be tested before merge vs. within the phase vs. nice-to-have
- **CRAP score analysis** — identifies complex methods with low coverage (complementary to Stryker)
- **Flaky test management** — common causes, detection, quarantine strategy
- **Missing architecture tests** — 9 arch test cases identified as not yet implemented
- Coverlet commands with HTML report generation
- Stryker.NET mutation testing (mutation score > 80%)
- Per-handler checklist (happy path, conflict, not-found, validation, auth, idempotency)
- Error code coverage: grep-based commands to find uncovered `Error.Code` values
- Architecture test drift detection
- Event handler coverage checklist, Permission coverage checklist
- i18n gap detection via Vitest `missing` handler
- Visual regression with Playwright screenshots
- Test quality indicators and red flags

### `references/snapshot-testing.md`
Snapshot/approval testing with the Verify library for .NET:
- When to use (API responses > 5 fields, email templates) and when NOT to use
- Setup: NuGet packages, module initializer, `.gitattributes`
- Scrubbing non-deterministic values: GUIDs, NodaTime `Instant`, timestamps
- Custom `InstantConverter` for NodaTime scrubbing
- Verifying API responses at Level 5 and email templates from Kakeibo.Email
- Parameterized snapshot tests with `[Theory]` + `UseParameters`
- Acceptance workflow: `dotnet verify accept`, snapshot file locations

---

## Key constraints

- **Never use `.WithReuse(true)`** in Testcontainers (Rule 4, KB-008)
- **Always add skip guard** for Docker-dependent tests (KB-008)
- **Never mock `DbContext`** — use `TestDbContextFactory` instead
- **Always use `FakeClock`** — never `SystemClock.Instance` in test code
- **Use real i18n locale files** in Vue component tests — detects missing keys
