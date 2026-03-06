using ArchitectureRules.Domain.AggregateRoots.Products;
using ArchitectureRules.Domain.AggregateRoots.Products.Specifications;

namespace ArchitectureRules.Tests.Unit;

[Trait("Part4-ArchitectureRules", "SpecTests")]
public class ProductSpecTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10, true)]
    public void ProductInStockSpec_ShouldReturnExpected(int stock, bool expected)
    {
        var product = new Product("Test", 1000, stock, "Test");
        var spec = new ProductInStockSpec();
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(500, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void ProductPriceRangeSpec_ShouldReturnExpected(decimal price, bool expected)
    {
        var product = new Product("Test", price, 1, "Test");
        var spec = new ProductPriceRangeSpec(100, 1000);
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(10, false)]
    public void ProductLowStockSpec_ShouldReturnExpected(int stock, bool expected)
    {
        var product = new Product("Test", 1000, stock, "Test");
        var spec = new ProductLowStockSpec(5);
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }
}
