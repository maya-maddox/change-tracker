using System.Text.Json;
using ChangeTracker.Application.DTOs;
using ChangeTracker.Application.Interfaces;
using ChangeTracker.Application.Messages;
using ChangeTracker.Domain.Entities;

namespace ChangeTracker.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMessagePublisher _publisher;

    public ProductService(IProductRepository repository, IMessagePublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(product, cancellationToken);

        await _publisher.PublishAsync(new EntityChangedMessage(
            "Product", product.Id, "Created",
            JsonSerializer.Serialize(new { product.Name, product.Description, product.Price, product.Quantity }),
            DateTime.UtcNow), cancellationToken);

        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Quantity = request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(product, cancellationToken);

        await _publisher.PublishAsync(new EntityChangedMessage(
            "Product", product.Id, "Updated",
            JsonSerializer.Serialize(new { product.Name, product.Description, product.Price, product.Quantity }),
            DateTime.UtcNow), cancellationToken);

        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return false;

        await _repository.DeleteAsync(id, cancellationToken);

        await _publisher.PublishAsync(new EntityChangedMessage(
            "Product", id, "Deleted", null, DateTime.UtcNow), cancellationToken);

        return true;
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.Quantity, product.CreatedAt, product.UpdatedAt);
}
