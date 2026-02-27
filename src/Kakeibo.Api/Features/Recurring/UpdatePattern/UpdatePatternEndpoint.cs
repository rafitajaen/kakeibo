using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Recurring.UpdatePattern;

public sealed class UpdatePatternEndpoint : IEndpoint
{
    public sealed record UpdatePatternRequest(
        string Name,
        decimal Amount,
        string Description,
        string TransactionType,
        string Frequency,
        Guid CategoryId,
        Guid WalletId,
        Guid? DestinationWalletId,
        string? EndDate);

    public sealed record UpdatePatternResponse(
        Guid Id,
        string Name,
        decimal Amount,
        string Description,
        string TransactionType,
        string Frequency,
        Guid CategoryId,
        string CategoryName,
        Guid WalletId,
        string WalletName,
        Guid? DestinationWalletId,
        string? DestinationWalletName,
        string StartDate,
        string? EndDate,
        string NextOccurrence,
        bool IsActive,
        Instant UpdatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/recurring-patterns/{id:guid}", HandleAsync)
            .WithTags("Recurring")
            .RequireAuthorization()
            .WithValidation<UpdatePatternRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdatePatternRequest request,
        UpdatePatternHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
