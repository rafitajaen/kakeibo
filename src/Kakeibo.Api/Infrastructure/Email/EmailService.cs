using Kakeibo.Api.Common.Utils;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Kakeibo.Api.Infrastructure.Email;

// Email service that renders templates via the email-renderer service and sends via SMTP.
public sealed class EmailService(
    IOptions<SmtpOptions> smtpOptions,
    IOptions<EmailRendererOptions> rendererOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;
    private readonly EmailRendererOptions _rendererOptions = rendererOptions.Value;

    public async Task SendWelcomeEmailAsync(
        Guid userId,
        string email,
        string firstName,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("Welcome", new { firstName }, cancellationToken);
        await SendEmailAsync(email, "Welcome to Kakeibo!", html, cancellationToken);
        logger.LogInformation("Welcome email sent to {Email} for user {UserId}", email, userId);
    }

    public async Task SendPasswordResetEmailAsync(
        Guid userId,
        string email,
        string firstName,
        string token,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("PasswordReset", new { firstName, token }, cancellationToken);
        await SendEmailAsync(email, "Reset Your Password", html, cancellationToken);
        logger.LogInformation("Password reset email sent to {Email} for user {UserId}", email, userId);
    }

    public async Task SendWalletInvitationEmailAsync(
        Guid invitationId,
        string email,
        string walletName,
        string inviterName,
        string token,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("WalletInvitation", new { walletName, inviterName, token }, cancellationToken);
        await SendEmailAsync(email, $"You're invited to {walletName}", html, cancellationToken);
        logger.LogInformation("Wallet invitation email sent to {Email} for invitation {InvitationId}", email, invitationId);
    }

    public async Task SendBudgetAlertEmailAsync(
        Guid userId,
        string email,
        string budgetName,
        decimal spent,
        decimal limit,
        CancellationToken cancellationToken = default)
    {
        var percentage = (int)(spent / limit * 100);
        var html = await RenderTemplateAsync("BudgetAlert", new { budgetName, spent, limit, percentage }, cancellationToken);
        await SendEmailAsync(email, $"Budget Alert: {budgetName}", html, cancellationToken);
        logger.LogInformation("Budget alert email sent to {Email} for user {UserId}", email, userId);
    }

    public async Task SendGoalMilestoneEmailAsync(
        Guid userId,
        string email,
        string goalName,
        decimal current,
        decimal target,
        int percentage,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("GoalMilestone", new { goalName, current, target, percentage }, cancellationToken);
        await SendEmailAsync(email, $"Goal Milestone: {goalName}", html, cancellationToken);
        logger.LogInformation("Goal milestone email sent to {Email} for user {UserId}", email, userId);
    }

    public async Task SendGoalAchievedEmailAsync(
        Guid userId,
        string email,
        string goalName,
        decimal target,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("GoalAchieved", new { goalName, target }, cancellationToken);
        await SendEmailAsync(email, $"Goal Achieved: {goalName}!", html, cancellationToken);
        logger.LogInformation("Goal achieved email sent to {Email} for user {UserId}", email, userId);
    }

    public async Task SendMemberJoinedEmailAsync(
        Guid userId,
        string email,
        string walletName,
        string newMemberName,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("MemberJoined", new { walletName, newMemberName }, cancellationToken);
        await SendEmailAsync(email, $"New member joined {walletName}", html, cancellationToken);
        logger.LogInformation("Member joined email sent to {Email} for user {UserId}", email, userId);
    }

    public async Task SendRecurringTransactionGeneratedEmailAsync(
        Guid userId,
        string email,
        string patternName,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var html = await RenderTemplateAsync("RecurringGenerated", new { patternName, amount }, cancellationToken);
        await SendEmailAsync(email, $"Recurring transaction generated: {patternName}", html, cancellationToken);
        logger.LogInformation("Recurring generated email sent to {Email} for user {UserId}", email, userId);
    }

    // Renders an email template via the external email-renderer service.
    private async Task<string> RenderTemplateAsync(
        string template,
        object props,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("EmailRenderer");
        httpClient.BaseAddress = new Uri(_rendererOptions.BaseUrl);

        var payload = DefaultSerializer.Serialize(new { template, props });
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/render", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    // Sends a plain HTML email via SMTP.
    private async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtpOptions.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, _smtpOptions.UseSsl, cancellationToken);

        if (!string.IsNullOrEmpty(_smtpOptions.Username) && !string.IsNullOrEmpty(_smtpOptions.Password))
        {
            await smtpClient.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, cancellationToken);
        }

        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);
    }
}
