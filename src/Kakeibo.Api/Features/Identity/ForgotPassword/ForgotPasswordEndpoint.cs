using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kakeibo.Api.Features.Identity.ForgotPassword;

public sealed class ForgotPasswordEndpoint : IEndpoint
{
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ForgotPasswordResponse(string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/forgot-password", HandleAsync)
            .WithTags("Identity")
            .WithValidation<ForgotPasswordRequest>();
    }

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        ForgotPasswordHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        // Always return OK to prevent email enumeration
        return TypedResults.Ok(result.IsSuccess
            ? result.Value
            : new ForgotPasswordResponse("If that email exists, a reset link has been sent."));
    }
}
