using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

// An in-app notification delivered to a user.
public sealed class Notification : Entity
{
    public required Guid UserId { get; set; }

    // Notification type — matches NotificationTypes constants.
    public required string Type { get; set; }

    public required string Title { get; set; }
    public required string Body { get; set; }

    // Optional JSON metadata for linking to related entities (e.g., budget ID, goal ID).
    public string? Metadata { get; set; }

    public bool IsRead { get; set; } = false;

    // Navigation properties
    public User? User { get; set; }
}
