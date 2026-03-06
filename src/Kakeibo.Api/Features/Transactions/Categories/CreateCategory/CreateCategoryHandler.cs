using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Transactions.Categories.CreateCategory;

// Creates a new custom category for the authenticated user.
public sealed class CreateCategoryHandler(AppDbContext db)
{
    public async Task<Result<CreateCategoryEndpoint.CreateCategoryResponse>> HandleAsync(
        CreateCategoryEndpoint.CreateCategoryRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Name must be unique across system categories and the user's active custom categories.
        var nameConflicts = await db.Categories
            .AnyAsync(c =>
                c.Name == request.Name &&
                c.DeletedAt == null &&
                (c.UserId == null || c.UserId == userId), ct);

        if (nameConflicts)
        {
            return Error.Conflict($"A category named '{request.Name}' already exists.");
        }

        var category = new Category
        {
            Name = request.Name,
            UserId = userId,
            BackgroundColor = request.BackgroundColor,
            TextColor = request.TextColor,
            Icon = request.Icon
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return new CreateCategoryEndpoint.CreateCategoryResponse(
            category.Id,
            category.Name,
            IsSystem: false,
            IsArchived: false,
            category.BackgroundColor,
            category.TextColor,
            category.Icon);
    }
}
