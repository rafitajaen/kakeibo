using Kakeibo.Api.Domain.Entities;
using NodaTime;

namespace Kakeibo.Api.Features.Recurring;

// Calculates the next occurrence date for a recurring pattern using NodaTime.
// This class is pure (no side effects) and reused by CRUD handlers (initial NextOccurrence)
// and by GenerateRecurringTransactionsJob (advancing NextOccurrence after generation).
public static class RecurrenceCalculator
{
    // Returns the next occurrence date after the given date based on the recurrence frequency.
    // NodaTime's PlusMonths handles month-end correctly (Jan 31 + 1 month = Feb 28).
    // Note: PlusMonths(1) from Feb 28 gives Mar 28, not Mar 31 — this is acceptable for MVP.
    public static LocalDate CalculateNext(LocalDate current, RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Daily => current.PlusDays(1),
            RecurrenceFrequency.Weekly => current.PlusDays(7),
            RecurrenceFrequency.Biweekly => current.PlusDays(14),
            RecurrenceFrequency.Monthly => current.PlusMonths(1),
            RecurrenceFrequency.Yearly => current.PlusYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown recurrence frequency.")
        };
    }
}
