using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Kakeibo.Api.Features.Identity.ListSessions;

public sealed class ListSessionsEndpoint : IEndpoint
{
    public sealed record ListSessionsResponse(IReadOnlyList<SessionItem> Sessions);

    public sealed record SessionItem(
        Guid Id,
        string? IpAddress,
        string? UserAgent,
        string CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/me/sessions", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListSessionsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(result.Error.Message, statusCode: 500);
    }
}
