using FluentValidation;

namespace Kakeibo.Api.Features.Admin.CreateAdminUser;

public sealed class CreateAdminUserValidator : AbstractValidator<CreateAdminUserEndpoint.CreateAdminUserRequest>
{
    public CreateAdminUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Role).Must(r => r == "User" || r == "Admin")
            .WithMessage("Role must be 'User' or 'Admin'.");
    }
}
