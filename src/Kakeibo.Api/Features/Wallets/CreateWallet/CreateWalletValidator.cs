using FluentValidation;
using Kakeibo.Api.Domain.Entities;

namespace Kakeibo.Api.Features.Wallets.CreateWallet;

public sealed class CreateWalletValidator : AbstractValidator<CreateWalletEndpoint.CreateWalletRequest>
{
    private static readonly string[] ValidTypes =
        [WalletType.Personal.ToString(), WalletType.Shared.ToString()];

    public CreateWalletValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Type must be one of: {string.Join(", ", ValidTypes)}.");
    }
}
