using FluentValidation;

namespace Kakeibo.Api.Features.Identity.VerifyEmail;

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailEndpoint.VerifyEmailRequest>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .Length(32, 128);
    }
}
