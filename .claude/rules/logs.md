# Logging Pattern

All logging must use the `[LoggerMessage]` source generator. Direct `logger.Log*()` calls
are prohibited and will fail the build (CA1848 is set to `error` in `.editorconfig`).

## Rules

1. **Dedicated file**: Log definitions live in a `*Logs.cs` file — never inline in the
   handler or service class.
2. **Naming**: File and class named `{ServiceOrFeature}Logs` (e.g., `EmailServiceLogs`,
   `NotificationHandlerLogs`).
3. **Class signature**: `internal static partial class` in the same namespace as the consumer.
4. **Method signature**: `internal static partial void`. Use `this ILogger logger` as the
   first parameter (extension method syntax) — this enables the fluent `logger.Method(...)`
   call site. Then structured log properties, and `Exception exception` last (if applicable).
5. **No allocations**: The source generator emits an `IsEnabled` check automatically.
   Do not call `IsEnabled` manually.

## Example

```csharp
// EmailServiceLogs.cs
namespace Kakeibo.Api.Infrastructure.Email;

internal static partial class EmailServiceLogs
{
    [LoggerMessage(1201, LogLevel.Information,
        "Verification email sent to {Email} for user {UserId}")]
    internal static partial void VerificationEmailSent(this ILogger logger, string email, Guid userId);

    [LoggerMessage(1202, LogLevel.Error,
        "Failed to send verification email to {Email} for user {UserId}")]
    internal static partial void VerificationEmailFailed(this ILogger logger, string email, Guid userId, Exception exception);
}

// Call site in EmailService.cs
logger.VerificationEmailSent(email, userId);
logger.VerificationEmailFailed(email, userId, ex);
```

## EventId Ranges

| Range     | Location |
|-----------|----------|
| 1100–1199 | Infrastructure/Audit |
| 1200–1299 | Infrastructure/Email |
| 1300–1399 | Infrastructure/Storage |
| 1400–1499 | Infrastructure/WebPush |
| 1500–1599 | Infrastructure/Events |
| 2100–2199 | Features/Identity/Jobs |
| 2200–2299 | Features/Recurring/Jobs |
| 2300–2399 | Features/Identity/ImportData + ExportData |
| 3000–3099 | Features/Wallets |
| 3100–3199 | Features/Notifications/Events |
| 3200–3299 | Features/Friends |

## Compatibility

`[LoggerMessage]` works with any `ILogger` implementation. Serilog and OpenTelemetry both
consume structured log properties from the generated delegates unchanged.
