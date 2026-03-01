using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Kakeibo.Api.Persistence;

// Used exclusively by EF Core CLI tools (dotnet ef migrations add, database update).
// When present, EF tools call CreateDbContext() directly instead of running the full
// application host — avoiding Program.cs startup complexity and env-loading path issues.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    // Creates an AppDbContext for design-time use.
    // Resolves env files via the solution root (found by walking up the directory tree)
    // so the factory works regardless of which directory EF tools use as CWD.
    public AppDbContext CreateDbContext(string[] args)
    {
        var solutionRoot = FindSolutionRoot();

        if (solutionRoot is not null)
        {
            // Load credential primitives (POSTGRES_USER, POSTGRES_PASSWORD, etc.)
            Env.Load(Path.Combine(solutionRoot, ".env"));
            // Load connection strings assembled via ${VAR} interpolation from the primitives above
            Env.Load(Path.Combine(solutionRoot, "src", "Kakeibo.Api", ".env.local"));
        }

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException(
                "Connection string 'PostgreSQL' not found. " +
                "Ensure .env and src/Kakeibo.Api/.env.local exist in the solution root, " +
                "or set ConnectionStrings__PostgreSQL in the environment.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    // Walks up from the current directory until a directory containing a .slnx file is found.
    // Returns null if no solution root is found (e.g. in CI without a local workspace).
    private static string? FindSolutionRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (dir.EnumerateFiles("*.slnx").Any())
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
