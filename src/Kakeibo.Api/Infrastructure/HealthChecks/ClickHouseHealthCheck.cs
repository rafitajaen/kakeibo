using ClickHouse.Client.ADO;
using Kakeibo.Api.Infrastructure.Audit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kakeibo.Api.Infrastructure.HealthChecks;

// Health check that verifies ClickHouse connectivity by running a simple ping query.
public sealed class ClickHouseHealthCheck(IOptions<ClickHouseOptions> options) : IHealthCheck
{
    private readonly ClickHouseOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = $"Host={_options.Host};Port={_options.Port};" +
                                   $"Database={_options.Database};Username={_options.Username};" +
                                   $"Password={_options.Password}";

            using var connection = new ClickHouseConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("ClickHouse is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ClickHouse is not reachable.", ex);
        }
    }
}
