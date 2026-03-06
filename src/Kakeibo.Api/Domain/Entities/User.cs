using Kakeibo.Api.Common.Abstractions;
using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

public enum UserRole
{
    User,
    Admin
}

public sealed class User : Entity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    // Email verification
    public bool IsVerified { get; set; }
    public Instant? VerifiedAt { get; set; }
    public string? EmailVerificationToken { get; set; }
    public Instant? EmailVerificationTokenExpiresAt { get; set; }

    // User preferences
    public required string Currency { get; set; }
    public string? Name { get; set; }

    // Display preferences — stored in DB for consistency across devices.
    // WeekStartDay: 0=Sunday … 6=Saturday (default 1 = Monday).
    public int WeekStartDay { get; set; } = 1;
    // MonthStartDay: 1–28 (default 1 = first of month).
    public int MonthStartDay { get; set; } = 1;
    // CurrencyDecimalSeparator: "." or "," (default ".").
    public string CurrencyDecimalSeparator { get; set; } = ".";
    // CurrencyGroupSeparator: "," "." " " or "" (default ",").
    public string CurrencyGroupSeparator { get; set; } = ",";
    // CurrencySymbolPosition: "before" or "after" (default "before").
    public string CurrencySymbolPosition { get; set; } = "before";
    // CurrencyDisplay: "symbol" "code" or "none" (default "symbol").
    public string CurrencyDisplay { get; set; } = "symbol";

    // URL of the uploaded avatar image (stored in RustFS).
    public string? AvatarUrl { get; set; }

    // GDPR account deletion — 30-day grace period before permanent deletion.
    public Instant? DeletionRequestedAt { get; set; }
}
