namespace ChangeTracker.Application.DTOs;

public record CreateProductRequest(string Name, string? Description, decimal Price, int Quantity);
