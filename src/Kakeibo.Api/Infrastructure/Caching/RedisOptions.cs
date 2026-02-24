namespace Kakeibo.Api.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public required string ConnectionString { get; init; }
}
