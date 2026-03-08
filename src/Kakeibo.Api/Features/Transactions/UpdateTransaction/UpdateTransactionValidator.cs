using FluentValidation;
using Kakeibo.Api.Common.Utils;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.UpdateTransaction;

public sealed class UpdateTransactionValidator
    : AbstractValidator<UpdateTransactionEndpoint.UpdateTransactionRequest>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(BusinessConstraints.AmountMin)
            .LessThanOrEqualTo(BusinessConstraints.AmountMax);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Date)
            .NotEmpty()
            .Must(d => LocalDatePattern.Iso.Parse(d ?? "").Success)
            .WithMessage("Date must be a valid ISO 8601 date (YYYY-MM-DD).");

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
