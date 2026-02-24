using FluentValidation;

namespace Kakeibo.Api.Features.Identity.ForgotPassword;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordEndpoint.ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}
