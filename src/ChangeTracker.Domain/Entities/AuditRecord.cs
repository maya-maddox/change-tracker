namespace ChangeTracker.Domain.Entities;

public class AuditRecord
{
    public Guid Id { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public string? Changes { get; set; }
    public DateTime Timestamp { get; set; }
}
