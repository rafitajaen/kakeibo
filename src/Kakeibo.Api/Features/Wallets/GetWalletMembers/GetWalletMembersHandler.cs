using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.GetWalletMembers;

// Returns all members of a shared wallet (owner + WalletMembers). Access: owner or any member.
public sealed class GetWalletMembersHandler(AppDbContext db)
{
    public async Task<Result<IReadOnlyList<GetWalletMembersEndpoint.MemberDto>>> HandleAsync(
        Guid walletId,
        Guid requesterId,
        CancellationToken ct)
    {
        var wallet = await db.Wallets
            .Include(w => w.Owner)
            .FirstOrDefaultAsync(w => w.Id == walletId, ct);

        if (wallet is null)
            return Error.NotFound("Wallet not found.");

        var isOwner = wallet.OwnerId == requesterId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == walletId && m.UserId == requesterId, ct);

        if (!isOwner && !isMember)
            return Error.Forbidden("You are not a member of this wallet.");

        // Build member list: owner first, then all WalletMembers with their User email
        var memberRecords = await db.WalletMembers
            .Where(m => m.WalletId == walletId)
            .Include(m => m.User)
            .ToListAsync(ct);

        var result = new List<GetWalletMembersEndpoint.MemberDto>
        {
            // Owner entry — JoinedAt = wallet CreatedAt
            new(wallet.OwnerId, wallet.Owner!.Email, IsOwner: true, JoinedAt: wallet.CreatedAt)
        };

        result.AddRange(memberRecords.Select(m => new GetWalletMembersEndpoint.MemberDto(
            m.UserId,
            m.User!.Email,
            IsOwner: false,
            JoinedAt: m.CreatedAt)));

        return result;
    }
}
