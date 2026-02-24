using Medo;

namespace Kakeibo.Api.Common.Utils;

// Wrapper for UUID v7 with correct byte order for PostgreSQL indexing.
// NEVER use Guid.CreateVersion7() — it has broken byte order for PostgreSQL sorting.
public static class Guid7
{
    public static Uuid7 NewGuid() => Uuid7.NewUuid7();
}
