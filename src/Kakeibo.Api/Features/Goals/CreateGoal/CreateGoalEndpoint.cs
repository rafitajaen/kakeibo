using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Kakeibo.Api.Features.Goals.CreateGoal;

public sealed class CreateGoalEndpoint : IEndpoint
{
    public sealed record CreateGoalRequest(
        string Name,
        decimal TargetAmount,
        string? Deadline,
        Guid WalletId);

    public sealed record CreateGoalResponse(
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
        app.MapPost("/api/goals", HandleAsync)
            .WithTags("Goals")
            .RequireAuthorization()
            .WithValidation<CreateGoalRequest>();
    }

    private static async Task<IResult> HandleAsync(
        CreateGoalRequest request,
        CreateGoalHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/goals/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
