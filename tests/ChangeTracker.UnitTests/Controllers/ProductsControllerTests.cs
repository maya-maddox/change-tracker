using ChangeTracker.Api.Controllers;
using ChangeTracker.Application.DTOs;
using ChangeTracker.Application.Interfaces;
using ChangeTracker.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ChangeTracker.UnitTests.Controllers;

public class ProductsControllerTests
{
    private readonly IProductRepository _repository;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _repository = Substitute.For<IProductRepository>();
        _controller = new ProductsController(_repository);
    }

    private static Product CreateProduct(Guid? id = null, string name = "Test", decimal price = 5m, int quantity = 1) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name, Price = price, Quantity = quantity, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    [Fact]
    public async Task GetAll_ReturnsAllProducts()
    {
        var products = new List<Product>
        {
            CreateProduct(name: "A", price: 10m),
            CreateProduct(name: "B", price: 20m, quantity: 2)
        };
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(products);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ProductResponse>>(ok.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsEmptyResult()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Product>());

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ProductResponse>>(ok.Value).ToList();
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsProduct()
    {
        var id = Guid.NewGuid();
        var product = CreateProduct(id: id);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(ok.Value);
        Assert.Equal("Test", response.Name);
    }

    [Fact]
    public async Task GetById_NonExistingProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _controller.GetById(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedProduct()
    {
        var request = new CreateProductRequest("Test", "A test product", 9.99m, 10);

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(created.Value);
        Assert.Equal("Test", response.Name);
        Assert.Equal(9.99m, response.Price);
        Assert.Equal(10, response.Quantity);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ExistingProduct_ReturnsUpdatedProduct()
    {
        var id = Guid.NewGuid();
        var product = CreateProduct(id: id, name: "Old");
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(product);
        var request = new UpdateProductRequest("New", "Updated", 15m, 5);

        var result = await _controller.Update(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(ok.Value);
        Assert.Equal("New", response.Name);
        Assert.Equal(15m, response.Price);
        await _repository.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var request = new UpdateProductRequest("X", null, 1m, 1);

        var result = await _controller.Update(id, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var product = CreateProduct(id: id);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await _repository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_NonExistingProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
