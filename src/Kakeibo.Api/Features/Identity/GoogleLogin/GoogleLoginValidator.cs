using FluentValidation;

namespace Kakeibo.Api.Features.Identity.GoogleLogin;

public sealed class GoogleLoginValidator : AbstractValidator<GoogleLoginEndpoint.GoogleLoginRequest>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
