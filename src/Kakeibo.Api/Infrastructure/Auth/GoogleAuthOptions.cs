namespace Kakeibo.Api.Infrastructure.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    // Null when GOOGLE_CLIENT_ID is not configured — Google login is disabled in that case.
    public string? ClientId { get; init; }
}
