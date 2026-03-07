namespace Kakeibo.Api.Infrastructure.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public required string ClientId { get; init; }
}
