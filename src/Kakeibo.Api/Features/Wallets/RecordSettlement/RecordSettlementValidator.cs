using FluentValidation;

namespace Kakeibo.Api.Features.Wallets.RecordSettlement;

public sealed class RecordSettlementValidator
    : AbstractValidator<RecordSettlementEndpoint.RecordSettlementRequest>
{
    public RecordSettlementValidator()
    {
        RuleFor(x => x.ToUserId).NotEmpty();
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(999_999_999.99m);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
