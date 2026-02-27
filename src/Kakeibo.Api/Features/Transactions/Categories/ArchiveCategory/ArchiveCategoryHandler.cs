using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.Categories.ArchiveCategory;

// Soft-deletes a custom category. System categories and other users' categories are rejected.
// Idempotent: archiving an already-archived category returns success.
public sealed class ArchiveCategoryHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<bool>> HandleAsync(
        Guid categoryId,
        Guid userId,
        CancellationToken ct)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
        {
            return Error.NotFound("Category not found.");
        }

        // System categories are immutable.
        if (category.IsSystem)
        {
            return Error.Forbidden("System categories cannot be archived.");
        }

        // Only the owner may archive their custom category.
        if (category.UserId != userId)
        {
            return Error.Forbidden("You do not have permission to archive this category.");
        }

        // Idempotent: already archived — return success without modifying state.
        if (category.DeletedAt is not null)
        {
            return true;
        }

        // Cannot archive a category that is still referenced by active transactions.
        var hasActiveTransactions = await db.Transactions
            .AnyAsync(t => t.CategoryId == categoryId && t.DeletedAt == null, ct);

        if (hasActiveTransactions)
        {
            return Error.Conflict("Cannot archive a category that has active transactions.");
        }

        category.DeletedAt = clock.GetCurrentInstant();
        category.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return true;
    }
}
