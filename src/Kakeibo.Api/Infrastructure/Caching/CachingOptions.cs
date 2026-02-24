namespace Kakeibo.Api.Infrastructure.Caching;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public int DefaultExpirationMinutes { get; init; } = 5;
}
