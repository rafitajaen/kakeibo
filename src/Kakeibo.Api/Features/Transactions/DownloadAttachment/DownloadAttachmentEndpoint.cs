using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Transactions.DownloadAttachment;

public sealed class DownloadAttachmentEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/{id:guid}/attachments/{attachmentId:guid}", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid attachmentId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        DownloadAttachmentHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, attachmentId, userId, ct);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
        }

        // Return the raw binary stream — caller receives the file directly.
        return Results.File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
    }
}
