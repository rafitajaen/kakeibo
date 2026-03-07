using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Friends.SendFriendRequest;

public sealed class SendFriendRequestEndpoint : IEndpoint
{
    public sealed record SendFriendRequestRequest(Guid ReceiverUserId);
    public sealed record SendFriendRequestResponse(Guid Id, Guid SenderUserId, Guid ReceiverUserId);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/friends/requests", HandleAsync)
            .WithTags("Friends")
            .RequireAuthorization()
            .WithValidation<SendFriendRequestRequest>();
    }

    private static async Task<IResult> HandleAsync(
        SendFriendRequestRequest request,
        ClaimsPrincipal principal,
        SendFriendRequestHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(request, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/friends/requests/{result.Value.Id}", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "conflict" => TypedResults.Conflict(result.Error),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
