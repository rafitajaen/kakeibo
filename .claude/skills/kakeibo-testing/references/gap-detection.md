# Gap Detection — Finding Untested Code

Strategies and tools to proactively find coverage gaps before they become production bugs.

---

## P1 / P2 / P3 Test Coverage Priorities

Use these priorities to decide which tests to write first and which can wait:

### P1 — Required before merge (blocking)

```
□ Happy path for every handler feature (creates/updates/deletes successfully)
□ All mandatory business rules (Rule 1, Rule 2, Rule 5 from mandatory.md)
□ Auth: unauthenticated returns 401, wrong role returns 403
□ Duplicate/conflict detection (all unique constraint violations)
□ Not-found paths (entity doesn't exist → NotFound error)
□ Critical validation rules (required fields, max lengths for business-critical fields)
```

### P2 — Required within the phase (non-blocking but tracked)

```
□ Edge cases: null optional inputs, exact boundary values, empty collections
□ Idempotency for all consumers (same event twice → no duplicate data)
□ Domain event handler publishes correct integration event + stages audit entry
□ External service failure: notification fails, handler doesn't throw
□ Architecture tests for new module naming conventions
□ Permission coverage: each permission has a 2xx test and a 403 test
```

### P3 — Nice to have (tracked in debt)

```
□ Mutation testing score > 80% per module (Stryker)
□ CRAP score < 30 for all critical paths (complex handlers)
□ E2E tests for complete user flows (registration → verification → login)
□ Email template snapshot tests (Verify against rendered HTML)
□ Visual regression screenshots for key admin pages (Playwright)
```

---

## Coverage with Coverlet

### runsettings file

Place `tests/coverlet.runsettings` at the root of the `tests/` directory and reference it with `--settings`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <!-- Cobertura format is required by ReportGenerator and GitLab CI -->
          <Format>cobertura</Format>
          <!-- Exclude test assemblies from coverage metrics -->
          <Exclude>[*.Tests]*,[*.ArchitectureTests]*,[*.FunctionalTests]*,[*.SmokeTests]*</Exclude>
          <!-- Exclude generated and migration code from coverage -->
          <ExcludeByFile>**/Migrations/**/*.cs,**/obj/**/*.cs</ExcludeByFile>
          <IncludeTestAssembly>false</IncludeTestAssembly>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### Full pipeline

```bash
# Step 1: Run tests with coverage (uses runsettings to configure exclusions and format)
dotnet test Kakeibo.slnx \
  --settings tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage-results

# Step 2: Install ReportGenerator (one-time, globally)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Step 3: Generate HTML + text summary + badge
reportgenerator \
  -reports:"./coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:"Html;TextSummary;Badges"

# Step 4: Print the summary to the terminal
cat ./coverage-report/Summary.txt

# Step 5: Open the full HTML report (macOS / WSL)
open ./coverage-report/index.html
# or: explorer.exe ./coverage-report/index.html  (WSL)
```

### CRAP score report

To include CRAP scores in the report (identifies high-complexity, low-coverage methods):

```bash
reportgenerator \
  -reports:"./coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:"Html;CrapScore;TextSummary"
```

**Prioritization strategy — address gaps in this order:**

1. All `Error.Code` values defined in `Errors/` → at least one test must reach each one
2. All validation rules → test both the valid boundary and the invalid boundary
3. All happy paths in handlers → basic creation/update test
4. All authorization checks → at least one 403 test per permission-gated endpoint
5. All domain event handlers → at least one publish test and one audit-stage test

---

## CRAP Score Analysis

CRAP (Change Risk Anti-Patterns) combines cyclomatic complexity with code coverage to identify
high-risk methods. Use it alongside Stryker to find undertested complex code.

```
CRAP = complexity² × (1 − coverage)³ + complexity

CRAP < 5   → acceptable
CRAP 5–30  → monitor
CRAP > 30  → prioritize for testing
CRAP > 50  → refactor or test immediately
```

