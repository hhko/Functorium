using Functorium.Domains.Specifications;
using TestingStrategies;
using TestingStrategies.Specifications;

namespace TestingStrategies.Tests.Unit;

[Trait("Part4-TestingStrategies", "Level2")]
public class Level2_CompositionTests
{
    private static readonly Product InStockElectronics = new("Laptop", 1500, 10, "Electronics");
    private static readonly Product OutOfStockElectronics = new("Monitor", 500, 0, "Electronics");
    private static readonly Product InStockFurniture = new("Chair", 200, 5, "Furniture");
    private static readonly Product ExpensiveFurniture = new("Desk", 3000, 2, "Furniture");

    [Fact]
    public void And_ShouldRequireBothConditions()
    {
        // Arrange: Electronics AND InStock
        var spec = new CategorySpec("Electronics") & new InStockSpec();

        // Act & Assert
        spec.IsSatisfiedBy(InStockElectronics).ShouldBeTrue();
        spec.IsSatisfiedBy(OutOfStockElectronics).ShouldBeFalse();
        spec.IsSatisfiedBy(InStockFurniture).ShouldBeFalse();
    }

    [Fact]
    public void Or_ShouldRequireEitherCondition()
    {
        // Arrange: Electronics OR InStock
        var spec = new CategorySpec("Electronics") | new InStockSpec();

        // Act & Assert
        spec.IsSatisfiedBy(InStockElectronics).ShouldBeTrue();
        spec.IsSatisfiedBy(OutOfStockElectronics).ShouldBeTrue();
        spec.IsSatisfiedBy(InStockFurniture).ShouldBeTrue();
    }

    [Fact]
    public void Not_ShouldNegateCondition()
    {
        // Arrange: NOT InStock (= 재고 없는 상품)
        var spec = !new InStockSpec();

        // Act & Assert
        spec.IsSatisfiedBy(InStockElectronics).ShouldBeFalse();
        spec.IsSatisfiedBy(OutOfStockElectronics).ShouldBeTrue();
    }

    [Fact]
    public void ComplexComposition_ShouldWorkCorrectly()
    {
        // Arrange: Furniture AND InStock AND PriceRange(100, 1000)
        var spec = new CategorySpec("Furniture") & new InStockSpec() & new PriceRangeSpec(100, 1000);

        // Act & Assert
        spec.IsSatisfiedBy(InStockFurniture).ShouldBeTrue();      // Furniture, in stock, $200
        spec.IsSatisfiedBy(ExpensiveFurniture).ShouldBeFalse();    // Furniture, in stock, $3000 (out of range)
        spec.IsSatisfiedBy(InStockElectronics).ShouldBeFalse();    // Electronics
    }

    [Fact]
    public void All_And_Spec_ShouldReturnSpec()
    {
        // Arrange
        var inStock = new InStockSpec();
        var spec = Specification<Product>.All & inStock;

        // Act & Assert
        spec.IsSatisfiedBy(InStockElectronics).ShouldBeTrue();
        spec.IsSatisfiedBy(OutOfStockElectronics).ShouldBeFalse();
    }
}
