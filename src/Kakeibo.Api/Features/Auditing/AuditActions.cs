namespace Kakeibo.Api.Features.Auditing;

// Audit action constants used when recording events to the audit trail.
public static class AuditActions
{
    public const string UserRegistered = "user.registered";
    public const string UserLoggedIn = "user.logged_in";
    public const string UserLoggedOut = "user.logged_out";
    public const string UserLoggedOutAllSessions = "user.logged_out_all";
    public const string PasswordResetRequested = "user.password_reset_requested";
    public const string PasswordReset = "user.password_reset";
    public const string EmailVerified = "user.email_verified";
    public const string WalletCreated = "wallet.created";
    public const string WalletArchived = "wallet.archived";
    public const string InvitationSent = "wallet.invitation_sent";
    public const string InvitationAccepted = "wallet.invitation_accepted";
    public const string MemberJoined = "wallet.member_joined";
    public const string MemberLeft = "wallet.member_left";
    public const string SettlementRecorded = "wallet.settlement_recorded";
    public const string TransactionRecorded = "transaction.recorded";
    public const string TransactionUpdated = "transaction.updated";
    public const string TransactionDeleted = "transaction.deleted";
}
