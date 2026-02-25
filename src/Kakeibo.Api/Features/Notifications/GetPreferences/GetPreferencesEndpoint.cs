using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Notifications.GetPreferences;

public sealed class GetPreferencesEndpoint : IEndpoint
{
    public sealed record GetPreferencesResponse(
        bool EmailBudgetAlerts,
        bool EmailGoalMilestones,
        bool EmailInvitations,
        bool EmailRecurringUpdates,
        bool PushBudgetAlerts,
        bool PushGoalMilestones,
        bool PushInvitations,
        bool PushRecurringUpdates);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications/preferences", HandleAsync)
            .WithTags("Notifications")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        GetPreferencesHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(userId, ct);
        return TypedResults.Ok(result);
    }
}
