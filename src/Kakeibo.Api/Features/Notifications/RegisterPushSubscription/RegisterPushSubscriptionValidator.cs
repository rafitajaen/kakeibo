using FluentValidation;

namespace Kakeibo.Api.Features.Notifications.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionValidator
    : AbstractValidator<RegisterPushSubscriptionEndpoint.RegisterPushSubscriptionRequest>
{
    public RegisterPushSubscriptionValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.P256dh).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Auth).NotEmpty().MaximumLength(100);
    }
}
