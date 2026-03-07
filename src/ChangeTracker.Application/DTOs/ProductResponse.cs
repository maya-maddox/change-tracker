namespace ChangeTracker.Application.DTOs;

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Quantity,
    DateTime CreatedAt,
    DateTime UpdatedAt);
