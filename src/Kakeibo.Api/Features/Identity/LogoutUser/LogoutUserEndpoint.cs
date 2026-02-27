using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.LogoutUser;

public sealed class LogoutUserEndpoint : IEndpoint
{
    public sealed record LogoutUserResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        LogoutUserHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(result.Error.Message, statusCode: 500);
    }
}
