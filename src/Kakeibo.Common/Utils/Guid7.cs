using Medo;

namespace Kakeibo.Common.Utils;

// Wrapper for Guid7 with correct byte order for PostgreSQL indexing
// NEVER use Guid.CreateVersion7() - it has broken byte order
public static class Guid7
{
    public static Uuid7 NewGuid() => Uuid7.NewUuid7();
}
