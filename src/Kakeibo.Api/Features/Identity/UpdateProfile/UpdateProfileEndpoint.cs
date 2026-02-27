using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Identity.UpdateProfile;

public sealed class UpdateProfileEndpoint : IEndpoint
{
    public sealed record UpdateProfileRequest(string? Name, string? Currency);
    public sealed record UpdateProfileResponse(Guid Id, string Email, string? Name, string Currency);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/me/profile", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .WithValidation<UpdateProfileRequest>();
    }

    private static async Task<IResult> HandleAsync(
        UpdateProfileRequest request,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        UpdateProfileHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "validation" => TypedResults.UnprocessableEntity(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
