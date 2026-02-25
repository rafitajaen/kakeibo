using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.CreatePattern;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Tests.Features.Recurring.CreatePattern;

public sealed class CreatePatternHandlerTests
{
    // Fixed clock: 2026-03-01 → max EndDate = 2036-03-01
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static async Task<User> CreateUserAsync(Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<(Wallet wallet, WalletBalance balance)> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId, string name = "Checking")
    {
        var wallet = new Wallet { Name = name, OwnerId = ownerId, Currency = "EUR" };
        var balance = new WalletBalance { WalletId = wallet.Id, Balance = 0m };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(balance);
        await db.SaveChangesAsync();
        return (wallet, balance);
    }

    private static async Task<Category> CreateCategoryAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid? userId = null, string name = "Test Category")
    {
        var cat = new Category { Name = name, UserId = userId };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    [Fact]
    public async Task ValidExpensePattern_ReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Monthly Rent", 1200m, "Rent payment", "Expense", "Monthly",
            category.Id, wallet.Id, null, "2026-03-01", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Monthly Rent", result.Value.Name);
        Assert.Equal(1200m, result.Value.Amount);
        Assert.Equal("Expense", result.Value.TransactionType);
        Assert.Equal("Monthly", result.Value.Frequency);
        Assert.Equal("2026-03-01", result.Value.StartDate);
        Assert.Equal("2026-03-01", result.Value.NextOccurrence);
        Assert.Null(result.Value.EndDate);
        Assert.Equal(wallet.Name, result.Value.WalletName);
        Assert.Equal(category.Name, result.Value.CategoryName);
    }

    [Fact]
    public async Task ValidTransferPattern_ReturnsSuccess()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (source, _) = await CreateWalletAsync(db, user.Id, "Source");
        var (dest, _) = await CreateWalletAsync(db, user.Id, "Destination");
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Monthly Transfer", 500m, "Savings", "Transfer", "Monthly",
            category.Id, source.Id, dest.Id, "2026-03-01", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(dest.Id, result.Value.DestinationWalletId);
        Assert.Equal("Destination", result.Value.DestinationWalletName);
    }

    [Fact]
    public async Task TransferWithoutDestinationWallet_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Transfer", 500m, "No dest", "Transfer", "Monthly",
            category.Id, wallet.Id, null, "2026-03-01", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task TransferSameSourceAndDestination_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Transfer", 500m, "Same", "Transfer", "Monthly",
            category.Id, wallet.Id, wallet.Id, "2026-03-01", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task InvalidFrequency_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Bad Freq", 100m, "Desc", "Expense", "Annually",
            category.Id, wallet.Id, null, "2026-03-01", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task EndDateBeforeStartDate_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Bad Dates", 100m, "Desc", "Expense", "Monthly",
            category.Id, wallet.Id, null, "2026-03-01", "2026-02-01");

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task EndDateMoreThan10Years_ReturnsValidation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        // Clock is 2026-03-01 → max is 2036-03-01 → 2036-03-02 is invalid
        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Far Future", 100m, "Desc", "Expense", "Monthly",
            category.Id, wallet.Id, null, "2026-03-01", "2036-03-02");

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task OtherUserWallet_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (walletA, _) = await CreateWalletAsync(db, userA.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Bad", 100m, "Desc", "Expense", "Monthly",
            category.Id, walletA.Id, null, "2026-03-01", null);

        var result = await handler.HandleAsync(request, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task SharedWalletMember_CanCreatePattern()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, userA.Id, "Shared");
        db.WalletMembers.Add(new WalletMember { WalletId = wallet.Id, UserId = userB.Id });
        await db.SaveChangesAsync(ct);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Shared Pattern", 100m, "Desc", "Expense", "Monthly",
            category.Id, wallet.Id, null, "2026-03-01", null);

        var result = await handler.HandleAsync(request, userB.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Shared", result.Value.WalletName);
    }

    [Fact]
    public async Task NextOccurrence_InitializedToStartDate()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, _) = await CreateWalletAsync(db, user.Id);
        var category = await CreateCategoryAsync(db);
        var handler = new CreatePatternHandler(db, TestClock);

        var request = new CreatePatternEndpoint.CreatePatternRequest(
            "Pattern", 100m, "Desc", "Expense", "Weekly",
            category.Id, wallet.Id, null, "2026-04-15", null);

        var result = await handler.HandleAsync(request, user.Id, ct);

        Assert.True(result.IsSuccess);
        // NextOccurrence must equal StartDate at creation
        Assert.Equal("2026-04-15", result.Value.NextOccurrence);
        Assert.Equal("2026-04-15", result.Value.StartDate);

        var inDb = await db.RecurringPatterns.FirstOrDefaultAsync(r => r.Id == result.Value.Id, ct);
        Assert.NotNull(inDb);
        Assert.Equal(inDb.StartDate, inDb.NextOccurrence);
    }
}
