using CleanArchitecture.Application.Products.Create;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Moq;

namespace CleanArchitecture.Application.Tests.Products;

public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateProductHandler(_repositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsProductId()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repositoryMock.Setup(r => r.ExistsAsync("LAP-001", default)).ReturnsAsync(false);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateSku_ThrowsApplicationException()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repositoryMock.Setup(r => r.ExistsAsync("LAP-001", default)).ReturnsAsync(true);

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
        _repositoryMock.Setup(r => r.ExistsAsync("LAP-001", default)).ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Product>(p => p.Name == "Laptop" && p.Sku == "LAP-001"),
            default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_CallsUnitOfWorkSaveChanges()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repositoryMock.Setup(r => r.ExistsAsync("LAP-001", default)).ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
