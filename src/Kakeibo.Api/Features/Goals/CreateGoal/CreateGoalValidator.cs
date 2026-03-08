using FluentValidation;
using Kakeibo.Api.Common.Utils;

namespace Kakeibo.Api.Features.Goals.CreateGoal;

public sealed class CreateGoalValidator : AbstractValidator<CreateGoalEndpoint.CreateGoalRequest>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetAmount).GreaterThan(0).LessThanOrEqualTo(BusinessConstraints.AmountMax);
        RuleFor(x => x.WalletId).NotEmpty();
    }
}
