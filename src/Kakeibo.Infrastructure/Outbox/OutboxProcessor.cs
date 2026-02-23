using Kakeibo.Common.Modules;
using Kakeibo.Common.Utils;
using Kakeibo.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Polly;
using Polly.Retry;

namespace Kakeibo.Infrastructure.Outbox;

// Background service that polls per-module outbox tables, dispatches to IEventConsumer<T>,
// and marks messages as processed. Includes Polly retry (3x exponential: 1s, 5s, 15s).
public sealed class OutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        logger.LogInformation("OutboxProcessor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContexts = scope.ServiceProvider.GetServices<DbContext>()
            .OfType<IOutboxSource>()
            .ToList();

        foreach (var outboxSource in dbContexts)
        {
            var unprocessedMessages = await outboxSource.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var message in unprocessedMessages)
            {
                try
                {
                    await _retryPipeline.ExecuteAsync(async ct =>
                    {
                        await DispatchEventAsync(message, scope.ServiceProvider, ct);
                    }, cancellationToken);

                    message.ProcessedAt = SystemClock.Instance.GetCurrentInstant();
                    await ((DbContext)outboxSource).SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process outbox message {MessageId} after retries", message.Id);
                }
            }
        }
    }

    private async Task DispatchEventAsync(
        Common.Persistence.OutboxMessage message,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        // Deserialize event type
        var eventType = Type.GetType(message.EventType)
            ?? throw new InvalidOperationException($"Could not resolve event type '{message.EventType}'");

        var @event = DefaultSerializer.Deserialize(message.Payload, eventType)
            ?? throw new InvalidOperationException($"Failed to deserialize event payload for type '{message.EventType}'");

        // Resolve all IEventConsumer<T> for this event type
        var consumerType = typeof(IEventConsumer<>).MakeGenericType(eventType);
        var consumers = serviceProvider.GetServices(consumerType);

        foreach (var consumer in consumers)
        {
            var method = consumerType.GetMethod(nameof(IEventConsumer<object>.ConsumeAsync))
                ?? throw new InvalidOperationException($"ConsumeAsync method not found on consumer type '{consumerType.Name}'");

            var task = method.Invoke(consumer, [@event, cancellationToken]) as Task
                ?? throw new InvalidOperationException("ConsumeAsync did not return a Task");

            await task;
        }
    }
}
