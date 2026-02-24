using FluentValidation;

namespace Kakeibo.Api.Features.Identity.RegisterUser;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserEndpoint.RegisterUserRequest>
{
    // Supported currencies for MVP
    private static readonly string[] SupportedCurrencies =
        ["USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "INR", "BRL", "MXN"];

    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage("Currency must be a supported ISO 4217 code.");
    }
}
