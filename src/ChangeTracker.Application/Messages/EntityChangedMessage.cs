namespace ChangeTracker.Application.Messages;

public record EntityChangedMessage(
    string EntityType,
    Guid EntityId,
    string Action,
    string? Changes,
    DateTime Timestamp);
