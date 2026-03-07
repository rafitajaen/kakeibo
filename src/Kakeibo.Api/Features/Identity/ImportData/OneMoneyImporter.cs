using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Persistence;
using Microsoft.Data.Sqlite;
using NodaTime;

namespace Kakeibo.Api.Features.Identity.ImportData;

// Imports data from a OneMoney (Android "Money Manager") SQLite backup.
// Schema uses tables: ba (books), de (accounts + categories), tr (transactions).
// Only the primary active book (_ty=1) is imported.
public sealed class OneMoneyImporter(AppDbContext db, IClock clock) : IDataImporter
{
    public string Source => "onemoney";

    // Magic bytes for SQLite format detection
    private static readonly byte[] SqliteMagic = [0x53, 0x51, 0x4C, 0x69]; // "SQLi"

    // OneMoney transaction type constants
    private const int OneMoneyExpense = 0;
    private const int OneMoneyIncome = 1;

    // OneMoney entity type (_ty) constants
    private const int DeTypeAccountOrIncome = 0;
    private const int DeTypeExpenseCategory = 1;
    private const int DeTypeVirtualAggregate = 4;

    public async Task<Result<ImportResult>> ImportAsync(Stream fileStream, string fileName, Guid userId, CancellationToken ct)
    {
        // Validate it is a SQLite file
        var magic = new byte[4];
        var read = await fileStream.ReadAsync(magic.AsMemory(0, 4), ct);
        fileStream.Seek(0, SeekOrigin.Begin);

        if (read < 4 || !magic.SequenceEqual(SqliteMagic))
            return Error.Validation("OneMoney backup must be a SQLite file (.backup or .db).");

        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var tmpOut = File.OpenWrite(tempFile))
                await fileStream.CopyToAsync(tmpOut, ct);

            using var conn = new SqliteConnection($"Data Source={tempFile};Mode=ReadOnly");
            await conn.OpenAsync(ct);

            // Verify it is a OneMoney backup (has 'ba' table, not Kakeibo's 'wallets')
            if (!await TableExistsAsync(conn, "ba", ct))
                return Error.Validation("The SQLite file does not appear to be a OneMoney backup (missing 'ba' table).");

            return await ImportFromConnectionAsync(conn, userId, ct);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private async Task<Result<ImportResult>> ImportFromConnectionAsync(SqliteConnection conn, Guid userId, CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();

        // Step 1 — Find the primary active book
        long primaryBookId;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT _id FROM ba WHERE _ty = 1 LIMIT 1";
            var result = await cmd.ExecuteScalarAsync(ct);
            if (result is null or DBNull)
                return Error.Validation("No active book found in OneMoney backup.");

            primaryBookId = Convert.ToInt64(result);
        }

