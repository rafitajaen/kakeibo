using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Recurring.CreatePattern;

public sealed class CreatePatternEndpoint : IEndpoint
{
    public sealed record CreatePatternRequest(
        string Name,
        decimal Amount,
        string Description,
        string TransactionType,
        string Frequency,
        Guid CategoryId,
        Guid WalletId,
        Guid? DestinationWalletId,
        string StartDate,
        string? EndDate);

    public sealed record CreatePatternResponse(
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
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/recurring-patterns", HandleAsync)
            .WithTags("Recurring")
            .RequireAuthorization()
            .WithValidation<CreatePatternRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreatePatternRequest request,
        CreatePatternHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/recurring-patterns/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
