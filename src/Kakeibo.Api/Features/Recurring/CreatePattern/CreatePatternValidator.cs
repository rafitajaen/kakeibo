using FluentValidation;

namespace Kakeibo.Api.Features.Recurring.CreatePattern;

public sealed class CreatePatternValidator : AbstractValidator<CreatePatternEndpoint.CreatePatternRequest>
{
    public CreatePatternValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(999_999_999.99m);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TransactionType).NotEmpty();
        RuleFor(x => x.Frequency).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.WalletId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
    }
}
