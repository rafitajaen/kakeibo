# Architecture Tests

Architecture tests use NetArchTest to enforce naming conventions and structural rules across the entire API assembly at compile time. They catch violations before they reach code review and make it impossible to accidentally break conventions.

---

## What Are Architecture Tests?

Architecture tests are regular xUnit tests that use NetArchTest's fluent API to assert structural properties of the codebase — without running any code. They analyze type names, namespaces, base classes, and interfaces in the compiled assembly.

**Location:** `tests/Kakeibo.Tests/Architecture/`

These tests run automatically as part of `bun run api:test`. They do not require Docker.

---

## What Rules Are Currently Enforced

The existing architecture tests validate these conventions:

| Rule | What it checks |
|------|---------------|
| Handler naming | All classes in `Features/*/` that call into `AppDbContext` must end with `Handler` |
| Validator naming | All classes extending `AbstractValidator<T>` must end with `Validator` |
| Endpoint naming | All classes implementing `IEndpoint` must end with `Endpoint` |
| Configuration naming | All classes implementing `IEntityTypeConfiguration<T>` must end with `Configuration` |
| No DateTime | No class in `src/` may reference `System.DateTime` (use NodaTime instead) |
| No Guid.CreateVersion7 | No class may call `Guid.CreateVersion7()` (use `Guid7.NewGuid()` instead) |

---

## How Architecture Tests Work

### Getting a reference to the assembly

All rules target the API assembly. It is referenced once and reused across all tests.

```csharp
private static readonly Assembly ApiAssembly = typeof(Program).Assembly;
```

### Checking that types in a namespace have a specific name suffix

```csharp
[Fact]
public void Endpoints_ShouldEndWithEndpoint()
{
    var result = Types.InAssembly(ApiAssembly)
        .That()
        .ImplementInterface(typeof(IEndpoint))
        .Should()
        .HaveNameEndingWith("Endpoint")
        .GetResult();

    Assert.True(result.IsSuccessful, FormatFailures(result));
}
```

### Checking that types in a namespace have a specific base class

```csharp
[Fact]
public void Validators_ShouldEndWithValidator()
{
    var result = Types.InAssembly(ApiAssembly)
        .That()
        .Inherit(typeof(AbstractValidator<>))
        .Should()
        .HaveNameEndingWith("Validator")
        .GetResult();

    Assert.True(result.IsSuccessful, FormatFailures(result));
}
```

### Checking for forbidden type usage

```csharp
[Fact]
public void NoCode_ShouldUseDateTime()
{
    var result = Types.InAssembly(ApiAssembly)
        .That()
        .ResideInNamespace("Kakeibo")
        .ShouldNot()
        .HaveDependencyOn("System.DateTime")
        .GetResult();

    Assert.True(result.IsSuccessful, FormatFailures(result));
}
```

### Formatting failure messages

NetArchTest's `GetResult()` returns a `TestResult` with a list of failing type names. Format them clearly for the test output.

```csharp
private static string FormatFailures(TestResult result)
{
    if (result.IsSuccessful) return string.Empty;

    var failing = result.FailingTypes?
        .Select(t => $"  - {t.FullName}")
        ?? [];

    return $"The following types violate the rule:\n{string.Join("\n", failing)}";
}
```

---

## Complete Example — Current Architecture Test File

```csharp
namespace Kakeibo.Tests.Architecture;

public sealed class NamingConventionTests
{
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Endpoints_ShouldImplementIEndpoint_AndEndWithEndpoint()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .Should()
            .HaveNameEndingWith("Endpoint")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Validators_ShouldInheritAbstractValidator_AndEndWithValidator()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void EntityConfigurations_ShouldImplementIEntityTypeConfiguration_AndEndWithConfiguration()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .HaveNameEndingWith("Configuration")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void EventHandlers_ShouldImplementIEventHandler_AndEndWithHandler()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ImplementInterface(typeof(IEventHandler<>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.IsSuccessful) return string.Empty;

        var failing = result.FailingTypes?
            .Select(t => $"  - {t.FullName}")
            ?? [];

        return $"The following types violate the naming convention:\n{string.Join("\n", failing)}";
    }
}
```

---

## How to Add a New Architecture Rule

Follow this process whenever you introduce a new structural convention:

**Step 1 — Identify the rule.**
Describe it in plain English: "All classes that inherit from `X` must end with `Y`."

**Step 2 — Write the test.**
Add a new `[Fact]` in the appropriate test class in `tests/Kakeibo.Tests/Architecture/`. Use the same `Types.InAssembly(ApiAssembly).That()...Should()...GetResult()` pattern.

**Step 3 — Verify it fails before fixing.**
Comment out a compliant type's name and confirm the test catches it. Then restore the name.

**Step 4 — Document the rule.**
Add a row to the rules table in this document so future developers know the rule exists.

### Example — Adding a rule for repository classes

If you introduce a repository pattern where all repositories must end with `Repository`:

```csharp
[Fact]
public void Repositories_ShouldEndWithRepository()
{
    var result = Types.InAssembly(ApiAssembly)
        .That()
        .ImplementInterface(typeof(IRepository<>))
        .Should()
        .HaveNameEndingWith("Repository")
        .GetResult();

    Assert.True(result.IsSuccessful, FormatFailures(result));
}
```

---

## Architecture Tests vs. Other Tests

| Question | Answer |
|----------|--------|
| Do they require Docker? | No — they analyze compiled types, not running code |
| Do they run in CI? | Yes — automatically in `bun run api:test` |
| Do they catch runtime bugs? | No — they only catch structural violations |
| When do they fail? | When a class name does not match its role's convention |
| Who should write them? | Anyone introducing a new structural convention |

---

## Checklist Before Submitting Architecture Tests

- [ ] Rule described clearly in plain English before writing the test
- [ ] Test uses `Types.InAssembly(ApiAssembly)` with the correct filter and assertion
- [ ] `FormatFailures()` helper used so failing type names appear in the output
- [ ] Rule documented in the rules table in this file
- [ ] Test verified to fail on a non-compliant type before confirming it passes
