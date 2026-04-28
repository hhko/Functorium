using EntityAndIdentity;

namespace EntityAndIdentity.Tests.Unit;

public sealed class ProductTests
{
    [Fact]
    public void Create_AssignsNewId()
    {
        // Arrange & Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        product.Id.ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Create_SetsNameAndPrice()
    {
        // Arrange & Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        product.Name.ShouldBe("노트북");
        product.Price.ShouldBe(1_500_000m);
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenSameId()
    {
        // Arrange
        var id = ProductId.New();
        var product1 = Product.CreateFromValidated(id, "노트북", 1_500_000m);
        var product2 = Product.CreateFromValidated(id, "노트북", 1_500_000m);

        // Act
        var actual = product1.Equals(product2);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentId()
    {
        // Arrange
        var product1 = Product.Create("노트북", 1_500_000m);
        var product2 = Product.Create("노트북", 1_500_000m);

        // Act
        var actual = product1.Equals(product2);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ReturnsSame_WhenSameId()
    {
        // Arrange
        var id = ProductId.New();
        var product1 = Product.CreateFromValidated(id, "노트북", 1_500_000m);
        var product2 = Product.CreateFromValidated(id, "마우스", 25_000m);

        // Act & Assert
        product1.GetHashCode().ShouldBe(product2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_ReturnsTrue_WhenSameId()
    {
        // Arrange
        var id = ProductId.New();
        var product1 = Product.CreateFromValidated(id, "노트북", 1_500_000m);
        var product2 = Product.CreateFromValidated(id, "노트북", 1_500_000m);

        // Act & Assert
        (product1 == product2).ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_ReturnsTrue_WhenDifferentId()
    {
        // Arrange
        var product1 = Product.Create("노트북", 1_500_000m);
        var product2 = Product.Create("노트북", 1_500_000m);

        // Act & Assert
        (product1 != product2).ShouldBeTrue();
    }

    [Fact]
    public void ProductId_Create_RestoresFromString()
    {
        // Arrange
        var original = ProductId.New();
        var idString = original.ToString();

        // Act
        var restored = ProductId.Create(idString);

        // Assert
        restored.ShouldBe(original);
    }
}
