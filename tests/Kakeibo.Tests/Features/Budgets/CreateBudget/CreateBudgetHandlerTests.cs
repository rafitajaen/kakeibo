using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Budgets.CreateBudget;

public sealed class CreateBudgetHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            Username = $"user_{Guid.NewGuid():N}"[..12],
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, string name = "My Wallet")
    {
        var wallet = new Wallet { Name = name, Currency = "EUR" };
        var balance = new WalletBalance { WalletId = wallet.Id, Balance = 0m };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(balance);
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = ownerId, Role = WalletMemberRole.Owner });
        await db.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task ValidRequest_AllWallets_ReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        // Use Housing system category (fixed GUID)
        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Housing Budget", housingId, 500m, "2026-01-01", "2026-12-31", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Housing Budget", result.Value.Name);
        Assert.Equal(housingId, result.Value.CategoryId);
        Assert.Equal("Housing", result.Value.CategoryName);
        Assert.Equal(500m, result.Value.Limit);
        Assert.Equal("2026-01-01", result.Value.StartDate);
        Assert.Equal("2026-12-31", result.Value.EndDate);
        Assert.Null(result.Value.WalletId);
        Assert.Null(result.Value.WalletName);
        Assert.Equal(0m, result.Value.CurrentSpending);
    }

    [Fact]
    public async Task ValidRequest_WithWalletFilter_ReturnsWalletName()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id, "Checking");
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Housing Budget", housingId, 500m, "2026-01-01", "2026-12-31", wallet.Id);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(wallet.Id, result.Value.WalletId);
        Assert.Equal("Checking", result.Value.WalletName);
    }

    [Fact]
    public async Task CurrentSpendingInitializedToZero()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Test Budget", housingId, 100m, "2026-01-01", "2026-01-31", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.CurrentSpending);

        // Verify in database
        var inDb = await db.Budgets.FirstOrDefaultAsync(b => b.Id == result.Value.Id, ct);
        Assert.NotNull(inDb);
        Assert.Equal(0m, inDb.CurrentSpending);
    }

    [Fact]
    public async Task EndDateBeforeStartDate_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Budget", housingId, 100m, "2026-12-31", "2026-01-01", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task PeriodExceedsFiveYears_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Budget", housingId, 100m, "2026-01-01", "2031-01-02", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task InvalidCategory_OtherUsersCategory_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        // UserA creates a custom category
        var category = new Category { Name = "UserA Cat", UserId = userA.Id };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        // UserB tries to use UserA's category in a budget
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Budget", category.Id, 100m, "2026-01-01", "2026-12-31", null);

        var result = await handler.HandleAsync(request, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task InvalidWallet_OtherUsersWallet_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var walletA = await CreateWalletAsync(db, userA.Id);
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Budget", housingId, 100m, "2026-01-01", "2026-12-31", walletA.Id);

        var result = await handler.HandleAsync(request, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task NullWalletId_MonitorsAllWallets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "All Wallets Budget", housingId, 500m, "2026-01-01", "2026-12-31", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.WalletId);
    }

    [Fact]
    public async Task SystemCategory_Succeeds()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        // All 12 system categories have fixed GUIDs 10000000-0000-0000-0000-00000000000N
        var foodId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var request = new CreateBudgetEndpoint.CreateBudgetRequest(
            "Food Budget", foodId, 300m, "2026-01-01", "2026-01-31", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Food & Dining", result.Value.CategoryName);
    }

    [Fact]
    public async Task SamePeriod_DifferentCategories_AllowsMultipleBudgets()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var handler = new CreateBudgetHandler(db);

        var housingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var foodId = Guid.Parse("10000000-0000-0000-0000-000000000003");

        var r1 = await handler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Housing", housingId, 1000m, "2026-01-01", "2026-01-31", null),
            user.Id, ct);

        var r2 = await handler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Food", foodId, 300m, "2026-01-01", "2026-01-31", null),
            user.Id, ct);

        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
    }
}
