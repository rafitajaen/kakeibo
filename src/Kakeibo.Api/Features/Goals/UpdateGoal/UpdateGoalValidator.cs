using FluentValidation;
using Kakeibo.Api.Common.Utils;

namespace Kakeibo.Api.Features.Goals.UpdateGoal;

public sealed class UpdateGoalValidator : AbstractValidator<UpdateGoalEndpoint.UpdateGoalRequest>
{
    public UpdateGoalValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetAmount).GreaterThan(0).LessThanOrEqualTo(BusinessConstraints.AmountMax);
        RuleFor(x => x.WalletId).NotEmpty();
    }
}
