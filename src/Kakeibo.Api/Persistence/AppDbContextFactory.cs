using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kakeibo.Api.Persistence;

// Provides a DbContext instance for EF Core design-time tools (migrations).
// Uses a localhost connection string so `dotnet ef migrations add` works without a running server.
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Design-time connection string — only used by `dotnet ef` CLI, never at runtime
        var connectionString = "Host=localhost;Port=5432;Database=kakeibo;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
