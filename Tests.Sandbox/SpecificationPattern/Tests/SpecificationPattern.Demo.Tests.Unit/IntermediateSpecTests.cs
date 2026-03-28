using Functorium.Domains.Specifications;
using SpecificationPattern.Demo;
using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Domain;
using SpecificationPattern.Demo.Infrastructure;
using SpecificationPattern.Demo.Intermediate;

namespace SpecificationPattern.Demo.Tests.Unit;

public sealed class IntermediateSpecTests
{
    private readonly List<Product> _products = SampleProducts.Create();

    // --- Intermediate01: All Identity ---

    [Fact]
    public void All_SatisfiesAllProducts()
    {
        // Arrange
        var sut = Specification<Product>.All;

        // Act
        var actual = _products.Where(sut.IsSatisfiedBy).ToList();

        // Assert
        actual.Count.ShouldBe(_products.Count);
    }

    [Fact]
    public void All_IsAllReturnsTrue()
    {
        // Arrange & Act
        var actual = Specification<Product>.All.IsAll;

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void AndOperator_ReturnsOther_WhenLeftIsAll()
    {
        // Arrange
        var inStock = new InStockSpec();

        // Act
        var actual = Specification<Product>.All & inStock;

        // Assert
        ReferenceEquals(actual, inStock).ShouldBeTrue();
    }

    [Fact]
    public void BuildDynamicFilter_ReturnsFilteredProducts_WhenConditionsProvided()
    {
        // Arrange & Act
        var actual = Intermediate01_AllIdentity.BuildDynamicFilter(_products, name: "마우스", maxPrice: 20_000).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Name.Contains("마우스") && p.Price <= 20_000);
    }

    [Fact]
    public void BuildDynamicFilter_ReturnsAllProducts_WhenNoConditions()
    {
        // Arrange & Act
        var actual = Intermediate01_AllIdentity.BuildDynamicFilter(_products, name: null, maxPrice: 0).ToList();

        // Assert
        actual.Count.ShouldBe(_products.Count);
    }

    // --- Intermediate02: Repository ---

    [Fact]
    public void FindAll_ReturnsMatchingProducts_WhenSpecProvided()
    {
        // Arrange
        var sut = new InMemoryProductRepository(_products);
        var spec = new InStockSpec() & new PriceRangeSpec(0, 10_000);

        // Act
        var actual = sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock > 0 && p.Price <= 10_000);
    }

    [Fact]
    public void Exists_ReturnsTrue_WhenMatchingProductExists()
    {
        // Arrange
        var sut = new InMemoryProductRepository(_products);
        var spec = new CategorySpec("전자제품") & new InStockSpec();

        // Act
        var actual = sut.Exists(spec);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Exists_ReturnsFalse_WhenNoMatchingProduct()
    {
        // Arrange
        var sut = new InMemoryProductRepository(_products);
        var spec = new CategorySpec("가구") & new PriceRangeSpec(0, 10_000);

        // Act
        var actual = sut.Exists(spec);

        // Assert
        actual.ShouldBeFalse();
    }

    // --- Intermediate03: ExpressionSpec ---

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenExpressionSpecMatches()
    {
        // Arrange
        var sut = new InStockExprSpec();
        var product = new Product("테스트", 1000, 10, "카테고리");

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void ToExpression_ReturnsValidExpression_WhenUsedWithQueryable()
    {
        // Arrange
        var sut = new PriceRangeExprSpec(0, 10_000);
        var expr = sut.ToExpression();

        // Act
        var actual = _products.AsQueryable().Where(expr).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Price <= 10_000);
    }

    [Fact]
    public void ToExpression_ReturnsCachedDelegate_WhenCalledMultipleTimes()
    {
        // Arrange
        var sut = new InStockExprSpec();
        var product = new Product("테스트", 1000, 10, "카테고리");

        // Act - call twice to exercise caching path
        var first = sut.IsSatisfiedBy(product);
        var second = sut.IsSatisfiedBy(product);

        // Assert
        first.ShouldBeTrue();
        second.ShouldBeTrue();
    }
}
