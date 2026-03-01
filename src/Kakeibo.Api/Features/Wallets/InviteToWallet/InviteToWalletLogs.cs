namespace Kakeibo.Api.Features.Wallets.InviteToWallet;

internal static partial class InviteToWalletLogs
{
    [LoggerMessage(3001, LogLevel.Error,
        "Failed to send wallet invitation email to {InviteeEmail} for wallet {WalletId}.")]
    internal static partial void InvitationEmailFailed(ILogger logger, string inviteeEmail, Guid walletId, Exception exception);
}
