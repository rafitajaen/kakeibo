namespace Kakeibo.Api.Infrastructure.Email;

public sealed class KakeiboEmailOptions
{
    public const string SectionName = "KakeiboEmail";

    public required string BaseUrl { get; init; }
}
