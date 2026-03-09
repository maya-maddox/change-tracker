using RabbitMQ.Client;

namespace ChangeTracker.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string ExchangeName = "change-tracker";
    public const string QueueName = "entity-changes";
    public const string RoutingKey = "entity.changed";
    public const string DeadLetterExchange = "change-tracker.dlx";
    public const string DeadLetterQueue = "entity-changes.dlq";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = RoutingKey
            },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);
    }
}
