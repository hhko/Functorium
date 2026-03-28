using CleanArchitecture.Application.Products.Create;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;

using NSubstitute;

namespace CleanArchitecture.Application.Tests.Products;

public class CreateProductHandlerTests
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        _repository = Substitute.For<IProductRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateProductHandler(_repository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsProductId()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repository.ExistsAsync("LAP-001", default).Returns(false);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(ProductId.Empty, result);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), default);
        await _unitOfWork.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateSku_ThrowsApplicationException()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repository.ExistsAsync("LAP-001", default).Returns(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApplicationException>(() =>
            _handler.HandleAsync(command));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_CallsRepositoryAdd()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repository.ExistsAsync("LAP-001", default).Returns(false);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Name == "Laptop" && p.Sku == "LAP-001"),
            default);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_CallsUnitOfWorkSaveChanges()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repository.ExistsAsync("LAP-001", default).Returns(false);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(default);
    }
}
