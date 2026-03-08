using NodaTime;

namespace Kakeibo.Api.Domain.Entities;

// Singleton row that stores platform-wide policies.
// Exactly one row must exist at all times — seeded on first startup.
public sealed class PlatformPolicy
{
    // Fixed primary key — always 1 to enforce the singleton invariant.
    public int Id { get; set; } = 1;

    // When false, new registrations (email and Google OAuth) are rejected.
    public bool RegistrationEnabled { get; set; } = true;

    // When true, all non-admin users receive a 503 on every API call.
    public bool MaintenanceMode { get; set; }

    public Instant UpdatedAt { get; set; }
}