```bash
# Generate coverage + CRAP report
dotnet test Kakeibo.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Generate HTML report with CRAP score column
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:"Html;CrapScore"

# Open the report
open ./coverage/report/index.html
```

**Use CRAP to find:** complex methods with low coverage (high complexity, few tests).
**Use Stryker to find:** well-covered methods with weak assertions (tests exist but don't catch mutations).
They are complementary — run both.

**High CRAP targets in Kakeibo:**
- Handlers with multiple branching paths (conflict detection, conditional event publishing)
- Domain methods with multiple validation rules
- Background jobs with complex query logic

---

## Mutation Testing with Stryker.NET

Mutation testing changes the source code in small ways (mutants) and checks if your tests catch each change. Surviving mutants = tests that don't verify the actual logic.

```bash
# Install globally (one-time)
dotnet tool install -g dotnet-stryker

# Run on a specific module
dotnet stryker --project "src/Kakeibo.Modules.Members/Kakeibo.Modules.Members.csproj"

# Run with specific test project
dotnet stryker \
    --project "src/Kakeibo.Modules.Members/Kakeibo.Modules.Members.csproj" \
    --test-project "tests/Kakeibo.Modules.Members.Tests/Kakeibo.Modules.Members.Tests.csproj"

# View HTML report
open StrykerOutput/{timestamp}/reports/mutation-report.html
```

**Interpreting results:**
- **Killed**: mutant was caught by a test — good
- **Survived**: mutant was NOT caught — test gap, add a targeted test
- **No coverage**: no test even executes this code — critical gap

**Target score:** Mutation score > 80% indicates solid logic coverage.

**High-priority mutation targets:**
- Comparison operators: `<` vs `<=`, `==` vs `!=`, `>` vs `>=`
- Conditional boundaries: `MaxTotalUses` limit, date comparisons
- Boolean returns: `IsActive`, `IsDeleted` checks
- Null checks: `?? throw`, `is null` conditions
- String comparisons in `Error.Code` returns

---

## Flaky Test Management

Flaky tests pass and fail non-deterministically. They erode trust in the test suite.
Find, fix, or quarantine them immediately.

### Common causes in Kakeibo

| Cause | Symptom | Fix |
|-------|---------|-----|
| Time-dependent logic using `SystemClock` | Test passes at 11:59 PM, fails at midnight | Replace with `FakeClock` injection |
| External service not mocked | Test fails when Docker is slow | Check mocking rules table in SKILL.md |
| Shared state between tests | Test fails when run after another test | Each test must use `TestDbContextFactory.CreateAsync()` |
| Race condition in async code | Non-deterministic order of completions | Use `CancellationToken` from `TestContext.Current` |
| Playwright test without proper wait | Element not found despite being present | Use `getByRole()` with auto-retry assertions |
| Testcontainers slow startup | Container not ready before first query | Container starts once per assembly — verify `EnsureContainerStartedAsync()` |
| Test depends on wall clock | Expired token in `TestDataBuilder` | Always seed with `FakeClock`-compatible timestamps |

### Detection

```bash
# Run tests 5 times to detect flaky tests
dotnet test Kakeibo.slnx --count 5

# Run Playwright tests with retries (already configured in playwright.config.ts for CI)
npx playwright test --retries 3
```

### Quarantine strategy

```csharp
// Step 1: Mark it
[Trait("Flaky", "true")]
[Fact]
public async Task MyFlakyTest() { ... }

// Step 2: Skip temporarily while tracking the issue
[Fact(Skip = "Flaky: intermittent timeout in CI — tracked in #123")]
public async Task MyFlakyTest() { ... }

// Step 3: Fix within the same sprint — never leave permanently skipped
// A permanently skipped test is a hole in the test suite
```

---

## Per-Handler Checklist

For every new handler implemented, verify these test cases exist:

```
□ Happy path:     valid input → correct output, data persisted correctly
□ Conflict:       unique constraint violated → Conflict error with correct Error.Code
□ Not found:      entity doesn't exist → NotFound error with correct Error.Code
□ Validation:     required fields empty → Validation error mentioning the field
□ Authorization:  caller lacks permission → Forbidden (tested at Level 5)
□ Idempotency:    (for consumers) calling twice → same final state, no duplicates
□ Domain event:   correct domain event added to the entity (if applicable)
□ Integration event: published to outbox via eventBus (if applicable)
□ External failure: external service unavailable → error handled gracefully, operation continues or fails cleanly
```

---

## Error Code Coverage

Every `Error` defined in `Errors/` of a module must have at least one test that reaches it.

```bash
# Find all Error.Code values defined in a module
grep -rh "Error\." src/Kakeibo.Modules.Members/Errors/ --include="*.cs" \
    | grep -oP '"[A-Z][^"]*"' | sort -u

# Find all Error.Code values asserted in tests
grep -rh "Error\.Code" tests/Kakeibo.Modules.Members.Tests/ --include="*.cs" \
    | grep -oP '"[A-Z][^"]*"' | sort -u

# The diff = uncovered error codes (gaps to fill)
```

---

## Architecture Test Drift Detection

When adding a new type that must follow a naming convention, verify the architecture test already
covers it — or add the test in the same PR:

| New type | Verify this architecture test exists |
|----------|--------------------------------------|
| New `IEndpoint` implementation | `EndpointImplementations_ShouldEndWithEndpoint` |
| New `IEventConsumer<T>` | `Consumers_ShouldEndWithConsumer` |
| New `IDomainEventHandler<T>` | `DomainEventHandlers_ShouldEndWithDomainEventHandler` |
| New `AbstractValidator<T>` | `Validators_ShouldEndWithValidator` |
| New cross-module reference | `Modules_ShouldNotReferenceOtherModules` |
| New configuration class | `ConfigurationClasses_ShouldEndWithOptions` |

If the architecture test doesn't exist yet, create it in the same PR as the new type.

---

## Consumer Coverage Checklist

Every `IEventConsumer<T>` must have:

```
□ Happy path: event processed correctly, expected state change in DB
□ Idempotency: same event received twice → same final state (no duplicate data)
□ Failure case: consumer throws exception → message remains unprocessed
□ Edge case: event with null/missing optional fields → handled gracefully
```

---

## Permission Coverage Checklist

Every permission seeded in the Identity module seeder must appear in at least one test:

```
□ User WITH the permission → accesses the endpoint (2xx response)
□ User WITHOUT the permission → receives 403
□ Unauthenticated user → receives 401
```

To find unseeded permissions:

```bash
grep -r "permissions:" src/Kakeibo.Modules.Identity/Persistence/Seeders/ --include="*.cs" \
    | grep -oP '"[a-z]+:[a-z]+"' | sort -u
```

To find permissions tested at Level 5:

```bash
grep -r "403\|Forbidden\|WithoutPermission" tests/Kakeibo.Api.IntegrationTests/ --include="*.cs" \
    | grep -oP '"[a-z]+:[a-z]+"' | sort -u
```

---

## i18n Gap Detection in Vitest

Automatically catch missing translation keys during component tests:

```typescript
// In vitest.setup.ts — accumulate missing keys across the test run
const missingKeys: string[] = []

export const i18n = createI18n({
    legacy: false,
    locale: 'es',
    messages: { en, es },
    missingWarn: false,  // suppress individual warnings
    missing: (_locale, key) => {
        missingKeys.push(key)
        return key  // return key as fallback to avoid breaking rendering
    },
})

afterEach(() => {
    if (missingKeys.length > 0) {
        const unique = [...new Set(missingKeys)]
        missingKeys.length = 0
        throw new Error(`Missing i18n keys detected:\n  ${unique.join('\n  ')}`)
    }
})
```

Script to find i18n keys used in code but missing from locale files:

```bash
# Find all t('key') calls in Vue/TS files
grep -rh "t('" sites/Kakeibo.App/src/ --include="*.vue" --include="*.ts" \
    | grep -oP "t\('([^']+)'\)" | sort -u > /tmp/used-keys.txt

# Find all keys defined in locale files
node -e "
const en = require('./sites/Kakeibo.App/locales/en.json')
const flatten = (obj, prefix='') => Object.keys(obj).flatMap(k =>
    typeof obj[k] === 'object' ? flatten(obj[k], prefix + k + '.') : [prefix + k])
console.log(flatten(en).join('\n'))
" | sort > /tmp/defined-keys.txt

# Show keys used in code but missing from locale files
comm -23 /tmp/used-keys.txt /tmp/defined-keys.txt
```

---

## Visual Regression (Playwright, optional)

Capture visual snapshots of key screens to detect unintended UI changes:

```typescript
test('member list page matches visual snapshot', async ({ page }) => {
    await page.goto('/admin/members')
    await page.waitForLoadState('networkidle')

    await expect(page).toHaveScreenshot('member-list-page.png', {
        maxDiffPixels: 100,  // tolerance for anti-aliasing differences
    })
})
```

Run with `--update-snapshots` to regenerate baseline when intentional UI changes are made.

---

## Test Quality Indicators

### A well-written test:

- Name describes exactly what is verified: `HandleAsync_DuplicateEmail_ReturnsConflictError`
- Single responsibility: one behavior per test (or tightly related assertions for one behavior)
- Clear Arrange/Act/Assert separation
- No logic in the test body (no loops, no conditionals)
- Test data created inline or via factory helpers (no mutable shared state)
- Deterministic: same result on every execution
- Does not test framework behavior or trivial delegations

### When to decompose a large test:

```
❌ Warning signs:
  - More than one "// Act" section
  - Verifies multiple distinct behaviors
  - Name uses "and" to describe what it verifies
  - Arrange section has > 20 lines → extract factory helper

✅ Correct:
  - One business scenario per test
  - The name describes the full scenario in one line
```

### When to create factory helpers:

When the same entity creation code appears in 3+ tests in the same class. Use static private methods:

```csharp
// API (C#) — static method, no shared mutable state
private static Member CreateActiveMember(string email = "test@test.com") => new()
{
    UserId = Guid.NewGuid(),
    Email = email,
    Status = MemberStatusCodes.Active,
    CreatedAt = Instant.FromUtc(2026, 1, 1, 0, 0),
};
```

```typescript
// Frontend (TypeScript) — function with overrides
function createMemberFixture(overrides: Partial<Member> = {}): Member {
    return {
        id: crypto.randomUUID(),
        name: 'Ana García',
        email: 'ana@test.com',
        status: 'active',
        ...overrides,
    }
}
```

### When to add automatic convention tests:

| Situation | Action |
|-----------|--------|
| New abstract base class | Test that all subclasses are registered in DI |
| New naming convention | NetArchTest rule in the same PR |
| New required pattern (e.g., idempotent consumers) | Architecture test that verifies it |
| New permission type | Positive and negative authorization test |

---

## Quick Audit Workflow

When reviewing test coverage for an existing module:

1. **Grep error codes**: find all `Error.Code` in `Errors/` — check each has a test
2. **Grep consumers**: find all `IEventConsumer<T>` — check each has idempotency test
3. **Grep validators**: find all `AbstractValidator<T>` — check each rule has a boundary test
4. **Run Stryker**: identify surviving mutants in comparison operators and null checks
5. **Check permissions**: find all permissions in seeder — verify 2xx and 403 tests exist
6. **Run Coverlet**: identify uncovered branches in handlers
7. **Run Vitest with i18n gap detection**: check for missing translation keys in components
