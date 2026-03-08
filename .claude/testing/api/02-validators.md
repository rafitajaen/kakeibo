# Testing Validators

Validators enforce input rules before a request reaches the handler. They are pure classes with no database access and no external dependencies — which makes them the fastest and simplest component to test.

---

## What Is a Validator?

A validator is a class that ends with `Validator` and extends `AbstractValidator<{Op}Request>`. It defines rules using FluentValidation's fluent API. The `ValidationFilter` registered on each endpoint automatically runs the validator before the handler is called. If validation fails, the endpoint returns HTTP 400 with a structured error body — the handler is never invoked.

**Location:** `src/Kakeibo.Api/Features/{Domain}/{Operation}/{Op}Validator.cs`

**Example:**

```csharp
public sealed class CreateWalletValidator : AbstractValidator<CreateWalletEndpoint.CreateWalletRequest>
{
    public CreateWalletValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => new[] { "Personal", "Shared" }.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Type must be one of: Personal, Shared.");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0m)
            .LessThanOrEqualTo(999_999_999.99m);
    }
}
```

---

## How to Test a Validator

Validators require no setup beyond instantiation. The FluentValidation test extension method `TestValidate` executes the validator against a given input and returns a result object that you can assert against.

**No database. No Docker. No mocks.**

### Basic pattern

```csharp
// Arrange
var validator = new CreateWalletValidator();
var request = new CreateWalletEndpoint.CreateWalletRequest(
    Name: "Checking Account",
    Type: "Personal",
    InitialBalance: 500m);

// Act
var result = validator.TestValidate(request);

// Assert — valid request should have no errors
result.ShouldNotHaveAnyValidationErrors();
```

### Asserting that a specific field has an error

```csharp
var request = new CreateWalletEndpoint.CreateWalletRequest(
    Name: "",        // empty — should fail
    Type: "Personal",
    InitialBalance: 0m);

var result = validator.TestValidate(request);

result.ShouldHaveValidationErrorFor(x => x.Name);
```

### Asserting that a specific field has NO error

```csharp
result.ShouldNotHaveValidationErrorFor(x => x.Name);
```

### Asserting the error message

```csharp
result.ShouldHaveValidationErrorFor(x => x.Type)
      .WithErrorMessage("Type must be one of: Personal, Shared.");
```

---

## What to Test

For each validator, cover all four categories below.

### 1. Valid Request — No Errors

Always start with a test that confirms a well-formed request passes without any errors. This is the baseline.

```csharp
[Fact]
public void TestValidate_ValidRequest_HasNoValidationErrors()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "Checking Account",
        Type: "Personal",
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
}
```

### 2. Required Fields — Empty or Null

Test each required field individually with an empty or null value.

```csharp
[Fact]
public void TestValidate_EmptyName_HasValidationError()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "",
        Type: "Personal",
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Name);
}

[Fact]
public void TestValidate_EmptyType_HasValidationError()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "My Wallet",
        Type: "",
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Type);
}
```

### 3. Boundary Values — Max Length and Numeric Limits

Test the edges of numeric and string constraints. One test should sit exactly at the limit (valid), and one should exceed it (invalid).

```csharp
[Fact]
public void TestValidate_NameExactly100Chars_HasNoValidationError()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: new string('a', 100),    // exactly at limit — valid
        Type: "Personal",
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.Name);
}

[Fact]
public void TestValidate_Name101Chars_HasValidationError()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: new string('a', 101),    // one over the limit — invalid
        Type: "Personal",
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Name);
}

[Fact]
public void TestValidate_NegativeInitialBalance_HasValidationError()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "My Wallet",
        Type: "Personal",
        InitialBalance: -0.01m);

    validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.InitialBalance);
}
```

### 4. Enum / Allowed Values — Invalid Values

Test that values outside the allowed set produce an error, and that all allowed values are accepted.

```csharp
[Fact]
public void TestValidate_InvalidType_HasValidationError()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "My Wallet",
        Type: "Unknown",
        InitialBalance: 0m);

    validator.TestValidate(request)
             .ShouldHaveValidationErrorFor(x => x.Type)
             .WithErrorMessage("Type must be one of: Personal, Shared.");
}

[Theory]
[InlineData("Personal")]
[InlineData("personal")]   // case-insensitive
[InlineData("Shared")]
[InlineData("SHARED")]
public void TestValidate_ValidTypeValues_HasNoValidationError(string type)
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "My Wallet",
        Type: type,
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.Type);
}
```

---

## Using `[Theory]` for Multiple Inputs

When you need to test the same rule against many values, use `[Theory]` + `[InlineData]` instead of writing a separate `[Fact]` for each value.

```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]    // whitespace only
[InlineData(null)]
public void TestValidate_BlankName_HasValidationError(string? name)
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: name!,
        Type: "Personal",
        InitialBalance: 0m);

    validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Name);
}
```

---

## Multiple Errors in One Test

A single invalid request can trigger errors on multiple fields. `TestValidate` collects all errors, so you can assert several at once.

```csharp
[Fact]
public void TestValidate_MultipleInvalidFields_HasMultipleErrors()
{
    var validator = new CreateWalletValidator();
    var request = new CreateWalletEndpoint.CreateWalletRequest(
        Name: "",
        Type: "BadType",
        InitialBalance: -1m);

    var result = validator.TestValidate(request);

    Assert.Multiple(
        () => result.ShouldHaveValidationErrorFor(x => x.Name),
        () => result.ShouldHaveValidationErrorFor(x => x.Type),
        () => result.ShouldHaveValidationErrorFor(x => x.InitialBalance)
    );
}
```

---

## Checklist Before Submitting Validator Tests

- [ ] One test confirms a fully valid request has no errors
- [ ] Each required field is tested with an empty/null value
- [ ] Each string field with `MaximumLength` is tested at the boundary (exactly at limit and one over)
- [ ] Each numeric field with `GreaterThanOrEqualTo` / `LessThanOrEqualTo` is tested at the boundary
- [ ] Each enum/allowed-values field is tested with an invalid value and all valid values
- [ ] `[Theory]` + `[InlineData]` used where multiple input variants test the same rule
- [ ] Test names follow `TestValidate_{Scenario}_{ExpectedResult}` convention
