using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.Data.Sqlite;
using NodaTime;
using NodaTime.Text;

namespace Kakeibo.Api.Features.Identity.ImportData;

// Imports data from a Kakeibo SQLite (.db) or CSV ZIP (.zip) backup.
// Additive import: creates new records without deleting existing data.
// All IDs are regenerated with Guid7 to avoid collisions with existing data.
public sealed class KakeiboImporter(AppDbContext db, IClock clock) : IDataImporter
{
    public string Source => "kakeibo";

    // Magic byte sequences for format detection
    private static readonly byte[] SqliteMagic = [0x53, 0x51, 0x4C, 0x69]; // "SQLi"
    private static readonly byte[] ZipMagic    = [0x50, 0x4B, 0x03, 0x04]; // "PK\x03\x04"

    public async Task<Result<ImportResult>> ImportAsync(Stream fileStream, string fileName, Guid userId, CancellationToken ct)
    {
        // Detect format from magic bytes
        var magic = new byte[4];
        var read = await fileStream.ReadAsync(magic.AsMemory(0, 4), ct);
        fileStream.Seek(0, SeekOrigin.Begin);

        if (read < 4)
            return Error.Validation("File is too small to be a valid Kakeibo backup.");

        if (magic.SequenceEqual(SqliteMagic))
            return await ImportSqliteAsync(fileStream, userId, ct);

        if (magic.SequenceEqual(ZipMagic))
            return await ImportCsvZipAsync(fileStream, userId, ct);

        return Error.Validation("File format not recognized. Expected a .db (SQLite) or .zip (CSV) Kakeibo backup.");
    }

    // --- SQLite path ---

    private async Task<Result<ImportResult>> ImportSqliteAsync(Stream fileStream, Guid userId, CancellationToken ct)
    {
        // Copy stream to a temp file so SQLite can open it by path
        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var tmpOut = File.OpenWrite(tempFile))
                await fileStream.CopyToAsync(tmpOut, ct);

            using var conn = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly");
            await conn.OpenAsync(ct);

            // Validate it is a Kakeibo backup (has 'wallets' table, not OneMoney's 'ba')
            if (!await TableExistsAsync(conn, "wallets", ct))
                return Error.Validation("The SQLite file does not appear to be a Kakeibo backup (missing 'wallets' table).");

