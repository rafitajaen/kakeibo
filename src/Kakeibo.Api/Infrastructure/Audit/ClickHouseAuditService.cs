using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kakeibo.Api.Infrastructure.Audit;

// Writes audit events to a ClickHouse table.
// The table is created on first startup if it does not exist.
public sealed class ClickHouseAuditService(
    IOptions<ClickHouseOptions> options,
    ILogger<ClickHouseAuditService> logger) : IAuditService
{
    private readonly ClickHouseOptions _options = options.Value;
    private bool _tableEnsured;

    // Builds the ClickHouse connection string from options.
    private string BuildConnectionString()
    {
        return $"Host={_options.Host};Port={_options.Port};Database={_options.Database};" +
               $"Username={_options.Username};Password={_options.Password}";
    }

    // Creates the audit_events table in ClickHouse if it does not exist.
    private async Task EnsureTableCreatedAsync(CancellationToken cancellationToken)
    {
        if (_tableEnsured)
            return;

        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS audit_events (
                id UUID,
                user_id UUID NOT NULL,
                action LowCardinality(String) NOT NULL,
                entity_type Nullable(String),
                entity_id Nullable(UUID),
                occurred_at DateTime64(3, 'UTC') NOT NULL,
                ip_address Nullable(String),
                user_agent Nullable(String),
                changes Nullable(String)
            ) ENGINE = MergeTree()
            ORDER BY (user_id, occurred_at)
            SETTINGS index_granularity = 8192
            """;

        using var connection = new ClickHouseConnection(BuildConnectionString());
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync(cancellationToken);

        _tableEnsured = true;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureTableCreatedAsync(cancellationToken);

            using var connection = new ClickHouseConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            // Use ClickHouseBulkCopy for efficient single-row insert
            using var bulkCopy = new ClickHouseBulkCopy(connection)
            {
                DestinationTableName = "audit_events",
                BatchSize = 1
            };

            var rows = new[]
            {
                new object?[]
                {
                    entry.UserId,
                    entry.UserId,
                    entry.Action,
                    entry.EntityType,
                    entry.EntityId,
                    entry.OccurredAt.ToUnixTimeTicks() / TimeSpan.TicksPerMillisecond,
                    entry.IpAddress,
                    entry.UserAgent,
                    entry.Changes
                }
            };

            await bulkCopy.WriteToServerAsync(rows, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit failures must not affect the main request flow
            logger.LogError(ex, "Failed to record audit event: Action={Action}, UserId={UserId}",
                entry.Action, entry.UserId);
        }
    }
}
