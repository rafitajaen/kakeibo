using FluentValidation;

namespace Kakeibo.Api.Features.Wallets.TransferWallet;

public sealed class TransferWalletValidator : AbstractValidator<TransferWalletEndpoint.TransferWalletRequest>
{
    public TransferWalletValidator()
    {
        RuleFor(x => x.ToUserId).NotEmpty();
    }
}
