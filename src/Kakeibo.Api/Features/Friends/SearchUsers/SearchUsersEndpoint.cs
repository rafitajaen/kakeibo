using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

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
        [FromHeader(Name = "X-User-Id")] Guid userId,
        SearchUsersHandler handler,
        CancellationToken ct)
    {
        var results = await handler.HandleAsync(q, userId, ct);
        return TypedResults.Ok(results);
    }
}
