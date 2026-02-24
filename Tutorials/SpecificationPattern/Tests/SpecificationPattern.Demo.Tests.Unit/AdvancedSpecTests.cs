using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using SpecificationPattern.Demo;
using SpecificationPattern.Demo.Advanced;
using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Domain;
using SpecificationPattern.Demo.Intermediate;

namespace SpecificationPattern.Demo.Tests.Unit;

public sealed class AdvancedSpecTests
{
    // --- Advanced01: ExpressionResolver ---

    [Fact]
    public void TryResolve_ReturnsExpression_WhenSingleExpressionSpec()
    {
        // Arrange
        var spec = new InStockExprSpec();

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
    }

    [Fact]
    public void TryResolve_ReturnsComposedExpression_WhenAndComposite()
    {
        // Arrange
        var spec = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();

        // Verify the composed expression works
        var products = SampleProducts.Create();
        var filtered = products.AsQueryable().Where(actual).ToList();
        filtered.ShouldNotBeEmpty();
        filtered.ShouldAllBe(p => p.Stock > 0 && p.Price <= 10_000);
    }

    [Fact]
    public void TryResolve_ReturnsExpression_WhenNotComposite()
    {
        // Arrange
        Specification<Product> spec = !new PriceRangeExprSpec(50_000, decimal.MaxValue);

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
    }

    [Fact]
    public void TryResolve_ReturnsNull_WhenNonExpressionSpecMixed()
    {
        // Arrange - InStockSpec is NOT an ExpressionSpecification
        var spec = new InStockSpec() & new PriceRangeExprSpec(0, 10_000);

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldBeNull();
    }

    // --- Advanced02: PropertyMap ---

    [Fact]
    public void Translate_ConvertsEntityExpression_ToModelExpression()
    {
        // Arrange
        var map = new PropertyMap<Product, ProductDbModel>()
            .Map(p => p.Name, m => m.ProductName)
            .Map(p => p.Price, m => m.UnitPrice)
            .Map(p => p.Stock, m => m.StockQuantity)
            .Map(p => p.Category, m => m.CategoryCode);

        var domainExpr = new InStockExprSpec().ToExpression();

        // Act
        var actual = map.Translate(domainExpr);

        // Assert
        actual.ShouldNotBeNull();

        // Verify the translated expression works on model objects
        var dbModels = new List<ProductDbModel>
        {
            new() { ProductName = "A", UnitPrice = 1000, StockQuantity = 10, CategoryCode = "X" },
            new() { ProductName = "B", UnitPrice = 2000, StockQuantity = 0, CategoryCode = "Y" },
        };
        var filtered = dbModels.AsQueryable().Where(actual).ToList();
        filtered.Count.ShouldBe(1);
        filtered[0].ProductName.ShouldBe("A");
    }

    [Fact]
    public void Translate_WorksWithCompositeExpression()
    {
        // Arrange
        var map = new PropertyMap<Product, ProductDbModel>()
            .Map(p => p.Name, m => m.ProductName)
            .Map(p => p.Price, m => m.UnitPrice)
            .Map(p => p.Stock, m => m.StockQuantity)
            .Map(p => p.Category, m => m.CategoryCode);

        var composite = new InStockExprSpec() & new PriceRangeExprSpec(0, 10_000);
        var compositeExpr = SpecificationExpressionResolver.TryResolve(composite);

        // Act
        var actual = map.Translate(compositeExpr!);

        // Assert
        actual.ShouldNotBeNull();

        var dbModels = new List<ProductDbModel>
        {
            new() { ProductName = "Cheap", UnitPrice = 5_000, StockQuantity = 10, CategoryCode = "X" },
            new() { ProductName = "Expensive", UnitPrice = 50_000, StockQuantity = 10, CategoryCode = "Y" },
            new() { ProductName = "OutOfStock", UnitPrice = 3_000, StockQuantity = 0, CategoryCode = "Z" },
        };
        var filtered = dbModels.AsQueryable().Where(actual).ToList();
        filtered.Count.ShouldBe(1);
        filtered[0].ProductName.ShouldBe("Cheap");
    }
}
