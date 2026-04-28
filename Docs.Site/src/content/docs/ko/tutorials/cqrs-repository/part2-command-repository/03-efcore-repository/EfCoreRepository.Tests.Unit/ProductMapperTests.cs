using EfCoreRepository;

namespace EfCoreRepository.Tests.Unit;

public sealed class ProductMapperTests
{
    [Fact]
    public void ToModel_ShouldMapAllProperties_WhenValidProduct()
    {
        // Arrange
        var id = ProductId.New();
        var product = new Product(id, "Keyboard", 49_900m, isActive: true);

        // Act
        var model = ProductMapper.ToModel(product);

        // Assert
        model.Id.ShouldBe(id.ToString());
        model.Name.ShouldBe("Keyboard");
        model.Price.ShouldBe(49_900m);
        model.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ToDomain_ShouldMapAllProperties_WhenValidModel()
    {
        // Arrange
        var id = ProductId.New();
        var model = new ProductModel
        {
            Id = id.ToString(),
            Name = "Mouse",
            Price = 29_900m,
            IsActive = false,
        };

        // Act
        var product = ProductMapper.ToDomain(model);

        // Assert
        product.Id.ShouldBe(id);
        product.Name.ShouldBe("Mouse");
        product.Price.ShouldBe(29_900m);
        product.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveIdentity_WhenMappedBothWays()
    {
        // Arrange
        var original = new Product(ProductId.New(), "Monitor", 350_000m);

        // Act
        var model = ProductMapper.ToModel(original);
        var restored = ProductMapper.ToDomain(model);

        // Assert
        restored.Id.ShouldBe(original.Id);
        restored.Name.ShouldBe(original.Name);
        restored.Price.ShouldBe(original.Price);
        restored.ShouldBe(original); // Entity equality by ID
    }

    [Fact]
    public void ToModel_ShouldConvertIdToString_WhenUlidBased()
    {
        // Arrange
        var id = ProductId.New();
        var product = new Product(id, "Keyboard", 49_900m);

        // Act
        var model = ProductMapper.ToModel(product);

        // Assert
        model.Id.ShouldBeOfType<string>();
        model.Id.Length.ShouldBe(26); // Ulid string length
    }

    [Fact]
    public void ToDomain_ShouldParseStringId_WhenValidUlidString()
    {
        // Arrange
        var expectedId = ProductId.New();
        var model = new ProductModel
        {
            Id = expectedId.ToString(),
            Name = "Keyboard",
            Price = 49_900m,
            IsActive = true,
        };

        // Act
        var product = ProductMapper.ToDomain(model);

        // Assert
        product.Id.ShouldBe(expectedId);
    }
}
