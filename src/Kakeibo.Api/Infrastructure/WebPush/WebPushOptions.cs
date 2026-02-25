namespace Kakeibo.Api.Infrastructure.WebPush;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    // VAPID public key (base64url-encoded). Generate with: bunx web-push generate-vapid-keys
    public required string VapidPublicKey { get; init; }

    // VAPID private key (base64url-encoded).
    public required string VapidPrivateKey { get; init; }

    // VAPID subject — mailto: or https: URI identifying the application server.
    public required string VapidSubject { get; init; }
}
