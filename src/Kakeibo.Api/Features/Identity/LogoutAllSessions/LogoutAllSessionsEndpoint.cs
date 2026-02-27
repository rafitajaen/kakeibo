using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.LogoutAllSessions;

public sealed class LogoutAllSessionsEndpoint : IEndpoint
{
    public sealed record LogoutAllSessionsResponse(string Message, int RevokedCount);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout-all", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        LogoutAllSessionsHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "unauthorized" => TypedResults.Unauthorized(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
