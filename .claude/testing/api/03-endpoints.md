# Testing Endpoints

Endpoints define the HTTP surface of the API: which URL responds to which HTTP method, what auth policy applies, and whether the validation filter is attached. They delegate all business logic to the handler.

---

## What Is an Endpoint?

An endpoint is a class that ends with `Endpoint` and implements `IEndpoint`. It has a static `MapEndpoint` method that registers the route. Inside that method, a handler delegate is attached that extracts the user ID from the JWT principal and calls the handler.

**Location:** `src/Kakeibo.Api/Features/{Domain}/{Operation}/{Op}Endpoint.cs`

**What an endpoint is responsible for:**
- Registering the HTTP route (e.g., `POST /api/wallets`)
- Declaring the auth requirement (`RequireAuthorization()`)
- Attaching the validation filter (`.WithValidation<TRequest>()`)
- Extracting the user ID from `ClaimsPrincipal`
- Translating `Result<T>` into the correct HTTP status code

**What an endpoint is NOT responsible for:**
- Business logic (that lives in the handler)
- Input validation rules (that live in the validator)
- Database access (that lives in the handler)

---

## Do Not Unit Test Endpoints

Endpoints are thin wrappers. Unit testing a static method that registers a route provides almost no value. The interesting behavior — routing, auth, validation, status code mapping — only manifests when the full ASP.NET Core pipeline runs.

**Unit tests for endpoints: not recommended.**
**Integration tests via `WebApplicationFactory`: required for critical paths.**

---

## When to Write Endpoint Integration Tests

Write HTTP-level integration tests when you need to verify:

| Scenario | What to test |
|----------|-------------|
| Auth is required | A request without a token returns `401 Unauthorized` |
| Admin-only routes | A non-admin token returns `403 Forbidden` |
| Validation filter fires | A bad request body returns `400 Bad Request` with an error body |
| Success status codes | A valid request returns `200 OK` or `201 Created` |
| Response body shape | The JSON response contains the expected fields |
| Route exists | The endpoint responds (not `404`) |

If a feature is already well-tested at the handler level, an endpoint test is optional unless auth or validation behavior is complex.

---

## Setting Up `WebApplicationFactory`

`WebApplicationFactory<Program>` boots the entire ASP.NET Core application in memory — including middleware, DI, routing, and authentication. The test sends real HTTP requests to the in-memory server.

### Base class pattern

Create a shared base class for your integration tests to avoid repeating setup:

```csharp
public abstract class ApiIntegrationTest : IAsyncLifetime
{
    protected HttpClient Client { get; private set; } = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        // Check Docker availability early
        try
        {
            await TestDbContextFactory.EnsureAvailableAsync();
        }
        catch
        {
            Assert.Skip("Docker is not available. These tests require Testcontainers.");
            return;
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureServices(services =>
                {
                    // Replace the real DB connection string with the Testcontainers one
                    // (depends on how Program.cs reads configuration)
                });
            });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true    // Important: cookies carry the JWT token
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        Client.Dispose();
    }

    /// <summary>
    /// Registers a user, logs in, and returns the authenticated client with the
    /// access_token cookie set. The cookie is automatically sent on subsequent requests.
    /// </summary>
    protected async Task<HttpClient> LoginAsAsync(string email, string password)
    {
        // Register
        await Client.PostAsJsonAsync("/api/auth/register", new { email, password, username = email });

        // Login — the response sets the HttpOnly access_token cookie
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        return Client; // Cookie is now stored in the client's cookie container
    }
}
```

---

## Testing Authentication (401 and 403)

### 401 — No token

```csharp
[Fact]
public async Task GetWallets_NoToken_Returns401()
{
    // No login — unauthenticated client
    var response = await Client.GetAsync("/api/wallets");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

### 403 — Authenticated but wrong role

```csharp
[Fact]
public async Task GetAdminUsers_RegularUser_Returns403()
{
    var client = await LoginAsAsync("alice@example.com", "password123");

    var response = await client.GetAsync("/api/admin/users");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## Testing Validation (400)

The `ValidationFilter` runs before the handler. When validation fails, it returns `400 Bad Request` with a structured error body.

```csharp
[Fact]
public async Task CreateWallet_EmptyName_Returns400()
{
    var client = await LoginAsAsync("alice@example.com", "password123");

    var response = await client.PostAsJsonAsync("/api/wallets", new
    {
        name = "",           // invalid: empty
        type = "Personal",
        initialBalance = 0
    });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
    Assert.NotNull(body);
    Assert.Contains(body.Errors, e => e.Field == "name");
}
```

> The `ValidationErrorResponse` type is defined in your test project to deserialize the error body. Its exact shape depends on how `ValidationFilter` formats errors.

---

## Testing Success Responses

```csharp
[Fact]
public async Task CreateWallet_ValidRequest_Returns201WithWallet()
{
    var client = await LoginAsAsync("alice@example.com", "password123");

    var response = await client.PostAsJsonAsync("/api/wallets", new
    {
        name = "Checking Account",
        type = "Personal",
        initialBalance = 500.00
    });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var body = await response.Content.ReadFromJsonAsync<CreateWalletResponse>();
    Assert.NotNull(body);
    Assert.Multiple(
        () => Assert.Equal("Checking Account", body.Name),
        () => Assert.Equal("Personal", body.Type),
        () => Assert.NotEqual(Guid.Empty, body.Id)
    );
}
```

---

## Testing Response Body Shape

When verifying JSON responses, define simple record types in your test file to deserialize into:

```csharp
// Define locally in the test file or in a shared test helpers file
private sealed record CreateWalletResponse(Guid Id, string Name, string Type, decimal Balance);
private sealed record ValidationErrorResponse(IReadOnlyList<FieldError> Errors);
private sealed record FieldError(string Field, string Message);
```

---

## What NOT to Re-Test at the Endpoint Level

If handler tests already cover these paths, do not duplicate them at the HTTP level:

- Not found scenarios (404) — covered by handler tests
- Conflict scenarios (409) — covered by handler tests
- Complex business logic outcomes — covered by handler tests

The endpoint test exists to verify the HTTP plumbing is correct, not to re-test business rules.

---

## Checklist Before Submitting Endpoint Tests

- [ ] `401 Unauthorized` verified for protected endpoints without a token
- [ ] `403 Forbidden` verified for admin-only endpoints with a non-admin token (where applicable)
- [ ] `400 Bad Request` verified for at least one invalid input (validation filter fires)
- [ ] `200 OK` or `201 Created` verified for the happy path
- [ ] Response body shape verified for at least the critical fields
- [ ] `HandleCookies = true` set on the `WebApplicationFactory` client
- [ ] Tests are skipped gracefully when Docker is unavailable
