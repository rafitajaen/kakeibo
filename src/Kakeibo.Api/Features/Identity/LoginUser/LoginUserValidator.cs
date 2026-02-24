using FluentValidation;

namespace Kakeibo.Api.Features.Identity.LoginUser;

public sealed class LoginUserValidator : AbstractValidator<LoginUserEndpoint.LoginUserRequest>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
