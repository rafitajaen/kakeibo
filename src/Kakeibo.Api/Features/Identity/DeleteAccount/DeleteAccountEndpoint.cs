using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Identity.DeleteAccount;

public sealed class DeleteAccountEndpoint : IEndpoint
{
    public sealed record DeleteAccountRequest(string Password);
    public sealed record DeleteAccountResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/me", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .WithValidation<DeleteAccountRequest>();
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] DeleteAccountRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        DeleteAccountHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "unauthorized" => TypedResults.Unauthorized(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
