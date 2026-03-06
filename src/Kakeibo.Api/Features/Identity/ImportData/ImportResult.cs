using NodaTime;

namespace Kakeibo.Api.Features.Identity.ImportData;

// Summary returned after a successful import operation.
public sealed record ImportResult(
    Instant ImportedAt,
    string Source,
    string Format,
    ImportCounts Counts,
    ImportSkipped Skipped);

public sealed record ImportCounts(
    int Wallets,
    int Categories,
    int Transactions,
    int Budgets,
    int Goals,
    int RecurringPatterns,
    int Settlements);

public sealed record ImportSkipped(
    int SharedWalletMembers);
