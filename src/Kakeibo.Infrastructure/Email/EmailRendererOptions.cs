namespace Kakeibo.Infrastructure.Email;

public sealed class EmailRendererOptions
{
    public const string SectionName = "EmailRenderer";

    public required string BaseUrl { get; init; }
}
