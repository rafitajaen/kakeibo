using FluentValidation;

namespace Kakeibo.Api.Features.Identity.UpdateUsername;

public sealed class UpdateUsernameValidator : AbstractValidator<UpdateUsernameEndpoint.UpdateUsernameRequest>
{
    public UpdateUsernameValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-z0-9_]+$")
            .WithMessage("Username must contain only lowercase letters, numbers, and underscores.");
    }
}
