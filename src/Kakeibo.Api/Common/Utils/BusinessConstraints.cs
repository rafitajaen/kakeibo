namespace Kakeibo.Api.Common.Utils;

/// <summary>
/// Centralizes business rule limits to avoid magic numbers scattered across validators and handlers.
/// Values match the canonical business constraints documented in .claude/rules/constraints.md.
/// </summary>
public static class BusinessConstraints
{
    // Amounts
    public const decimal AmountMin = 0.01m;
    public const decimal AmountMax = 999_999_999.99m;

    // Date ranges
    public const int BudgetMaxYears = 5;
    public const int GoalMaxYears = 10;
    public const int TransactionMaxFutureYears = 1;

    // Invitations
    public const int MaxPendingInvitations = 50;
    public const int InvitationExpiryDays = 7;
}
