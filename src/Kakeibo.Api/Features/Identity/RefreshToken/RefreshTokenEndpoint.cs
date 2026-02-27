using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.RefreshToken;

public sealed class RefreshTokenEndpoint : IEndpoint
{
    public sealed record RefreshTokenResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", HandleAsync)
            .WithTags("Identity");
    }

    private static async Task<IResult> HandleAsync(
        RefreshTokenHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Unauthorized();
    }
}
