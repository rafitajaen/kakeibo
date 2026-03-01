namespace Kakeibo.Api.Infrastructure.Email;

public interface IEmailService
{
    Task SendEmailVerificationAsync(Guid userId, string email, string token, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(Guid userId, string email, string firstName, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(Guid userId, string email, string firstName, string token, CancellationToken cancellationToken = default);
    Task SendWalletInvitationEmailAsync(Guid invitationId, string email, string walletName, string inviterName, string token, CancellationToken cancellationToken = default);
    Task SendBudgetAlertEmailAsync(Guid userId, string email, string budgetName, decimal spent, decimal limit, CancellationToken cancellationToken = default);
    Task SendGoalMilestoneEmailAsync(Guid userId, string email, string goalName, decimal current, decimal target, int percentage, CancellationToken cancellationToken = default);
    Task SendGoalAchievedEmailAsync(Guid userId, string email, string goalName, decimal target, CancellationToken cancellationToken = default);
    Task SendMemberJoinedEmailAsync(Guid userId, string email, string walletName, string newMemberName, CancellationToken cancellationToken = default);
    Task SendRecurringTransactionGeneratedEmailAsync(Guid userId, string email, string patternName, decimal amount, CancellationToken cancellationToken = default);
}
