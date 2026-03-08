using ChangeTracker.Application.DTOs;
using ChangeTracker.Application.Interfaces;
using ChangeTracker.Application.Messages;
using ChangeTracker.Application.Services;
using ChangeTracker.Domain.Entities;
using NSubstitute;

namespace ChangeTracker.UnitTests.Services;

public class ProductServiceTests
{
    private readonly IProductRepository _repository;
    private readonly IMessagePublisher _publisher;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _repository = Substitute.For<IProductRepository>();
        _publisher = Substitute.For<IMessagePublisher>();
        _service = new ProductService(_repository, _publisher);
    }

    private static Product CreateProduct(Guid? id = null, string name = "Test", decimal price = 5m, int quantity = 1) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name, Price = price, Quantity = quantity, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        var products = new List<Product> { CreateProduct(name: "A"), CreateProduct(name: "B") };
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(products);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsResponse()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(CreateProduct(id: id));

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_PersistsAndPublishes()
    {
        var request = new CreateProductRequest("Product", "A product", 9.99m, 10);

        var result = await _service.CreateAsync(request);

        Assert.Equal("Product", result.Name);
        Assert.Equal(9.99m, result.Price);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<EntityChangedMessage>(m => m.Action == "Created" && m.EntityType == "Product"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesAndPublishes()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(CreateProduct(id: id, name: "Old"));
        var request = new UpdateProductRequest("New", "Updated", 15m, 5);

        var result = await _service.UpdateAsync(id, request);

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.Equal(15m, result.Price);
        await _repository.Received(1).UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<EntityChangedMessage>(m => m.Action == "Updated" && m.EntityType == "Product"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NonExistingProduct_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateProductRequest("X", null, 1m, 1));

        Assert.Null(result);
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<EntityChangedMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_DeletesAndPublishes()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(CreateProduct(id: id));

        var result = await _service.DeleteAsync(id);

        Assert.True(result);
        await _repository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<EntityChangedMessage>(m => m.Action == "Deleted" && m.EntityType == "Product"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProduct_ReturnsFalse()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<EntityChangedMessage>(), Arg.Any<CancellationToken>());
    }
}
