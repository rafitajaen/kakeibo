using FluentValidation;

namespace Kakeibo.Api.Features.Notifications.UpdatePreferences;

public sealed class UpdatePreferencesValidator
    : AbstractValidator<UpdatePreferencesEndpoint.UpdatePreferencesRequest>
{
    public UpdatePreferencesValidator()
    {
        // All boolean fields — no special validation needed; booleans are always valid.
    }
}
