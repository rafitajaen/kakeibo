using FluentValidation;

namespace Kakeibo.Api.Features.Budgets.CreateBudget;

public sealed class CreateBudgetValidator : AbstractValidator<CreateBudgetEndpoint.CreateBudgetRequest>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(999_999_999.99m);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
    }
}
