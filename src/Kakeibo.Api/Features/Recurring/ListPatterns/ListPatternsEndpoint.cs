using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Recurring.ListPatterns;

public sealed class ListPatternsEndpoint : IEndpoint
{
    public sealed record ListPatternsResponse(
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
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/recurring-patterns", HandleAsync)
            .WithTags("Recurring")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ListPatternsHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, ct);
        return TypedResults.Ok(result);
    }
}
