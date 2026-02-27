using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Identity.ChangePassword;

public sealed class ChangePasswordEndpoint : IEndpoint
{
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
    public sealed record ChangePasswordResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/me/password", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .WithValidation<ChangePasswordRequest>();
    }

    private static async Task<IResult> HandleAsync(
        ChangePasswordRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ChangePasswordHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "unauthorized" => TypedResults.Unauthorized(),
                "validation" => TypedResults.UnprocessableEntity(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
