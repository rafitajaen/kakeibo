using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.GetForecast;

namespace Kakeibo.Tests.Features.Recurring.GetForecast;

public sealed class GetForecastHandlerTests
{
    // Clock fixed at 2026-03-01
    private static readonly FakeClock TestClock = new(Instant.FromUtc(2026, 3, 1, 12, 0));

    private static async Task<(User user, Wallet wallet, Category category)> SetupAsync(
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
        var category = new Category { Name = "Housing", UserId = null };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return (user, wallet, category);
    }

    [Fact]
    public async Task DailyPattern_30DayWindow_Returns30Items()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Daily pattern starting today — should yield 30 items in a 30-day window
        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Daily Expense",
            Amount = 10m,
            Description = "Coffee",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Daily,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        // today (Mar 1) + 29 more = 30 items
        Assert.Equal(30, result.Count);
        Assert.Equal("2026-03-01", result[0].Date);
        Assert.Equal("2026-03-30", result[^1].Date);
    }

    [Fact]
    public async Task MonthlyPattern_30DayWindow_ReturnsOneItem()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Monthly Rent",
            Amount = 1000m,
            Description = "Rent",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        Assert.Single(result);
        Assert.Equal("2026-03-01", result[0].Date);
    }

    [Fact]
    public async Task PatternWithEndDate_StopsAtEndDate()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Weekly pattern ending Mar 10 — in 30-day window but only 2 occurrences (Mar 1, Mar 8)
        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Limited Weekly",
            Amount = 50m,
            Description = "Gym",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Weekly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1),
            EndDate = new LocalDate(2026, 3, 10) // Stops after Mar 8 (next would be Mar 15)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        Assert.Equal(2, result.Count);
        Assert.Equal("2026-03-01", result[0].Date);
        Assert.Equal("2026-03-08", result[1].Date);
    }

    [Fact]
    public async Task SoftDeletedPattern_ExcludedFromForecast()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Deleted Pattern",
            Amount = 100m,
            Description = "Desc",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1),
            DeletedAt = Instant.FromUtc(2026, 2, 28, 0, 0)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task OtherUsersPatterns_ExcludedFromForecast()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (userA, walletA, categoryA) = await SetupAsync(db);
        var (userB, _, _) = await SetupAsync(db);

        // UserA has a monthly pattern due in the window
        db.RecurringPatterns.Add(new RecurringPattern
        {
            UserId = userA.Id,
            Name = "UserA Pattern",
            Amount = 500m,
            Description = "UserA",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = categoryA.Id,
            WalletId = walletA.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        });
        await db.SaveChangesAsync(ct);

        // UserB requests forecast — must see nothing (user isolation)
        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(userB.Id, 30, ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task BiweeklyPattern_ReturnsCorrectOccurrences()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Biweekly from Mar 1 in a 30-day window (end = Mar 31): Mar 1, Mar 15, Mar 29
        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Biweekly Paycheck",
            Amount = 2000m,
            Description = "Salary",
            TransactionType = TransactionType.Income,
            Frequency = RecurrenceFrequency.Biweekly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        Assert.Equal(3, result.Count);
        Assert.Equal("2026-03-01", result[0].Date);
        Assert.Equal("2026-03-15", result[1].Date);
        Assert.Equal("2026-03-29", result[2].Date);
    }

    [Fact]
    public async Task YearlyPattern_ReturnsOneOccurrenceInWindow()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Yearly pattern due Mar 10 — within the 30-day window from Mar 1
        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Annual Fee",
            Amount = 99m,
            Description = "Yearly subscription",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Yearly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2025, 3, 10),
            NextOccurrence = new LocalDate(2026, 3, 10)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        Assert.Single(result);
        Assert.Equal("2026-03-10", result[0].Date);
        Assert.Equal("Annual Fee", result[0].PatternName);
    }

    [Fact]
    public async Task NinetyDayWindow_ReturnsMoreItemsThanThirtyDayWindow()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Weekly pattern from Mar 1: 30-day window → 5 items; 90-day window → 13 items
        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Weekly Gym",
            Amount = 45m,
            Description = "Gym membership",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Weekly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result30 = await handler.HandleAsync(user.Id, 30, ct);
        var result90 = await handler.HandleAsync(user.Id, 90, ct);

        Assert.True(result90.Count > result30.Count,
            $"90-day window ({result90.Count}) should return more items than 30-day window ({result30.Count})");
    }

    [Fact]
    public async Task TransferPattern_IncludesWalletInfo()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Add a destination wallet for the transfer
        var destinationWallet = new Wallet { Name = "Savings", OwnerId = user.Id, Currency = "EUR" };
        db.Wallets.Add(destinationWallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = destinationWallet.Id, Balance = 0m });

        var pattern = new RecurringPattern
        {
            UserId = user.Id,
            Name = "Monthly Savings Transfer",
            Amount = 500m,
            Description = "Move to savings",
            TransactionType = TransactionType.Transfer,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            DestinationWalletId = destinationWallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        };
        db.RecurringPatterns.Add(pattern);
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        Assert.Single(result);
        Assert.Equal("Transfer", result[0].TransactionType);
        Assert.Equal("Monthly Savings Transfer", result[0].PatternName);
        Assert.Equal("Wallet", result[0].WalletName);
    }

    [Fact]
    public async Task MultiplePatterns_ResultSortedByDateAscending()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var (user, wallet, category) = await SetupAsync(db);

        // Weekly (Mar 1, 8, 15) + Monthly (Mar 15)
        db.RecurringPatterns.Add(new RecurringPattern
        {
            UserId = user.Id,
            Name = "Weekly",
            Amount = 50m,
            Description = "Gym",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Weekly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 1),
            NextOccurrence = new LocalDate(2026, 3, 1)
        });
        db.RecurringPatterns.Add(new RecurringPattern
        {
            UserId = user.Id,
            Name = "Monthly",
            Amount = 1000m,
            Description = "Rent",
            TransactionType = TransactionType.Expense,
            Frequency = RecurrenceFrequency.Monthly,
            CategoryId = category.Id,
            WalletId = wallet.Id,
            StartDate = new LocalDate(2026, 3, 15),
            NextOccurrence = new LocalDate(2026, 3, 15)
        });
        await db.SaveChangesAsync(ct);

        var handler = new GetForecastHandler(db, TestClock);
        var result = await handler.HandleAsync(user.Id, 30, ct);

        // Verify dates are in ascending order
        for (var i = 0; i < result.Count - 1; i++)
        {
            Assert.True(string.Compare(result[i].Date, result[i + 1].Date, StringComparison.Ordinal) <= 0);
        }
        Assert.Equal("2026-03-01", result[0].Date);
    }
}
