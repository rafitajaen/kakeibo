using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.ListTransactions;

// Returns a paginated list of transactions for a wallet the user has access to.
public sealed class ListTransactionsHandler(AppDbContext db)
{
    public async Task<Result<ListTransactionsEndpoint.ListTransactionsResponse>> HandleAsync(
        Guid walletId,
        Guid userId,
        int page,
        int pageSize,
        string? from,
        string? to,
        Guid? categoryId,
        string? type,
        CancellationToken ct)
    {
        // Verify wallet exists and is accessible
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId && w.DeletedAt == null, ct);

        if (wallet is null)
            return Error.NotFound("Wallet not found.");

        var isOwner = wallet.OwnerId == userId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == walletId && m.UserId == userId, ct);

        if (!isOwner && !isMember)
            return Error.Forbidden("You do not have access to this wallet.");

        // Base query: transactions in this wallet (source or destination) that are not deleted
        var query = db.Transactions
            .Where(t =>
                (t.WalletId == walletId || t.DestinationWalletId == walletId) &&
                t.DeletedAt == null);

        // Apply optional filters
        if (!string.IsNullOrEmpty(from) && LocalDatePattern.Iso.Parse(from).Success)
        {
            var fromDate = LocalDatePattern.Iso.Parse(from).Value;
            query = query.Where(t => t.Date >= fromDate);
        }

        if (!string.IsNullOrEmpty(to) && LocalDatePattern.Iso.Parse(to).Success)
        {
            var toDate = LocalDatePattern.Iso.Parse(to).Value;
            query = query.Where(t => t.Date <= toDate);
        }

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (!string.IsNullOrEmpty(type) &&
            Enum.TryParse<TransactionType>(type, ignoreCase: true, out var parsedType))
        {
            query = query.Where(t => t.Type == parsedType);
        }

        var total = await query.CountAsync(ct);

        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var safePageNum = Math.Max(1, page);

        // Select raw DB values and format on the client side to avoid EF translation issues.
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
                t.WalletId,
                t.DestinationWalletId,
                t.UserId,
                t.CreatedAt
            })
            .ToListAsync(ct);

        var items = rawItems
            .Select(t => new ListTransactionsEndpoint.TransactionItem(
                t.Id,
                t.Type.ToString(),
                t.Amount,
                t.Description,
                LocalDatePattern.Iso.Format(t.Date),
                t.CategoryId,
                t.CategoryName,
                t.WalletId,
                t.DestinationWalletId,
                t.UserId,
                t.CreatedAt))
            .ToList();

        return new ListTransactionsEndpoint.ListTransactionsResponse(items, total);
    }
}
