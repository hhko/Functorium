using QueryPortInterface;

namespace QueryPortInterface.Tests.Unit;

public sealed class ProductDtoTests
{
    [Fact]
    public void ProductDto_Create_T1_AllProperties_T2_ShouldRetainValues_T3()
    {
        // Arrange
        var id = ProductId.New().ToString();

        // Act
        var dto = new ProductDto(id, "Keyboard", 89_000m, 50, "Electronics");

        // Assert
        dto.Id.ShouldBe(id);
        dto.Name.ShouldBe("Keyboard");
        dto.Price.ShouldBe(89_000m);
        dto.Stock.ShouldBe(50);
        dto.Category.ShouldBe("Electronics");
    }

    [Fact]
    public void ProductDto_EqualValues_T1_TwoIdenticalDtos_T2_ShouldBeEqual_T3()
    {
        // Arrange
        var id = ProductId.New().ToString();

        // Act
        var dto1 = new ProductDto(id, "Mouse", 35_000m, 100, "Electronics");
        var dto2 = new ProductDto(id, "Mouse", 35_000m, 100, "Electronics");

        // Assert
        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void ProductDto_DifferentValues_T1_TwoDifferentDtos_T2_ShouldNotBeEqual_T3()
    {
        // Arrange & Act
        var dto1 = new ProductDto(ProductId.New().ToString(), "Keyboard", 89_000m, 50, "Electronics");
        var dto2 = new ProductDto(ProductId.New().ToString(), "Mouse", 35_000m, 100, "Electronics");

        // Assert
        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void Product_Create_T1_ValidParameters_T2_ShouldCreateEntity_T3()
    {
        // Arrange
        var id = ProductId.New();

        // Act
        var product = new Product(id, "Keyboard", 89_000m, 50, "Electronics");

        // Assert
        product.Id.ShouldBe(id);
        product.Name.ShouldBe("Keyboard");
        product.Price.ShouldBe(89_000m);
        product.Stock.ShouldBe(50);
        product.Category.ShouldBe("Electronics");
    }

    [Fact]
    public void IProductQuery_T1_Interface_T2_ShouldExtendIQueryPort_T3()
    {
        // Assert - IProductQuery는 IQueryPort<Product, ProductDto>를 확장
        typeof(IProductQuery)
            .GetInterfaces()
            .ShouldContain(typeof(Functorium.Applications.Queries.IQueryPort<Product, ProductDto>));
    }
}
