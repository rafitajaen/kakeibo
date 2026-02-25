using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            return Error.Unauthorized("User is not authenticated.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Error.NotFound("User not found.");

        return new GetCurrentUserEndpoint.GetCurrentUserResponse(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsVerified,
            user.Currency,
            user.Name);
    }
}
