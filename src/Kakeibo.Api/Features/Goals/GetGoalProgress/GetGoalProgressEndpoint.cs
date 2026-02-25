using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Goals.GetGoalProgress;

public sealed class GetGoalProgressEndpoint : IEndpoint
{
    public sealed record GetGoalProgressResponse(
        Guid Id,
        string Name,
        decimal TargetAmount,
        decimal CurrentProgress,
        decimal Remaining,
        decimal PercentageComplete,
        string Status,
        string? Deadline,
        int? DaysRemaining);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals/{id:guid}/progress", HandleAsync)
            .WithTags("Goals")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetGoalProgressHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
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
