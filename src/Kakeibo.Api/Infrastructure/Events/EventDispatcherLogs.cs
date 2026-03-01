namespace Kakeibo.Api.Infrastructure.Events;

internal static partial class EventDispatcherLogs
{
    [LoggerMessage(1501, LogLevel.Error,
        "Error dispatching event {EventType} to handler {HandlerType}")]
    internal static partial void EventDispatchFailed(ILogger logger, string eventType, string? handlerType, Exception exception);
}
