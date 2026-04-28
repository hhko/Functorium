using EcommerceFiltering.Domain;
using EcommerceFiltering.Domain.Specifications;
using EcommerceFiltering.Domain.ValueObjects;

namespace EcommerceFiltering.Tests.Unit;

public class CompositionTests
{
    private static readonly Product _재고있는전자기기 = new(
        new ProductName("아이패드"), new Money(929_000m), new Quantity(12), new Category("전자기기"));

    private static readonly Product _재고없는전자기기 = new(
        new ProductName("에어팟"), new Money(359_000m), new Quantity(0), new Category("전자기기"));

    private static readonly Product _재고있는의류 = new(
        new ProductName("운동화"), new Money(189_000m), new Quantity(30), new Category("의류"));

    [Fact]
    public void And_ShouldCombineSpecs_WhenCategoryAndInStock()
    {
        // Arrange
        var spec = new ProductCategorySpec(new Category("전자기기"))
            & new ProductInStockSpec();

        // Act & Assert
        spec.IsSatisfiedBy(_재고있는전자기기).ShouldBeTrue();
        spec.IsSatisfiedBy(_재고없는전자기기).ShouldBeFalse();
        spec.IsSatisfiedBy(_재고있는의류).ShouldBeFalse();
    }

    [Fact]
    public void Or_ShouldCombineSpecs_WhenCategoryOrLowStock()
    {
        // Arrange
        var spec = new ProductCategorySpec(new Category("전자기기"))
            | new ProductLowStockSpec(new Quantity(1));

        // Act & Assert
        spec.IsSatisfiedBy(_재고있는전자기기).ShouldBeTrue();
        spec.IsSatisfiedBy(_재고없는전자기기).ShouldBeTrue();
        spec.IsSatisfiedBy(_재고있는의류).ShouldBeFalse();
    }

    [Fact]
    public void Not_ShouldNegateSpec_WhenInStockNegated()
    {
        // Arrange: 재고 없는 상품 = !InStock
        var outOfStockSpec = !new ProductInStockSpec();

        // Act & Assert
        outOfStockSpec.IsSatisfiedBy(_재고없는전자기기).ShouldBeTrue();
        outOfStockSpec.IsSatisfiedBy(_재고있는전자기기).ShouldBeFalse();
    }

    [Fact]
    public void Complex_ShouldCombineMultipleSpecs()
    {
        // Arrange: 전자기기 AND 재고 있음 AND 100만원 이하
        var spec = new ProductCategorySpec(new Category("전자기기"))
            & new ProductInStockSpec()
            & new ProductPriceRangeSpec(new Money(0m), new Money(1_000_000m));

        // Act & Assert
        spec.IsSatisfiedBy(_재고있는전자기기).ShouldBeTrue();
        spec.IsSatisfiedBy(_재고없는전자기기).ShouldBeFalse();
        spec.IsSatisfiedBy(_재고있는의류).ShouldBeFalse();
    }
}
