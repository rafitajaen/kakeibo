using FluentValidation;

namespace Kakeibo.Api.Features.Admin.UpdateAdminUser;

public sealed class UpdateAdminUserValidator : AbstractValidator<UpdateAdminUserEndpoint.UpdateAdminUserRequest>
{
    public UpdateAdminUserValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Role)
            .Must(r => r == "User" || r == "Admin")
            .When(x => x.Role is not null)
            .WithMessage("Role must be 'User' or 'Admin'.");
    }
}
