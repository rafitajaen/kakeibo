using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.ListPatterns;

namespace Kakeibo.Tests.Features.Recurring.ListPatterns;

public sealed class ListPatternsHandlerTests
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

    private static async Task<(Wallet wallet, Category category)> CreateWalletAndCategoryAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId)
    {
        var wallet = new Wallet { Name = "Wallet", OwnerId = userId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        var category = new Category { Name = "Category", UserId = null };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return (wallet, category);
    }

    private static RecurringPattern CreatePattern(Guid userId, Guid walletId, Guid categoryId,
        string name, LocalDate nextOccurrence, Instant? deletedAt = null)
    {
        return new RecurringPattern
        {
            UserId = userId,
            Name = name,
            Amount = 100m,
            Description = "Desc",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = categoryId,
            WalletId = walletId,
            StartDate = new LocalDate(2026, 1, 1),
            NextOccurrence = nextOccurrence,
            DeletedAt = deletedAt
        };
    }

    [Fact]
    public async Task ReturnsUserPatternsOrderedByNextOccurrence()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, category) = await CreateWalletAndCategoryAsync(db, user.Id);

        db.RecurringPatterns.Add(CreatePattern(user.Id, wallet.Id, category.Id, "B", new LocalDate(2026, 4, 1)));
        db.RecurringPatterns.Add(CreatePattern(user.Id, wallet.Id, category.Id, "A", new LocalDate(2026, 3, 15)));
        db.RecurringPatterns.Add(CreatePattern(user.Id, wallet.Id, category.Id, "C", new LocalDate(2026, 5, 1)));
        await db.SaveChangesAsync(ct);

        var handler = new ListPatternsHandler(db);
        var result = await handler.HandleAsync(user.Id, ct);

        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("B", result[1].Name);
        Assert.Equal("C", result[2].Name);
    }

    [Fact]
    public async Task ExcludesSoftDeletedPatterns()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, category) = await CreateWalletAndCategoryAsync(db, user.Id);

        db.RecurringPatterns.Add(CreatePattern(user.Id, wallet.Id, category.Id, "Active", new LocalDate(2026, 3, 1)));
        db.RecurringPatterns.Add(CreatePattern(user.Id, wallet.Id, category.Id, "Deleted", new LocalDate(2026, 3, 1),
            deletedAt: Instant.FromUtc(2026, 2, 1, 0, 0)));
        await db.SaveChangesAsync(ct);

        var handler = new ListPatternsHandler(db);
        var result = await handler.HandleAsync(user.Id, ct);

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task ExcludesOtherUsersPatterns()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (walletA, categoryA) = await CreateWalletAndCategoryAsync(db, userA.Id);
        var (walletB, categoryB) = await CreateWalletAndCategoryAsync(db, userB.Id);

        db.RecurringPatterns.Add(CreatePattern(userA.Id, walletA.Id, categoryA.Id, "A's Pattern", new LocalDate(2026, 3, 1)));
        db.RecurringPatterns.Add(CreatePattern(userB.Id, walletB.Id, categoryB.Id, "B's Pattern", new LocalDate(2026, 3, 1)));
        await db.SaveChangesAsync(ct);

        var handler = new ListPatternsHandler(db);
        var resultA = await handler.HandleAsync(userA.Id, ct);
        var resultB = await handler.HandleAsync(userB.Id, ct);

        Assert.Single(resultA);
        Assert.Equal("A's Pattern", resultA[0].Name);
        Assert.Single(resultB);
        Assert.Equal("B's Pattern", resultB[0].Name);
    }

    [Fact]
    public async Task EmptyList_WhenNoPatterns()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);

        var handler = new ListPatternsHandler(db);
        var result = await handler.HandleAsync(user.Id, ct);

        Assert.Empty(result);
    }
}
