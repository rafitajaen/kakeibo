using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Notifications.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionEndpoint : IEndpoint
{
    public sealed record RegisterPushSubscriptionRequest(
        string Endpoint,
        string P256dh,
        string Auth);

    public sealed record RegisterPushSubscriptionResponse(Guid Id);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/me/push-subscriptions", HandleAsync)
            .WithTags("Notifications")
            .RequireAuthorization()
            .WithValidation<RegisterPushSubscriptionRequest>();
    }

    private static async Task<IResult> HandleAsync(
        RegisterPushSubscriptionRequest request,
        RegisterPushSubscriptionHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, userId, ct);
        return TypedResults.Created($"/api/users/me/push-subscriptions/{result.Id}", result);
    }
}
