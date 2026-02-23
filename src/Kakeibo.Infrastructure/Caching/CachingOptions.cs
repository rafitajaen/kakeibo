namespace Kakeibo.Infrastructure.Caching;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    public required string RedisConnectionString { get; init; }
    public int DefaultExpirationMinutes { get; init; } = 5;
}
