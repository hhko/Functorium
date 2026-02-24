using FirstSpecification;
using FirstSpecification.Specifications;

namespace FirstSpecification.Tests.Unit;

/// <summary>
/// PriceRangeSpec 경계값 테스트
/// </summary>
[Trait("Part1-FirstSpecification", "PriceRangeSpecTests")]
public class PriceRangeSpecTests
{
    private readonly PriceRangeSpec _sut = new(100m, 500m);

    // 테스트 시나리오: 가격이 정확히 최솟값이면 조건을 만족해야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenPriceIsExactlyMin()
    {
        // Arrange
        var product = new Product("테스트", 100m, 1, "카테고리");

        // Act
        var actual = _sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 가격이 정확히 최댓값이면 조건을 만족해야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenPriceIsExactlyMax()
    {
        // Arrange
        var product = new Product("테스트", 500m, 1, "카테고리");

        // Act
        var actual = _sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: 가격이 최솟값보다 낮으면 조건을 만족하지 않아야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenPriceIsBelowMin()
    {
        // Arrange
        var product = new Product("테스트", 99.99m, 1, "카테고리");

        // Act
        var actual = _sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: 가격이 최댓값보다 높으면 조건을 만족하지 않아야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenPriceIsAboveMax()
    {
        // Arrange
        var product = new Product("테스트", 500.01m, 1, "카테고리");

        // Act
        var actual = _sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }
}