        // Step 2 — Classify 'de' entries:
        // Collect IDs referenced as wallet accounts (_a_i) and category (_d_i) in transactions
        var walletDeIds   = new HashSet<long>();
        var categoryDeIds = new HashSet<long>();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT _a_i, _d_i, _ty FROM tr WHERE _b_i = @bookId";
            cmd.Parameters.AddWithValue("@bookId", primaryBookId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var accountId = reader.GetInt64(0);
                var categoryId = reader.GetInt64(1);
                var txType = reader.GetInt32(2);

                // _a_i is always the wallet/account
                walletDeIds.Add(accountId);

                // _d_i on expense is an expense category; on income it can be an income type
                categoryDeIds.Add(categoryId);
            }
        }

        // Step 3 — Load 'de' entries and classify
        // de._ty: 0 = Account or income type, 1 = Expense category, 4 = Virtual aggregate (skip)
        var deEntities = new Dictionary<long, (string Name, int Type)>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT _id, _na, _ty FROM de WHERE _b_i = @bookId AND _ty != @skip";
            cmd.Parameters.AddWithValue("@bookId", primaryBookId);
            cmd.Parameters.AddWithValue("@skip", DeTypeVirtualAggregate);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var id = reader.GetInt64(0);
                var name = reader.GetString(1);
                var type = reader.GetInt32(2);
                deEntities[id] = (name, type);
            }
        }

        // Step 4 — Create wallets (all accounts from _a_i references)
        var walletIdMap = new Dictionary<long, Guid>();
        var walletCount = 0;
        foreach (var deId in walletDeIds)
        {
            if (!deEntities.TryGetValue(deId, out var entry)) continue;

            var newId = Guid7.NewGuid();
            walletIdMap[deId] = newId;

            db.Wallets.Add(new Wallet
            {
                Id = newId,
                Name = entry.Name,
                Type = WalletType.Personal,
                Currency = "EUR", // OneMoney doesn't store currency per account in the schema
                CreatedAt = now,
                UpdatedAt = now
            });
            db.WalletMembers.Add(new WalletMember
            {
                WalletId = newId,
                UserId = userId,
                Role = WalletMemberRole.Owner,
                CreatedAt = now,
                UpdatedAt = now
            });
            walletCount++;
        }

        // Step 5 — Create categories (expense categories + income types)
        var categoryIdMap = new Dictionary<long, Guid>();
        var catCount = 0;
        foreach (var deId in categoryDeIds)
        {
            // Skip if it's actually a wallet account (already processed above)
            if (walletDeIds.Contains(deId)) continue;
            if (!deEntities.TryGetValue(deId, out var entry)) continue;

            var newId = Guid7.NewGuid();
            categoryIdMap[deId] = newId;

            db.Categories.Add(new Category
            {
                Id = newId,
                Name = entry.Name,
                UserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            });
            catCount++;
        }

        // System fallback category (Other) for unmapped references
        var fallbackCategoryId = Guid.Parse("10000000-0000-0000-0000-00000000000c");

        // Step 6 — Create transactions
        var txCount = 0;
        var walletBalances = new Dictionary<Guid, decimal>();

        await using (var cmd = conn.CreateCommand())
        {
            // _da = date in Unix milliseconds, _a_m = amount as TEXT, _ty = 0/1, _co = comment/description
            cmd.CommandText = "SELECT _da, _a_m, _ty, _co, _a_i, _d_i FROM tr WHERE _b_i = @bookId";
            cmd.Parameters.AddWithValue("@bookId", primaryBookId);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var unixMs = reader.GetInt64(0);
                var amountStr = reader.GetString(1);
                var txType = reader.GetInt32(2);
                var description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var accountDeId = reader.GetInt64(4);
                var categoryDeId = reader.GetInt64(5);

                if (!walletIdMap.TryGetValue(accountDeId, out var walletId)) continue;

                // Resolve category — may be an expense category or income type
                if (!categoryIdMap.TryGetValue(categoryDeId, out var categoryId))
                    categoryId = fallbackCategoryId;

                // Parse amount (TEXT in OneMoney, always positive)
                if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
                    continue;

                // Convert Unix milliseconds to LocalDate UTC using NodaTime
                var instant = Instant.FromUnixTimeMilliseconds(unixMs);
                var date = instant.InUtc().Date;

                var type = txType == OneMoneyIncome ? TransactionType.Income : TransactionType.Expense;

                db.Transactions.Add(new Transaction
                {
                    Id = Guid7.NewGuid(),
                    Type = type,
                    Amount = amount,
                    Description = description,
                    Date = date,
                    CategoryId = categoryId,
                    WalletId = walletId,
                    UserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                txCount++;

                // Track balance for WalletBalance creation
                if (!walletBalances.ContainsKey(walletId)) walletBalances[walletId] = 0m;
                walletBalances[walletId] += type == TransactionType.Income ? amount : -amount;
            }
        }

        // Step 7 — Create WalletBalances calculated from imported transactions
        foreach (var (wid, balance) in walletBalances)
        {
            db.WalletBalances.Add(new WalletBalance
            {
                WalletId = wid,
                Balance = balance,
                UpdatedAt = now
            });
        }

        // Ensure WalletBalance exists for wallets with no transactions
        foreach (var (_, wid) in walletIdMap)
        {
            if (!walletBalances.ContainsKey(wid))
            {
                db.WalletBalances.Add(new WalletBalance
                {
                    WalletId = wid,
                    Balance = 0m,
                    UpdatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return new ImportResult(
            ImportedAt: now,
            Source: "onemoney",
            Format: "sqlite",
            Counts: new ImportCounts(walletCount, catCount, txCount, 0, 0, 0, 0),
            Skipped: new ImportSkipped(0));
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result) > 0;
    }
}
