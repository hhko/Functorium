using Composition;
using Composition.Specifications;

namespace Composition.Tests.Unit;

/// <summary>
/// Specification And/Or/Not 조합 테스트
/// </summary>
[Trait("Part1-Composition", "CompositionTests")]
public class CompositionTests
{
    private readonly ProductInStockSpec _inStock = new();
    private readonly ProductPriceRangeSpec _affordable = new(10_000m, 100_000m);
    private readonly ProductCategorySpec _electronics = new("전자제품");

    // 테스트 시나리오: And 조합은 두 조건 모두 만족할 때만 true
    [Fact]
    public void And_ShouldReturnTrue_WhenBothSpecsAreSatisfied()
    {
        // Arrange
        var product = new Product("키보드", 89_000m, 3, "주변기기");
        var spec = _inStock.And(_affordable);

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: And 조합은 하나라도 불만족하면 false
    [Fact]
    public void And_ShouldReturnFalse_WhenOneSpecIsNotSatisfied()
    {
        // Arrange
        var product = new Product("마우스", 25_000m, 0, "전자제품"); // 재고 없음
        var spec = _inStock.And(_affordable);

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: Or 조합은 하나라도 만족하면 true
    [Fact]
    public void Or_ShouldReturnTrue_WhenEitherSpecIsSatisfied()
    {
        // Arrange
        var product = new Product("노트북", 1_200_000m, 5, "전자제품"); // 전자제품이지만 비쌈
        var spec = _electronics.Or(_affordable);

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: Or 조합은 둘 다 불만족하면 false
    [Fact]
    public void Or_ShouldReturnFalse_WhenNeitherSpecIsSatisfied()
    {
        // Arrange
        var product = new Product("모니터", 350_000m, 2, "주변기기"); // 전자제품 아님, 비쌈
        var spec = _electronics.Or(_affordable);

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: Not은 원래 결과를 반전
    [Fact]
    public void Not_ShouldNegateResult_WhenApplied()
    {
        // Arrange
        var electronicsProduct = new Product("노트북", 1_200_000m, 5, "전자제품");
        var nonElectronicsProduct = new Product("키보드", 89_000m, 3, "주변기기");
        var spec = _electronics.Not();

        // Act & Assert
        spec.IsSatisfiedBy(electronicsProduct).ShouldBeFalse();
        spec.IsSatisfiedBy(nonElectronicsProduct).ShouldBeTrue();
    }

    // 테스트 시나리오: 복합 조합 (And + Not)
    [Fact]
    public void AndNot_ShouldCombineCorrectly_WhenChained()
    {
        // Arrange: 재고 있고 AND 전자제품이 아닌 상품
        var product = new Product("키보드", 89_000m, 3, "주변기기");
        var spec = _inStock.And(_electronics.Not());

        // Act
        var actual = spec.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
