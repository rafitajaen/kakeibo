using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.DeletePattern;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Tests.Features.Recurring.DeletePattern;

public sealed class DeletePatternHandlerTests
{
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static async Task<(User user, RecurringPattern pattern)> SetupAsync(
        Kakeibo.Api.Persistence.AppDbContext db)
    {
        var user = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(user);
        var wallet = new Wallet { Name = "Wallet", OwnerId = user.Id, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        var category = new Category { Name = "Category", UserId = null };
        db.Categories.Add(category);
        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "To Delete",
            Amount = 100m,
            Description = "Desc",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 1, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync();
        return (user, pattern);
    }

    [Fact]
    public async Task ValidDelete_SoftDeletesPattern()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, pattern) = await SetupAsync(db);
        var handler = new DeletePatternHandler(db, TestClock);

        var result = await handler.HandleAsync(pattern.Id, user.Id, ct);

        Assert.True(result.IsSuccess);

        var inDb = await db.RecurringPatterns
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == pattern.Id, ct);
        Assert.NotNull(inDb);
        Assert.NotNull(inDb.DeletedAt);
    }

    [Fact]
    public async Task OtherUsersPattern_ReturnsForbidden()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (userA, pattern) = await SetupAsync(db);
        var userB = new User
        {
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            IsVerified = true,
            Currency = "EUR"
        };
        db.Users.Add(userB);
        await db.SaveChangesAsync(ct);
        var handler = new DeletePatternHandler(db, TestClock);

        var result = await handler.HandleAsync(pattern.Id, userB.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task NonExistentPattern_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, _) = await SetupAsync(db);
        var handler = new DeletePatternHandler(db, TestClock);

        var result = await handler.HandleAsync(Guid.NewGuid(), user.Id, ct);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task AlreadyDeletedPattern_ReturnsNotFound()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, pattern) = await SetupAsync(db);
        var handler = new DeletePatternHandler(db, TestClock);

        // First delete succeeds
        var firstResult = await handler.HandleAsync(pattern.Id, user.Id, ct);
        Assert.True(firstResult.IsSuccess);

        // Second delete on the same already-soft-deleted pattern returns not_found
        var secondResult = await handler.HandleAsync(pattern.Id, user.Id, ct);
        Assert.True(secondResult.IsFailure);
        Assert.Equal("not_found", secondResult.Error.Code);
    }
}
