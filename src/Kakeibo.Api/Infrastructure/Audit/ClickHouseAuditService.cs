using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Kakeibo.Api.Infrastructure.Audit;

// Writes audit events to a ClickHouse table.
// The table is created on first startup if it does not exist.
public sealed class ClickHouseAuditService(
    IOptions<ClickHouseOptions> options,
    ILogger<ClickHouseAuditService> logger) : IAuditService
{
    private readonly ClickHouseOptions _options = options.Value;
    private bool _tableEnsured;

    // Set to true after the first connection failure — prevents spamming logs on every audit call
    // when ClickHouse is not running (e.g. local dev without docker compose).
    private bool _unavailable;

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
        {
            return;
        }

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
        if (_unavailable)
        {
            logger.LogDebug("ClickHouse unavailable — skipping audit event: Action={Action}, UserId={UserId}",
                entry.Action, entry.UserId);
            return;
        }

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
            // Audit failures must not affect the main request flow.
            // Mark as unavailable to avoid retrying (and spamming logs) on every subsequent call.
            _unavailable = true;
            logger.LogWarning(ex, "ClickHouse unavailable — audit events will be skipped until restart. Action={Action}, UserId={UserId}",
                entry.Action, entry.UserId);
        }
    }

    // Queries the audit_events table for a user's activity feed with optional filters and pagination.
    public async Task<(IReadOnlyList<ActivityEntry> Items, int Total)> GetActivitiesAsync(
        Guid userId,
        LocalDate? start,
        LocalDate? end,
        string? actionType,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (_unavailable)
        {
            logger.LogDebug("ClickHouse unavailable — returning empty activity feed for user {UserId}", userId);
            return ([], 0);
        }

        try
        {
            await EnsureTableCreatedAsync(cancellationToken);

            // Build WHERE clause dynamically
            var conditions = new System.Text.StringBuilder("user_id = {userId:UUID}");
            if (start.HasValue)
            {
                conditions.Append(" AND occurred_at >= {start:DateTime64}");
            }

            if (end.HasValue)
            {
                conditions.Append(" AND occurred_at < {end:DateTime64}");
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                conditions.Append(" AND action = {actionType:String}");
            }

            var countSql = $"SELECT count() FROM audit_events WHERE {conditions}";
            var dataSql = $"""
                SELECT id, action, entity_type, entity_id, occurred_at
                FROM audit_events
                WHERE {conditions}
                ORDER BY occurred_at DESC
                LIMIT {limit} OFFSET {offset}
                """;

            using var connection = new ClickHouseConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            // Execute count query
            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = countSql;
            AddQueryParameters(countCmd, userId, start, end, actionType);
            var totalObj = await countCmd.ExecuteScalarAsync(cancellationToken);
            var total = Convert.ToInt32(totalObj);

            // Execute data query
            using var dataCmd = connection.CreateCommand();
            dataCmd.CommandText = dataSql;
            AddQueryParameters(dataCmd, userId, start, end, actionType);

            var items = new List<ActivityEntry>();
            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var action = reader.GetString(1);
                var entityType = reader.IsDBNull(2) ? null : reader.GetString(2);
                Guid? entityId = reader.IsDBNull(3) ? null : reader.GetGuid(3);

                // ClickHouse DateTime64 comes as ticks since Unix epoch
                var ticks = reader.GetInt64(4);
                var occurredAt = Instant.FromUnixTimeTicks(ticks * (TimeSpan.TicksPerMillisecond / 1));

                items.Add(new ActivityEntry(id, action, entityType, entityId, occurredAt));
            }

            return (items, total);
        }
        catch (Exception ex)
        {
            _unavailable = true;
            logger.LogWarning(ex, "ClickHouse unavailable — activity feed will return empty until restart. UserId={UserId}", userId);
            return ([], 0);
        }
    }

    // Adds named parameters for parameterized ClickHouse queries.
    private static void AddQueryParameters(
        System.Data.Common.DbCommand cmd,
        Guid userId,
        LocalDate? start,
        LocalDate? end,
        string? actionType)
    {
        var userIdParam = cmd.CreateParameter();
        userIdParam.ParameterName = "userId";
        userIdParam.Value = userId;
        cmd.Parameters.Add(userIdParam);

        if (start.HasValue)
        {
            var startParam = cmd.CreateParameter();
            startParam.ParameterName = "start";
            // Convert LocalDate to Unix milliseconds (start of day UTC)
            var startInstant = start.Value.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
            startParam.Value = startInstant.ToUnixTimeTicks() / TimeSpan.TicksPerMillisecond;
            cmd.Parameters.Add(startParam);
        }

        if (end.HasValue)
        {
            var endParam = cmd.CreateParameter();
            endParam.ParameterName = "end";
            // Convert LocalDate to exclusive end (next day start)
            var endInstant = end.Value.PlusDays(1).AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
            endParam.Value = endInstant.ToUnixTimeTicks() / TimeSpan.TicksPerMillisecond;
            cmd.Parameters.Add(endParam);
        }

        if (!string.IsNullOrEmpty(actionType))
        {
            var actionParam = cmd.CreateParameter();
            actionParam.ParameterName = "actionType";
            actionParam.Value = actionType;
            cmd.Parameters.Add(actionParam);
        }
    }
}
