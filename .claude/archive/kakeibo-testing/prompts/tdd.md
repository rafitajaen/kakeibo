---
name: tdd
description: Test-driven development with red-green-refactor loop. Use when user wants to build features or fix bugs using TDD, mentions "red-green-refactor", wants integration tests, or asks for test-first development.
---

# Test-Driven Development

## Philosophy

**Core principle**: Tests should verify behavior through public interfaces, not implementation details. Code can change entirely; tests shouldn't.

**Good tests** are integration-style: they exercise real code paths through public APIs. They describe _what_ the system does, not _how_ it does it. A good test reads like a specification - "user can checkout with valid cart" tells you exactly what capability exists. These tests survive refactors because they don't care about internal structure.

**Bad tests** are coupled to implementation. They mock internal collaborators, test private methods, or verify through external means (like querying a database directly instead of using the interface). The warning sign: your test breaks when you refactor, but behavior hasn't changed. If you rename an internal function and tests fail, those tests were testing implementation, not behavior.

See [test-doubles.md](../references/test-doubles.md) for mocking guidelines.

## Anti-Pattern: Horizontal Slices

**DO NOT write all tests first, then all implementation.** This is "horizontal slicing" - treating RED as "write all tests" and GREEN as "write all code."

This produces **crap tests**:

- Tests written in bulk test _imagined_ behavior, not _actual_ behavior
- You end up testing the _shape_ of things (data structures, function signatures) rather than user-facing behavior
- Tests become insensitive to real changes - they pass when behavior breaks, fail when behavior is fine
- You outrun your headlights, committing to test structure before understanding the implementation

**Correct approach**: Vertical slices via tracer bullets. One test → one implementation → repeat. Each test responds to what you learned from the previous cycle. Because you just wrote the code, you know exactly what behavior matters and how to verify it.

```
WRONG (horizontal):
  RED:   test1, test2, test3, test4, test5
  GREEN: impl1, impl2, impl3, impl4, impl5

RIGHT (vertical):
  RED→GREEN: test1→impl1
  RED→GREEN: test2→impl2
  RED→GREEN: test3→impl3
  ...
```

## Workflow

### 1. Planning

Before writing any code:

- [ ] Confirm with user what interface changes are needed
- [ ] Confirm with user which behaviors to test (prioritize)
- [ ] Design interfaces for testability
- [ ] List the behaviors to test (not implementation steps)
- [ ] Get user approval on the plan

Ask: "What should the public interface look like? Which behaviors are most important to test?"

**You can't test everything.** Confirm with the user exactly which behaviors matter most. Focus testing effort on critical paths and complex logic, not every possible edge case.

### 2. Tracer Bullet

Write ONE test that confirms ONE thing about the system:

```
RED:   Write test for first behavior → test fails
GREEN: Write minimal code to pass → test passes
```

This is your tracer bullet - proves the path works end-to-end.

### 3. Incremental Loop

For each remaining behavior:

```
RED:   Write next test → fails
GREEN: Minimal code to pass → passes
```

Rules:

- One test at a time
- Only enough code to pass current test
- Don't anticipate future tests
- Keep tests focused on observable behavior

### 4. Refactor

After all tests pass, look for refactor candidates:

- [ ] Extract duplication
- [ ] Deepen modules (move complexity behind simple interfaces)
- [ ] Apply SOLID principles where natural
- [ ] Consider what new code reveals about existing code
- [ ] Run tests after each refactor step

**Never refactor while RED.** Get to GREEN first.

## Checklist Per Cycle

```
[ ] Test describes behavior, not implementation
[ ] Test uses public interface only
[ ] Test would survive internal refactor
[ ] Code is minimal for this test
[ ] No speculative features added
```

## Kakeibo-Specific TDD Commands

```bash
# API — run after each RED→GREEN cycle
bun run api:test:unit

# Frontend — run after each RED→GREEN cycle
bun run app:test:unit

# Mobile
bun run mobile:test:unit

# Email
bun run email:test
```

## Interface Design for Testability

Before writing the first red test, design the public interface with testability in mind:

### 1. Accept dependencies, don't create them

```csharp
// ❌ Hard to test — DbContext created internally
public class CreateMemberHandler
{
    private readonly AppDbContext _db = new();
}

// ✅ Testable — all dependencies injected via constructor
public class CreateMemberHandler(AppDbContext db, IEventBus eventBus, IClock clock) { }
```

### 2. Return results, don't rely on output parameters

```csharp
// ❌ Side effects only — hard to assert
public async Task HandleAsync(CreateMemberRequest request)
{
    // writes to DB but returns nothing
}

// ✅ Return Result<T> — callers assert on it
public async Task<Result<CreateMemberResponse>> HandleAsync(CreateMemberRequest request, CancellationToken ct) { }
```

### 3. Small surface area — fewer public methods = fewer tests needed

```csharp
// ❌ Multiple public methods expose internal steps
public class CreateMemberHandler
{
    public Task ValidateAsync(CreateMemberRequest request) { ... }
    public Task<Member> BuildMemberAsync(CreateMemberRequest request) { ... }
    public Task PersistAsync(Member member) { ... }
    public async Task<Result<CreateMemberResponse>> HandleAsync(...) { ... }
}

// ✅ One HandleAsync — internal complexity encapsulated
public class CreateMemberHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    public async Task<Result<CreateMemberResponse>> HandleAsync(CreateMemberRequest request, CancellationToken ct)
    {
        // validation, building, persisting — all internal
    }
}
```

### 4. Deep modules — simple interface, complex implementation

From "A Philosophy of Software Design" (Ousterhout): a module with a small interface and lots
of implementation is better than one that distributes complexity across many shallow classes.

```
Handler with 3 constructor args, 1 public method = good (deep module)
Handler split into 5 thin services, each with 5 methods = bad (shallow)
```

Ask yourself: does splitting this make the system easier or harder to test?
If each new class requires its own test doubles and its own test class, the split probably
introduced accidental complexity.

### 5. Refactor candidates after green

After all tests pass, scan for these patterns:

```
□ Duplicated test setup (3+ identical entity creations) → extract private factory helper
□ Handler method > 20 lines → extract private method with clear name
□ Same condition checked in 3+ tests → move to domain method on the entity
□ String or number appears in 3+ places → extract constant or config value
□ Handler reads data from another object's internals → move logic closer to that data
□ Value passed as string that has a constrained set of values → introduce Value Object
```

---

## Cycle Example (API Handler)

```bash
# 1. Write test (fails — handler doesn't exist yet)
bun run api:test:unit
# → FAIL: CreateMemberHandlerTests.HandleAsync_ValidRequest_CreatesMember

# 2. Write minimum implementation
# 3. Verify it passes
bun run api:test:unit
# → PASS (1 test added, all passing)

# 4. Write next test (conflict scenario)
bun run api:test:unit
# → FAIL: HandleAsync_DuplicateEmail_ReturnsConflictError

# 5. Add duplicate check to handler
bun run api:test:unit
# → PASS

# 6. Continue through: not-found, validation, auth, edge cases
```
