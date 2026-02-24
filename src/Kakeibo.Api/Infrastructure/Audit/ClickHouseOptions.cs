namespace Kakeibo.Api.Infrastructure.Audit;

public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";

    public required string Host { get; init; }
    public int Port { get; init; } = 8123;
    public required string Database { get; init; }
    public string Username { get; init; } = "default";
    public string Password { get; init; } = string.Empty;
}
