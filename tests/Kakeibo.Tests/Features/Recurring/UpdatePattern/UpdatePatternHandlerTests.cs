using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.UpdatePattern;
using NodaTime;

namespace Kakeibo.Tests.Features.Recurring.UpdatePattern;

public sealed class UpdatePatternHandlerTests
{
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

    private static async Task<(Wallet wallet, Category category, RecurringPattern pattern)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid userId, string patternName = "Monthly Rent")
    {
        var wallet = new Wallet { Name = "Wallet", OwnerId = userId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        var category = new Category { Name = "Category", UserId = null };
        db.Categories.Add(category);
        var pattern = new RecurringPattern
        {
            UserId = userId,
            Name = patternName,
            Amount = 1000m,
            Description = "Old desc",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 1, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync();
        return (wallet, category, pattern);
    }

    [Fact]
    public async Task ValidUpdate_UpdatesPattern()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, category, pattern) = await SetupAsync(db, user.Id);
        var handler = new UpdatePatternHandler(db, TestClock);

        var request = new UpdatePatternEndpoint.UpdatePatternRequest(
            "Updated Rent", 1200m, "New desc", "Expense", "Monthly",
            category.Id, wallet.Id, null, null);

        var result = await handler.HandleAsync(pattern.Id, request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Rent", result.Value.Name);
        Assert.Equal(1200m, result.Value.Amount);
        Assert.Equal("New desc", result.Value.Description);
    }

    [Fact]
    public async Task FrequencyChange_RecalculatesNextOccurrence()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, category, pattern) = await SetupAsync(db, user.Id);
        // Pattern NextOccurrence is 2026-03-01 (same as today in clock)
        var handler = new UpdatePatternHandler(db, TestClock);

        var request = new UpdatePatternEndpoint.UpdatePatternRequest(
            pattern.Name, pattern.Amount, pattern.Description, "Expense", "Weekly",
            category.Id, wallet.Id, null, null);

        var result = await handler.HandleAsync(pattern.Id, request, user.Id, ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("Weekly", result.Value.Frequency);
        // NextOccurrence should be recalculated: today (2026-03-01) + 7 days = 2026-03-08
        Assert.Equal("2026-03-08", result.Value.NextOccurrence);
    }

    [Fact]
    public async Task StartDate_IsImmutableAfterCreation()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, category, pattern) = await SetupAsync(db, user.Id);
        var handler = new UpdatePatternHandler(db, TestClock);

        var request = new UpdatePatternEndpoint.UpdatePatternRequest(
            pattern.Name, pattern.Amount, pattern.Description, "Expense", "Monthly",
            category.Id, wallet.Id, null, null);

        var result = await handler.HandleAsync(pattern.Id, request, user.Id, ct);

        Assert.True(result.IsSuccess);
        // StartDate should remain unchanged (2026-01-01)
        Assert.Equal("2026-01-01", result.Value.StartDate);
    }

    [Fact]
    public async Task OtherUsersPattern_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var userA = await CreateUserAsync(db);
        var userB = await CreateUserAsync(db);
        var (wallet, category, pattern) = await SetupAsync(db, userA.Id);
        var handler = new UpdatePatternHandler(db, TestClock);

        var request = new UpdatePatternEndpoint.UpdatePatternRequest(
            "Hack", 1m, "Hack", "Expense", "Monthly",
            category.Id, wallet.Id, null, null);

        var result = await handler.HandleAsync(pattern.Id, request, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task NonExistentPattern_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var (wallet, category, _) = await SetupAsync(db, user.Id);
        var handler = new UpdatePatternHandler(db, TestClock);

        var request = new UpdatePatternEndpoint.UpdatePatternRequest(
            "Ghost", 1m, "Ghost", "Expense", "Monthly",
            category.Id, wallet.Id, null, null);

        var result = await handler.HandleAsync(Guid.NewGuid(), request, user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }
}
