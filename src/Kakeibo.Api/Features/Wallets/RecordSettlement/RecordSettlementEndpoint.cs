using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Wallets.RecordSettlement;

public sealed class RecordSettlementEndpoint : IEndpoint
{
    public sealed record RecordSettlementRequest(
        Guid ToUserId,
        decimal Amount,
        string? Notes);

    public sealed record RecordSettlementResponse(
        Guid Id,
        Guid WalletId,
        Guid FromUserId,
        Guid ToUserId,
        decimal Amount,
        string? Notes,
        string RecordedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets/{walletId:guid}/settlements", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<RecordSettlementRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid walletId,
        RecordSettlementRequest request,
        RecordSettlementHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(walletId, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/wallets/{walletId}/settlements/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
