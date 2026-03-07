using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.GoogleLogin;

public sealed class GoogleLoginEndpoint : IEndpoint
{
    public sealed record GoogleLoginRequest(string IdToken, string Currency = "EUR");
    public sealed record GoogleLoginResponse(Guid Id, string Email, string Role, string Currency, string Username, bool IsNewAccount);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/google", HandleAsync)
            .WithTags("Identity")
            .WithValidation<GoogleLoginRequest>();
    }

    private static async Task<IResult> HandleAsync(
        GoogleLoginRequest request,
        GoogleLoginHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.BadRequest(result.Error),
                "unauthorized" => TypedResults.Unauthorized(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
