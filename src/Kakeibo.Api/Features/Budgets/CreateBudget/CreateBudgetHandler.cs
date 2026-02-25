using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Budgets.CreateBudget;

// Creates a new budget for a user with a spending limit over a date range.
public sealed class CreateBudgetHandler(AppDbContext db)
{
    public async Task<Result<CreateBudgetEndpoint.CreateBudgetResponse>> HandleAsync(
        CreateBudgetEndpoint.CreateBudgetRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Parse StartDate and EndDate using NodaTime ISO pattern
        var startParseResult = LocalDatePattern.Iso.Parse(request.StartDate);
        if (!startParseResult.Success)
            return Error.Validation("Invalid StartDate format. Expected ISO 8601 (YYYY-MM-DD).");

        var endParseResult = LocalDatePattern.Iso.Parse(request.EndDate);
        if (!endParseResult.Success)
            return Error.Validation("Invalid EndDate format. Expected ISO 8601 (YYYY-MM-DD).");

        var startDate = startParseResult.Value;
        var endDate = endParseResult.Value;

        if (endDate < startDate)
            return Error.Validation("EndDate must be on or after StartDate.");

        // Enforce 5-year maximum period as per business constraints
        if (endDate > startDate.PlusYears(5))
            return Error.Validation("Budget period cannot exceed 5 years.");

        // Verify category is accessible — system categories (UserId == null) are always accessible
        var category = await db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId) &&
                c.DeletedAt == null, ct);

        if (category is null)
            return Error.NotFound("Category not found or not accessible.");

        // If a wallet filter is specified, verify the user has access to it
        string? walletName = null;
        if (request.WalletId.HasValue)
        {
            var wallet = await db.Wallets
                .FirstOrDefaultAsync(w => w.Id == request.WalletId.Value && w.DeletedAt == null, ct);

            if (wallet is null)
                return Error.Forbidden("You do not have access to the specified wallet.");

            var isOwner = wallet.OwnerId == userId;
            var isMember = await db.WalletMembers
                .AnyAsync(m => m.WalletId == request.WalletId.Value && m.UserId == userId, ct);

            if (!isOwner && !isMember)
                return Error.Forbidden("You do not have access to the specified wallet.");

            walletName = wallet.Name;
        }

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Limit = request.Limit,
            StartDate = startDate,
            EndDate = endDate,
            WalletId = request.WalletId,
            CurrentSpending = 0m
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);

        return new CreateBudgetEndpoint.CreateBudgetResponse(
            budget.Id,
            budget.Name,
            budget.CategoryId,
            category.Name,
            budget.Limit,
            LocalDatePattern.Iso.Format(budget.StartDate),
            LocalDatePattern.Iso.Format(budget.EndDate),
            budget.WalletId,
            walletName,
            budget.CurrentSpending,
            budget.CreatedAt);
    }
}
