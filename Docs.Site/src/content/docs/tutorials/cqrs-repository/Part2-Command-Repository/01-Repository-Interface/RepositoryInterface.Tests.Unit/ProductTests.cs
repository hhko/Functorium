using RepositoryInterface;

namespace RepositoryInterface.Tests.Unit;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldSetProperties_WithValidInput()
    {
        // Arrange
        var id = ProductId.New();

        // Act
        var product = new Product(id, "Keyboard", 49_900m);

        // Assert
        product.Id.ShouldBe(id);
        product.Name.ShouldBe("Keyboard");
        product.Price.ShouldBe(49_900m);
    }

    [Fact]
    public void UpdatePrice_ShouldChangePrice_WhenCalled()
    {
        // Arrange
        var product = new Product(ProductId.New(), "Keyboard", 49_900m);

        // Act
        product.UpdatePrice(39_900m);

        // Assert
        product.Price.ShouldBe(39_900m);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameId()
    {
        // Arrange
        var id = ProductId.New();
        var product1 = new Product(id, "Keyboard", 49_900m);
        var product2 = new Product(id, "Keyboard", 49_900m);

        // Assert
        product1.ShouldBe(product2);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentId()
    {
        // Arrange
        var product1 = new Product(ProductId.New(), "Keyboard", 49_900m);
        var product2 = new Product(ProductId.New(), "Keyboard", 49_900m);

        // Assert
        product1.ShouldNotBe(product2);
    }

    [Fact]
    public void ProductId_ShouldBeUlidBased_WhenCreated()
    {
        // Act
        var id = ProductId.New();

        // Assert
        id.Value.ShouldNotBe(Ulid.Empty);
        id.ToString().Length.ShouldBe(26); // Ulid string length
    }

    [Fact]
    public void ProductId_ShouldRoundTrip_WhenParsedFromString()
    {
        // Arrange
        var original = ProductId.New();
        var str = original.ToString();

        // Act
        var parsed = ProductId.Create(str);

        // Assert
        parsed.ShouldBe(original);
    }
}
