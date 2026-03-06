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

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0m)
            .LessThanOrEqualTo(999_999_999.99m);

        RuleFor(x => x.Icon)
            .MaximumLength(100)
            .When(x => x.Icon is not null);

        RuleFor(x => x.BackgroundColor)
            .MaximumLength(20)
            .When(x => x.BackgroundColor is not null);

        RuleFor(x => x.TextColor)
            .MaximumLength(20)
            .When(x => x.TextColor is not null);
    }
}
