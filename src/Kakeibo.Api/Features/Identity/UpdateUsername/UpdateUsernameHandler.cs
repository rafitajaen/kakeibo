using System.Security.Claims;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.UpdateUsername;

// Updates the authenticated user's username if the new value is not taken.
public sealed class UpdateUsernameHandler(AppDbContext db, IClock clock)
{
    public async Task<Result<UpdateUsernameEndpoint.UpdateUsernameResponse>> HandleAsync(
        UpdateUsernameEndpoint.UpdateUsernameRequest request,
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

        var normalizedUsername = request.Username.ToLowerInvariant();

        // Check for uniqueness
        var isTaken = await db.Users
            .AnyAsync(u => u.Username == normalizedUsername && u.Id != userId, ct);

        if (isTaken)
        {
            return Error.Conflict($"Username '{normalizedUsername}' is already taken.");
        }

        user.Username = normalizedUsername;
        user.UpdatedAt = clock.GetCurrentInstant();

        await db.SaveChangesAsync(ct);

        return new UpdateUsernameEndpoint.UpdateUsernameResponse(user.Username);
    }
}
