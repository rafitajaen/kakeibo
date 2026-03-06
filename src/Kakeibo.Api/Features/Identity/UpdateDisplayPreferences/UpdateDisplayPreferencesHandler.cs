using System.Security.Claims;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.UpdateDisplayPreferences;

// Updates the display preferences (week/month start day, currency format) of the authenticated user.
public sealed class UpdateDisplayPreferencesHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesResponse>> HandleAsync(
        UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesRequest request,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Error.Unauthorized("User is not authenticated.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        // Apply only the fields that were provided in the request (partial update).
        if (request.WeekStartDay.HasValue) user.WeekStartDay = request.WeekStartDay.Value;
        if (request.MonthStartDay.HasValue) user.MonthStartDay = request.MonthStartDay.Value;
        if (request.CurrencyDecimalSeparator is not null) user.CurrencyDecimalSeparator = request.CurrencyDecimalSeparator;
        if (request.CurrencyGroupSeparator is not null) user.CurrencyGroupSeparator = request.CurrencyGroupSeparator;
        if (request.CurrencySymbolPosition is not null) user.CurrencySymbolPosition = request.CurrencySymbolPosition;
        if (request.CurrencyDisplay is not null) user.CurrencyDisplay = request.CurrencyDisplay;

        user.UpdatedAt = clock.GetCurrentInstant();
        await db.SaveChangesAsync(ct);

        return new UpdateDisplayPreferencesEndpoint.UpdateDisplayPreferencesResponse(
            user.WeekStartDay,
            user.MonthStartDay,
            user.CurrencyDecimalSeparator,
            user.CurrencyGroupSeparator,
            user.CurrencySymbolPosition,
            user.CurrencyDisplay);
    }
}
