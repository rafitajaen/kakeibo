namespace Kakeibo.Api.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string From { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; } = true;
}
