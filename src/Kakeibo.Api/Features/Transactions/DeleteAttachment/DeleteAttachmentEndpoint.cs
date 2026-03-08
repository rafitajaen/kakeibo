using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Transactions.DeleteAttachment;

public sealed class DeleteAttachmentEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/transactions/{id:guid}/attachments/{attachmentId:guid}", HandleAsync)
            .WithTags("Transactions")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid attachmentId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        DeleteAttachmentHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, attachmentId, userId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
