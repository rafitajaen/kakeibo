using FluentValidation;

namespace Kakeibo.Api.Features.Identity.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileEndpoint.UpdateProfileRequest>
{
    // Supported currencies — must match RegisterUserValidator
    private static readonly string[] SupportedCurrencies =
        ["USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "INR", "BRL", "MXN"];

    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name is not null);

        RuleFor(x => x.Currency)
            .Must(c => c is null || SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage("Currency must be a supported ISO 4217 code.")
            .When(x => x.Currency is not null);
    }
}
