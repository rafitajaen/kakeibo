namespace Kakeibo.Api.Features.Friends;

internal static partial class FriendshipLogs
{
    [LoggerMessage(3200, LogLevel.Information,
        "Friend request {RequestId} sent from {SenderUserId} to {ReceiverUserId}")]
    internal static partial void FriendRequestSent(this ILogger logger, Guid requestId, Guid senderUserId, Guid receiverUserId);

    [LoggerMessage(3201, LogLevel.Information,
        "Friend request {RequestId} accepted — friendship {FriendshipId} created")]
    internal static partial void FriendRequestAccepted(this ILogger logger, Guid requestId, Guid friendshipId);

    [LoggerMessage(3202, LogLevel.Information,
        "Friend request {RequestId} rejected")]
    internal static partial void FriendRequestRejected(this ILogger logger, Guid requestId);

    [LoggerMessage(3203, LogLevel.Information,
        "Friend request {RequestId} cancelled by sender")]
    internal static partial void FriendRequestCancelled(this ILogger logger, Guid requestId);

    [LoggerMessage(3204, LogLevel.Information,
        "Friendship {FriendshipId} deleted by user {UserId}")]
    internal static partial void FriendshipDeleted(this ILogger logger, Guid friendshipId, Guid userId);
}
