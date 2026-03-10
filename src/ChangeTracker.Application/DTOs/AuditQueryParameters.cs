namespace ChangeTracker.Application.DTOs;

public record AuditQueryParameters
{
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? Action { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
