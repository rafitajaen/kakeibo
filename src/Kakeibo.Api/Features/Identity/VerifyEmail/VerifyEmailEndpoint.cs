using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kakeibo.Api.Features.Identity.VerifyEmail;

public sealed class VerifyEmailEndpoint : IEndpoint
{
    public sealed record VerifyEmailRequest(string Token);
    public sealed record VerifyEmailResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/verify-email", HandleAsync)
            .WithTags("Identity")
            .WithValidation<VerifyEmailRequest>();
    }

    private static async Task<IResult> HandleAsync(
        VerifyEmailRequest request,
        VerifyEmailHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
