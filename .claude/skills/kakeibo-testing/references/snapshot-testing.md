# Snapshot Testing with Verify

Snapshot/approval testing for complex API responses and email templates.
Use the Verify library for .NET. Do NOT use Vitest `toMatchSnapshot()` for Vue component HTML
(that is an anti-pattern — see SKILL.md red flags).

---

## When to Use Snapshot Testing

Use Verify (not manual `Assert.Equal`) when:
- API integration response has > 5 fields to verify
- Email template HTML needs regression protection
- A complex aggregate or report structure must match exactly
- Refactoring should NOT change the output shape

**Do NOT use:**
- For simple scalar outputs (use `Assert.Equal`)
- For Vue component HTML (`toMatchSnapshot()` is brittle — use targeted assertions)
- When the output is fully non-deterministic without scrubbing

---

## Setup (.csproj)

Add to the integration test project:

```xml
<PackageReference Include="Verify.Xunit" />
<PackageReference Include="Verify.Http" />   <!-- for HttpResponseMessage -->
```

Module initializer (one per test assembly, placed in `VerifyConfig.cs`):

```csharp
[assembly: ModuleInitializer]
internal static class VerifyConfig
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Scrub non-deterministic fields globally
        VerifierSettings.AddScrubber(s =>
        {
            // GUIDs are replaced with a stable placeholder
        });

        // Scrub common non-deterministic members
        VerifierSettings.ScrubMember("id");
        VerifierSettings.ScrubMember("createdAt");
        VerifierSettings.ScrubMember("updatedAt");

        // Strict JSON: keys must be in the same order as the verified file
        VerifierSettings.UseStrictJson();
    }
}
```

Add to `.gitattributes` to avoid line ending issues in verified files:

```
*.verified.txt text eol=lf
*.verified.json text eol=lf
```

---

## Verifying API Responses (Level 5)

```csharp
[Collection("Integration")]
public sealed class GetWalletTests(WebApplicationFactory factory)
{
    private const string SkipReason =
        "Docker is not available. Integration tests require Docker to run Testcontainers.";

    [Fact]
    public async Task GetWallet_ExistingMember_MatchesSnapshot()
    {
        if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

        using var client = factory.CreateAuthClient();
        var data = factory.CreateTestDataBuilder();
        var memberId = await data.CreateVerifiedMemberAsync("member@test.com", "Test#12345Abc");

        await client.LoginAsync("admin@test.com", "Test#12345Abc");
        var response = await client.GetAsync($"/api/members/{memberId}");

        // Generates: tests/Kakeibo.Tests/snapshots/
        //   GetWalletTests.GetWallet_ExistingMember_MatchesSnapshot.verified.json
        await Verify(response);
    }
}
```

When the response shape is complex (nested objects, many fields), `await Verify(response)` is
preferred over 8 individual `Assert.Equal` calls. It captures the full response in a JSON file
and diffs against it on every future run.

---

## Scrubbing Non-Deterministic Values

```csharp
// Scrub specific member names (applies to the response JSON)
settings.ScrubMember("id");              // field named "id"
settings.ScrubMember("createdAt");       // timestamps
settings.ScrubMember("updatedAt");

// Scrub all GUIDs in the document
settings.ScrubGuid();

// Scrub a known dynamic value with a readable placeholder
var memberId = Guid.NewGuid();
settings.AddScrubber(s => s.Replace(memberId.ToString(), "MEMBER_ID"));

// Custom NodaTime scrubber — replaces Instant values with a stable placeholder
public class InstantConverter : WriteOnlyJsonConverter<Instant>
{
    public override void Write(VerifyJsonWriter writer, Instant value)
        => writer.WriteValue("Instant_scrubbed");
}

// Register in VerifyConfig.Initialize():
VerifierSettings.AddExtraSettings(s => s.Converters.Add(new InstantConverter()));
```

**Scrubbing order of operations:**
1. Global scrubbers registered in `VerifyConfig.Initialize()` apply to all tests
2. Per-test scrubbers override globals for that test only
3. `ScrubMember("x")` scrubs any JSON field named `x` at any depth

---

## Verifying Email Templates (Kakeibo.Email)

The email renderer service runs on port 3050. Use it directly to snapshot-test rendered HTML:

```csharp
[Fact]
public async Task WelcomeEmail_RendersCorrectly()
{
    // Requires the email renderer to be running (docker compose up email-renderer)
    using var client = new HttpClient { BaseAddress = new Uri("http://localhost:3050") };

    var response = await client.PostAsJsonAsync("/render/welcome", new
    {
        memberName = "Ana García",
        memberNumber = "CW000001"
    });

    // Snapshots the rendered HTML — detects regressions in template structure
    await Verify(response);
}
```

In integration tests where the email renderer is stubbed, snapshot the rendered content
returned by the stub rather than calling the real service.

---

## Snapshot File Location

Verified files live alongside the test project:

```
tests/Kakeibo.Tests/snapshots/
    CreateWalletHandlerTests.HandleAsync_WithValidRequest_MatchesSnapshot.verified.json
    GetWalletTests.GetWallet_ExistingMember_MatchesSnapshot.verified.json
```

Commit `.verified.json` files to source control — they are the baseline the test diffs against.

---

## Workflow

```
1. Write test with `await Verify(response)`
2. Run test → FAILS (no .verified.json exists yet)
3. Inspect the generated .received.json file — verify its content is correct
4. Accept it:
     dotnet verify accept
   This renames .received.json → .verified.json and commits the baseline
5. Future runs: test PASSES if output matches .verified.json
6. If output changes intentionally: review the diff, accept if correct
```

```bash
# Accept all pending snapshots
dotnet verify accept

# Accept a specific test snapshot
dotnet verify accept --test "GetWalletTests.GetWallet_ExistingMember_MatchesSnapshot"

# View diff (requires a diff tool configured in verify settings)
dotnet verify diff
```

---

## Parameterized Snapshot Tests

When testing multiple variants of the same response shape, use `[Theory]`:

```csharp
[Theory]
[InlineData("standard")]
[InlineData("premium")]
[InlineData("family")]
public async Task GetSubscriptionPlan_ByPlanCode_MatchesSnapshot(string planCode)
{
    if (!factory.IsDockerAvailable) Assert.Skip(SkipReason);

    using var client = factory.CreateAuthClient();
    var response = await client.GetAsync($"/api/plans/{planCode}");

    // Each variant generates a separate .verified.json:
    //   GetSubscriptionPlan_ByPlanCode_MatchesSnapshot_planCode=standard.verified.json
    await Verify(response).UseParameters(planCode);
}
```

---

## Key Constraints

- **Always scrub GUIDs and timestamps** — otherwise every new test run creates a diff
- **Commit `.verified.json` files** — they are the contract the test enforces
- **Review diffs before accepting** — `dotnet verify accept` is permanent
- **Use targeted assertions first** — only switch to `Verify` when > 5 fields need checking
- **Do not use in Level 2 (Handler Unit)** — Verify is overkill for handler tests; use `Assert.Multiple` instead
