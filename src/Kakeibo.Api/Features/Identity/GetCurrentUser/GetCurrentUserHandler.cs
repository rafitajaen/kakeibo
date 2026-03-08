using System.Security.Claims;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Features.Identity.GetCurrentUser;

// Returns the profile of the currently authenticated user.
public sealed class GetCurrentUserHandler(AppDbContext db)
{
    public async Task<Result<GetCurrentUserEndpoint.GetCurrentUserResponse>> HandleAsync(
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Error.Unauthorized("User is not authenticated.");
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        return new GetCurrentUserEndpoint.GetCurrentUserResponse(
            user.Id,
            user.Email,
            user.Username,
            user.Role.ToString(),
            user.IsVerified,
            user.Currency,
            user.Name,
            user.PasswordHash is not null,
            user.WeekStartDay,
            user.MonthStartDay,
            user.CurrencyDecimalSeparator,
            user.CurrencyGroupSeparator,
            user.CurrencySymbolPosition,
            user.CurrencyDisplay);
    }
}
