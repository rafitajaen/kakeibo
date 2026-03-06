using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.DeleteSeedData;

// Soft-deletes all seed data for the authenticated user. Idempotent — safe to call multiple times.
public sealed class DeleteSeedDataHandler(AppDbContext db, IClock clock)
{
    public async Task HandleAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();

        // Soft-delete seed wallets (cascades to transactions and balances via the app logic)
        var seedWalletIds = await db.Wallets
            .Where(w => w.OwnerId == userId && w.IsSeedData && w.DeletedAt == null)
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (seedWalletIds.Count > 0)
        {
            // Soft-delete transactions belonging to seed wallets
            await db.Transactions
                .Where(t => seedWalletIds.Contains(t.WalletId) && t.DeletedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.DeletedAt, now)
                    .SetProperty(t => t.UpdatedAt, now), ct);

            // Soft-delete the seed wallets themselves
            await db.Wallets
                .Where(w => seedWalletIds.Contains(w.Id) && w.DeletedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(w => w.DeletedAt, now)
                    .SetProperty(w => w.UpdatedAt, now), ct);
        }

        // Soft-delete seed categories (only custom categories — system categories never have IsSeedData=true)
        await db.Categories
            .Where(c => c.UserId == userId && c.IsSeedData && c.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.DeletedAt, now)
                .SetProperty(c => c.UpdatedAt, now), ct);

        // Soft-delete seed budgets
        await db.Budgets
            .Where(b => b.UserId == userId && b.IsSeedData && b.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.DeletedAt, now)
                .SetProperty(b => b.UpdatedAt, now), ct);

        // Soft-delete seed goals
        await db.Goals
            .Where(g => g.UserId == userId && g.IsSeedData && g.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.DeletedAt, now)
                .SetProperty(g => g.UpdatedAt, now), ct);

        // Soft-delete seed recurring patterns
        await db.RecurringPatterns
            .Where(r => r.UserId == userId && r.IsSeedData && r.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.DeletedAt, now)
                .SetProperty(r => r.UpdatedAt, now), ct);
    }
}
