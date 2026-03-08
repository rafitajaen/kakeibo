using FluentValidation;
using Kakeibo.Api.Common.Utils;

namespace Kakeibo.Api.Features.Budgets.UpdateBudget;

public sealed class UpdateBudgetValidator : AbstractValidator<UpdateBudgetEndpoint.UpdateBudgetRequest>
{
    public UpdateBudgetValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(BusinessConstraints.AmountMax);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
    }
}
