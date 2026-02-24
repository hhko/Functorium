using TestingStrategies;
using TestingStrategies.Specifications;

namespace TestingStrategies.Tests.Unit;

[Trait("Part4-TestingStrategies", "Level1")]
public class Level1_SpecSelfTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    public void InStockSpec_ShouldReturnExpected_WhenStockIs(int stock, bool expected)
    {
        var product = new Product("Test", 1000, stock, "Test");
        var spec = new InStockSpec();
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }

    [Theory]
    [InlineData(999, false)]    // below min
    [InlineData(1000, true)]    // exactly min
    [InlineData(5000, true)]    // in range
    [InlineData(10000, true)]   // exactly max
    [InlineData(10001, false)]  // above max
    public void PriceRangeSpec_ShouldReturnExpected_WhenPriceIs(decimal price, bool expected)
    {
        var product = new Product("Test", price, 1, "Test");
        var spec = new PriceRangeSpec(1000, 10000);
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Electronics", true)]
    [InlineData("electronics", true)]
    [InlineData("ELECTRONICS", true)]
    [InlineData("Furniture", false)]
    public void CategorySpec_ShouldReturnExpected_WhenCategoryIs(string category, bool expected)
    {
        var product = new Product("Test", 1000, 1, "Electronics");
        var spec = new CategorySpec(category);
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Laptop", true)]
    [InlineData("laptop", true)]
    [InlineData("Mouse", false)]
    public void ProductNameUniqueSpec_ShouldReturnExpected_WhenNameIs(string name, bool expected)
    {
        var product = new Product("Laptop", 1000, 1, "Test");
        var spec = new ProductNameUniqueSpec(name);
        spec.IsSatisfiedBy(product).ShouldBe(expected);
    }
}
