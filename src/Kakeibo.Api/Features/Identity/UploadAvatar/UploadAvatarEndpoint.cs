using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Identity.UploadAvatar;

public sealed class UploadAvatarEndpoint : IEndpoint
{
    public sealed record UploadAvatarResponse(string AvatarUrl);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/me/avatar", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleAsync(
        IFormFile file,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        UploadAvatarHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(file, userId, ct);
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
