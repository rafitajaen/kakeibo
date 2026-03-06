using FluentValidation;

namespace Kakeibo.Api.Features.Transactions.Categories.UpdateCategory;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryEndpoint.UpdateCategoryRequest>
{
    // Regex for a valid 6-digit hex color, e.g. "#A3B4C5".
    private const string HexColorPattern = @"^#[0-9A-Fa-f]{6}$";

    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.BackgroundColor)
            .Matches(HexColorPattern)
            .WithMessage("BackgroundColor must be a valid hex color (e.g. '#A3B4C5').")
            .When(x => x.BackgroundColor is not null);

        RuleFor(x => x.TextColor)
            .Matches(HexColorPattern)
            .WithMessage("TextColor must be a valid hex color (e.g. '#A3B4C5').")
            .When(x => x.TextColor is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(50)
            .When(x => x.Icon is not null);
    }
}