            return await ImportFromSqliteConnectionAsync(conn, userId, ct);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private async Task<Result<ImportResult>> ImportFromSqliteConnectionAsync(SqliteConnection conn, Guid userId, CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();
        var systemCategoryIds = GetSystemCategoryIds();

        // Maps old IDs (from backup) → new IDs (Guid7)
        var walletIdMap   = new Dictionary<Guid, Guid>();
        var categoryIdMap = new Dictionary<Guid, Guid>();

        var skippedMembers = 0;
        var counts = new { Wallets = 0, Categories = 0, Transactions = 0, Budgets = 0, Goals = 0, Recurring = 0, Settlements = 0 };

        // --- 1. Categories (custom only) ---
        var catCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name FROM categories WHERE user_id IS NOT NULL";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldId = reader.GetGuid(0);
                var newId = Guid7.NewGuid();
                categoryIdMap[oldId] = newId;

                db.Categories.Add(new Category
                {
                    Id = newId,
                    Name = reader.GetString(1),
                    UserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                catCount++;
            }
        }

        // --- 2. Wallets (import as Personal for the importing user) ---
        var walletCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name, currency, icon, background_color, text_color FROM wallets";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldId = reader.GetGuid(0);
                var newId = Guid7.NewGuid();
                walletIdMap[oldId] = newId;

                db.Wallets.Add(new Wallet
                {
                    Id = newId,
                    Name = reader.GetString(1),
                    Type = WalletType.Personal,
                    OwnerId = userId,
                    Currency = reader.IsDBNull(2) ? "EUR" : reader.GetString(2),
                    Icon = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BackgroundColor = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TextColor = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                walletCount++;
            }
        }

        // --- 3. WalletBalances ---
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT wallet_id, balance FROM wallet_balances";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldWalletId = reader.GetGuid(0);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                db.WalletBalances.Add(new WalletBalance
                {
                    WalletId = newWalletId,
                    Balance = reader.GetDecimal(1),
                    UpdatedAt = now
                });
            }
        }

        // --- 4. WalletMembers (only those belonging to the importing user; skip foreign members) ---
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT wallet_id, user_id FROM wallet_members";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldWalletId = reader.GetGuid(0);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId))
                {
                    skippedMembers++;
                    continue;
                }
                // Members that aren't the importing user are skipped
                skippedMembers++;
            }
        }

        // --- 5. Transactions ---
        var txCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, type, amount, description, date, category_id, wallet_id, destination_wallet_id FROM transactions WHERE deleted_at IS NULL";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldWalletId = reader.GetGuid(6);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                var oldCategoryId = reader.GetGuid(5);
                // Resolve to new category ID; fall back to system category if it was a system category
                if (!categoryIdMap.TryGetValue(oldCategoryId, out var newCategoryId))
                {
                    // Keep system category IDs as-is (they are stable across installations)
                    newCategoryId = systemCategoryIds.Contains(oldCategoryId) ? oldCategoryId : systemCategoryIds[^1];
                }

                Guid? newDestWalletId = null;
                if (!reader.IsDBNull(7))
                {
                    var oldDest = reader.GetGuid(7);
                    walletIdMap.TryGetValue(oldDest, out var mapped);
                    newDestWalletId = mapped == Guid.Empty ? null : mapped;
                }

                var dateStr = reader.GetString(4);
                var date = LocalDatePattern.Iso.Parse(dateStr).Value;

                db.Transactions.Add(new Transaction
                {
                    Id = Guid7.NewGuid(),
                    Type = (TransactionType)reader.GetInt32(1),
                    Amount = reader.GetDecimal(2),
                    Description = reader.GetString(3),
                    Date = date,
                    CategoryId = newCategoryId,
                    WalletId = newWalletId,
                    DestinationWalletId = newDestWalletId,
                    UserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                txCount++;
            }
        }

        // --- 6. Budgets ---
        var budgetCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, category_id, name, \"limit\", start_date, end_date, wallet_id, current_spending FROM budgets";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldCategoryId = reader.GetGuid(1);
                if (!categoryIdMap.TryGetValue(oldCategoryId, out var newCategoryId))
                    newCategoryId = systemCategoryIds.Contains(oldCategoryId) ? oldCategoryId : systemCategoryIds[^1];

                Guid? newWalletId = null;
                if (!reader.IsDBNull(6))
                {
                    var oldWid = reader.GetGuid(6);
                    if (walletIdMap.TryGetValue(oldWid, out var mapped)) newWalletId = mapped;
                }

                db.Budgets.Add(new Budget
                {
                    Id = Guid7.NewGuid(),
                    UserId = userId,
                    CategoryId = newCategoryId,
                    Name = reader.GetString(2),
                    Limit = reader.GetDecimal(3),
                    StartDate = LocalDatePattern.Iso.Parse(reader.GetString(4)).Value,
                    EndDate = LocalDatePattern.Iso.Parse(reader.GetString(5)).Value,
                    WalletId = newWalletId,
                    CurrentSpending = reader.GetDecimal(7),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                budgetCount++;
            }
        }

        // --- 7. Goals ---
        var goalCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name, target_amount, deadline, wallet_id, current_progress FROM goals";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldWalletId = reader.GetGuid(4);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                LocalDate? deadline = null;
                if (!reader.IsDBNull(3))
                    deadline = LocalDatePattern.Iso.Parse(reader.GetString(3)).Value;

                db.Goals.Add(new Goal
                {
                    Id = Guid7.NewGuid(),
                    UserId = userId,
                    Name = reader.GetString(1),
                    TargetAmount = reader.GetDecimal(2),
                    Deadline = deadline,
                    WalletId = newWalletId,
                    CurrentProgress = reader.GetDecimal(5),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                goalCount++;
            }
        }

        // --- 8. RecurringPatterns ---
        var recurringCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name, amount, description, transaction_type, frequency, category_id, wallet_id, destination_wallet_id, start_date, end_date, next_occurrence FROM recurring_patterns WHERE deleted_at IS NULL";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldWalletId = reader.GetGuid(7);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                var oldCategoryId = reader.GetGuid(6);
                if (!categoryIdMap.TryGetValue(oldCategoryId, out var newCategoryId))
                    newCategoryId = systemCategoryIds.Contains(oldCategoryId) ? oldCategoryId : systemCategoryIds[^1];

                Guid? newDestWalletId = null;
                if (!reader.IsDBNull(8))
                {
                    var oldDest = reader.GetGuid(8);
                    if (walletIdMap.TryGetValue(oldDest, out var mapped)) newDestWalletId = mapped;
                }

                LocalDate? endDate = null;
                if (!reader.IsDBNull(10))
                    endDate = LocalDatePattern.Iso.Parse(reader.GetString(10)).Value;

                var startDate = LocalDatePattern.Iso.Parse(reader.GetString(9)).Value;
                var nextOcc = LocalDatePattern.Iso.Parse(reader.GetString(11)).Value;

                db.RecurringPatterns.Add(new RecurringPattern
                {
                    Id = Guid7.NewGuid(),
                    UserId = userId,
                    Name = reader.GetString(1),
                    Amount = reader.GetDecimal(2),
                    Description = reader.GetString(3),
                    TransactionType = (TransactionType)reader.GetInt32(4),
                    Frequency = (RecurrenceFrequency)reader.GetInt32(5),
                    CategoryId = newCategoryId,
                    WalletId = newWalletId,
                    DestinationWalletId = newDestWalletId,
                    StartDate = startDate,
                    EndDate = endDate,
                    NextOccurrence = nextOcc,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                recurringCount++;
            }
        }

        // --- 9. Settlements ---
        var settlementCount = 0;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, wallet_id, amount, notes FROM settlements";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var oldWalletId = reader.GetGuid(1);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                // Settlements reference other users (from/to) — on import, both sides map to the importing user
                db.Settlements.Add(new Settlement
                {
                    Id = Guid7.NewGuid(),
                    WalletId = newWalletId,
                    FromUserId = userId,
                    ToUserId = userId,
                    Amount = reader.GetDecimal(2),
                    Notes = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                settlementCount++;
            }
        }

        await db.SaveChangesAsync(ct);

        return new ImportResult(
            ImportedAt: now,
            Source: "kakeibo",
            Format: "sqlite",
            Counts: new ImportCounts(walletCount, catCount, txCount, budgetCount, goalCount, recurringCount, settlementCount),
            Skipped: new ImportSkipped(skippedMembers));
    }

    // --- CSV ZIP path ---

    private async Task<Result<ImportResult>> ImportCsvZipAsync(Stream fileStream, Guid userId, CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();
        var systemCategoryIds = GetSystemCategoryIds();

        var walletIdMap   = new Dictionary<Guid, Guid>();
        var categoryIdMap = new Dictionary<Guid, Guid>();
        var skippedMembers = 0;
        var catCount = 0; var walletCount = 0; var txCount = 0;
        var budgetCount = 0; var goalCount = 0; var recurringCount = 0; var settlementCount = 0;

        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

        // Helper: read a CSV entry by name
        ZipArchiveEntry? GetEntry(string name) => archive.GetEntry(name);

        // --- 1. Categories ---
        var catEntry = GetEntry("categories.csv");
        if (catEntry is not null)
        {
            await using var entryStream = catEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                // Only import custom categories (UserId column is non-empty)
                var userIdStr = csv.GetField("UserId");
                if (string.IsNullOrEmpty(userIdStr)) continue;

                var oldId = Guid.Parse(csv.GetField("Id")!);
                var newId = Guid7.NewGuid();
                categoryIdMap[oldId] = newId;

                db.Categories.Add(new Category
                {
                    Id = newId,
                    Name = csv.GetField("Name")!,
                    UserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                catCount++;
            }
        }

        // --- 2. Wallets ---
        var walletEntry = GetEntry("wallets.csv");
        if (walletEntry is not null)
        {
            await using var entryStream = walletEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldId = Guid.Parse(csv.GetField("Id")!);
                var newId = Guid7.NewGuid();
                walletIdMap[oldId] = newId;

                db.Wallets.Add(new Wallet
                {
                    Id = newId,
                    Name = csv.GetField("Name")!,
                    Type = WalletType.Personal,
                    OwnerId = userId,
                    Currency = csv.GetField("Currency") ?? "EUR",
                    Icon = NullIfEmpty(csv.GetField("Icon")),
                    BackgroundColor = NullIfEmpty(csv.GetField("BackgroundColor")),
                    TextColor = NullIfEmpty(csv.GetField("TextColor")),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                walletCount++;
            }
        }

        // --- 3. WalletBalances ---
        var balanceEntry = GetEntry("wallet_balances.csv");
        if (balanceEntry is not null)
        {
            await using var entryStream = balanceEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldWalletId = Guid.Parse(csv.GetField("WalletId")!);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                db.WalletBalances.Add(new WalletBalance
                {
                    WalletId = newWalletId,
                    Balance = decimal.Parse(csv.GetField("Balance")!, CultureInfo.InvariantCulture),
                    UpdatedAt = now
                });
            }
        }

        // WalletMembers skipped (foreign users)
        var membersEntry = GetEntry("wallet_members.csv");
        if (membersEntry is not null)
        {
            await using var entryStream = membersEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync()) skippedMembers++;
        }

        // --- 5. Transactions ---
        var txEntry = GetEntry("transactions.csv");
        if (txEntry is not null)
        {
            await using var entryStream = txEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldWalletId = Guid.Parse(csv.GetField("WalletId")!);
                if (!walletIdMap.TryGetValue(oldWalletId, out var newWalletId)) continue;

                var oldCategoryId = Guid.Parse(csv.GetField("CategoryId")!);
                if (!categoryIdMap.TryGetValue(oldCategoryId, out var newCategoryId))
                    newCategoryId = systemCategoryIds.Contains(oldCategoryId) ? oldCategoryId : systemCategoryIds[^1];

                Guid? newDestWalletId = null;
                var destStr = csv.GetField("DestinationWalletId");
                if (!string.IsNullOrEmpty(destStr) && Guid.TryParse(destStr, out var oldDest))
                    if (walletIdMap.TryGetValue(oldDest, out var mappedDest)) newDestWalletId = mappedDest;

                db.Transactions.Add(new Transaction
                {
                    Id = Guid7.NewGuid(),
                    Type = Enum.Parse<TransactionType>(csv.GetField("Type")!),
                    Amount = decimal.Parse(csv.GetField("Amount")!, CultureInfo.InvariantCulture),
                    Description = csv.GetField("Description") ?? string.Empty,
                    Date = LocalDatePattern.Iso.Parse(csv.GetField("Date")!).Value,
                    CategoryId = newCategoryId,
                    WalletId = newWalletId,
                    DestinationWalletId = newDestWalletId,
                    UserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                txCount++;
            }
        }

        // --- 6. Budgets ---
        var budgetEntry = GetEntry("budgets.csv");
        if (budgetEntry is not null)
        {
            await using var entryStream = budgetEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldCatId = Guid.Parse(csv.GetField("CategoryId")!);
                if (!categoryIdMap.TryGetValue(oldCatId, out var newCatId))
                    newCatId = systemCategoryIds.Contains(oldCatId) ? oldCatId : systemCategoryIds[^1];

                Guid? newWalletId = null;
                var wStr = csv.GetField("WalletId");
                if (!string.IsNullOrEmpty(wStr) && Guid.TryParse(wStr, out var oldWid))
                    if (walletIdMap.TryGetValue(oldWid, out var mapped)) newWalletId = mapped;

                db.Budgets.Add(new Budget
                {
                    Id = Guid7.NewGuid(),
                    UserId = userId,
                    CategoryId = newCatId,
                    Name = csv.GetField("Name")!,
                    Limit = decimal.Parse(csv.GetField("Limit")!, CultureInfo.InvariantCulture),
                    StartDate = LocalDatePattern.Iso.Parse(csv.GetField("StartDate")!).Value,
                    EndDate = LocalDatePattern.Iso.Parse(csv.GetField("EndDate")!).Value,
                    WalletId = newWalletId,
                    CurrentSpending = decimal.Parse(csv.GetField("CurrentSpending") ?? "0", CultureInfo.InvariantCulture),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                budgetCount++;
            }
        }

        // --- 7. Goals ---
        var goalEntry = GetEntry("goals.csv");
        if (goalEntry is not null)
        {
            await using var entryStream = goalEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldWid = Guid.Parse(csv.GetField("WalletId")!);
                if (!walletIdMap.TryGetValue(oldWid, out var newWalletId)) continue;

                LocalDate? deadline = null;
                var dlStr = csv.GetField("Deadline");
                if (!string.IsNullOrEmpty(dlStr)) deadline = LocalDatePattern.Iso.Parse(dlStr).Value;

                db.Goals.Add(new Goal
                {
                    Id = Guid7.NewGuid(),
                    UserId = userId,
                    Name = csv.GetField("Name")!,
                    TargetAmount = decimal.Parse(csv.GetField("TargetAmount")!, CultureInfo.InvariantCulture),
                    Deadline = deadline,
                    WalletId = newWalletId,
                    CurrentProgress = decimal.Parse(csv.GetField("CurrentProgress") ?? "0", CultureInfo.InvariantCulture),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                goalCount++;
            }
        }

        // --- 8. RecurringPatterns ---
        var recurringEntry = GetEntry("recurring_patterns.csv");
        if (recurringEntry is not null)
        {
            await using var entryStream = recurringEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldWid = Guid.Parse(csv.GetField("WalletId")!);
                if (!walletIdMap.TryGetValue(oldWid, out var newWalletId)) continue;

                var oldCatId = Guid.Parse(csv.GetField("CategoryId")!);
                if (!categoryIdMap.TryGetValue(oldCatId, out var newCatId))
                    newCatId = systemCategoryIds.Contains(oldCatId) ? oldCatId : systemCategoryIds[^1];

                Guid? newDestWalletId = null;
                var destStr = csv.GetField("DestinationWalletId");
                if (!string.IsNullOrEmpty(destStr) && Guid.TryParse(destStr, out var oldDest))
                    if (walletIdMap.TryGetValue(oldDest, out var mapped)) newDestWalletId = mapped;

                LocalDate? endDate = null;
                var edStr = csv.GetField("EndDate");
                if (!string.IsNullOrEmpty(edStr)) endDate = LocalDatePattern.Iso.Parse(edStr).Value;

                db.RecurringPatterns.Add(new RecurringPattern
                {
                    Id = Guid7.NewGuid(),
                    UserId = userId,
                    Name = csv.GetField("Name")!,
                    Amount = decimal.Parse(csv.GetField("Amount")!, CultureInfo.InvariantCulture),
                    Description = csv.GetField("Description") ?? string.Empty,
                    TransactionType = Enum.Parse<TransactionType>(csv.GetField("TransactionType")!),
                    Frequency = Enum.Parse<RecurrenceFrequency>(csv.GetField("Frequency")!),
                    CategoryId = newCatId,
                    WalletId = newWalletId,
                    DestinationWalletId = newDestWalletId,
                    StartDate = LocalDatePattern.Iso.Parse(csv.GetField("StartDate")!).Value,
                    EndDate = endDate,
                    NextOccurrence = LocalDatePattern.Iso.Parse(csv.GetField("NextOccurrence")!).Value,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                recurringCount++;
            }
        }

        // --- 9. Settlements ---
        var settlementEntry = GetEntry("settlements.csv");
        if (settlementEntry is not null)
        {
            await using var entryStream = settlementEntry.Open();
            using var reader = new StreamReader(entryStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.ReadAsync();
            csv.ReadHeader();
            while (await csv.ReadAsync())
            {
                var oldWid = Guid.Parse(csv.GetField("WalletId")!);
                if (!walletIdMap.TryGetValue(oldWid, out var newWalletId)) continue;

                db.Settlements.Add(new Settlement
                {
                    Id = Guid7.NewGuid(),
                    WalletId = newWalletId,
                    FromUserId = userId,
                    ToUserId = userId,
                    Amount = decimal.Parse(csv.GetField("Amount")!, CultureInfo.InvariantCulture),
                    Notes = NullIfEmpty(csv.GetField("Notes")),
                    CreatedAt = now,
                    UpdatedAt = now
                });
                settlementCount++;
            }
        }

        await db.SaveChangesAsync(ct);

        return new ImportResult(
            ImportedAt: now,
            Source: "kakeibo",
            Format: "csv",
            Counts: new ImportCounts(walletCount, catCount, txCount, budgetCount, goalCount, recurringCount, settlementCount),
            Skipped: new ImportSkipped(skippedMembers));
    }

    // --- Helpers ---

    private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result) > 0;
    }

    // System category IDs seeded by CategoryConfiguration
    private static Guid[] GetSystemCategoryIds() =>
    [
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        Guid.Parse("10000000-0000-0000-0000-000000000002"),
        Guid.Parse("10000000-0000-0000-0000-000000000003"),
        Guid.Parse("10000000-0000-0000-0000-000000000004"),
        Guid.Parse("10000000-0000-0000-0000-000000000005"),
        Guid.Parse("10000000-0000-0000-0000-000000000006"),
        Guid.Parse("10000000-0000-0000-0000-000000000007"),
        Guid.Parse("10000000-0000-0000-0000-000000000008"),
        Guid.Parse("10000000-0000-0000-0000-000000000009"),
        Guid.Parse("10000000-0000-0000-0000-00000000000a"),
        Guid.Parse("10000000-0000-0000-0000-00000000000b"),
        Guid.Parse("10000000-0000-0000-0000-00000000000c"), // "Other" — last element used as fallback
    ];

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
