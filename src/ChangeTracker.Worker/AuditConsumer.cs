using System.Text;
using System.Text.Json;
using ChangeTracker.Application.Messages;
using ChangeTracker.Domain.Entities;
using ChangeTracker.Infrastructure.Data;
using ChangeTracker.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChangeTracker.Worker;

public class AuditConsumer(
    IServiceScopeFactory scopeFactory,
    IConnection connection,
    ILogger<AuditConsumer> logger) : BackgroundService
{
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(channel, stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: RabbitMqTopology.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Audit consumer started, listening on queue {Queue}", RabbitMqTopology.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.Span);
            var message = JsonSerializer.Deserialize<EntityChangedMessage>(body);

            if (message is null)
            {
                logger.LogWarning("Received null message, sending to dead-letter queue");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            logger.LogInformation("Processing {Action} event for {EntityType} {EntityId}",
                message.Action, message.EntityType, message.EntityId);

            var auditRecord = new AuditRecord
            {
                Id = Guid.NewGuid(),
                EntityType = message.EntityType,
                EntityId = message.EntityId,
                Action = message.Action,
                Changes = message.Changes,
                Timestamp = message.Timestamp
            };

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChangeTrackerDbContext>();
            dbContext.AuditRecords.Add(auditRecord);
            await dbContext.SaveChangesAsync(stoppingToken);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

            logger.LogInformation("Persisted audit record {AuditId} for {EntityType} {EntityId}",
                auditRecord.Id, message.EntityType, message.EntityId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message");

            try
            {
                await HandleFailureAsync(channel, ea, stoppingToken);
            }
            catch (Exception retryEx)
            {
                logger.LogError(retryEx, "Error during failure handling, message will be redelivered on channel recovery");
            }
        }
    }

    private async Task HandleFailureAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var retryCount = GetRetryCount(ea.BasicProperties);

        if (retryCount >= MaxRetries)
        {
            logger.LogWarning("Message exceeded max retries ({MaxRetries}), sending to dead-letter queue", MaxRetries);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            return;
        }

        var properties = new BasicProperties(ea.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
        };
        properties.Headers["x-retry-count"] = retryCount + 1;

        await channel.BasicPublishAsync(
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: RabbitMqTopology.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: ea.Body,
            cancellationToken: stoppingToken);

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

        logger.LogWarning("Requeued message, retry {RetryCount} of {MaxRetries}", retryCount + 1, MaxRetries);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue("x-retry-count", out var value))
            return 0;

        return value switch
        {
            int i => i,
            long l => (int)l,
            _ => 0
        };
    }
}
