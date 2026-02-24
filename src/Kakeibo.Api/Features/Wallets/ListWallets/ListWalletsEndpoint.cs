using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NodaTime;
using System.Security.Claims;

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
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        bool? includeArchived,
        ClaimsPrincipal principal,
        ListWalletsHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var result = await handler.HandleAsync(userId, includeArchived ?? false, ct);
        return TypedResults.Ok(result);
    }
}
