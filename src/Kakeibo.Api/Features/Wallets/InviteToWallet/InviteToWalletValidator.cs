using FluentValidation;

namespace Kakeibo.Api.Features.Wallets.InviteToWallet;

public sealed class InviteToWalletValidator : AbstractValidator<InviteToWalletEndpoint.InviteToWalletRequest>
{
    public InviteToWalletValidator()
    {
        RuleFor(x => x.InviteeEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}
