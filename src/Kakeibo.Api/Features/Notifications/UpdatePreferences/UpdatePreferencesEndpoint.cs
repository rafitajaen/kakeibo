using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Notifications.UpdatePreferences;

public sealed class UpdatePreferencesEndpoint : IEndpoint
{
    public sealed record UpdatePreferencesRequest(
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
        app.MapPut("/api/notifications/preferences", HandleAsync)
            .WithTags("Notifications")
            .RequireAuthorization()
            .WithValidation<UpdatePreferencesRequest>();
    }

    private static async Task<IResult> HandleAsync(
        UpdatePreferencesRequest request,
        UpdatePreferencesHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        await handler.HandleAsync(request, userId, ct);
        return TypedResults.NoContent();
    }
}
