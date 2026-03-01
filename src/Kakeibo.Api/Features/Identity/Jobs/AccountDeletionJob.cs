using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.Jobs;

// Hangfire daily job that permanently soft-deletes accounts where the 30-day GDPR grace period has expired.
// Soft-delete preserves referential integrity for auditing while preventing account access.
public sealed class AccountDeletionJob(AppDbContext db, IClock clock, ILogger<AccountDeletionJob> logger)
{
    private const int GracePeriodDays = 30;

    // Entry point called by Hangfire scheduler daily at 02:00 UTC.
    public async Task ExecuteAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var cutoff = clock.GetCurrentInstant().Minus(Duration.FromDays(GracePeriodDays));

        // Find accounts where deletion was requested more than 30 days ago and not yet deleted
        var accounts = await db.Users
            .IgnoreQueryFilters() // Bypass soft-delete global filter
            .Where(u => u.DeletionRequestedAt != null && u.DeletionRequestedAt <= cutoff && u.DeletedAt == null)
            .ToListAsync(ct);

        logger.PendingDeletionAccounts(accounts.Count);

        var now = clock.GetCurrentInstant();
        foreach (var user in accounts)
        {
            user.DeletedAt = now;
            user.UpdatedAt = now;
        }

        if (accounts.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
