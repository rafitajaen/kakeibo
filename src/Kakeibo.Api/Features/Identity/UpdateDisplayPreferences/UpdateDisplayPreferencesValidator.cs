using FluentValidation;

namespace Kakeibo.Api.Features.Identity.UpdateDisplayPreferences;

public sealed class UpdateDisplayPreferencesValidator
    : AbstractValidator<UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest>
{
    // Allowed values for currency-related string preferences.
    private static readonly string[] ValidDecimalSeparators = [".", ","];
    private static readonly string[] ValidGroupSeparators = [",", ".", " ", ""];
    private static readonly string[] ValidSymbolPositions = ["before", "after"];
    private static readonly string[] ValidDisplayModes = ["symbol", "code", "none"];

    public UpdateDisplayPreferencesValidator()
    {
        // WeekStartDay: 0 (Sunday) through 6 (Saturday).
        RuleFor(x => x.WeekStartDay)
            .InclusiveBetween(0, 6)
            .When(x => x.WeekStartDay.HasValue);

        // MonthStartDay: 1 through 28 (avoids edge cases in short months).
        RuleFor(x => x.MonthStartDay)
            .InclusiveBetween(1, 28)
            .When(x => x.MonthStartDay.HasValue);

        RuleFor(x => x.CurrencyDecimalSeparator)
            .Must(v => ValidDecimalSeparators.Contains(v))
            .WithMessage("CurrencyDecimalSeparator must be '.' or ','.")
            .When(x => x.CurrencyDecimalSeparator is not null);

        RuleFor(x => x.CurrencyGroupSeparator)
            .Must(v => ValidGroupSeparators.Contains(v))
            .WithMessage("CurrencyGroupSeparator must be ',', '.', ' ', or ''.")
            .When(x => x.CurrencyGroupSeparator is not null);

        RuleFor(x => x.CurrencySymbolPosition)
            .Must(v => ValidSymbolPositions.Contains(v))
            .WithMessage("CurrencySymbolPosition must be 'before' or 'after'.")
            .When(x => x.CurrencySymbolPosition is not null);

        RuleFor(x => x.CurrencyDisplay)
            .Must(v => ValidDisplayModes.Contains(v))
            .WithMessage("CurrencyDisplay must be 'symbol', 'code', or 'none'.")
            .When(x => x.CurrencyDisplay is not null);
    }
}
