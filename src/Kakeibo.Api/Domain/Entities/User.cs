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

    // GDPR account deletion — 30-day grace period before permanent deletion.
    public Instant? DeletionRequestedAt { get; set; }
}
