using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Goals.CreateGoal;

// Creates a new savings goal linked to a specific wallet.
// Initializes CurrentProgress from WalletBalance.Balance of the linked wallet.
public sealed class CreateGoalHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<CreateGoalEndpoint.CreateGoalResponse>> HandleAsync(
        CreateGoalEndpoint.CreateGoalRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Parse optional Deadline using NodaTimeParser
        LocalDate? deadline = null;
        if (request.Deadline is not null)
        {
            var deadlineResult = NodaTimeParser.ParseLocalDate(request.Deadline, "Deadline");
            if (deadlineResult.IsFailure) return deadlineResult.Error;
            deadline = deadlineResult.Value;

            // Enforce maximum deadline as per business constraints
            var today = clock.GetCurrentInstant().InUtc().Date;
            if (deadline.Value > today.PlusYears(BusinessConstraints.GoalMaxYears))
            {
                return Error.Validation("Deadline cannot be more than 10 years in the future.");
            }
        }

        // Verify the user has access to the specified wallet
        var wallet = await db.Wallets
            .FirstOrDefaultAsync(w => w.Id == request.WalletId && w.DeletedAt == null, ct);

        if (wallet is null)
        {
            return Error.Forbidden("You do not have access to the specified wallet.");
        }

        var role = await Wallets.WalletAccessChecker.GetRoleAsync(db, request.WalletId, userId, ct);
        if (role is null)
        {
            return Error.Forbidden("You do not have access to the specified wallet.");
        }

        // Seed CurrentProgress from the wallet's current balance
        var walletBalance = await db.WalletBalances
            .FirstOrDefaultAsync(wb => wb.WalletId == request.WalletId, ct);

        var currentProgress = walletBalance?.Balance ?? 0m;

        var goal = new Goal
        {
            UserId = userId,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            Deadline = deadline,
            WalletId = request.WalletId,
            CurrentProgress = currentProgress,
            LastMilestone = 0
        };

        db.Goals.Add(goal);
        await db.SaveChangesAsync(ct);

        return new CreateGoalEndpoint.CreateGoalResponse(
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
