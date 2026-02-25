using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Goals.ListGoals;

public sealed class ListGoalsEndpoint : IEndpoint
{
    public sealed record ListGoalsResponse(
        Guid Id,
        string Name,
        decimal TargetAmount,
        string? Deadline,
        Guid WalletId,
        string WalletName,
        decimal CurrentProgress,
        int LastMilestone,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals", HandleAsync)
            .WithTags("Goals")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        ListGoalsHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        Guid? walletId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, walletId, ct);
        return TypedResults.Ok(result);
    }
}
