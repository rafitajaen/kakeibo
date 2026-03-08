using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Transactions.UploadAttachment;

public sealed class UploadAttachmentEndpoint : IEndpoint
{
    public sealed record UploadAttachmentResponse(
        Guid Id,
        Guid TransactionId,
        string FileName,
        string ContentType,
        long FileSizeBytes,
        Guid UploadedByUserId,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions/{id:guid}/attachments", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IFormFile file,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        UploadAttachmentHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, file, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/transactions/{id}/attachments/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "validation" => TypedResults.UnprocessableEntity(result.Error),
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
