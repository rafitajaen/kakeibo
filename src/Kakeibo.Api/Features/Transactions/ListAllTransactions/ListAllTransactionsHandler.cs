using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.ListAllTransactions;

// Returns a paginated list of transactions across all wallets accessible to the user.
public sealed class ListAllTransactionsHandler(AppDbContext db)
{
    public async Task<Result<ListAllTransactionsEndpoint.ListAllTransactionsResponse>> HandleAsync(
        Guid userId,
        int page,
        int pageSize,
        string? from,
        string? to,
        Guid? categoryId,
        string? type,
        CancellationToken ct)
    {
        // All wallets the user belongs to (personal wallets also have a WalletMember row with Owner role)
        var accessibleWalletIds = await db.WalletMembers
            .Where(m => m.UserId == userId && m.Wallet!.DeletedAt == null)
            .Select(m => m.WalletId)
            .ToHashSetAsync(ct);

        var query = db.Transactions
            .Where(t =>
                accessibleWalletIds.Contains(t.WalletId) &&
                t.DeletedAt == null);

        if (!string.IsNullOrEmpty(from))
        {
            var fromParse = LocalDatePattern.Iso.Parse(from);
            if (fromParse.Success)
            {
                query = query.Where(t => t.Date >= fromParse.Value);
            }
        }

        if (!string.IsNullOrEmpty(to))
        {
            var toParse = LocalDatePattern.Iso.Parse(to);
            if (toParse.Success)
            {
                query = query.Where(t => t.Date <= toParse.Value);
            }
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(type) &&
            Enum.TryParse<TransactionType>(type, ignoreCase: true, out var parsedType))
        {
            query = query.Where(t => t.Type == parsedType);
        }

        var total = await query.CountAsync(ct);

        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var safePageNum = Math.Max(1, page);

        var rawItems = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((safePageNum - 1) * safePageSize)
            .Take(safePageSize)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.Amount,
                t.Description,
                t.Date,
                t.CategoryId,
                CategoryName = t.Category!.Name,
                CategoryColor = t.Category.BackgroundColor,
                CategoryIcon = t.Category.Icon,
                t.WalletId,
                WalletName = t.Wallet!.Name,
                t.DestinationWalletId,
                t.Notes,
                t.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var items = rawItems
            .Select(t => new ListAllTransactionsEndpoint.TransactionItem(
                t.Id,
                t.Type.ToString(),
                t.Amount,
                t.Description,
                LocalDatePattern.Iso.Format(t.Date),
                t.CategoryId,
                t.CategoryName,
                t.CategoryColor,
                t.CategoryIcon,
                t.WalletId,
                t.WalletName,
                t.DestinationWalletId,
                t.Notes,
                t.CreatedAt))
            .ToList();

        return new ListAllTransactionsEndpoint.ListAllTransactionsResponse(items, total);
    }
}
