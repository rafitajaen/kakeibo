using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.UpdateUsername;

public sealed class UpdateUsernameEndpoint : IEndpoint
{
    public sealed record UpdateUsernameRequest(string Username);
    public sealed record UpdateUsernameResponse(string Username);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/me/username", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .WithValidation<UpdateUsernameRequest>();
    }

    private static async Task<IResult> HandleAsync(
        UpdateUsernameRequest request,
        UpdateUsernameHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "unauthorized" => TypedResults.Unauthorized(),
                "not_found" => TypedResults.NotFound(result.Error),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
