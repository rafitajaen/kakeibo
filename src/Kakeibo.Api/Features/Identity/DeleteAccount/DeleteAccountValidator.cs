using FluentValidation;

namespace Kakeibo.Api.Features.Identity.DeleteAccount;

public sealed class DeleteAccountValidator : AbstractValidator<DeleteAccountEndpoint.DeleteAccountRequest>
{
    public DeleteAccountValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
    }
}
