using FluentValidation;

namespace Kakeibo.Api.Features.Wallets.UpdateWallet;

public sealed class UpdateWalletValidator : AbstractValidator<UpdateWalletEndpoint.UpdateWalletRequest>
{
    public UpdateWalletValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
