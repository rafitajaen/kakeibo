using FluentValidation;

namespace Kakeibo.Api.Features.Friends.SendFriendRequest;

public sealed class SendFriendRequestValidator
    : AbstractValidator<SendFriendRequestEndpoint.SendFriendRequestRequest>
{
    public SendFriendRequestValidator()
    {
        RuleFor(x => x.ReceiverUserId).NotEmpty();
    }
}
