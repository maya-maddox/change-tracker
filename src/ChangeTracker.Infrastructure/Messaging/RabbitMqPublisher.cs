using System.Text;
using System.Text.Json;
using ChangeTracker.Application.Interfaces;
using ChangeTracker.Application.Messages;
using RabbitMQ.Client;

namespace ChangeTracker.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        var connection = await factory.CreateConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await RabbitMqTopology.DeclareAsync(channel, cancellationToken);

        return new RabbitMqPublisher(connection, channel);
    }

    public async Task PublishAsync(EntityChangedMessage message, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties { Persistent = true };

        await _channel.BasicPublishAsync(
            exchange: RabbitMqTopology.ExchangeName,
            routingKey: RabbitMqTopology.RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
