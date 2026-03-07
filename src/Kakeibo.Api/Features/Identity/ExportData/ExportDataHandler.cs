using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Identity.ExportData;

// Exports all user financial data as SQLite (.db) or CSV ZIP (.zip).
// Streams the file directly to the response — no storage in MinIO.
public sealed class ExportDataHandler(AppDbContext db, ILogger<ExportDataHandler> logger)
{
    public async Task<Result<IResult>> HandleAsync(Guid userId, string format, CancellationToken ct)
    {
        logger.ExportStarted(format, userId);

        try
        {
            return format == "sqlite"
                ? await ExportSqliteAsync(userId, ct)
                : await ExportCsvZipAsync(userId, ct);
        }
        catch (Exception ex)
        {
            logger.ExportFailed(userId, ex);
            return Error.Internal("Export failed due to an unexpected error.");
        }
    }

    // --- SQLite export ---

    private async Task<Result<IResult>> ExportSqliteAsync(Guid userId, CancellationToken ct)
    {
        var data = await LoadUserDataAsync(userId, ct);
        var tempFile = Path.GetTempFileName();

        try
        {
            using var conn = new SqliteConnection($"Data Source={tempFile}");
            await conn.OpenAsync(ct);

            await CreateSchemaAsync(conn, ct);
            await WriteDataToSqliteAsync(conn, data, ct);

            logger.ExportCompleted("sqlite", data.Wallets.Count, data.Transactions.Count);

            // Read the file into memory to allow cleanup before streaming
            var fileBytes = await File.ReadAllBytesAsync(tempFile, ct);
            var memStream = new MemoryStream(fileBytes);
            return Result<IResult>.Success(Results.Stream(memStream, "application/x-sqlite3", "kakeibo-export.db"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static async Task CreateSchemaAsync(SqliteConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS wallets (
                id TEXT PRIMARY KEY, name TEXT NOT NULL, type INTEGER NOT NULL,
                currency TEXT NOT NULL, icon TEXT, background_color TEXT, text_color TEXT,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS wallet_balances (
                wallet_id TEXT PRIMARY KEY, balance REAL NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS wallet_members (
                id TEXT PRIMARY KEY, wallet_id TEXT NOT NULL, user_id TEXT NOT NULL, created_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS categories (
                id TEXT PRIMARY KEY, name TEXT NOT NULL, user_id TEXT, created_at TEXT NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS transactions (
                id TEXT PRIMARY KEY, type INTEGER NOT NULL, amount REAL NOT NULL, description TEXT NOT NULL,
                date TEXT NOT NULL, category_id TEXT NOT NULL, wallet_id TEXT NOT NULL,
                destination_wallet_id TEXT, user_id TEXT NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS budgets (
                id TEXT PRIMARY KEY, user_id TEXT NOT NULL, category_id TEXT NOT NULL, name TEXT NOT NULL,
                "limit" REAL NOT NULL, start_date TEXT NOT NULL, end_date TEXT NOT NULL,
                wallet_id TEXT, current_spending REAL NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS goals (
                id TEXT PRIMARY KEY, user_id TEXT NOT NULL, name TEXT NOT NULL, target_amount REAL NOT NULL,
                deadline TEXT, wallet_id TEXT NOT NULL, current_progress REAL NOT NULL,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS recurring_patterns (
                id TEXT PRIMARY KEY, user_id TEXT NOT NULL, name TEXT NOT NULL, amount REAL NOT NULL,
                description TEXT NOT NULL, transaction_type INTEGER NOT NULL, frequency INTEGER NOT NULL,
                category_id TEXT NOT NULL, wallet_id TEXT NOT NULL, destination_wallet_id TEXT,
                start_date TEXT NOT NULL, end_date TEXT, next_occurrence TEXT NOT NULL,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL);

            CREATE TABLE IF NOT EXISTS settlements (
                id TEXT PRIMARY KEY, wallet_id TEXT NOT NULL, from_user_id TEXT NOT NULL,
                to_user_id TEXT NOT NULL, amount REAL NOT NULL, notes TEXT,
                created_at TEXT NOT NULL, updated_at TEXT NOT NULL);
            """;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task WriteDataToSqliteAsync(SqliteConnection conn, UserExportData data, CancellationToken ct)
    {
        // SqliteConnection.BeginTransaction() returns SqliteTransaction (sync, non-blocking for in-process SQLite)
        using var tx = conn.BeginTransaction();

        // Wallets
        foreach (var w in data.Wallets)
        {
            await ExecAsync(conn, tx,
                "INSERT INTO wallets VALUES (@id,@name,@type,@currency,@icon,@bg,@fg,@ca,@ua)",
                [("@id", w.Id), ("@name", w.Name), ("@type", (int)w.Type),
                 ("@currency", w.Currency), ("@icon", (object?)w.Icon ?? DBNull.Value),
                 ("@bg", (object?)w.BackgroundColor ?? DBNull.Value), ("@fg", (object?)w.TextColor ?? DBNull.Value),
                 ("@ca", w.CreatedAt.ToString()), ("@ua", w.UpdatedAt.ToString())], ct);
        }

        // WalletBalances
        foreach (var wb in data.WalletBalances)
        {
            await ExecAsync(conn, tx, "INSERT INTO wallet_balances VALUES (@wid,@bal,@ua)",
                [("@wid", wb.WalletId), ("@bal", wb.Balance), ("@ua", wb.UpdatedAt.ToString())], ct);
        }

        // WalletMembers
        foreach (var wm in data.WalletMembers)
        {
            await ExecAsync(conn, tx, "INSERT INTO wallet_members VALUES (@id,@wid,@uid,@ca)",
                [("@id", wm.Id), ("@wid", wm.WalletId), ("@uid", wm.UserId), ("@ca", wm.CreatedAt.ToString())], ct);
        }

        // Categories
        foreach (var c in data.Categories)
        {
            await ExecAsync(conn, tx, "INSERT INTO categories VALUES (@id,@name,@uid,@ca,@ua)",
                [("@id", c.Id), ("@name", c.Name), ("@uid", (object?)c.UserId ?? DBNull.Value),
                 ("@ca", c.CreatedAt.ToString()), ("@ua", c.UpdatedAt.ToString())], ct);
        }

        // Transactions
        foreach (var t in data.Transactions)
        {
            await ExecAsync(conn, tx,
                "INSERT INTO transactions VALUES (@id,@type,@amount,@desc,@date,@cat,@wid,@dest,@uid,@ca,@ua)",
                [("@id", t.Id), ("@type", (int)t.Type), ("@amount", t.Amount), ("@desc", t.Description),
                 ("@date", LocalDatePattern.Iso.Format(t.Date)), ("@cat", t.CategoryId), ("@wid", t.WalletId),
                 ("@dest", (object?)t.DestinationWalletId ?? DBNull.Value), ("@uid", t.UserId),
                 ("@ca", t.CreatedAt.ToString()), ("@ua", t.UpdatedAt.ToString())], ct);
        }

        // Budgets
        foreach (var b in data.Budgets)
        {
            await ExecAsync(conn, tx,
                "INSERT INTO budgets VALUES (@id,@uid,@cat,@name,@limit,@sd,@ed,@wid,@sp,@ca,@ua)",
                [("@id", b.Id), ("@uid", b.UserId), ("@cat", b.CategoryId), ("@name", b.Name),
                 ("@limit", b.Limit), ("@sd", LocalDatePattern.Iso.Format(b.StartDate)),
                 ("@ed", LocalDatePattern.Iso.Format(b.EndDate)),
                 ("@wid", (object?)b.WalletId ?? DBNull.Value), ("@sp", b.CurrentSpending),
                 ("@ca", b.CreatedAt.ToString()), ("@ua", b.UpdatedAt.ToString())], ct);
        }

        // Goals
        foreach (var g in data.Goals)
        {
            await ExecAsync(conn, tx,
                "INSERT INTO goals VALUES (@id,@uid,@name,@target,@deadline,@wid,@progress,@ca,@ua)",
                [("@id", g.Id), ("@uid", g.UserId), ("@name", g.Name), ("@target", g.TargetAmount),
                 ("@deadline", g.Deadline.HasValue ? LocalDatePattern.Iso.Format(g.Deadline.Value) : (object)DBNull.Value),
                 ("@wid", g.WalletId), ("@progress", g.CurrentProgress),
                 ("@ca", g.CreatedAt.ToString()), ("@ua", g.UpdatedAt.ToString())], ct);
        }

        // RecurringPatterns
        foreach (var rp in data.RecurringPatterns)
        {
            await ExecAsync(conn, tx,
                "INSERT INTO recurring_patterns VALUES (@id,@uid,@name,@amount,@desc,@txtype,@freq,@cat,@wid,@dest,@sd,@ed,@next,@ca,@ua)",
                [("@id", rp.Id), ("@uid", rp.UserId), ("@name", rp.Name), ("@amount", rp.Amount),
                 ("@desc", rp.Description), ("@txtype", (int)rp.TransactionType), ("@freq", (int)rp.Frequency),
                 ("@cat", rp.CategoryId), ("@wid", rp.WalletId),
                 ("@dest", (object?)rp.DestinationWalletId ?? DBNull.Value),
                 ("@sd", LocalDatePattern.Iso.Format(rp.StartDate)),
                 ("@ed", rp.EndDate.HasValue ? LocalDatePattern.Iso.Format(rp.EndDate.Value) : (object)DBNull.Value),
                 ("@next", LocalDatePattern.Iso.Format(rp.NextOccurrence)),
                 ("@ca", rp.CreatedAt.ToString()), ("@ua", rp.UpdatedAt.ToString())], ct);
        }

        // Settlements
        foreach (var s in data.Settlements)
        {
            await ExecAsync(conn, tx,
                "INSERT INTO settlements VALUES (@id,@wid,@from,@to,@amount,@notes,@ca,@ua)",
                [("@id", s.Id), ("@wid", s.WalletId), ("@from", s.FromUserId), ("@to", s.ToUserId),
                 ("@amount", s.Amount), ("@notes", (object?)s.Notes ?? DBNull.Value),
                 ("@ca", s.CreatedAt.ToString()), ("@ua", s.UpdatedAt.ToString())], ct);
        }

        tx.Commit();
    }

    private static async Task ExecAsync(SqliteConnection conn, SqliteTransaction tx, string sql,
        (string Name, object Value)[] parameters, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // --- CSV ZIP export ---

    private async Task<Result<IResult>> ExportCsvZipAsync(Guid userId, CancellationToken ct)
    {
        var data = await LoadUserDataAsync(userId, ct);
        var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteCsvEntry(archive, "wallets.csv", data.Wallets.Select(w => new
            {
                w.Id, w.Name, Type = w.Type.ToString(),
                w.Currency, Icon = w.Icon ?? string.Empty,
                BackgroundColor = w.BackgroundColor ?? string.Empty,
                TextColor = w.TextColor ?? string.Empty,
                CreatedAt = w.CreatedAt.ToString(), UpdatedAt = w.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "wallet_balances.csv", data.WalletBalances.Select(wb => new
            {
                wb.WalletId, wb.Balance, UpdatedAt = wb.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "wallet_members.csv", data.WalletMembers.Select(wm => new
            {
                wm.Id, wm.WalletId, wm.UserId, CreatedAt = wm.CreatedAt.ToString()
            }));

            WriteCsvEntry(archive, "categories.csv", data.Categories.Select(c => new
            {
                c.Id, c.Name, UserId = c.UserId?.ToString() ?? string.Empty,
                CreatedAt = c.CreatedAt.ToString(), UpdatedAt = c.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "transactions.csv", data.Transactions.Select(t => new
            {
                t.Id, Type = t.Type.ToString(), t.Amount, t.Description,
                Date = LocalDatePattern.Iso.Format(t.Date),
                t.CategoryId, t.WalletId,
                DestinationWalletId = t.DestinationWalletId?.ToString() ?? string.Empty,
                t.UserId, CreatedAt = t.CreatedAt.ToString(), UpdatedAt = t.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "budgets.csv", data.Budgets.Select(b => new
            {
                b.Id, b.UserId, b.CategoryId, b.Name, b.Limit,
                StartDate = LocalDatePattern.Iso.Format(b.StartDate),
                EndDate = LocalDatePattern.Iso.Format(b.EndDate),
                WalletId = b.WalletId?.ToString() ?? string.Empty,
                b.CurrentSpending, CreatedAt = b.CreatedAt.ToString(), UpdatedAt = b.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "goals.csv", data.Goals.Select(g => new
            {
                g.Id, g.UserId, g.Name, g.TargetAmount,
                Deadline = g.Deadline.HasValue ? LocalDatePattern.Iso.Format(g.Deadline.Value) : string.Empty,
                g.WalletId, g.CurrentProgress,
                CreatedAt = g.CreatedAt.ToString(), UpdatedAt = g.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "recurring_patterns.csv", data.RecurringPatterns.Select(rp => new
            {
                rp.Id, rp.UserId, rp.Name, rp.Amount, rp.Description,
                TransactionType = rp.TransactionType.ToString(), Frequency = rp.Frequency.ToString(),
                rp.CategoryId, rp.WalletId,
                DestinationWalletId = rp.DestinationWalletId?.ToString() ?? string.Empty,
                StartDate = LocalDatePattern.Iso.Format(rp.StartDate),
                EndDate = rp.EndDate.HasValue ? LocalDatePattern.Iso.Format(rp.EndDate.Value) : string.Empty,
                NextOccurrence = LocalDatePattern.Iso.Format(rp.NextOccurrence),
                CreatedAt = rp.CreatedAt.ToString(), UpdatedAt = rp.UpdatedAt.ToString()
            }));

            WriteCsvEntry(archive, "settlements.csv", data.Settlements.Select(s => new
            {
                s.Id, s.WalletId, s.FromUserId, s.ToUserId, s.Amount,
                Notes = s.Notes ?? string.Empty,
                CreatedAt = s.CreatedAt.ToString(), UpdatedAt = s.UpdatedAt.ToString()
            }));
        }

        logger.ExportCompleted("csv", data.Wallets.Count, data.Transactions.Count);

        zipStream.Seek(0, SeekOrigin.Begin);
        return Result<IResult>.Success(Results.Stream(zipStream, "application/zip", "kakeibo-export.zip"));
    }

    private static void WriteCsvEntry<T>(ZipArchive archive, string entryName, IEnumerable<T> records)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, leaveOpen: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(records);
    }

    // --- Data loading ---

    private async Task<UserExportData> LoadUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Load all wallets the user is a member of (ownership is now tracked via WalletMemberRole.Owner)
        var allWalletIds = await db.WalletMembers
            .Where(wm => wm.UserId == userId)
            .Select(wm => wm.WalletId)
            .ToHashSetAsync(ct);

        var wallets = await db.Wallets
            .Where(w => allWalletIds.Contains(w.Id) && w.DeletedAt == null)
            .ToListAsync(ct);

        var walletBalances = await db.WalletBalances
            .Where(wb => allWalletIds.Contains(wb.WalletId))
            .ToListAsync(ct);

        var walletMembers = await db.WalletMembers
            .Where(wm => allWalletIds.Contains(wm.WalletId) && wm.DeletedAt == null)
            .ToListAsync(ct);

        // Custom categories owned by the user
        var categories = await db.Categories
            .Where(c => c.UserId == userId && c.DeletedAt == null)
            .ToListAsync(ct);

        var transactions = await db.Transactions
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .ToListAsync(ct);

        var budgets = await db.Budgets
            .Where(b => b.UserId == userId && b.DeletedAt == null)
            .ToListAsync(ct);

        var goals = await db.Goals
            .Where(g => g.UserId == userId && g.DeletedAt == null)
            .ToListAsync(ct);

        var recurringPatterns = await db.RecurringPatterns
            .Where(rp => rp.UserId == userId && rp.DeletedAt == null)
            .ToListAsync(ct);

        var settlements = await db.Settlements
            .Where(s => allWalletIds.Contains(s.WalletId) && s.DeletedAt == null)
            .ToListAsync(ct);

        return new UserExportData(wallets, walletBalances, walletMembers, categories,
            transactions, budgets, goals, recurringPatterns, settlements);
    }
}
