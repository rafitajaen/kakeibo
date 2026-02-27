using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.ResetPassword;

public sealed class ResetPasswordEndpoint : IEndpoint
{
    public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);
    public sealed record ResetPasswordResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/reset-password", HandleAsync)
            .WithTags("Identity")
            .WithValidation<ResetPasswordRequest>();
    }

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequest request,
        ResetPasswordHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.BadRequest(result.Error),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
