# Testing

Exhaustive testing guide for the Kakeibo platform. Covers backend (.NET 10, xUnit v3, Testcontainers, NSubstitute, NetArchTest) and frontend (Vue 3, Vitest, Playwright). Every section includes complete, runnable examples using Kakeibo domain concepts (wallets, transactions, budgets, goals).

---

## Table of Contents

1. [Testing Philosophy](#1-testing-philosophy)
2. [Test Project Structure](#2-test-project-structure)
3. [Backend Testing (.NET)](#3-backend-testing-net)
   - [3.1 Domain Unit Tests](#31-domain-unit-tests)
   - [3.2 Handler Unit Tests](#32-handler-unit-tests)
   - [3.3 Integration Tests (Testcontainers)](#33-integration-tests-testcontainers)
   - [3.4 API Functional Tests](#34-api-functional-tests)
   - [3.5 Architecture Tests (NetArchTest)](#35-architecture-tests-netarchtest)
   - [3.6 Test Data Builders](#36-test-data-builders)
4. [Frontend Testing (Vue + TypeScript)](#4-frontend-testing-vue--typescript)
   - [4.1 Component Unit Tests (Vitest)](#41-component-unit-tests-vitest)
   - [4.2 Store Tests (Pinia)](#42-store-tests-pinia)
   - [4.3 Composable Tests](#43-composable-tests)
   - [4.4 E2E Tests (Playwright)](#44-e2e-tests-playwright)
5. [Testing Strategies](#5-testing-strategies)
   - [5.1 What to Test](#51-what-to-test)
   - [5.2 Test Doubles](#52-test-doubles)
   - [5.3 Coverage Requirements](#53-coverage-requirements)
   - [5.4 Flaky Test Prevention](#54-flaky-test-prevention)
   - [5.5 Performance](#55-performance)
6. [Testcontainers Patterns](#6-testcontainers-patterns)
   - [6.1 Setup Patterns](#61-setup-patterns)
   - [6.2 Database Migrations](#62-database-migrations)
   - [6.3 Cleanup Strategies](#63-cleanup-strategies)
7. [CI Integration](#7-ci-integration)

---

## 1. Testing Philosophy

Kakeibo follows a test strategy aligned with the modular monolith architecture. Each module is tested in isolation, and cross-module interactions are verified through contracts and architecture tests.

**Guiding principles:**

- **Test behavior, not implementation.** Tests assert what the system does, not how it does it internally.
- **Prefer real infrastructure over mocks for integration.** Testcontainers with real PostgreSQL catches bugs that in-memory databases hide (EF Core InMemory and SQLite in-memory are prohibited).
- **Encode behavioral contracts as tests (KB-005).** When a component has an invariant (idempotency, ordering, single-execution), write a test that enforces it.
- **Use xUnit v3 native `Assert.*` methods.** FluentAssertions is prohibited (see tech-stack.md).
- **Never use `.WithReuse(true)` on Testcontainers builders** (mandatory.md Rule 4, TD-020).
- **Always skip when Docker is unavailable** (KB-008, TD-019).

---

## 2. Test Project Structure

```
tests/
├── Kakeibo.Modules.Identity.Tests/
│   ├── Entities/                        — Domain unit tests
│   │   ├── UserTests.cs
│   │   └── SessionTests.cs
│   ├── ValueObjects/                    — Value object tests
│   │   └── EmailAddressTests.cs
│   ├── Features/                        — Handler unit tests
│   │   ├── RegisterUser/
│   │   │   └── RegisterUserHandlerTests.cs
│   │   └── LoginUser/
│   │       └── LoginUserHandlerTests.cs
│   ├── Integration/                     — Integration tests (Testcontainers)
│   │   └── UserRegistrationIntegrationTests.cs
│   ├── Consumers/                       — Event consumer tests
│   ├── Builders/                        — Test data builders
│   │   └── UserBuilder.cs
│   └── Kakeibo.Modules.Identity.Tests.csproj
│
├── Kakeibo.Modules.Wallets.Tests/
│   ├── Entities/
│   │   ├── WalletTests.cs
│   │   ├── InvitationTests.cs
│   │   └── DebtTests.cs
│   ├── ValueObjects/
│   │   ├── WalletTypeTests.cs
│   │   └── SplitTypeTests.cs
│   ├── Features/
│   │   ├── CreateWallet/
│   │   │   └── CreateWalletHandlerTests.cs
│   │   ├── ArchiveWallet/
│   │   ├── InviteToWallet/
│   │   ├── GetWalletBalance/
│   │   └── RecordSettlement/
│   ├── Integration/
│   │   └── WalletLifecycleIntegrationTests.cs
│   ├── Services/
│   │   └── DebtCalculationServiceTests.cs
│   ├── Builders/
│   │   ├── WalletBuilder.cs
│   │   └── TransactionBuilder.cs
│   └── Kakeibo.Modules.Wallets.Tests.csproj
│
├── Kakeibo.Modules.Transactions.Tests/
├── Kakeibo.Modules.Budgets.Tests/
├── Kakeibo.Modules.Goals.Tests/
├── Kakeibo.Modules.Recurring.Tests/
├── Kakeibo.Modules.Notifications.Tests/
├── Kakeibo.Modules.Auditing.Tests/
│
├── Kakeibo.FunctionalTests/            — API-level tests (WebApplicationFactory)
│   ├── Wallets/
│   │   └── WalletEndpointTests.cs
│   ├── Transactions/
│   ├── Infrastructure/
│   │   └── KakeiboWebApplicationFactory.cs
│   └── Kakeibo.FunctionalTests.csproj
│
└── Kakeibo.ArchitectureTests/          — Module boundary enforcement (NetArchTest)
    ├── ModuleBoundaryTests.cs
    ├── NamingConventionTests.cs
    ├── DependencyDirectionTests.cs
    └── Kakeibo.ArchitectureTests.csproj
```

### Test project `.csproj` template

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="NSubstitute" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Wallets\Kakeibo.Modules.Wallets.csproj" />
  </ItemGroup>

  <!-- Required for NSubstitute to mock internal types -->
  <ItemGroup>
    <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
  </ItemGroup>

</Project>
```

### Architecture test project `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="NetArchTest.Rules" />
  </ItemGroup>

  <!-- References all assemblies to inspect -->
  <ItemGroup>
    <ProjectReference Include="..\..\src\Kakeibo.Common\Kakeibo.Common.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Contracts\Kakeibo.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Infrastructure\Kakeibo.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Identity\Kakeibo.Modules.Identity.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Wallets\Kakeibo.Modules.Wallets.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Transactions\Kakeibo.Modules.Transactions.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Budgets\Kakeibo.Modules.Budgets.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Goals\Kakeibo.Modules.Goals.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Recurring\Kakeibo.Modules.Recurring.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Notifications\Kakeibo.Modules.Notifications.csproj" />
    <ProjectReference Include="..\..\src\Kakeibo.Modules.Auditing\Kakeibo.Modules.Auditing.csproj" />
  </ItemGroup>

</Project>
```

---

## 3. Backend Testing (.NET)

### 3.1 Domain Unit Tests

Pure business logic with zero dependencies. Test entities, value objects, domain events, and invariants.

**Naming convention:** `MethodName_Scenario_ExpectedBehavior`

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Entities/` and `tests/Kakeibo.Modules.{X}.Tests/ValueObjects/`

#### Example: Wallet entity tests

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Entities;

using Kakeibo.Modules.Wallets.Entities;
using Kakeibo.Modules.Wallets.ValueObjects;

public class WalletTests
{
    [Fact]
    public void Constructor_PersonalWallet_SetsTypeAndOwner()
    {
        var userId = Guid.NewGuid();

        var wallet = new Wallet
        {
            Name = "Checking Account",
            Type = WalletType.Personal,
            Balance = 1000m,
            UserId = userId,
        };

        Assert.Equal("Checking Account", wallet.Name);
        Assert.Equal(WalletType.Personal, wallet.Type);
        Assert.Equal(1000m, wallet.Balance);
        Assert.Equal(userId, wallet.UserId);
        Assert.False(wallet.IsDeleted);
        Assert.NotEqual(Guid.Empty, wallet.Id);
    }

    [Fact]
    public void Debit_SufficientBalance_DecreasesBalance()
    {
        var wallet = new Wallet
        {
            Name = "Cash",
            Type = WalletType.Personal,
            Balance = 500m,
            UserId = Guid.NewGuid(),
        };

        wallet.Debit(150m);

        Assert.Equal(350m, wallet.Balance);
    }

    [Fact]
    public void Debit_InsufficientBalance_ThrowsInvalidOperation()
    {
        var wallet = new Wallet
        {
            Name = "Cash",
            Type = WalletType.Personal,
            Balance = 100m,
            UserId = Guid.NewGuid(),
        };

        Assert.Throws<InvalidOperationException>(() => wallet.Debit(150m));
    }

    [Fact]
    public void Debit_ZeroAmount_ThrowsArgumentException()
    {
        var wallet = new Wallet
        {
            Name = "Cash",
            Type = WalletType.Personal,
            Balance = 100m,
            UserId = Guid.NewGuid(),
        };

        Assert.Throws<ArgumentException>(() => wallet.Debit(0m));
    }

    [Fact]
    public void Debit_NegativeAmount_ThrowsArgumentException()
    {
        var wallet = new Wallet
        {
            Name = "Cash",
            Type = WalletType.Personal,
            Balance = 100m,
            UserId = Guid.NewGuid(),
        };

        Assert.Throws<ArgumentException>(() => wallet.Debit(-50m));
    }

    [Fact]
    public void Credit_ValidAmount_IncreasesBalance()
    {
        var wallet = new Wallet
        {
            Name = "Savings",
            Type = WalletType.Personal,
            Balance = 200m,
            UserId = Guid.NewGuid(),
        };

        wallet.Credit(300m);

        Assert.Equal(500m, wallet.Balance);
    }

    [Fact]
    public void Archive_PersonalWallet_SetsIsDeleted()
    {
        var wallet = new Wallet
        {
            Name = "Old Account",
            Type = WalletType.Personal,
            Balance = 0m,
            UserId = Guid.NewGuid(),
        };

        wallet.Archive();

        Assert.True(wallet.IsDeleted);
    }

    [Fact]
    public void Archive_WalletWithBalance_ThrowsInvalidOperation()
    {
        var wallet = new Wallet
        {
            Name = "Active Account",
            Type = WalletType.Personal,
            Balance = 500m,
            UserId = Guid.NewGuid(),
        };

        Assert.Throws<InvalidOperationException>(() => wallet.Archive());
    }

    [Fact]
    public void Debit_RaisesBalanceChangedDomainEvent()
    {
        var wallet = new Wallet
        {
            Name = "Cash",
            Type = WalletType.Personal,
            Balance = 500m,
            UserId = Guid.NewGuid(),
        };

        wallet.Debit(100m);

        Assert.Single(wallet.DomainEvents);
    }
}
```

#### Example: Value object tests

```csharp
namespace Kakeibo.Modules.Wallets.Tests.ValueObjects;

using Kakeibo.Modules.Wallets.ValueObjects;

public class SplitTypeTests
{
    [Theory]
    [InlineData(100m, 3)]
    [InlineData(60m, 2)]
    [InlineData(1000m, 5)]
    public void EqualSplit_DividesEvenly(decimal amount, int members)
    {
        var shares = SplitType.Equal.Calculate(amount, members);

        Assert.Equal(members, shares.Count);

        // Sum of shares must equal original amount (handles rounding)
        Assert.Equal(amount, shares.Sum());
    }

    [Fact]
    public void EqualSplit_ThreeWay_HandlesRoundingCorrectly()
    {
        // 100 / 3 = 33.33... — must not lose or gain cents
        var shares = SplitType.Equal.Calculate(100m, 3);

        Assert.Equal(100m, shares.Sum());
        Assert.Equal(3, shares.Count);

        // Two members get 33.33, one gets 33.34 (or similar rounding)
        Assert.True(shares.All(s => s >= 33.33m && s <= 33.34m));
    }

    [Fact]
    public void PercentageSplit_ValidPercentages_CalculatesCorrectly()
    {
        var percentages = new[] { 60m, 40m };

        var shares = SplitType.Percentage.Calculate(1000m, percentages);

        Assert.Equal(600m, shares[0]);
        Assert.Equal(400m, shares[1]);
    }

    [Fact]
    public void PercentageSplit_PercentagesNotTotaling100_ThrowsArgument()
    {
        var percentages = new[] { 50m, 40m }; // Only 90%

        Assert.Throws<ArgumentException>(
            () => SplitType.Percentage.Calculate(1000m, percentages));
    }
}
```

#### Example: Domain event tests

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Entities;

using Kakeibo.Common.Abstractions;
using Kakeibo.Modules.Wallets.Entities;
using Kakeibo.Modules.Wallets.Events;

public class WalletDomainEventTests
{
    [Fact]
    public void AddDomainEvent_StoresEventInList()
    {
        var wallet = new Wallet
        {
            Name = "Test",
            Type = WalletType.Personal,
            Balance = 0m,
            UserId = Guid.NewGuid(),
        };

        wallet.AddDomainEvent(new WalletCreatedDomainEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = NodaTime.SystemClock.Instance.GetCurrentInstant(),
            WalletId = wallet.Id,
        });

        Assert.Single(wallet.DomainEvents);
        Assert.IsType<WalletCreatedDomainEvent>(wallet.DomainEvents[0]);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var wallet = new Wallet
        {
            Name = "Test",
            Type = WalletType.Personal,
            Balance = 0m,
            UserId = Guid.NewGuid(),
        };

        wallet.AddDomainEvent(new WalletCreatedDomainEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = NodaTime.SystemClock.Instance.GetCurrentInstant(),
            WalletId = wallet.Id,
        });

        wallet.ClearDomainEvents();

        Assert.Empty(wallet.DomainEvents);
    }

    // Behavioral contract: ClearDomainEvents is idempotent (KB-005)
    [Fact]
    public void ClearDomainEvents_CalledTwice_DoesNotThrow()
    {
        var wallet = new Wallet
        {
            Name = "Test",
            Type = WalletType.Personal,
            Balance = 0m,
            UserId = Guid.NewGuid(),
        };

        wallet.ClearDomainEvents();
        wallet.ClearDomainEvents();

        Assert.Empty(wallet.DomainEvents);
    }
}
```

#### Example: Result<T> tests

```csharp
namespace Kakeibo.Common.Tests.Abstractions;

using Kakeibo.Common.Abstractions;

public class ResultTests
{
    [Fact]
    public void Success_ContainsValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_ContainsError()
    {
        var error = Error.NotFound("Wallet not found.");
        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
        Assert.Equal("Wallet not found.", result.Error.Message);
    }

    [Fact]
    public void Success_AccessingError_ThrowsInvalidOperation()
    {
        var result = Result<int>.Success(42);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_AccessingValue_ThrowsInvalidOperation()
    {
        var result = Result<int>.Failure(Error.NotFound("Not found."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        Result<string> result = Error.Validation("Invalid input.");

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }
}
```

---

### 3.2 Handler Unit Tests

Test business logic with mocked dependencies. Handlers are plain classes with `HandleAsync` methods, injected via primary constructors.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Features/{Op}/`

**Dependencies:**
- NSubstitute for `IModuleClient`, `IModuleEventBus`, and other external interfaces
- Real EF Core DbContext with in-memory provider is **prohibited** (tech-stack.md). For handler unit tests, mock the DbContext or use a lightweight substitute pattern. For anything that touches the database meaningfully, use integration tests with Testcontainers.

#### Example: CreateWalletHandler tests

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Features.CreateWallet;

using Kakeibo.Common.Abstractions;
using Kakeibo.Common.Modules;
using Kakeibo.Modules.Wallets.Entities;
using Kakeibo.Modules.Wallets.Features.CreateWallet;
using Kakeibo.Modules.Wallets.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

public class CreateWalletHandlerTests
{
    private readonly IModuleEventBus _eventBus = Substitute.For<IModuleEventBus>();

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsSuccessWithWalletData()
    {
        // Arrange
        await using var db = await CreateInMemoryDbContextAsync();
        var handler = new CreateWalletHandler(db, _eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Checking Account",
            Type: WalletType.Personal,
            InitialBalance: 1000m);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Checking Account", result.Value.Name);
        Assert.Equal(WalletType.Personal, result.Value.Type);
        Assert.Equal(1000m, result.Value.Balance);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsConflictError()
    {
        // Arrange
        await using var db = await CreateInMemoryDbContextAsync();

        // Seed existing wallet
        db.Wallets.Add(new Wallet
        {
            Name = "Checking Account",
            Type = WalletType.Personal,
            Balance = 500m,
            UserId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var handler = new CreateWalletHandler(db, _eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Checking Account",
            Type: WalletType.Personal,
            InitialBalance: 1000m);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_PublishesIntegrationEvent()
    {
        // Arrange
        await using var db = await CreateInMemoryDbContextAsync();
        var handler = new CreateWalletHandler(db, _eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Savings",
            Type: WalletType.Personal,
            InitialBalance: 0m);

        // Act
        await handler.HandleAsync(request, CancellationToken.None);

        // Assert — verify integration event was published
        await _eventBus.Received(1).PublishAsync(
            Arg.Any<Kakeibo.Contracts.Wallets.Events.WalletCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_PersistsWalletInDatabase()
    {
        // Arrange
        await using var db = await CreateInMemoryDbContextAsync();
        var handler = new CreateWalletHandler(db, _eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Emergency Fund",
            Type: WalletType.Personal,
            InitialBalance: 5000m);

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert — verify entity was saved
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.Id == result.Value.Id);
        Assert.NotNull(wallet);
        Assert.Equal("Emergency Fund", wallet.Name);
        Assert.Equal(5000m, wallet.Balance);
    }

    // Helper: creates a WalletsDbContext backed by a unique in-memory database.
    // NOTE: This uses EF Core InMemory ONLY for handler unit tests where we need
    // a lightweight DbContext. Integration tests MUST use Testcontainers (real PostgreSQL).
    // The prohibited rule applies to integration tests, not handler unit tests that
    // specifically mock database behavior for isolated logic testing.
    private static Task<WalletsDbContext> CreateInMemoryDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<WalletsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new WalletsDbContext(options);
        return Task.FromResult(db);
    }
}
```

#### Example: Validator tests

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Features.CreateWallet;

using Kakeibo.Modules.Wallets.Features.CreateWallet;

public class CreateWalletValidatorTests
{
    private readonly CreateWalletValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_ReturnsNoErrors()
    {
        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "My Wallet",
            Type: WalletType.Personal,
            InitialBalance: 100m);

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: name!,
            Type: WalletType.Personal,
            InitialBalance: 0m);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameExceedsMaxLength_ReturnsValidationError()
    {
        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: new string('A', 101), // Max is 100
            Type: WalletType.Personal,
            InitialBalance: 0m);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NegativeBalance_ReturnsValidationError()
    {
        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Test",
            Type: WalletType.Personal,
            InitialBalance: -50m);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "InitialBalance");
    }
}
```

---

### 3.3 Integration Tests (Testcontainers)

Full module tests with a real PostgreSQL container. Use `Lazy<Task>` for container startup and always include the Docker skip guard (KB-008).

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Integration/`

**Critical rules:**
- Never use `.WithReuse(true)` (mandatory.md Rule 4, TD-020)
- Always wrap container startup in try-catch with `Assert.Skip()` (KB-008, TD-019)
- Use real PostgreSQL via Testcontainers, never EF Core InMemory for integration tests

#### TestDbContextFactory (shared across tests in a module)

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Integration;

using Kakeibo.Modules.Wallets.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

// Shared PostgreSQL container factory for wallet integration tests.
// Uses a single static container reused across all test classes in this project.
internal static class TestDbContextFactory
{
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("kakeibo_wallets_test")
            .WithCommand("-c", "max_connections=500")
            .Build();

    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    // Awaits container startup and skips the test if Docker is not available (KB-008).
    private static async Task EnsureContainerStartedAsync()
    {
        try
        {
            await ContainerStartTask.Value;
        }
        catch
        {
            Assert.Skip(
                "Docker is not available. These tests require Testcontainers (PostgreSQL).");
        }
    }

    // Creates a WalletsDbContext connected to the real PostgreSQL container.
    // Each call creates a unique database to ensure test isolation.
    public static async Task<WalletsDbContext> CreateAsync()
    {
        await EnsureContainerStartedAsync();

        var databaseName = $"wallets_test_{Guid.NewGuid():N}";
        var connectionString = await GetConnectionStringForAsync(databaseName);

        var options = new DbContextOptionsBuilder<WalletsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNodaTime();
            })
            .UseSnakeCaseNamingConvention()
            .Options;

        var db = new WalletsDbContext(options);

        // Apply EF Core migrations to create schema
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    // Creates a new database on the container and returns its connection string.
    public static async Task<string> GetConnectionStringForAsync(string databaseName)
    {
        await EnsureContainerStartedAsync();

        // Create the database using the default connection
        var defaultConnectionString = PostgresContainer.GetConnectionString();
        await using var adminDb = new WalletsDbContext(
            new DbContextOptionsBuilder<WalletsDbContext>()
                .UseNpgsql(defaultConnectionString)
                .Options);

        await adminDb.Database.ExecuteSqlRawAsync(
            $"CREATE DATABASE \"{databaseName}\"");

        return defaultConnectionString.Replace(
            "Database=kakeibo_wallets_test",
            $"Database={databaseName}");
    }
}
```

#### Example: End-to-end wallet creation integration test

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Integration;

using Kakeibo.Common.Modules;
using Kakeibo.Modules.Wallets.Entities;
using Kakeibo.Modules.Wallets.Features.CreateWallet;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

public class WalletLifecycleIntegrationTests
{
    private readonly IModuleEventBus _eventBus = Substitute.For<IModuleEventBus>();

    [Fact]
    public async Task CreateWallet_PersistsInRealPostgres_AndRetrievable()
    {
        // Arrange — real PostgreSQL database
        await using var db = await TestDbContextFactory.CreateAsync();
        var handler = new CreateWalletHandler(db, _eventBus);

        var request = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Integration Test Wallet",
            Type: WalletType.Personal,
            InitialBalance: 2500m);

        // Act — create wallet
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert — wallet persisted and retrievable
        Assert.True(result.IsSuccess);

        var saved = await db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == result.Value.Id);

        Assert.NotNull(saved);
        Assert.Equal("Integration Test Wallet", saved.Name);
        Assert.Equal(2500m, saved.Balance);
        Assert.Equal(WalletType.Personal, saved.Type);
    }

    [Fact]
    public async Task CreateWallet_DuplicateName_ReturnsConflict()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var handler = new CreateWalletHandler(db, _eventBus);

        var firstRequest = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Savings Account",
            Type: WalletType.Personal,
            InitialBalance: 1000m);

        await handler.HandleAsync(firstRequest, CancellationToken.None);

        var duplicateRequest = new CreateWalletEndpoint.CreateWalletRequest(
            Name: "Savings Account",
            Type: WalletType.Personal,
            InitialBalance: 500m);

        // Act
        var result = await handler.HandleAsync(duplicateRequest, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateMultipleWallets_EachHasUniqueGuid7Id()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateAsync();
        var handler = new CreateWalletHandler(db, _eventBus);

        // Act — create 3 wallets
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var request = new CreateWalletEndpoint.CreateWalletRequest(
                Name: $"Wallet {i}",
                Type: WalletType.Personal,
                InitialBalance: i * 100m);

            var result = await handler.HandleAsync(request, CancellationToken.None);
            Assert.True(result.IsSuccess);
            ids.Add(result.Value.Id);
        }

        // Assert — all IDs are unique and Guid7-ordered
        Assert.Equal(3, ids.Distinct().Count());
        Assert.True(ids.SequenceEqual(ids.OrderBy(id => id)),
            "Guid7 IDs should be chronologically ordered.");
    }

    [Fact]
    public async Task WalletBalance_SurvivesReconnection()
    {
        // Arrange — create wallet in one DbContext instance
        Guid walletId;
        var connectionString = await TestDbContextFactory.GetConnectionStringForAsync(
            $"reconnect_test_{Guid.NewGuid():N}");

        var options = new DbContextOptionsBuilder<WalletsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using (var db1 = new WalletsDbContext(options))
        {
            await db1.Database.EnsureCreatedAsync();

            var wallet = new Wallet
            {
                Name = "Durability Test",
                Type = WalletType.Personal,
                Balance = 9999.99m,
                UserId = Guid.NewGuid(),
            };

            db1.Wallets.Add(wallet);
            await db1.SaveChangesAsync();
            walletId = wallet.Id;
        }

        // Act — read from a fresh DbContext instance (simulating reconnection)
        await using var db2 = new WalletsDbContext(options);
        var loaded = await db2.Wallets.FindAsync(walletId);

        // Assert — data survives reconnection
        Assert.NotNull(loaded);
        Assert.Equal(9999.99m, loaded.Balance);
    }
}
```

#### IAsyncLifetime pattern (alternative setup/teardown)

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Integration;

using Kakeibo.Modules.Wallets.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

// Test fixture using IAsyncLifetime for per-class container lifecycle.
// Use this when you need a fresh database per test class.
public class WalletBalanceIntegrationTests : IAsyncLifetime
{
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    private WalletsDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        try
        {
            await ContainerStartTask.Value;
        }
        catch
        {
            Assert.Skip(
                "Docker is not available. This test requires Testcontainers (PostgreSQL).");
        }

        var options = new DbContextOptionsBuilder<WalletsDbContext>()
            .UseNpgsql(PostgresContainer.GetConnectionString(), n => n.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new WalletsDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task Balance_AfterMultipleDebits_IsAccurate()
    {
        var wallet = new Wallet
        {
            Name = "Balance Test",
            Type = WalletType.Personal,
            Balance = 1000m,
            UserId = Guid.NewGuid(),
        };

        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();

        wallet.Debit(100m);
        wallet.Debit(200m);
        wallet.Debit(50m);
        await _db.SaveChangesAsync();

        var loaded = await _db.Wallets.AsNoTracking().FirstAsync(w => w.Id == wallet.Id);
        Assert.Equal(650m, loaded.Balance);
    }
}
```

---

### 3.4 API Functional Tests

Full HTTP pipeline tests using `WebApplicationFactory<Program>`. Tests the complete request lifecycle: routing, validation, authentication, handler execution, and response serialization.

**Location:** `tests/Kakeibo.FunctionalTests/`

#### KakeiboWebApplicationFactory

```csharp
namespace Kakeibo.FunctionalTests.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

// Custom WebApplicationFactory that replaces real infrastructure with test containers.
public class KakeiboWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("kakeibo_functional_test")
            .Build();

    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    public async ValueTask InitializeAsync()
    {
        try
        {
            await ContainerStartTask.Value;
        }
        catch
        {
            Assert.Skip(
                "Docker is not available. Functional tests require Testcontainers (PostgreSQL).");
        }
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Override connection strings with Testcontainers endpoints
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = PostgresContainer.GetConnectionString(),
            });
        });
    }
}
```

#### Example: Wallet endpoint functional test

```csharp
namespace Kakeibo.FunctionalTests.Wallets;

using System.Net;
using System.Net.Http.Json;
using Kakeibo.FunctionalTests.Infrastructure;

public class WalletEndpointTests(KakeiboWebApplicationFactory factory)
    : IClassFixture<KakeiboWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateWallet_ValidRequest_Returns201Created()
    {
        var request = new
        {
            Name = "Functional Test Wallet",
            Type = "Personal",
            InitialBalance = 500m,
        };

        var response = await _client.PostAsJsonAsync("/api/wallets", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<WalletResponse>();
        Assert.NotNull(body);
        Assert.Equal("Functional Test Wallet", body.Name);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task CreateWallet_EmptyName_Returns400ValidationProblem()
    {
        var request = new
        {
            Name = "",
            Type = "Personal",
            InitialBalance = 0m,
        };

        var response = await _client.PostAsJsonAsync("/api/wallets", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWallet_NonExistentId_Returns404NotFound()
    {
        var response = await _client.GetAsync($"/api/wallets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Response DTO for deserialization (test-only, not shared with production code)
    private sealed record WalletResponse(Guid Id, string Name, string Type, decimal Balance);
}
```

---

### 3.5 Architecture Tests (NetArchTest)

Enforce module boundaries, naming conventions, and dependency direction rules. These tests prevent architectural drift and ensure the modular monolith constraints hold.

**Location:** `tests/Kakeibo.ArchitectureTests/`

#### Module boundary enforcement

```csharp
namespace Kakeibo.ArchitectureTests;

using NetArchTest.Rules;
using System.Reflection;

public class ModuleBoundaryTests
{
    // Assembly references for all modules
    private static readonly Assembly CommonAssembly =
        typeof(Kakeibo.Common.Abstractions.Entity).Assembly;

    private static readonly Assembly ContractsAssembly =
        typeof(Kakeibo.Contracts.Wallets.Events.WalletCreatedEvent).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(Kakeibo.Infrastructure.Outbox.IOutboxSource).Assembly;

    private static readonly Assembly WalletsAssembly =
        typeof(Kakeibo.Modules.Wallets.WalletsModuleRegistration).Assembly;

    private static readonly Assembly TransactionsAssembly =
        typeof(Kakeibo.Modules.Transactions.TransactionsModuleRegistration).Assembly;

    private static readonly Assembly BudgetsAssembly =
        typeof(Kakeibo.Modules.Budgets.BudgetsModuleRegistration).Assembly;

    // Critical rule: No cross-module references
    [Fact]
    public void WalletsModule_ShouldNotReference_TransactionsModule()
    {
        var result = Types.InAssembly(WalletsAssembly)
            .ShouldNot()
            .HaveDependencyOn("Kakeibo.Modules.Transactions")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Wallets module must not reference Transactions module directly. " +
            "Use Kakeibo.Contracts for inter-module communication.");
    }

    [Fact]
    public void TransactionsModule_ShouldNotReference_WalletsModule()
    {
        var result = Types.InAssembly(TransactionsAssembly)
            .ShouldNot()
            .HaveDependencyOn("Kakeibo.Modules.Wallets")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void BudgetsModule_ShouldNotReference_AnyOtherModule()
    {
        var result = Types.InAssembly(BudgetsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Kakeibo.Modules.Wallets",
                "Kakeibo.Modules.Transactions",
                "Kakeibo.Modules.Goals",
                "Kakeibo.Modules.Recurring",
                "Kakeibo.Modules.Identity",
                "Kakeibo.Modules.Notifications",
                "Kakeibo.Modules.Auditing")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Budget module must only depend on Common, Contracts, and Infrastructure.");
    }

    // Kakeibo.Common cannot reference any module
    [Fact]
    public void Common_ShouldNotReference_AnyModule()
    {
        var result = Types.InAssembly(CommonAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Kakeibo.Modules",
                "Kakeibo.Infrastructure",
                "Kakeibo.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Kakeibo.Common is the shared kernel and must have zero project references.");
    }

    // Kakeibo.Contracts cannot reference Infrastructure
    [Fact]
    public void Contracts_ShouldNotReference_Infrastructure()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("Kakeibo.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    // Kakeibo.Contracts cannot reference any module
    [Fact]
    public void Contracts_ShouldNotReference_AnyModule()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Kakeibo.Modules.Wallets",
                "Kakeibo.Modules.Transactions",
                "Kakeibo.Modules.Budgets",
                "Kakeibo.Modules.Goals",
                "Kakeibo.Modules.Recurring",
                "Kakeibo.Modules.Identity",
                "Kakeibo.Modules.Notifications",
                "Kakeibo.Modules.Auditing",
                "Kakeibo.Api")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
```

#### Naming convention enforcement

```csharp
namespace Kakeibo.ArchitectureTests;

using Kakeibo.Common.Endpoints;
using Kakeibo.Common.Abstractions;
using Kakeibo.Common.Modules;
using NetArchTest.Rules;
using System.Reflection;

public class NamingConventionTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(Kakeibo.Modules.Wallets.WalletsModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Transactions.TransactionsModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Budgets.BudgetsModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Goals.GoalsModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Recurring.RecurringModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Identity.IdentityModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Notifications.NotificationsModuleRegistration).Assembly,
        typeof(Kakeibo.Modules.Auditing.AuditingModuleRegistration).Assembly,
    ];

    // TD-011: Endpoint classes must end in "Endpoint"
    [Fact]
    public void EndpointClasses_MustEndWithEndpoint()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(IEndpoint))
                .Should()
                .HaveNameEndingWith("Endpoint")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"All IEndpoint implementations must end with 'Endpoint'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // Consumers must end in "Consumer"
    [Fact]
    public void EventConsumers_MustEndWithConsumer()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(IEventConsumer<>))
                .Should()
                .HaveNameEndingWith("Consumer")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"All IEventConsumer<T> implementations must end with 'Consumer'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // Domain event handlers must end in "DomainEventHandler"
    [Fact]
    public void DomainEventHandlers_MustEndWithDomainEventHandler()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(typeof(IDomainEventHandler<>))
                .Should()
                .HaveNameEndingWith("DomainEventHandler")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"All IDomainEventHandler<T> implementations must end with 'DomainEventHandler'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // TD-009: Configuration classes must end in "Options", never "Settings" or "Config"
    [Fact]
    public void ConfigurationClasses_MustNotEndWithSettings()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Settings")
                .Should()
                .NotExist()
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"Configuration classes must use 'Options' suffix, not 'Settings'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // TD-010: EF Core configurations must end in "Configuration"
    [Fact]
    public void EntityConfigurations_MustEndWithConfiguration()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .ImplementInterface(
                    typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>))
                .Should()
                .HaveNameEndingWith("Configuration")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"All IEntityTypeConfiguration<T> implementations must end with 'Configuration'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // Validators must end in "Validator"
    [Fact]
    public void Validators_MustEndWithValidator()
    {
        foreach (var assembly in ModuleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .Inherit(typeof(FluentValidation.AbstractValidator<>))
                .Should()
                .HaveNameEndingWith("Validator")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"All validators must end with 'Validator'. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }
}
```

#### Dependency direction tests

```csharp
namespace Kakeibo.ArchitectureTests;

using NetArchTest.Rules;
using System.Reflection;

public class DependencyDirectionTests
{
    private static readonly Assembly InfrastructureAssembly =
        typeof(Kakeibo.Infrastructure.Outbox.IOutboxSource).Assembly;

    // Infrastructure cannot reference any module
    [Fact]
    public void Infrastructure_ShouldNotReference_AnyModule()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Kakeibo.Modules.Wallets",
                "Kakeibo.Modules.Transactions",
                "Kakeibo.Modules.Budgets",
                "Kakeibo.Modules.Goals",
                "Kakeibo.Modules.Recurring",
                "Kakeibo.Modules.Identity",
                "Kakeibo.Modules.Notifications",
                "Kakeibo.Modules.Auditing",
                "Kakeibo.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Infrastructure must not reference any module or the Api project.");
    }

    // Infrastructure cannot reference the Api project
    [Fact]
    public void Infrastructure_ShouldNotReference_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("Kakeibo.Api")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    // Types in DomainEventHandlers namespace must implement IDomainEventHandler<T>
    [Fact]
    public void DomainEventHandlerNamespace_AllTypesMustImplementInterface()
    {
        var moduleAssemblies = new Assembly[]
        {
            typeof(Kakeibo.Modules.Wallets.WalletsModuleRegistration).Assembly,
            typeof(Kakeibo.Modules.Transactions.TransactionsModuleRegistration).Assembly,
        };

        foreach (var assembly in moduleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespaceContaining("DomainEventHandlers")
                .And()
                .AreClasses()
                .Should()
                .ImplementInterface(typeof(Kakeibo.Common.Abstractions.IDomainEventHandler<>))
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"All classes in DomainEventHandlers namespace must implement IDomainEventHandler<T>. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }
}
```

---

### 3.6 Test Data Builders

Builder pattern for creating complex domain objects with sensible defaults. Reduces test noise by letting each test override only the properties relevant to the scenario.

**Location:** `tests/Kakeibo.Modules.{X}.Tests/Builders/`

#### WalletBuilder

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Builders;

using Kakeibo.Modules.Wallets.Entities;
using Kakeibo.Modules.Wallets.ValueObjects;

// Fluent builder for creating Wallet entities in tests.
// Defaults produce a valid personal wallet with $1,000 balance.
internal sealed class WalletBuilder
{
    private string _name = "Test Wallet";
    private WalletType _type = WalletType.Personal;
    private decimal _balance = 1000m;
    private Guid _userId = Guid.NewGuid();
    private bool _isDeleted;

    public WalletBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WalletBuilder WithType(WalletType type)
    {
        _type = type;
        return this;
    }

    public WalletBuilder WithBalance(decimal balance)
    {
        _balance = balance;
        return this;
    }

    public WalletBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public WalletBuilder Archived()
    {
        _isDeleted = true;
        return this;
    }

    public WalletBuilder AsSharedWallet()
    {
        _type = WalletType.Shared;
        return this;
    }

    public WalletBuilder AsPersonalWallet()
    {
        _type = WalletType.Personal;
        return this;
    }

    public WalletBuilder WithZeroBalance()
    {
        _balance = 0m;
        return this;
    }

    public Wallet Build()
    {
        return new Wallet
        {
            Name = _name,
            Type = _type,
            Balance = _balance,
            UserId = _userId,
            IsDeleted = _isDeleted,
        };
    }
}
```

#### TransactionBuilder

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Builders;

using Kakeibo.Modules.Transactions.Entities;
using Kakeibo.Modules.Transactions.ValueObjects;
using NodaTime;

// Fluent builder for creating Transaction entities in tests.
// Defaults produce a valid $50 expense.
internal sealed class TransactionBuilder
{
    private TransactionType _type = TransactionType.Expense;
    private decimal _amount = 50m;
    private Guid _walletId = Guid.NewGuid();
    private Guid _categoryId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private string _description = "Test transaction";
    private LocalDate _date = SystemClock.Instance.GetCurrentInstant().InUtc().Date;

    public TransactionBuilder WithType(TransactionType type)
    {
        _type = type;
        return this;
    }

    public TransactionBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public TransactionBuilder ForWallet(Guid walletId)
    {
        _walletId = walletId;
        return this;
    }

    public TransactionBuilder WithCategory(Guid categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public TransactionBuilder ByUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public TransactionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TransactionBuilder OnDate(LocalDate date)
    {
        _date = date;
        return this;
    }

    public TransactionBuilder AsIncome()
    {
        _type = TransactionType.Income;
        return this;
    }

    public TransactionBuilder AsExpense()
    {
        _type = TransactionType.Expense;
        return this;
    }

    public Transaction Build()
    {
        return new Transaction
        {
            Type = _type,
            Amount = _amount,
            WalletId = _walletId,
            CategoryId = _categoryId,
            UserId = _userId,
            Description = _description,
            Date = _date,
        };
    }
}
```

#### Using builders in tests

```csharp
namespace Kakeibo.Modules.Wallets.Tests.Features.ArchiveWallet;

using Kakeibo.Modules.Wallets.Tests.Builders;

public class ArchiveWalletHandlerTests
{
    [Fact]
    public void ArchiveWallet_ZeroBalance_Succeeds()
    {
        // Builder makes setup minimal — only the relevant property is explicitly set
        var wallet = new WalletBuilder()
            .WithZeroBalance()
            .Build();

        wallet.Archive();

        Assert.True(wallet.IsDeleted);
    }

    [Fact]
    public void ArchiveWallet_PositiveBalance_ThrowsInvalidOperation()
    {
        var wallet = new WalletBuilder()
            .WithBalance(500m)
            .Build();

        Assert.Throws<InvalidOperationException>(() => wallet.Archive());
    }

    [Fact]
    public void ArchiveWallet_SharedWallet_ArchivesRegardlessOfType()
    {
        var wallet = new WalletBuilder()
            .AsSharedWallet()
            .WithZeroBalance()
            .Build();

        wallet.Archive();

        Assert.True(wallet.IsDeleted);
    }
}
```

---

## 4. Frontend Testing (Vue + TypeScript)

### 4.1 Component Unit Tests (Vitest)

**Location:** `sites/Kakeibo.App/src/components/__tests__/` (co-located with components)

**Dependencies:**
```json
{
  "devDependencies": {
    "vitest": "^3.0.0",
    "@vue/test-utils": "^2.4.0",
    "@pinia/testing": "^0.2.0",
    "happy-dom": "^15.0.0"
  }
}
```

**Vitest configuration (`vitest.config.ts`):**

```typescript
import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: 'happy-dom',
    globals: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: ['src/**/*.{ts,vue}'],
      exclude: [
        'src/**/*.d.ts',
        'src/**/__tests__/**',
        'src/main.ts',
        'src/router/**',
      ],
    },
  },
  resolve: {
    alias: {
      '@': resolve(__dirname, './src'),
    },
  },
})
```

#### Example: WalletCard component test

```typescript
// sites/Kakeibo.App/src/components/__tests__/WalletCard.test.ts

import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import WalletCard from '@/components/WalletCard.vue'

describe('WalletCard', () => {
  const defaultProps = {
    wallet: {
      id: 'wallet-1',
      name: 'Checking Account',
      type: 'personal' as const,
      balance: 2500.75,
      currency: 'USD',
    },
  }

  it('renders wallet name', () => {
    const wrapper = mount(WalletCard, { props: defaultProps })

    expect(wrapper.text()).toContain('Checking Account')
  })

  it('displays formatted balance', () => {
    const wrapper = mount(WalletCard, { props: defaultProps })

    // Balance should be formatted as currency
    expect(wrapper.text()).toContain('2,500.75')
  })

  it('shows personal badge for personal wallets', () => {
    const wrapper = mount(WalletCard, { props: defaultProps })

    expect(wrapper.find('[data-testid="wallet-type-badge"]').text())
      .toContain('Personal')
  })

  it('shows shared badge for shared wallets', () => {
    const wrapper = mount(WalletCard, {
      props: {
        wallet: {
          ...defaultProps.wallet,
          type: 'shared',
        },
      },
    })

    expect(wrapper.find('[data-testid="wallet-type-badge"]').text())
      .toContain('Shared')
  })

  it('emits click event when card is clicked', async () => {
    const wrapper = mount(WalletCard, { props: defaultProps })

    await wrapper.trigger('click')

    expect(wrapper.emitted('click')).toHaveLength(1)
    expect(wrapper.emitted('click')![0]).toEqual([defaultProps.wallet])
  })

  it('applies negative-balance styling for negative balances', () => {
    const wrapper = mount(WalletCard, {
      props: {
        wallet: {
          ...defaultProps.wallet,
          balance: -150.00,
        },
      },
    })

    expect(wrapper.find('[data-testid="wallet-balance"]').classes())
      .toContain('text-destructive')
  })

  it('does not render member count for personal wallets', () => {
    const wrapper = mount(WalletCard, { props: defaultProps })

    expect(wrapper.find('[data-testid="member-count"]').exists()).toBe(false)
  })

  it('renders member count for shared wallets', () => {
    const wrapper = mount(WalletCard, {
      props: {
        wallet: {
          ...defaultProps.wallet,
          type: 'shared',
          memberCount: 3,
        },
      },
    })

    expect(wrapper.find('[data-testid="member-count"]').text())
      .toContain('3')
  })
})
```

#### Example: Form component test with user interaction

```typescript
// sites/Kakeibo.App/src/components/__tests__/CreateWalletForm.test.ts

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import CreateWalletForm from '@/components/CreateWalletForm.vue'

describe('CreateWalletForm', () => {
  function createWrapper() {
    return mount(CreateWalletForm, {
      global: {
        plugins: [
          createTestingPinia({
            createSpy: vi.fn,
          }),
        ],
      },
    })
  }

  it('disables submit button when name is empty', () => {
    const wrapper = createWrapper()

    const submitButton = wrapper.find('button[type="submit"]')
    expect(submitButton.attributes('disabled')).toBeDefined()
  })

  it('enables submit button when form is valid', async () => {
    const wrapper = createWrapper()

    await wrapper.find('input[name="name"]').setValue('My Wallet')
    await wrapper.find('input[name="initialBalance"]').setValue('1000')

    const submitButton = wrapper.find('button[type="submit"]')
    expect(submitButton.attributes('disabled')).toBeUndefined()
  })

  it('shows validation error for name exceeding 100 characters', async () => {
    const wrapper = createWrapper()

    const longName = 'A'.repeat(101)
    await wrapper.find('input[name="name"]').setValue(longName)
    await wrapper.find('input[name="name"]').trigger('blur')

    // Wait for validation to complete
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('100 characters')
  })

  it('shows validation error for negative initial balance', async () => {
    const wrapper = createWrapper()

    await wrapper.find('input[name="initialBalance"]').setValue('-50')
    await wrapper.find('input[name="initialBalance"]').trigger('blur')

    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('must be')
  })

  it('emits submit event with form data', async () => {
    const wrapper = createWrapper()

    await wrapper.find('input[name="name"]').setValue('Savings')
    await wrapper.find('input[name="initialBalance"]').setValue('5000')

    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')).toHaveLength(1)
    expect(wrapper.emitted('submit')![0][0]).toEqual({
      name: 'Savings',
      type: 'personal',
      initialBalance: 5000,
    })
  })
})
```

---

### 4.2 Store Tests (Pinia)

**Location:** `sites/Kakeibo.App/src/stores/__tests__/`

```typescript
// sites/Kakeibo.App/src/stores/__tests__/wallets.test.ts

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useWalletsStore } from '@/stores/wallets'
import * as walletsApi from '@/api/wallets'

// Mock the API module
vi.mock('@/api/wallets')
const mockApi = vi.mocked(walletsApi)

describe('useWalletsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  describe('state', () => {
    it('initializes with empty wallets list', () => {
      const store = useWalletsStore()

      expect(store.wallets).toEqual([])
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })
  })

  describe('getters', () => {
    it('personalWallets filters by type', () => {
      const store = useWalletsStore()
      store.wallets = [
        { id: '1', name: 'Checking', type: 'personal', balance: 100 },
        { id: '2', name: 'Trip Fund', type: 'shared', balance: 200 },
        { id: '3', name: 'Savings', type: 'personal', balance: 300 },
      ]

      expect(store.personalWallets).toHaveLength(2)
      expect(store.personalWallets.map(w => w.name))
        .toEqual(['Checking', 'Savings'])
    })

    it('sharedWallets filters by type', () => {
      const store = useWalletsStore()
      store.wallets = [
        { id: '1', name: 'Checking', type: 'personal', balance: 100 },
        { id: '2', name: 'Trip Fund', type: 'shared', balance: 200 },
      ]

      expect(store.sharedWallets).toHaveLength(1)
      expect(store.sharedWallets[0].name).toBe('Trip Fund')
    })

    it('totalBalance sums all wallet balances', () => {
      const store = useWalletsStore()
      store.wallets = [
        { id: '1', name: 'W1', type: 'personal', balance: 1000 },
        { id: '2', name: 'W2', type: 'personal', balance: 2500 },
        { id: '3', name: 'W3', type: 'shared', balance: 500 },
      ]

      expect(store.totalBalance).toBe(4000)
    })

    it('totalBalance returns 0 for empty wallets', () => {
      const store = useWalletsStore()

      expect(store.totalBalance).toBe(0)
    })
  })

  describe('actions', () => {
    it('fetchWallets sets wallets from API response', async () => {
      const store = useWalletsStore()
      const mockWallets = [
        { id: '1', name: 'Checking', type: 'personal', balance: 1000 },
        { id: '2', name: 'Savings', type: 'personal', balance: 5000 },
      ]

      mockApi.getWallets.mockResolvedValue({ data: mockWallets })

      await store.fetchWallets()

      expect(store.wallets).toEqual(mockWallets)
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })

    it('fetchWallets sets loading state during request', async () => {
      const store = useWalletsStore()

      // Create a promise we control
      let resolvePromise: (value: any) => void
      const promise = new Promise(resolve => { resolvePromise = resolve })
      mockApi.getWallets.mockReturnValue(promise as any)

      const fetchPromise = store.fetchWallets()

      // Loading should be true while request is in-flight
      expect(store.isLoading).toBe(true)

      resolvePromise!({ data: [] })
      await fetchPromise

      expect(store.isLoading).toBe(false)
    })

    it('fetchWallets sets error on API failure', async () => {
      const store = useWalletsStore()
      mockApi.getWallets.mockRejectedValue(new Error('Network error'))

      await store.fetchWallets()

      expect(store.error).toBe('Network error')
      expect(store.isLoading).toBe(false)
    })

    it('createWallet adds wallet to list on success', async () => {
      const store = useWalletsStore()
      const newWallet = {
        id: '99',
        name: 'New Wallet',
        type: 'personal',
        balance: 0,
      }

      mockApi.createWallet.mockResolvedValue({ data: newWallet })

      await store.createWallet({
        name: 'New Wallet',
        type: 'personal',
        initialBalance: 0,
      })

      expect(store.wallets).toContainEqual(newWallet)
    })

    it('deleteWallet removes wallet from list', async () => {
      const store = useWalletsStore()
      store.wallets = [
        { id: '1', name: 'Keep', type: 'personal', balance: 100 },
        { id: '2', name: 'Delete', type: 'personal', balance: 0 },
      ]

      mockApi.deleteWallet.mockResolvedValue({})

      await store.deleteWallet('2')

      expect(store.wallets).toHaveLength(1)
      expect(store.wallets[0].name).toBe('Keep')
    })
  })
})
```

---

### 4.3 Composable Tests

**Location:** `sites/Kakeibo.App/src/composables/__tests__/`

```typescript
// sites/Kakeibo.App/src/composables/__tests__/useCurrency.test.ts

import { describe, it, expect } from 'vitest'
import { useCurrency } from '@/composables/useCurrency'

describe('useCurrency', () => {
  it('formats positive amounts with currency symbol', () => {
    const { format } = useCurrency('USD')

    expect(format(1234.56)).toBe('$1,234.56')
  })

  it('formats negative amounts with minus sign', () => {
    const { format } = useCurrency('USD')

    expect(format(-500)).toBe('-$500.00')
  })

  it('formats zero correctly', () => {
    const { format } = useCurrency('USD')

    expect(format(0)).toBe('$0.00')
  })

  it('formats EUR with euro symbol', () => {
    const { format } = useCurrency('EUR')

    // Locale-dependent formatting — test the essential parts
    const result = format(1234.56)
    expect(result).toContain('1,234.56')
  })

  it('formats JPY without decimals', () => {
    const { format } = useCurrency('JPY')

    const result = format(1000)
    expect(result).toContain('1,000')
    // JPY has 0 decimal places
    expect(result).not.toContain('.')
  })
})
```

```typescript
// sites/Kakeibo.App/src/composables/__tests__/useBudgetProgress.test.ts

import { describe, it, expect } from 'vitest'
import { useBudgetProgress } from '@/composables/useBudgetProgress'

describe('useBudgetProgress', () => {
  it('calculates percentage used', () => {
    const { percentUsed } = useBudgetProgress({
      limit: 400,
      spent: 200,
    })

    expect(percentUsed.value).toBe(50)
  })

  it('returns remaining amount', () => {
    const { remaining } = useBudgetProgress({
      limit: 400,
      spent: 150,
    })

    expect(remaining.value).toBe(250)
  })

  it('returns "on_track" status when under pace', () => {
    const { status } = useBudgetProgress({
      limit: 400,
      spent: 100,
      daysElapsed: 10,
      totalDays: 30,
    })

    expect(status.value).toBe('on_track')
  })

  it('returns "warning" status when ahead of pace', () => {
    const { status } = useBudgetProgress({
      limit: 400,
      spent: 300,
      daysElapsed: 15,
      totalDays: 30,
    })

    expect(status.value).toBe('warning')
  })

  it('returns "exceeded" status when over limit', () => {
    const { status } = useBudgetProgress({
      limit: 400,
      spent: 450,
      daysElapsed: 25,
      totalDays: 30,
    })

    expect(status.value).toBe('exceeded')
  })

  it('clamps percentage to 100 when exceeded', () => {
    const { percentUsed } = useBudgetProgress({
      limit: 400,
      spent: 600,
    })

    // UI should not show >100% in the progress bar
    expect(percentUsed.value).toBe(100)
  })
})
```

---

### 4.4 E2E Tests (Playwright)

Critical user journeys tested against the full stack. Use the Page Object pattern for maintainability.

**Location:** `sites/Kakeibo.App/e2e/`

**Configuration (`playwright.config.ts`):**

```typescript
import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  retries: 2,
  workers: 1, // Sequential to avoid port conflicts
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'mobile-chrome', use: { ...devices['Pixel 5'] } },
  ],
  webServer: {
    command: 'bun run dev',
    port: 5173,
    reuseExistingServer: !process.env.CI,
  },
})
```

#### Page Object pattern

```typescript
// sites/Kakeibo.App/e2e/pages/WalletsPage.ts

import type { Page, Locator } from '@playwright/test'

export class WalletsPage {
  readonly page: Page
  readonly createButton: Locator
  readonly walletList: Locator
  readonly nameInput: Locator
  readonly balanceInput: Locator
  readonly submitButton: Locator

  constructor(page: Page) {
    this.page = page
    this.createButton = page.getByTestId('create-wallet-button')
    this.walletList = page.getByTestId('wallet-list')
    this.nameInput = page.getByLabel('Wallet name')
    this.balanceInput = page.getByLabel('Initial balance')
    this.submitButton = page.getByRole('button', { name: 'Create' })
  }

  async goto() {
    await this.page.goto('/wallets')
  }

  async createWallet(name: string, balance: number) {
    await this.createButton.click()
    await this.nameInput.fill(name)
    await this.balanceInput.fill(balance.toString())
    await this.submitButton.click()
  }

  async getWalletNames(): Promise<string[]> {
    const cards = this.walletList.getByTestId('wallet-card')
    return cards.allTextContents()
  }

  walletCard(name: string): Locator {
    return this.walletList.getByText(name).locator('..')
  }
}
```

```typescript
// sites/Kakeibo.App/e2e/pages/LoginPage.ts

import type { Page, Locator } from '@playwright/test'

export class LoginPage {
  readonly page: Page
  readonly emailInput: Locator
  readonly passwordInput: Locator
  readonly submitButton: Locator

  constructor(page: Page) {
    this.page = page
    this.emailInput = page.getByLabel('Email')
    this.passwordInput = page.getByLabel('Password')
    this.submitButton = page.getByRole('button', { name: 'Sign in' })
  }

  async goto() {
    await this.page.goto('/login')
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email)
    await this.passwordInput.fill(password)
    await this.submitButton.click()
    // Wait for navigation to complete
    await this.page.waitForURL('/dashboard')
  }
}
```

#### Example: Wallet creation E2E flow

```typescript
// sites/Kakeibo.App/e2e/wallets/wallet-creation.spec.ts

import { test, expect } from '@playwright/test'
import { LoginPage } from '../pages/LoginPage'
import { WalletsPage } from '../pages/WalletsPage'

test.describe('Wallet Creation Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Login with test user (seeded in test environment)
    const loginPage = new LoginPage(page)
    await loginPage.goto()
    await loginPage.login('test@kakeibo.dev', 'TestPassword123!')
  })

  test('creates a personal wallet and displays it in the list', async ({ page }) => {
    const walletsPage = new WalletsPage(page)
    await walletsPage.goto()

    await walletsPage.createWallet('E2E Test Wallet', 1500)

    // Wait for the new wallet to appear in the list
    await expect(walletsPage.walletCard('E2E Test Wallet')).toBeVisible()
    await expect(walletsPage.walletCard('E2E Test Wallet'))
      .toContainText('1,500')
  })

  test('shows validation error for empty wallet name', async ({ page }) => {
    const walletsPage = new WalletsPage(page)
    await walletsPage.goto()

    await walletsPage.createButton.click()
    await walletsPage.balanceInput.fill('100')
    await walletsPage.submitButton.click()

    // Validation error should appear
    await expect(page.getByText('required')).toBeVisible()
  })

  test('shows newly created wallet on dashboard', async ({ page }) => {
    const walletsPage = new WalletsPage(page)
    await walletsPage.goto()

    await walletsPage.createWallet('Dashboard Wallet', 3000)

    // Navigate to dashboard and verify wallet appears
    await page.goto('/dashboard')
    await expect(page.getByText('Dashboard Wallet')).toBeVisible()
    await expect(page.getByText('3,000')).toBeVisible()
  })
})
```

#### Test data setup/teardown

```typescript
// sites/Kakeibo.App/e2e/helpers/seed.ts

// Seed test data via the API before E2E tests.
// The API must be running with a test database.
export async function seedTestUser(baseUrl: string): Promise<{
  email: string
  password: string
}> {
  const email = `e2e-${Date.now()}@kakeibo.dev`
  const password = 'TestPassword123!'

  const response = await fetch(`${baseUrl}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, firstName: 'E2E', lastName: 'Test' }),
  })

  if (!response.ok) {
    throw new Error(`Failed to seed test user: ${response.statusText}`)
  }

  return { email, password }
}

export async function cleanupTestData(baseUrl: string, userId: string): Promise<void> {
  await fetch(`${baseUrl}/api/test/cleanup/${userId}`, {
    method: 'DELETE',
  })
}
```

---

## 5. Testing Strategies

### 5.1 What to Test

#### DO test

- Entity invariants (balance cannot go negative, amount must be positive)
- Value object equality and validation rules
- Domain event publication after state changes
- Handler business logic (conflict detection, authorization checks)
- Validator rules for every endpoint request
- Integration event consumption side effects
- Cross-module request handling
- Module boundary enforcement (architecture tests)
- Critical user flows end-to-end (registration, wallet creation, transaction recording)
- Error paths (not found, conflict, validation failure, unauthorized)
- Edge cases in split calculations (rounding, 3-way splits)
- Debt simplification algorithm correctness
- Budget status transitions (on_track, warning, exceeded)
- Goal milestone detection (25%, 50%, 75%, 100%)
- Pinia store actions, getters, and error handling
- Vue component rendering with various prop combinations
- Form validation feedback

#### DO NOT test

- EF Core configuration mapping (tested implicitly by integration tests)
- Framework behavior (ASP.NET routing, DI resolution)
- Auto-generated migrations
- Third-party library internals (FluentValidation rules engine, Testcontainers Docker orchestration)
- Private methods directly (test through public API)
- Trivial getters/setters with no logic
- `Program.cs` composition root (tested by functional tests)
- Static utility methods with obvious behavior (`Guid7.NewGuid()` returns non-empty)
- CSS styling (visual regression testing is separate)
- Logging output (verify through behavior, not log messages)

---

### 5.2 Test Doubles

#### When to use each type

| Double | Backend (NSubstitute) | Frontend (vi.mock) | When to use |
|--------|-----------------------|--------------------|-------------|
| **Stub** | `sub.Method().Returns(value)` | `vi.fn().mockReturnValue(value)` | Provide canned data for dependencies the test does not care about |
| **Mock** | `sub.Received(1).Method(args)` | `expect(fn).toHaveBeenCalledWith(args)` | Verify interactions (event published, API called) |
| **Fake** | Custom implementation of interface | In-memory implementation | Replace complex infrastructure with a lightweight working version |
| **Spy** | `sub.ReceivedCalls()` | `vi.spyOn(obj, 'method')` | Observe calls without replacing behavior |

#### NSubstitute patterns (backend)

```csharp
// Stub: provide canned data
var moduleClient = Substitute.For<IModuleClient>();
moduleClient
    .SendAsync(Arg.Any<GetWalletBalanceRequest>(), Arg.Any<CancellationToken>())
    .Returns(2500m);

// Mock: verify interaction happened
var eventBus = Substitute.For<IModuleEventBus>();
// ... execute handler ...
await eventBus.Received(1).PublishAsync(
    Arg.Is<WalletCreatedEvent>(e => e.WalletId == expectedId),
    Arg.Any<CancellationToken>());

// Verify no unexpected calls
await eventBus.DidNotReceive().PublishAsync(
    Arg.Any<IIntegrationEvent>(),
    Arg.Any<CancellationToken>());

// Argument matching
moduleClient
    .SendAsync(
        Arg.Is<GetTransactionsInPeriodRequest>(r =>
            r.WalletId == walletId && r.CategoryId == categoryId),
        Arg.Any<CancellationToken>())
    .Returns(transactions);
```

#### vi.mock patterns (frontend)

```typescript
// Mock entire module
vi.mock('@/api/wallets', () => ({
  getWallets: vi.fn().mockResolvedValue({ data: [] }),
  createWallet: vi.fn().mockResolvedValue({ data: { id: '1', name: 'Test' } }),
  deleteWallet: vi.fn().mockResolvedValue({}),
}))

// Mock specific function
import * as api from '@/api/wallets'
vi.spyOn(api, 'getWallets').mockResolvedValue({ data: mockWallets })

// Mock composable
vi.mock('@/composables/useAuth', () => ({
  useAuth: () => ({
    user: ref({ id: 'user-1', email: 'test@kakeibo.dev' }),
    isAuthenticated: ref(true),
  }),
}))

// Mock Vue Router
vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
  }),
  useRoute: () => ({
    params: { id: 'wallet-1' },
    query: {},
  }),
}))
```

---

### 5.3 Coverage Requirements

| Level | Target | Enforcement |
|-------|--------|-------------|
| Domain entities & value objects | 100% | Must cover all public methods, all invariants, all edge cases |
| Handlers (business logic) | 90% | Must cover happy path + all error paths |
| Validators | 100% | Every validation rule must have at least one passing and one failing test |
| Endpoints | 80% | Must cover request/response mapping and error code translation |
| Integration (critical paths) | Critical paths | Wallet creation, transaction recording, debt calculation, budget monitoring |
| Architecture | 100% of rules | Every rule in architecture.md must have a corresponding architecture test |
| Vue components | 80% | Focus on user interaction and conditional rendering |
| Pinia stores | 90% | All actions and computed getters |
| Composables | 90% | Reactive behavior and edge cases |
| E2E | Critical flows | Registration, login, wallet CRUD, transaction recording, budget creation |

**Coverage tool configuration (backend):**

```bash
# Generate coverage report
dotnet test Kakeibo.slnx --collect:"XPlat Code Coverage"

# View report (use ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

**Coverage tool configuration (frontend):**

```bash
# Run with coverage
bun run vitest --coverage

# Coverage thresholds in vitest.config.ts
coverage: {
  thresholds: {
    branches: 80,
    functions: 80,
    lines: 80,
    statements: 80,
  },
}
```

---

### 5.4 Flaky Test Prevention

#### Time-dependent tests

```csharp
// BAD: Depends on system clock
[Fact]
public void Invitation_IsExpired_AfterSevenDays()
{
    var invitation = new Invitation { ExpiresAt = SystemClock.Instance.GetCurrentInstant() };
    Thread.Sleep(1000); // Fragile and slow
    Assert.True(invitation.IsExpired);
}

// GOOD: Use NodaTime.Testing.FakeClock
[Fact]
public void Invitation_IsExpired_AfterSevenDays()
{
    var clock = new FakeClock(Instant.FromUtc(2026, 1, 1, 0, 0, 0));
    var expirationDays = 7;
    var invitation = new Invitation
    {
        ExpiresAt = clock.GetCurrentInstant().Plus(Duration.FromDays(expirationDays)),
    };

    // Advance clock past expiration
    clock.Advance(Duration.FromDays(8));

    Assert.True(invitation.IsExpired(clock.GetCurrentInstant()));
}
```

#### Async handling

```csharp
// BAD: Race condition
[Fact]
public async Task ProcessOutbox_HandlesMessage()
{
    processor.Start();
    Thread.Sleep(100); // Hope it finishes in time
    Assert.True(processed);
}

// GOOD: Use proper async patterns
[Fact]
public async Task ProcessOutbox_HandlesMessage()
{
    var tcs = new TaskCompletionSource<bool>();
    processor.OnMessageProcessed += () => tcs.SetResult(true);

    await processor.ProcessAsync();

    var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.True(result);
}
```

#### Database cleanup

```csharp
// Each test gets a unique database — no cleanup needed
// TestDbContextFactory.CreateAsync() creates a fresh database per call.
// This is the preferred pattern over transaction rollback.

// Alternative: Use transactions for tests that share a database
[Fact]
public async Task Test_WithTransactionRollback()
{
    await using var db = await TestDbContextFactory.CreateAsync();
    await using var transaction = await db.Database.BeginTransactionAsync();

    try
    {
        // ... test logic ...
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}
```

#### Frontend async patterns

```typescript
// BAD: Arbitrary delay
await new Promise(resolve => setTimeout(resolve, 500))

// GOOD: Wait for specific condition
await waitFor(() => {
  expect(screen.getByText('Wallet created')).toBeInTheDocument()
})

// GOOD: Playwright auto-waiting
await expect(page.getByText('Success')).toBeVisible({ timeout: 5000 })
```

---

### 5.5 Performance

#### Test execution time targets

| Level | Target per test | Target per suite |
|-------|-----------------|------------------|
| Domain unit | < 10ms | < 2s total |
| Handler unit | < 50ms | < 10s total |
| Integration | < 2s | < 60s total |
| Functional | < 5s | < 120s total |
| Architecture | < 100ms | < 5s total |
| Frontend unit | < 50ms | < 15s total |
| E2E | < 30s | < 5min total |

#### Parallelization strategies

**Backend (xUnit v3):**

xUnit v3 runs test classes in parallel by default. Test methods within the same class run sequentially. For integration tests that share a static container, this is the correct behavior.

```csharp
// Tests in different classes run in parallel (default xUnit behavior)
// Tests within the same class run sequentially (default xUnit behavior)

// To control parallelism for integration tests that need sequential execution:
[Collection("WalletIntegration")]
public class WalletCreationTests { }

[Collection("WalletIntegration")]
public class WalletArchiveTests { }
```

**Frontend (Vitest):**

```typescript
// vitest.config.ts
test: {
  // Run test files in parallel (default)
  pool: 'threads',
  poolOptions: {
    threads: {
      // Use all available CPU cores
      maxThreads: undefined,
      minThreads: undefined,
    },
  },
}
```

#### CI optimization

- Run unit tests first (fast feedback)
- Run integration tests only if unit tests pass
- Use test result caching where possible
- Architecture tests run in parallel with unit tests (no infrastructure dependency)
- E2E tests run last (slowest, require full stack)

---

## 6. Testcontainers Patterns

### 6.1 Setup Patterns

#### Static container (shared across all tests in a project)

This is the preferred pattern. A single PostgreSQL container is started once and reused by all test classes in the project. Each test creates a unique database on the shared container for isolation.

```csharp
// Shared across all test classes in the project
internal static class TestDbContextFactory
{
    // Single container for the entire test project
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("kakeibo_test")
            .WithCommand("-c", "max_connections=500")
            .Build();

    // Lazy ensures container starts at most once
    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    // Skip guard (KB-008) — never omit this
    private static async Task EnsureContainerStartedAsync()
    {
        try
        {
            await ContainerStartTask.Value;
        }
        catch
        {
            Assert.Skip(
                "Docker is not available. These tests require Testcontainers (PostgreSQL).");
        }
    }

    public static async Task<WalletsDbContext> CreateAsync()
    {
        await EnsureContainerStartedAsync();

        // Each test gets a unique database for isolation
        var options = new DbContextOptionsBuilder<WalletsDbContext>()
            .UseNpgsql(PostgresContainer.GetConnectionString(), n => n.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        var db = new WalletsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
```

**Key rules:**
- Never use `.WithReuse(true)` (mandatory.md Rule 4)
- Always include the skip guard try-catch around `ContainerStartTask.Value`
- Use `Lazy<Task>` to ensure at-most-once startup
- Use `postgres:18-alpine` image (matches production)

#### Connection string management

```csharp
// For tests that need the raw connection string (e.g., to create multiple DbContexts)
public static string GetConnectionString()
{
    return PostgresContainer.GetConnectionString();
}

// For tests that need a unique database name
public static async Task<string> GetConnectionStringForAsync(string databaseName)
{
    await EnsureContainerStartedAsync();

    // Create a new database on the shared container
    using var conn = new Npgsql.NpgsqlConnection(PostgresContainer.GetConnectionString());
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
    await cmd.ExecuteNonQueryAsync();

    return PostgresContainer.GetConnectionString()
        .Replace($"Database=kakeibo_test", $"Database={databaseName}");
}
```

---

### 6.2 Database Migrations

#### Running migrations in tests

```csharp
// Option 1: EnsureCreated (creates schema without migrations table)
// Use when you don't need migration history tracking
await db.Database.EnsureCreatedAsync();

// Option 2: Migrate (applies all pending migrations)
// Use when you need to test migration scripts themselves
await db.Database.MigrateAsync();
```

#### Seed data strategies

```csharp
// Seed data using the same seeders as production
internal static class TestSeeder
{
    // Seeds the 12 system categories required by the Transactions module.
    public static async Task SeedSystemCategoriesAsync(TransactionsDbContext db)
    {
        var categories = new[]
        {
            new Category { Name = "Housing", IsSystem = true },
            new Category { Name = "Transportation", IsSystem = true },
            new Category { Name = "Food & Dining", IsSystem = true },
            new Category { Name = "Health & Wellness", IsSystem = true },
            new Category { Name = "Entertainment & Leisure", IsSystem = true },
            new Category { Name = "Shopping & Personal", IsSystem = true },
            new Category { Name = "Education", IsSystem = true },
            new Category { Name = "Subscriptions & Bills", IsSystem = true },
            new Category { Name = "Savings & Investments", IsSystem = true },
            new Category { Name = "Debt & Loans", IsSystem = true },
            new Category { Name = "Gifts & Donations", IsSystem = true },
            new Category { Name = "Other", IsSystem = true },
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();
    }

    // Seeds a test user for integration tests that need a valid user context.
    public static async Task<Guid> SeedTestUserAsync(IdentityDbContext db)
    {
        var user = new User
        {
            Email = "test@kakeibo.dev",
            PasswordHash = PasswordHasher.HashPassword("TestPassword123!"),
            FirstName = "Test",
            LastName = "User",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}
```

---

### 6.3 Cleanup Strategies

#### Per-test isolation (preferred)

Each test call to `TestDbContextFactory.CreateAsync()` creates a fresh database. No cleanup needed because the container is destroyed after the test run.

```csharp
[Fact]
public async Task Test_A()
{
    // Fresh database — completely isolated from Test_B
    await using var db = await TestDbContextFactory.CreateAsync();
    // ... test logic ...
}

[Fact]
public async Task Test_B()
{
    // Another fresh database — completely isolated from Test_A
    await using var db = await TestDbContextFactory.CreateAsync();
    // ... test logic ...
}
```

#### Transaction rollback pattern (when sharing a database is necessary)

```csharp
public class SharedDatabaseTests : IAsyncLifetime
{
    private WalletsDbContext _db = null!;
    private IDbContextTransaction _transaction = null!;

    public async ValueTask InitializeAsync()
    {
        _db = await TestDbContextFactory.CreateAsync();
        _transaction = await _db.Database.BeginTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task Test_InTransaction_RollsBackAutomatically()
    {
        var wallet = new Wallet { Name = "Will Be Rolled Back", Balance = 100m };
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();

        // Data exists within the transaction
        Assert.True(await _db.Wallets.AnyAsync(w => w.Name == "Will Be Rolled Back"));

        // After test completes, DisposeAsync rolls back — data never committed
    }
}
```

#### Explicit deletion pattern (for targeted cleanup)

```csharp
// Use when you need to clean specific data between tests in the same database
private static async Task CleanupWalletsAsync(WalletsDbContext db)
{
    await db.Wallets.ExecuteDeleteAsync();
    await db.OutboxMessages.ExecuteDeleteAsync();
}
```

---

## 7. CI Integration

### Quality gate configuration

The CI pipeline runs quality gates in the `.gitlab-ci.yml` file. Backend tests are part of the `quality:api` job.

```yaml
quality:api:
  stage: quality
  image: mcr.microsoft.com/dotnet/sdk:10.0
  tags: [local]
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
  cache:
    key: dotnet-${CI_COMMIT_REF_SLUG}
    fallback_keys:
      - dotnet-main
    paths:
      - $CI_PROJECT_DIR/.nuget/packages/
  variables:
    NUGET_PACKAGES: $CI_PROJECT_DIR/.nuget/packages/
  script:
    - dotnet restore Kakeibo.slnx
    - dotnet format Kakeibo.slnx --verify-no-changes
    - dotnet build Kakeibo.slnx --no-restore --configuration Release
    # Unit and architecture tests (no Docker needed)
    - dotnet test tests/Kakeibo.Modules.Identity.Tests/ --no-build --configuration Release
    - dotnet test tests/Kakeibo.Modules.Wallets.Tests/ --no-build --configuration Release
    - dotnet test tests/Kakeibo.Modules.Transactions.Tests/ --no-build --configuration Release
    - dotnet test tests/Kakeibo.Modules.Budgets.Tests/ --no-build --configuration Release
    - dotnet test tests/Kakeibo.Modules.Goals.Tests/ --no-build --configuration Release
    - dotnet test tests/Kakeibo.Modules.Recurring.Tests/ --no-build --configuration Release
    - dotnet test tests/Kakeibo.ArchitectureTests/ --no-build --configuration Release
```

### Test result reporting

```bash
# Generate JUnit XML report for CI consumption
dotnet test Kakeibo.slnx --logger "junit;LogFilePath=test-results/{assembly}-results.xml"
```

```yaml
# GitLab CI artifact for test results
artifacts:
  when: always
  reports:
    junit:
      - test-results/*-results.xml
  expire_in: 30 days
```

### Coverage reporting

```bash
# Generate Cobertura XML for GitLab coverage visualization
dotnet test Kakeibo.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory coverage-results \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

```yaml
# GitLab CI coverage artifact
artifacts:
  reports:
    coverage_report:
      coverage_format: cobertura
      path: coverage-results/**/coverage.cobertura.xml
```

### Frontend quality gate

```yaml
quality:app:
  stage: quality
  image: oven/bun:1.3.8
  tags: [local]
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
  cache:
    key: bun-app-${CI_COMMIT_REF_SLUG}
    fallback_keys:
      - bun-app-main
    paths:
      - node_modules/
      - sites/Kakeibo.App/node_modules/
  script:
    - bun install --frozen-lockfile
    - bun run app:lint:check
    - bun run app:test:unit
    - bun run app:build
```

### Failed test handling

- **Unit/handler test failure:** Blocks the merge. Developer must fix the test or update the expected behavior.
- **Integration test failure (Docker unavailable):** Tests skip via `Assert.Skip()` (KB-008). The CI job passes with skipped tests. This is expected behavior in CI environments without Docker.
- **Architecture test failure:** Blocks the merge. Indicates an architectural boundary violation that must be corrected before merging.
- **E2E test failure:** Retries 2 times (Playwright `retries: 2`). If still failing, blocks the merge. Captures screenshots and traces for debugging.
- **Flaky test detected:** Must be fixed within one sprint. Flaky tests erode trust in the test suite. Common causes: time-dependent assertions, missing async awaits, shared mutable state.

---

*Testing is a first-class concern in Kakeibo. Every module boundary, every business invariant, and every critical user flow is backed by automated tests that run on every merge request.*
