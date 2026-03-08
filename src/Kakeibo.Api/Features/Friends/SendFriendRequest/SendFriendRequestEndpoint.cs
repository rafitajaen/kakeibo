using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

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
        [FromHeader(Name = "X-User-Id")] Guid userId,
        SendFriendRequestHandler handler,
        CancellationToken ct)
    {
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
