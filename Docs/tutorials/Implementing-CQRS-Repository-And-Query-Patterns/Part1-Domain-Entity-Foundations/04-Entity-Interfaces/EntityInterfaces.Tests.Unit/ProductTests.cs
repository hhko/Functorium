using EntityInterfaces;

namespace EntityInterfaces.Tests.Unit;

public sealed class ProductTests
{
    [Fact]
    public void Create_SetsCreatedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        product.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        product.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void Create_SetsUpdatedAtToNone()
    {
        // Arrange & Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        product.UpdatedAt.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void UpdatePrice_SetsUpdatedAt()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);

        // Act
        product.UpdatePrice(1_350_000m);

        // Assert
        product.Price.ShouldBe(1_350_000m);
        product.UpdatedAt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void Delete_SetsDeletedAtAndDeletedBy()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);

        // Act
        product.Delete("admin@example.com");

        // Assert
        product.DeletedAt.IsSome.ShouldBeTrue();
        product.DeletedBy.IsSome.ShouldBeTrue();
        product.DeletedBy.IfNone("").ShouldBe("admin@example.com");
    }

    [Fact]
    public void IsDeleted_ReturnsTrue_AfterDelete()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);

        // Act
        product.Delete("admin@example.com");

        // Assert
        product.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void IsDeleted_ReturnsFalse_BeforeDelete()
    {
        // Arrange & Act
        var product = Product.Create("노트북", 1_500_000m);

        // Assert
        product.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Restore_ClearsDeletedAtAndDeletedBy()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);
        product.Delete("admin@example.com");

        // Act
        product.Restore();

        // Assert
        product.IsDeleted.ShouldBeFalse();
        product.DeletedAt.IsNone.ShouldBeTrue();
        product.DeletedBy.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Restore_SetsUpdatedAt()
    {
        // Arrange
        var product = Product.Create("노트북", 1_500_000m);
        product.Delete("admin@example.com");

        // Act
        product.Restore();

        // Assert
        product.UpdatedAt.IsSome.ShouldBeTrue();
    }
}
