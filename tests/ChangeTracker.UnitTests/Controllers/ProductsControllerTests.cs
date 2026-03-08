using ChangeTracker.Api.Controllers;
using ChangeTracker.Application.DTOs;
using ChangeTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ChangeTracker.UnitTests.Controllers;

public class ProductsControllerTests
{
    private readonly IProductService _productService;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _productService = Substitute.For<IProductService>();
        _controller = new ProductsController(_productService);
    }

    private static ProductResponse CreateResponse(Guid? id = null, string name = "Test", decimal price = 5m, int quantity = 1) =>
        new(id ?? Guid.NewGuid(), name, null, price, quantity, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        var products = new List<ProductResponse> { CreateResponse(name: "A"), CreateResponse(name: "B") };
        _productService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(products);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<ProductResponse>>(ok.Value);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _productService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(CreateResponse(id: id));

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ProductResponse>(ok.Value);
    }

    [Fact]
    public async Task GetById_NonExistingProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _productService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ProductResponse?)null);

        var result = await _controller.GetById(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var request = new CreateProductRequest("Test", "A test product", 9.99m, 10);
        var response = CreateResponse(name: "Test", price: 9.99m, quantity: 10);
        _productService.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.IsType<ProductResponse>(created.Value);
    }

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var request = new UpdateProductRequest("New", "Updated", 15m, 5);
        _productService.UpdateAsync(id, request, Arg.Any<CancellationToken>()).Returns(CreateResponse(id: id, name: "New", price: 15m, quantity: 5));

        var result = await _controller.Update(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<ProductResponse>(ok.Value);
    }

    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _productService.UpdateAsync(id, Arg.Any<UpdateProductRequest>(), Arg.Any<CancellationToken>()).Returns((ProductResponse?)null);

        var result = await _controller.Update(id, new UpdateProductRequest("X", null, 1m, 1), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _productService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingProduct_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _productService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.Delete(id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
