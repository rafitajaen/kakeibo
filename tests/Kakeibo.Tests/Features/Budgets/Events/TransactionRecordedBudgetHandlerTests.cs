using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Budgets.CreateBudget;
using Kakeibo.Api.Features.Budgets.Events;
using Kakeibo.Api.Features.Transactions.Events;
using Kakeibo.Api.Infrastructure.Events;
using NodaTime;

namespace Kakeibo.Tests.Features.Budgets.Events;

public sealed class TransactionRecordedBudgetHandlerTests
{
    private static readonly Guid HousingId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FoodId = Guid.Parse("10000000-0000-0000-0000-000000000003");

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

    private static async Task<Wallet> CreateWalletAsync(
        Kakeibo.Api.Persistence.AppDbContext db, Guid ownerId)
    {
        var wallet = new Wallet { Name = $"Wallet-{Guid.NewGuid():N}", OwnerId = ownerId, Currency = "EUR" };
        db.Wallets.Add(wallet);
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = 0m });
        await db.SaveChangesAsync();
        return wallet;
    }

    private static async Task<Guid> CreateBudgetAsync(
        Kakeibo.Api.Persistence.AppDbContext db,
        Guid userId,
        Guid categoryId,
        decimal limit,
        string start = "2026-01-01",
        string end = "2026-12-31",
        Guid? walletId = null)
    {
        var handler = new CreateBudgetHandler(db);
        var result = await handler.HandleAsync(
            new CreateBudgetEndpoint.CreateBudgetRequest("Budget", categoryId, limit, start, end, walletId),
            userId, CancellationToken.None);
        return result.Value.Id;
    }

    [Fact]
    public async Task IgnoresNonExpenseTransactions()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);

        // Income transaction — should not affect budget
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Income",
            Amount = 200m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(0m, budget!.CurrentSpending);
        eventBus.DidNotReceive().Publish(Arg.Any<BudgetWarningEvent>());
        eventBus.DidNotReceive().Publish(Arg.Any<BudgetExceededEvent>());
    }

    [Fact]
    public async Task IncreasesSpending_WhenExpenseMatchesBudget()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 100m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(100m, budget!.CurrentSpending);
    }

    [Fact]
    public async Task IgnoresBudgets_WithDifferentCategory()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);

        // Expense for Food, budget is for Housing
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 50m,
            CategoryId = FoodId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(0m, budget!.CurrentSpending);
    }

    [Fact]
    public async Task IgnoresBudgets_WithDateOutOfRange()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        // Budget only covers January 2026
        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m, "2026-01-01", "2026-01-31");

        // Expense in February — outside budget period
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 50m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 2, 15),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(0m, budget!.CurrentSpending);
    }

    [Fact]
    public async Task IgnoresBudgets_WithDifferentWallet_WhenWalletIdSet()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var walletA = await CreateWalletAsync(db, user.Id);
        var walletB = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        // Budget scoped to walletA only
        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m, walletId: walletA.Id);

        // Expense from walletB — should not match
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = walletB.Id,
            Type = "Expense",
            Amount = 50m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(0m, budget!.CurrentSpending);
    }

    [Fact]
    public async Task MatchesBudgets_WithNullWalletId()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        // Budget monitors all wallets (WalletId = null)
        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 500m);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 100m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        var budget = await db.Budgets.FindAsync([budgetId], ct);
        Assert.Equal(100m, budget!.CurrentSpending);
    }

    [Fact]
    public async Task PublishesBudgetWarningEvent_WhenCrossing75Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 100m);

        // Seed existing spending at 70 (below 75%)
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 70m;
        await db.SaveChangesAsync(ct);

        // Expense of 10 pushes to 80 (above 75%)
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 10m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.Received(1).Publish(Arg.Any<BudgetWarningEvent>());
        eventBus.DidNotReceive().Publish(Arg.Any<BudgetExceededEvent>());
    }

    [Fact]
    public async Task DoesNotPublishWarning_WhenAlreadyAbove75Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 100m);

        // Already at 80% — warning already past
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 80m;
        await db.SaveChangesAsync(ct);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 5m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.DidNotReceive().Publish(Arg.Any<BudgetWarningEvent>());
    }

    [Fact]
    public async Task PublishesBudgetExceededEvent_WhenCrossing100Percent()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 100m);

        // At 90% — below limit
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 90m;
        await db.SaveChangesAsync(ct);

        // Expense of 15 pushes to 105 (above 100%)
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 15m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.Received(1).Publish(Arg.Any<BudgetExceededEvent>());
    }

    [Fact]
    public async Task DoesNotPublishExceeded_WhenAlreadyExceeded()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 100m);

        // Already exceeded
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 105m;
        await db.SaveChangesAsync(ct);

        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 10m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.DidNotReceive().Publish(Arg.Any<BudgetExceededEvent>());
    }

    [Fact]
    public async Task PublishesBothEvents_WhenJumpingFromBelow75ToAbove100()
    {
        await using var db = await TestDbContextFactory.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var user = await CreateUserAsync(db);
        var wallet = await CreateWalletAsync(db, user.Id);
        var eventBus = Substitute.For<IEventBus>();
        var handler = new TransactionRecordedBudgetHandler(db, eventBus);

        var budgetId = await CreateBudgetAsync(db, user.Id, HousingId, 100m);

        // At 10% — well below 75%
        var budget = await db.Budgets.FindAsync([budgetId], ct);
        budget!.CurrentSpending = 10m;
        await db.SaveChangesAsync(ct);

        // Single large expense that jumps from 10 to 120 (above both thresholds)
        await handler.HandleAsync(new TransactionRecordedEvent
        {
            TransactionId = Guid.NewGuid(),
            UserId = user.Id,
            WalletId = wallet.Id,
            Type = "Expense",
            Amount = 110m,
            CategoryId = HousingId,
            Date = new LocalDate(2026, 6, 1),
        }, ct);

        eventBus.Received(1).Publish(Arg.Any<BudgetWarningEvent>());
        eventBus.Received(1).Publish(Arg.Any<BudgetExceededEvent>());
    }
}
