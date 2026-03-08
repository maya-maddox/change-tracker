using ChangeTracker.Application.Messages;

namespace ChangeTracker.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(EntityChangedMessage message, CancellationToken cancellationToken = default);
}
