namespace ChangeTracker.Application.DTOs;

public record UpdateProductRequest(string Name, string? Description, decimal Price, int Quantity);
