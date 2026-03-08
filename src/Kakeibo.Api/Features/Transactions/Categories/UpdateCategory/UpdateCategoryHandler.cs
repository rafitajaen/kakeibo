using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.Categories.UpdateCategory;

// Renames an existing custom category. System categories and categories owned by other users are rejected.
public sealed class UpdateCategoryHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateCategoryEndpoint.UpdateCategoryResponse>> HandleAsync(
        Guid categoryId,
        UpdateCategoryEndpoint.UpdateCategoryRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.DeletedAt == null, ct);

        if (category is null)
        {
            return Error.NotFound("Category not found.");
        }

        // System categories cannot be renamed.
        if (category.IsSystem)
        {
            return Error.Forbidden("System categories cannot be modified.");
        }

        // Only the owner of a custom category may rename it.
        if (category.UserId != userId)
        {
            return Error.Forbidden("You do not have permission to modify this category.");
        }

        // New name must not conflict with existing system or active user categories.
        var nameConflicts = await db.Categories
            .AnyAsync(c =>
                c.Id != categoryId &&
                c.Name == request.Name &&
                c.DeletedAt == null &&
                (c.UserId == null || c.UserId == userId), ct);

        if (nameConflicts)
        {
            return Error.Conflict($"A category named '{request.Name}' already exists.");
        }

        category.Name = request.Name;
        category.IsPrivate = request.IsPrivate;
        // Visual customization is only allowed on custom categories (system guard already passed above).
        if (request.BackgroundColor is not null) category.BackgroundColor = request.BackgroundColor;
        if (request.TextColor is not null) category.TextColor = request.TextColor;
        if (request.Icon is not null) category.Icon = request.Icon;
        category.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return new UpdateCategoryEndpoint.UpdateCategoryResponse(
            category.Id,
            category.Name,
            IsSystem: false,
            IsArchived: false,
            category.IsPrivate,
            category.BackgroundColor,
            category.TextColor,
            category.Icon);
    }
}
