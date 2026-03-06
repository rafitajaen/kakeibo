using Kakeibo.Api.Domain.Entities;

namespace Kakeibo.Api.Features.Identity.ExportData;

// Holds all user data loaded from the database for export.
internal sealed record UserExportData(
    List<Wallet> Wallets,
    List<WalletBalance> WalletBalances,
    List<WalletMember> WalletMembers,
    List<Category> Categories,
    List<Transaction> Transactions,
    List<Budget> Budgets,
    List<Goal> Goals,
    List<RecurringPattern> RecurringPatterns,
    List<Settlement> Settlements);
