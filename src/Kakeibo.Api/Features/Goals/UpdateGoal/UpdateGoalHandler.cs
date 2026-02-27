using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Goals.UpdateGoal;

// Updates goal fields. If WalletId changes, CurrentProgress is re-seeded from the new wallet's balance.
// LastMilestone is not reset when TargetAmount changes (milestones are "first time ever reached").
public sealed class UpdateGoalHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateGoalEndpoint.UpdateGoalResponse>> HandleAsync(
        Guid goalId,
        UpdateGoalEndpoint.UpdateGoalRequest request,
        Guid userId,
        CancellationToken ct)
    {
        var goal = await db.Goals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.DeletedAt == null, ct);

        if (goal is null)
        {
            return Error.NotFound("Goal not found.");
        }

        if (goal.UserId != userId)
        {
            return Error.Forbidden("You do not have access to this goal.");
        }

        // Parse optional Deadline
        LocalDate? deadline = null;
        if (request.Deadline is not null)
        {
            var parseResult = LocalDatePattern.Iso.Parse(request.Deadline);
            if (!parseResult.Success)
            {
                return Error.Validation("Invalid Deadline format. Expected ISO 8601 (YYYY-MM-DD).");
            }

            deadline = parseResult.Value;

            var today = clock.GetCurrentInstant().InUtc().Date;
            if (deadline.Value > today.PlusYears(10))
            {
                return Error.Validation("Deadline cannot be more than 10 years in the future.");
            }
        }

        // Verify user has access to the new wallet
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == request.WalletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.Forbidden("You do not have access to the specified wallet.");
        }

        var isOwner = wallet.OwnerId == userId;
        var isMember = await db.WalletMembers
            .AnyAsync(m => m.WalletId == request.WalletId && m.UserId == userId, ct);

        if (!isOwner && !isMember)
        {
            return Error.Forbidden("You do not have access to the specified wallet.");
        }

        // If wallet changed, re-seed CurrentProgress from the new wallet's balance
        if (goal.WalletId != request.WalletId)
        {
            var walletBalance = await db.WalletBalances
                .FirstOrDefaultAsync(wb => wb.WalletId == request.WalletId, ct);

            goal.CurrentProgress = walletBalance?.Balance ?? 0m;
        }

        goal.Name = request.Name;
        goal.TargetAmount = request.TargetAmount;
        goal.Deadline = deadline;
        goal.WalletId = request.WalletId;
        goal.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return new UpdateGoalEndpoint.UpdateGoalResponse(
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            deadline.HasValue ? LocalDatePattern.Iso.Format(deadline.Value) : null,
            goal.WalletId,
            wallet.Name,
            goal.CurrentProgress,
            goal.LastMilestone,
            goal.CreatedAt);
    }
}
