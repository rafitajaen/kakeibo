using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.SeedTestData;

// Creates a rich set of demo data for the authenticated user.
// Returns true if data was created, false if seed data already exists.
public sealed class SeedTestDataHandler(AppDbContext db, IClock clock)
{
    // System category IDs from CategoryConfiguration seed data.
    private static readonly Guid CatHousing        = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid CatTransport      = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid CatFood           = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid CatHealth         = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid CatEntertainment  = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid CatShopping       = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid CatEducation      = Guid.Parse("10000000-0000-0000-0000-000000000007");
    private static readonly Guid CatSubscriptions  = Guid.Parse("10000000-0000-0000-0000-000000000008");
    private static readonly Guid CatSavings        = Guid.Parse("10000000-0000-0000-0000-000000000009");
    private static readonly Guid CatDebt           = Guid.Parse("10000000-0000-0000-0000-00000000000a");
    private static readonly Guid CatGifts          = Guid.Parse("10000000-0000-0000-0000-00000000000b");
    private static readonly Guid CatOther          = Guid.Parse("10000000-0000-0000-0000-00000000000c");

    public async Task<Result<bool>> HandleAsync(Guid userId, CancellationToken ct)
    {
        // Guard: idempotent — skip if seed data already exists for this user
        var alreadySeeded = await db.Wallets
            .AnyAsync(w => w.WalletMembers.Any(m => m.UserId == userId && m.Role == WalletMemberRole.Owner) && w.IsSeedData, ct);
        if (alreadySeeded)
        {
            return false;
        }

        var now = clock.GetCurrentInstant();
        var today = now.InUtc().Date;

        // --- Custom categories ---
        var catSalary = new Category
        {
            Id = Guid7.NewGuid(), Name = "Salary", UserId = userId,
            IsSeedData = true, CreatedAt = now, UpdatedAt = now
        };
        var catFreelance = new Category
        {
            Id = Guid7.NewGuid(), Name = "Freelance", UserId = userId,
            IsSeedData = true, CreatedAt = now, UpdatedAt = now
        };
        var catDining = new Category
        {
            Id = Guid7.NewGuid(), Name = "Dining Out", UserId = userId,
            IsSeedData = true, CreatedAt = now, UpdatedAt = now
        };
        db.Categories.AddRange(catSalary, catFreelance, catDining);

        // --- Wallets ---
        var wChecking  = MakeWallet(userId, "Checking Account",  "Wallet",     "#3B82F6", "#FFFFFF", now);
        var wSavings   = MakeWallet(userId, "Savings",           "PiggyBank",  "#10B981", "#FFFFFF", now);
        var wVacation  = MakeWallet(userId, "Vacation Fund",     "Plane",      "#F59E0B", "#FFFFFF", now);
        var wEmergency = MakeWallet(userId, "Emergency Fund",    "ShieldAlert","#EF4444", "#FFFFFF", now);
        var wBusiness  = MakeWallet(userId, "Business",          "Briefcase",  "#8B5CF6", "#FFFFFF", now);
        var wCash      = MakeWallet(userId, "Cash",              "Banknote",   "#6B7280", "#FFFFFF", now);
        db.Wallets.AddRange(wChecking, wSavings, wVacation, wEmergency, wBusiness, wCash);

        // Create Owner WalletMember for each wallet
        foreach (var w in new[] { wChecking, wSavings, wVacation, wEmergency, wBusiness, wCash })
        {
            db.WalletMembers.Add(new WalletMember
            {
                WalletId = w.Id,
                UserId = userId,
                Role = WalletMemberRole.Owner,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        // Balances are tracked separately in WalletBalance
        var balances = new Dictionary<Guid, decimal>
        {
            [wChecking.Id]  = 0m,
            [wSavings.Id]   = 0m,
            [wVacation.Id]  = 0m,
            [wEmergency.Id] = 0m,
            [wBusiness.Id]  = 0m,
            [wCash.Id]      = 0m,
        };
        foreach (var (wid, _) in balances)
        {
            db.WalletBalances.Add(new WalletBalance { WalletId = wid, Balance = 0m, UpdatedAt = now });
        }

        // --- Transactions (12 months of data) ---
        var transactions = BuildTransactions(
            userId, today, wChecking.Id, wSavings.Id, wVacation.Id, wEmergency.Id, wBusiness.Id, wCash.Id,
            catSalary.Id, catFreelance.Id, catDining.Id, now);

        // Apply transactions to balances
        foreach (var tx in transactions)
        {
            ApplyBalance(balances, tx);
        }
        db.Transactions.AddRange(transactions);

        // Update WalletBalance records with computed totals
        foreach (var wb in db.WalletBalances.Local.Where(wb => balances.ContainsKey(wb.WalletId)))
        {
            wb.Balance = balances[wb.WalletId];
            wb.UpdatedAt = now;
        }

        // --- Budgets ---
        var budgetStart = today.With(x => new LocalDate(x.Year, x.Month, 1));
        var budgetEnd   = budgetStart.PlusMonths(1).PlusDays(-1);

        db.Budgets.AddRange(
            MakeBudget(userId, "Monthly Food",          CatFood,         500m,   budgetStart, budgetEnd, wChecking.Id,  IsSeedData: true, now),
            MakeBudget(userId, "Housing",               CatHousing,      1800m,  budgetStart, budgetEnd, wChecking.Id,  IsSeedData: true, now),
            MakeBudget(userId, "Entertainment",         CatEntertainment,200m,   budgetStart, budgetEnd, null,          IsSeedData: true, now),
            MakeBudget(userId, "Transport",             CatTransport,    300m,   budgetStart, budgetEnd, wChecking.Id,  IsSeedData: true, now),
            MakeBudget(userId, "Shopping (EXCEEDED)",   CatShopping,     100m,   budgetStart, budgetEnd, null,          IsSeedData: true, now, currentSpending: 187m));

        // --- Goals ---
        db.Goals.AddRange(
            MakeGoal(userId, "Europe Vacation",     5000m,  today.PlusYears(1), wVacation.Id,  currentProgress: balances[wVacation.Id],  IsSeedData: true, now),
            MakeGoal(userId, "Emergency Fund (6mo)",15000m, null,               wEmergency.Id, currentProgress: balances[wEmergency.Id], IsSeedData: true, now),
            MakeGoal(userId, "New Laptop",          2000m,  today.PlusMonths(6),wBusiness.Id,  currentProgress: balances[wBusiness.Id],  IsSeedData: true, now),
            MakeGoal(userId, "Annual Savings (done)",8000m, today.PlusDays(-7), wSavings.Id,   currentProgress: balances[wSavings.Id],   IsSeedData: true, now, lastMilestone: 100));

        // --- Recurring patterns ---
        db.RecurringPatterns.AddRange(
            MakePattern(userId, "Monthly Rent",    1500m,  "Monthly rent payment",   TransactionType.Expense, RecurrenceFrequency.Monthly,  CatHousing,       wChecking.Id,  null,        today.With(x => new LocalDate(x.Year, x.Month, 1)), now),
            MakePattern(userId, "Netflix",         15.99m, "Streaming subscription",  TransactionType.Expense, RecurrenceFrequency.Monthly,  CatSubscriptions, wChecking.Id,  null,        today.With(x => new LocalDate(x.Year, x.Month, 15)), now),
            MakePattern(userId, "Paycheck",        4500m,  "Monthly salary deposit",  TransactionType.Income,  RecurrenceFrequency.Monthly,  catSalary.Id,      wChecking.Id,  null,        today.With(x => new LocalDate(x.Year, x.Month, 1)), now),
            MakePattern(userId, "Savings Transfer",1000m,  "Transfer to savings",     TransactionType.Transfer,RecurrenceFrequency.Monthly,  CatSavings,        wChecking.Id,  wSavings.Id, today.With(x => new LocalDate(x.Year, x.Month, 5)), now));

        await db.SaveChangesAsync(ct);
        return true;
    }

    // Builds ~200 transactions spread over the last 12 months.
    private static List<Transaction> BuildTransactions(
        Guid userId,
        LocalDate today,
        Guid wChecking, Guid wSavings, Guid wVacation, Guid wEmergency, Guid wBusiness, Guid wCash,
        Guid catSalary, Guid catFreelance, Guid catDining,
        Instant now)
    {
        var list = new List<Transaction>();

        for (var m = 11; m >= 0; m--)
        {
            var monthStart = today.PlusMonths(-m).With(x => new LocalDate(x.Year, x.Month, 1));

            // Monthly salary (1st)
            list.Add(Tx(userId, wChecking, TransactionType.Income, 4500m, catSalary, "Monthly salary", monthStart, now));

            // Monthly rent (1st)
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 1500m, CatHousing, "Rent payment", monthStart, now));

            // Monthly savings transfer (5th)
            var d5 = monthStart.PlusDays(4);
            list.Add(Tx(userId, wChecking, TransactionType.Transfer, 1000m, CatSavings, "Savings transfer", d5, now, wSavings));
            list.Add(Tx(userId, wSavings,  TransactionType.Income,   1000m, CatSavings, "Savings transfer", d5, now));

            // Vacation fund (10th — growing over months)
            var vacAmt = 200m + (m > 6 ? 0m : 50m * (6 - m));
            list.Add(Tx(userId, wChecking,  TransactionType.Transfer, vacAmt, CatSavings, "Vacation fund",    monthStart.PlusDays(9), now, wVacation));
            list.Add(Tx(userId, wVacation,  TransactionType.Income,   vacAmt, CatSavings, "Vacation fund",    monthStart.PlusDays(9), now));

            // Emergency fund (15th)
            list.Add(Tx(userId, wChecking,  TransactionType.Transfer, 300m, CatSavings, "Emergency fund",   monthStart.PlusDays(14), now, wEmergency));
            list.Add(Tx(userId, wEmergency, TransactionType.Income,   300m, CatSavings, "Emergency fund",   monthStart.PlusDays(14), now));

            // Weekly grocery shopping (~4 per month)
            for (var w = 0; w < 4; w++)
            {
                var groceryDate = monthStart.PlusDays(w * 7);
                list.Add(Tx(userId, wChecking, TransactionType.Expense, 65m + w * 5m, CatFood, "Grocery shopping", groceryDate, now));
            }

            // Dining out (varied)
            list.Add(Tx(userId, wCash, TransactionType.Expense, 42m, catDining, "Restaurant lunch", monthStart.PlusDays(3), now));
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 89m, catDining, "Dinner with friends", monthStart.PlusDays(12), now));

            // Transport
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 85m, CatTransport, "Monthly transit pass", monthStart, now));
            list.Add(Tx(userId, wCash, TransactionType.Expense, 45m, CatTransport, "Gas", monthStart.PlusDays(10), now));

            // Subscriptions (15th)
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 15.99m, CatSubscriptions, "Netflix", monthStart.PlusDays(14), now));
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 9.99m, CatSubscriptions, "Spotify", monthStart.PlusDays(14), now));

            // Entertainment
            list.Add(Tx(userId, wCash, TransactionType.Expense, 25m, CatEntertainment, "Cinema tickets", monthStart.PlusDays(19), now));

            // Health
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 30m, CatHealth, "Gym membership", monthStart.PlusDays(1), now));

            // Freelance income (every other month)
            if (m % 2 == 0)
            {
                list.Add(Tx(userId, wBusiness, TransactionType.Income, 800m + m * 50m, catFreelance, "Freelance project", monthStart.PlusDays(17), now));
            }

            // Shopping (this month exceeds budget)
            if (m == 0)
            {
                list.Add(Tx(userId, wChecking, TransactionType.Expense, 120m, CatShopping, "New shoes", monthStart.PlusDays(5), now));
                list.Add(Tx(userId, wChecking, TransactionType.Expense, 67m,  CatShopping, "Clothing", monthStart.PlusDays(11), now));
            }
            else
            {
                list.Add(Tx(userId, wChecking, TransactionType.Expense, 60m, CatShopping, "Shopping", monthStart.PlusDays(8), now));
            }

            // Education (some months)
            if (m % 3 == 0)
            {
                list.Add(Tx(userId, wChecking, TransactionType.Expense, 49m, CatEducation, "Online course", monthStart.PlusDays(20), now));
            }

            // Debt payment (every month)
            list.Add(Tx(userId, wChecking, TransactionType.Expense, 200m, CatDebt, "Loan payment", monthStart.PlusDays(25), now));
        }

        return list;
    }

    // Applies a transaction's balance impact to the balance map.
    private static void ApplyBalance(Dictionary<Guid, decimal> balances, Transaction tx)
    {
        if (!balances.ContainsKey(tx.WalletId)) return;

        switch (tx.Type)
        {
            case TransactionType.Income:
                balances[tx.WalletId] += tx.Amount;
                break;
            case TransactionType.Expense:
                balances[tx.WalletId] -= tx.Amount;
                break;
            case TransactionType.Transfer:
                balances[tx.WalletId] -= tx.Amount;
                if (tx.DestinationWalletId is not null && balances.ContainsKey(tx.DestinationWalletId.Value))
                    balances[tx.DestinationWalletId.Value] += tx.Amount;
                break;
        }
    }

    private static Wallet MakeWallet(Guid userId, string name, string icon, string bg, string fg, Instant now) =>
        new()
        {
            Id = Guid7.NewGuid(),
            Name = name,
            Type = WalletType.Personal,
            Currency = "USD",
            Icon = icon,
            BackgroundColor = bg,
            TextColor = fg,
            IsSeedData = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static Transaction Tx(
        Guid userId, Guid walletId, TransactionType type, decimal amount,
        Guid categoryId, string description, LocalDate date, Instant now,
        Guid? destinationWalletId = null) =>
        new()
        {
            Id = Guid7.NewGuid(),
            Type = type,
            Amount = amount,
            Description = description,
            Date = date,
            CategoryId = categoryId,
            WalletId = walletId,
            DestinationWalletId = destinationWalletId,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static Budget MakeBudget(
        Guid userId, string name, Guid categoryId, decimal limit,
        LocalDate start, LocalDate end, Guid? walletId, bool IsSeedData, Instant now,
        decimal currentSpending = 0m) =>
        new()
        {
            Id = Guid7.NewGuid(),
            UserId = userId,
            Name = name,
            CategoryId = categoryId,
            Limit = limit,
            StartDate = start,
            EndDate = end,
            WalletId = walletId,
            CurrentSpending = currentSpending,
            IsSeedData = IsSeedData,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static Goal MakeGoal(
        Guid userId, string name, decimal target, LocalDate? deadline,
        Guid walletId, decimal currentProgress, bool IsSeedData, Instant now, int lastMilestone = 0) =>
        new()
        {
            Id = Guid7.NewGuid(),
            UserId = userId,
            Name = name,
            TargetAmount = target,
            Deadline = deadline,
            WalletId = walletId,
            CurrentProgress = currentProgress,
            LastMilestone = lastMilestone,
            IsSeedData = IsSeedData,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static RecurringPattern MakePattern(
        Guid userId, string name, decimal amount, string description,
        TransactionType txType, RecurrenceFrequency freq, Guid categoryId,
        Guid walletId, Guid? destWalletId, LocalDate nextOccurrence, Instant now) =>
        new()
        {
            Id = Guid7.NewGuid(),
            UserId = userId,
            Name = name,
            Amount = amount,
            Description = description,
            TransactionType = txType,
            Frequency = freq,
            CategoryId = categoryId,
            WalletId = walletId,
            DestinationWalletId = destWalletId,
            StartDate = nextOccurrence,
            NextOccurrence = nextOccurrence,
            IsSeedData = true,
            CreatedAt = now,
            UpdatedAt = now
        };
}
