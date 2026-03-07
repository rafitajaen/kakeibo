using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Friends.SearchUsers;

public sealed class SearchUsersEndpoint : IEndpoint
{
    public sealed record SearchUsersResponse(
        Guid Id,
        string Username,
        string? Name,
        string? AvatarUrl);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/search", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        string q,
        ClaimsPrincipal principal,
        SearchUsersHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var results = await handler.HandleAsync(q, userId, ct);
        return TypedResults.Ok(results);
    }
}
