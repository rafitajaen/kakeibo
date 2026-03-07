using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.LoginUser;

public sealed class LoginUserEndpoint : IEndpoint
{
    public sealed record LoginUserRequest(string Email, string Password);
    public sealed record LoginUserResponse(Guid Id, string Email, string Role, string Currency, string Username);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", HandleAsync)
            .WithTags("Identity")
            .WithValidation<LoginUserRequest>();
    }

    private static async Task<IResult> HandleAsync(
        LoginUserRequest request,
        LoginUserHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "unauthorized" => TypedResults.Unauthorized(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
