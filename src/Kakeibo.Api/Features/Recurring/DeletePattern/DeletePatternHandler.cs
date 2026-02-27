using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Recurring.DeletePattern;

// Soft-deletes a recurring pattern by setting DeletedAt and UpdatedAt.
// Future Hangfire job executions will skip soft-deleted patterns.
public sealed class DeletePatternHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var pattern = await db.RecurringPatterns
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, ct);

        if (pattern is null)
        {
            return Error.NotFound("Recurring pattern not found.");
        }

        if (pattern.UserId != userId)
        {
            return Error.Forbidden("You do not have access to this recurring pattern.");
        }

        var now = clock.GetCurrentInstant();
        pattern.DeletedAt = now;
        pattern.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return true;
    }
}
