using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.ListAttachments;

public sealed class ListAttachmentsEndpoint : IEndpoint
{
    public sealed record AttachmentItem(
        Guid Id,
        Guid TransactionId,
        string FileName,
        string ContentType,
        long FileSizeBytes,
        Guid UploadedByUserId,
        Instant CreatedAt);

    public sealed record ListAttachmentsResponse(IReadOnlyList<AttachmentItem> Items);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/{id:guid}/attachments", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListAttachmentsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
