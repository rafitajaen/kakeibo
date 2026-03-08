using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Wallets.MakeWalletPrivate;

public sealed class MakeWalletPrivateEndpoint : IEndpoint
{
    // Lists members who will lose access — returned before the caller confirms.
    public sealed record MakeWalletPrivatePreviewResponse(
        IReadOnlyList<string> MembersToRemove,
        string Message);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // GET returns a preview (who will lose access); POST with ?confirm=true executes.
        app.MapGet("/api/wallets/{id:guid}/make-private", PreviewAsync)
            .WithTags("Wallets")
            .RequireAuthorization();

        app.MapPost("/api/wallets/{id:guid}/make-private", ExecuteAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<Results<Ok<MakeWalletPrivatePreviewResponse>, NotFound, ForbidHttpResult>> PreviewAsync(
        Guid id,
        [Microsoft.AspNetCore.Mvc.FromHeader(Name = "X-User-Id")] Guid userId,
        MakeWalletPrivateHandler handler,
        CancellationToken ct)
    {
        var result = await handler.PreviewAsync(id, userId, ct);
        if (!result.IsSuccess)
        {
            return result.Error.Code == "not_found"
                ? TypedResults.NotFound()
                : TypedResults.Forbid();
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, BadRequest<string>>> ExecuteAsync(
        Guid id,
        [Microsoft.AspNetCore.Mvc.FromHeader(Name = "X-User-Id")] Guid userId,
        MakeWalletPrivateHandler handler,
        CancellationToken ct)
    {
        var result = await handler.ExecuteAsync(id, userId, ct);
        if (!result.IsSuccess)
        {
            return result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.BadRequest(result.Error.Message)
            };
        }

        return TypedResults.NoContent();
    }
}
