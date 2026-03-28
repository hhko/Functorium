using Functorium.Domains.Specifications;
using SpecificationPattern.Demo;
using SpecificationPattern.Demo.Basic;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Tests.Unit;

public sealed class BasicSpecTests
{
    private readonly List<Product> _products = SampleProducts.Create();

    // --- Basic01: IsSatisfiedBy ---

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenProductIsInStock()
    {
        // Arrange
        var sut = new InStockSpec();
        var product = new Product("테스트", 1000, 10, "카테고리");

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenProductIsOutOfStock()
    {
        // Arrange
        var sut = new InStockSpec();
        var product = new Product("테스트", 1000, 0, "카테고리");

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Theory]
    [InlineData(5000, true)]
    [InlineData(10000, true)]
    [InlineData(10001, false)]
    [InlineData(0, true)]
    public void IsSatisfiedBy_ReturnsExpected_WhenPriceRangeChecked(decimal price, bool expected)
    {
        // Arrange
        var sut = new PriceRangeSpec(0, 10_000);
        var product = new Product("테스트", price, 1, "카테고리");

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenCategoryMatches()
    {
        // Arrange
        var sut = new CategorySpec("전자제품");
        var product = new Product("마우스", 15000, 50, "전자제품");

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // --- Basic02: Composition ---

    [Fact]
    public void And_FiltersProducts_WhenBothConditionsMet()
    {
        // Arrange
        var sut = new InStockSpec().And(new PriceRangeSpec(0, 10_000));

        // Act
        var actual = _products.Where(sut.IsSatisfiedBy).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock > 0 && p.Price <= 10_000);
    }

    [Fact]
    public void Or_FiltersProducts_WhenEitherConditionMet()
    {
        // Arrange
        var sut = new CategorySpec("전자제품").Or(new PriceRangeSpec(0, 5_000));

        // Act
        var actual = _products.Where(sut.IsSatisfiedBy).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Category == "전자제품" || p.Price <= 5_000);
    }

    [Fact]
    public void Not_NegatesCondition()
    {
        // Arrange
        var sut = new InStockSpec().Not();

        // Act
        var actual = _products.Where(sut.IsSatisfiedBy).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock == 0);
    }

    // --- Basic03: Operators ---

    [Fact]
    public void Operators_ProduceSameResults_AsMethodCalls()
    {
        // Arrange
        var methodResult = new InStockSpec().And(new PriceRangeSpec(0, 10_000));
        var operatorResult = new InStockSpec() & new PriceRangeSpec(0, 10_000);

        // Act
        var methodFiltered = _products.Where(methodResult.IsSatisfiedBy).ToList();
        var operatorFiltered = _products.Where(operatorResult.IsSatisfiedBy).ToList();

        // Assert
        operatorFiltered.ShouldBe(methodFiltered);
    }

    [Fact]
    public void NotOperator_ProducesSameResults_AsNotMethod()
    {
        // Arrange
        var methodResult = new InStockSpec().Not();
        var operatorResult = !new InStockSpec();

        // Act
        var methodFiltered = _products.Where(methodResult.IsSatisfiedBy).ToList();
        var operatorFiltered = _products.Where(operatorResult.IsSatisfiedBy).ToList();

        // Assert
        operatorFiltered.ShouldBe(methodFiltered);
    }
}
