using FluentValidation;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Transactions.RecordTransaction;

public sealed class RecordTransactionValidator
    : AbstractValidator<RecordTransactionEndpoint.RecordTransactionRequest>
{
    public RecordTransactionValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => Enum.TryParse<TransactionType>(t, ignoreCase: true, out _))
            .WithMessage("Type must be Income, Expense, or Transfer.");

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

        RuleFor(x => x.WalletId)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        // DestinationWalletId is required when Type is Transfer
        RuleFor(x => x.DestinationWalletId)
            .NotNull()
            .When(x => string.Equals(x.Type, "Transfer", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Destination wallet is required for Transfer transactions.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
