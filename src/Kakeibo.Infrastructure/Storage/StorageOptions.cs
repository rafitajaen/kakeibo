namespace Kakeibo.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public required string Endpoint { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public bool UseSSL { get; init; } = false;
}
