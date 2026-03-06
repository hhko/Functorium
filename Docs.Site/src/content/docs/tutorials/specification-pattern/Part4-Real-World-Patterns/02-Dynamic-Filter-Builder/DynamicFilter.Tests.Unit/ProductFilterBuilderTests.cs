using DynamicFilter;

namespace DynamicFilter.Tests.Unit;

[Trait("Part4-DynamicFilter", "FilterBuilder")]
public class ProductFilterBuilderTests
{
    private static readonly List<Product> Products = SampleProducts.All;

    [Fact]
    public void Build_ShouldReturnAll_WhenEmptyRequest()
    {
        // Arrange
        var request = new SearchProductsRequest();

        // Act
        var spec = ProductFilterBuilder.Build(request);

        // Assert
        spec.IsAll.ShouldBeTrue();
        Products.Where(spec.IsSatisfiedBy).Count().ShouldBe(Products.Count);
    }

    [Fact]
    public void Build_ShouldReturnNameFilter_WhenOnlyNameProvided()
    {
        // Arrange
        var request = new SearchProductsRequest(Name: "Keyboard");

        // Act
        var spec = ProductFilterBuilder.Build(request);

        // Assert
        spec.IsAll.ShouldBeFalse();
        var results = Products.Where(spec.IsSatisfiedBy).ToList();
        results.ShouldAllBe(p => p.Name.Contains("Keyboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_ShouldCombineMultipleFilters_WhenMultipleFieldsProvided()
    {
        // Arrange
        var request = new SearchProductsRequest(
            Category: "Electronics",
            MinPrice: 50,
            MaxPrice: 500);

        // Act
        var spec = ProductFilterBuilder.Build(request);

        // Assert
        var results = Products.Where(spec.IsSatisfiedBy).ToList();
        results.ShouldAllBe(p =>
            p.Category == "Electronics" &&
            p.Price >= 50 && p.Price <= 500);
    }

    [Fact]
    public void Build_ShouldApplyAllFilters_WhenAllFieldsProvided()
    {
        // Arrange
        var request = new SearchProductsRequest(
            Name: "Mouse",
            Category: "Electronics",
            MinPrice: 10,
            MaxPrice: 100,
            InStockOnly: true);

        // Act
        var spec = ProductFilterBuilder.Build(request);

        // Assert
        var results = Products.Where(spec.IsSatisfiedBy).ToList();
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Wireless Mouse");
    }

    [Fact]
    public void Build_ShouldFilterInStockOnly_WhenInStockOnlyIsTrue()
    {
        // Arrange
        var request = new SearchProductsRequest(InStockOnly: true);

        // Act
        var spec = ProductFilterBuilder.Build(request);

        // Assert
        var results = Products.Where(spec.IsSatisfiedBy).ToList();
        results.ShouldAllBe(p => p.Stock > 0);
    }
}
