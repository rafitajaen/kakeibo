using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Kakeibo.Tests;

// Creates an isolated real PostgreSQL database per test via Testcontainers.
// Never add .WithReuse(true) — causes Build() to call Validate() at class load time,
// which throws DockerUnavailableException before Assert.Skip() can run (KB-008, Rule 4).
internal static class TestDbContextFactory
{
    private static readonly PostgreSqlContainer PostgresContainer =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCommand("-c", "max_connections=500")
            .Build();

    // Lazy<Task> guarantees a single container startup per test assembly
    private static readonly Lazy<Task> ContainerStartTask =
        new(() => PostgresContainer.StartAsync());

    // Awaits container startup; skips the test gracefully if Docker is unavailable.
    private static async Task EnsureContainerStartedAsync()
    {
        try
        {
            await ContainerStartTask.Value;
        }
        catch
        {
            Assert.Skip("Docker is not available. These tests require Testcontainers (PostgreSQL).");
        }
    }

    // Creates an isolated DB per test. Always use `await using var db = await TestDbContextFactory.CreateAsync()`.
    public static async Task<AppDbContext> CreateAsync()
    {
        await EnsureContainerStartedAsync();

        var databaseName = $"kakeibo_test_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(PostgresContainer.GetConnectionString())
        {
            Database = databaseName
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    // Creates a second DbContext pointing to the same database as an existing context.
    // Use this for concurrency tests where two contexts must share the same DB state.
    public static AppDbContext CreateSecondContext(AppDbContext existingContext)
    {
        var connStr = existingContext.Database.GetConnectionString()!;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr, npgsql => npgsql.UseNodaTime())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
