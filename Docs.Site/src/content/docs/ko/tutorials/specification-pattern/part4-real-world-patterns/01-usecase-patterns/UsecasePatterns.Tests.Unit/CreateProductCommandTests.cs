using UsecasePatterns;
using UsecasePatterns.Usecases;

namespace UsecasePatterns.Tests.Unit;

[Trait("Part4-UsecasePatterns", "Command")]
public class CreateProductCommandTests
{
    private static readonly List<Product> SampleProducts =
    [
        new("Laptop", 1500, 10, "Electronics"),
        new("Mouse", 25, 50, "Electronics"),
    ];

    [Fact]
    public void Handle_ShouldReturnFalse_WhenProductNameAlreadyExists()
    {
        // Arrange
        var repository = new InMemoryProductRepository(SampleProducts);
        var handler = new CreateProductCommandHandler(repository);
        var command = new CreateProductCommand("Laptop", 2000, 5, "Electronics");

        // Act
        var result = handler.Handle(command);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Handle_ShouldReturnFalse_WhenProductNameAlreadyExists_CaseInsensitive()
    {
        // Arrange
        var repository = new InMemoryProductRepository(SampleProducts);
        var handler = new CreateProductCommandHandler(repository);
        var command = new CreateProductCommand("laptop", 2000, 5, "Electronics");

        // Act
        var result = handler.Handle(command);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Handle_ShouldReturnTrue_WhenProductNameIsNew()
    {
        // Arrange
        var repository = new InMemoryProductRepository(SampleProducts);
        var handler = new CreateProductCommandHandler(repository);
        var command = new CreateProductCommand("Monitor", 500, 15, "Electronics");

        // Act
        var result = handler.Handle(command);

        // Assert
        result.ShouldBeTrue();
    }
}
