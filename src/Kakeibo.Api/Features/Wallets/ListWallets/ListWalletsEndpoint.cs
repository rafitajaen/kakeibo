using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.ListWallets;

public sealed class ListWalletsEndpoint : IEndpoint
{
    public sealed record ListWalletsResponse(
        Guid Id,
        string Name,
        string Type,
        string Currency,
        decimal Balance,
        bool IsArchived,
        string? Icon,
        string? BackgroundColor,
        string? TextColor,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        bool? includeArchived,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        ListWalletsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, includeArchived ?? false, ct);
        return TypedResults.Ok(result);
    }
}
