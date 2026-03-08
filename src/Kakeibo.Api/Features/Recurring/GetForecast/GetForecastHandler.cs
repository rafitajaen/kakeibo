using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Recurring.GetForecast;

// Simulates upcoming transaction occurrences for all active recurring patterns within a date window.
// Does not write to the database — pure projection of future dates.
public sealed class GetForecastHandler(AppDbContext db, IClock clock)
{
    public async Task<List<GetForecastEndpoint.ForecastItem>> HandleAsync(
        Guid userId,
        int days,
        CancellationToken ct)
    {
        var today = clock.GetCurrentInstant().InUtc().Date;
        var endWindow = today.PlusDays(days);

        // Load all active (non-deleted) patterns for the user with their related entities
        var patterns = await db.RecurringPatterns
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Wallet)
            .Where(r => r.UserId == userId && r.DeletedAt == null)
            .ToListAsync(ct);

        var items = new List<GetForecastEndpoint.ForecastItem>();

        foreach (var pattern in patterns)
        {
            // Start simulation from the pattern's current NextOccurrence
            var occurrence = pattern.NextOccurrence;

            // Simulate future occurrences within the window
            while (occurrence <= endWindow &&
                   (pattern.EndDate == null || occurrence <= pattern.EndDate))
            {
                // Only include dates within the forecast window (skip past-due dates)
                if (occurrence >= today)
                {
                    items.Add(new GetForecastEndpoint.ForecastItem(
                        pattern.Id,
                        pattern.Name,
                        pattern.TransactionType.ToString(),
                        pattern.Amount,
                        pattern.Category!.Name,
                        pattern.Wallet!.Name,
                        LocalDatePattern.Iso.Format(occurrence),
                        pattern.Frequency.ToString()));
                }

                occurrence = RecurrenceCalculator.CalculateNext(occurrence, pattern.Frequency);
            }
        }

        // Sort by date ascending for chronological forecast display
        return [.. items.OrderBy(i => i.Date)];
    }
}
