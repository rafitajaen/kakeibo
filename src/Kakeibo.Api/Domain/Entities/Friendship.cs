using Kakeibo.Api.Common.Abstractions;

namespace Kakeibo.Api.Domain.Entities;

// Represents a confirmed friendship between two users.
// UserAId is always the smaller GUID, UserBId the larger — prevents A-B/B-A duplicates.
public sealed class Friendship : Entity
{
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }

    public User? UserA { get; set; }
    public User? UserB { get; set; }
}
