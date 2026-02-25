using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring.Events;
using Kakeibo.Api.Features.Transactions.RecordTransaction;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Recurring.Jobs;

// Hangfire daily job that auto-generates transactions for all recurring patterns due today or earlier.
// Each pattern is processed independently — one failure does not block others.
// Calls RecordTransactionHandler directly via DI (simple monolith, no HTTP hop).
public sealed class GenerateRecurringTransactionsJob(
    AppDbContext db,
    RecordTransactionHandler recordHandler,
    IEventBus eventBus,
    IClock clock,
    ILogger<GenerateRecurringTransactionsJob> logger)
{
    // Entry point called by Hangfire scheduler daily at 01:00 UTC.
    public async Task ExecuteAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        var ct = cts.Token;

        var today = clock.GetCurrentInstant().InUtc().Date;

        // Load all active patterns due today or earlier (includes missed days)
        var patterns = await db.RecurringPatterns
            .Where(r =>
                r.DeletedAt == null &&
                r.NextOccurrence <= today &&
                (r.EndDate == null || r.NextOccurrence <= r.EndDate))
            .ToListAsync(ct);

        logger.LogInformation("GenerateRecurringTransactionsJob: {Count} pattern(s) due on {Today}",
            patterns.Count, today);

        foreach (var pattern in patterns)
        {
            try
            {
                await ProcessPatternAsync(pattern, today, ct);
            }
            catch (Exception ex)
            {
                // Isolate failures — log and continue processing remaining patterns.
                logger.LogError(ex,
                    "GenerateRecurringTransactionsJob: unhandled error processing pattern {PatternId}",
                    pattern.Id);
            }
        }
    }

    // Processes a single pattern, generating one transaction per missed occurrence up to today.
    private async Task ProcessPatternAsync(RecurringPattern pattern, LocalDate today, CancellationToken ct)
    {
        // While loop handles multiple missed occurrences (e.g., daily pattern missed 3 days)
        while (pattern.NextOccurrence <= today &&
               (pattern.EndDate == null || pattern.NextOccurrence <= pattern.EndDate))
        {
            var request = new RecordTransactionEndpoint.RecordTransactionRequest(
                pattern.TransactionType.ToString(),
                pattern.Amount,
                pattern.Description,
                LocalDatePattern.Iso.Format(pattern.NextOccurrence),
                pattern.CategoryId,
                pattern.WalletId,
                pattern.DestinationWalletId);

            var result = await recordHandler.HandleAsync(request, pattern.UserId, ct);

            if (result.IsSuccess)
            {
                // Fire-and-forget event — no consumers registered yet (Phase 7 adds Notifications handler)
                eventBus.Publish(new RecurringTransactionGeneratedEvent
                {
                    OccurredAt = clock.GetCurrentInstant(),
                    RecurringPatternId = pattern.Id,
                    TransactionId = result.Value.Id,
                    UserId = pattern.UserId
                });

                logger.LogDebug(
                    "Generated transaction {TransactionId} for pattern {PatternId} on {Date}",
                    result.Value.Id, pattern.Id, pattern.NextOccurrence);
            }
            else
            {
                // Log the handler failure but still advance NextOccurrence to avoid stuck patterns.
                logger.LogWarning(
                    "Failed to generate transaction for pattern {PatternId} on {Date}: {Error}",
                    pattern.Id, pattern.NextOccurrence, result.Error.Message);
            }

            // Advance NextOccurrence regardless of generation success
            pattern.NextOccurrence = RecurrenceCalculator.CalculateNext(pattern.NextOccurrence, pattern.Frequency);
            pattern.LastGeneratedAt = clock.GetCurrentInstant();
            pattern.UpdatedAt = clock.GetCurrentInstant();

            // Commit per occurrence to persist partial progress if the job is interrupted
            await db.SaveChangesAsync(ct);
        }
    }
}
