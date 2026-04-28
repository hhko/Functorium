using FirstSpecification;
using FirstSpecification.Specifications;

namespace FirstSpecification.Tests.Unit;

/// <summary>
/// ProductInStockSpec 경계값 테스트
/// </summary>
[Trait("Part1-FirstSpecification", "ProductInStockSpecTests")]
public class ProductInStockSpecTests
{
    private readonly ProductInStockSpec _sut = new();

    // 테스트 시나리오: 재고가 0이면 조건을 만족하지 않아야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenStockIsZero()
    {
        // Arrange
        var product = new Product("테스트", 1000m, 0, "카테고리");

        // Act
        var actual = _sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: 재고가 1이면 조건을 만족해야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenStockIsOne()
    {
        // Arrange
        var product = new Product("테스트", 1000m, 1, "카테고리");

        // Act
        var actual = _sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
