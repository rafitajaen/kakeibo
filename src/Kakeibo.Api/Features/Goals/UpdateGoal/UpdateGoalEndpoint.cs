using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Goals.UpdateGoal;

public sealed class UpdateGoalEndpoint : IEndpoint
{
    public sealed record UpdateGoalRequest(
        string Name,
        decimal TargetAmount,
        string? Deadline,
        Guid WalletId);

    public sealed record UpdateGoalResponse(
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
        app.MapPut("/api/goals/{id:guid}", HandleAsync)
            .WithTags("Goals")
            .RequireAuthorization()
            .WithValidation<UpdateGoalRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateGoalRequest request,
        UpdateGoalHandler handler,
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
