namespace ChangeTracker.Application.DTOs;

public record AuditRecordResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? Changes,
    DateTime Timestamp);
