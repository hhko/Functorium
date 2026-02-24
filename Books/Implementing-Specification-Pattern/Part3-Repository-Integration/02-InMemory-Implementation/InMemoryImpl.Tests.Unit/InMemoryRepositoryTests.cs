using InMemoryImpl;
using InMemoryImpl.Specifications;

namespace InMemoryImpl.Tests.Unit;

/// <summary>
/// InMemoryProductRepository 동작 테스트
/// FindAll과 Exists가 Specification과 올바르게 연동되는지 검증
/// </summary>
[Trait("Part3-InMemoryImpl", "InMemoryRepositoryTests")]
public class InMemoryRepositoryTests
{
    private readonly InMemoryProductRepository _sut = new(SampleProducts.Create());

    // 테스트 시나리오: FindAll은 조건을 만족하는 상품만 반환해야 한다
    [Fact]
    public void FindAll_ShouldReturnMatchingProducts_WhenInStockSpec()
    {
        // Arrange
        var spec = new InStockSpec();

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock > 0);
    }

    // 테스트 시나리오: FindAll은 가격 범위 내 상품만 반환해야 한다
    [Fact]
    public void FindAll_ShouldReturnMatchingProducts_WhenPriceRangeSpec()
    {
        // Arrange
        var spec = new PriceRangeSpec(0, 10_000);

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Price >= 0 && p.Price <= 10_000);
    }

    // 테스트 시나리오: FindAll은 조합된 Specification도 올바르게 처리해야 한다
    [Fact]
    public void FindAll_ShouldReturnMatchingProducts_WhenCombinedSpec()
    {
        // Arrange
        var spec = new InStockSpec() & new CategorySpec("전자제품");

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldNotBeEmpty();
        actual.ShouldAllBe(p => p.Stock > 0 && p.Category == "전자제품");
    }

    // 테스트 시나리오: FindAll은 조건을 만족하는 상품이 없으면 빈 컬렉션을 반환해야 한다
    [Fact]
    public void FindAll_ShouldReturnEmpty_WhenNoProductsMatch()
    {
        // Arrange
        var spec = new CategorySpec("존재하지않는카테고리");

        // Act
        var actual = _sut.FindAll(spec).ToList();

        // Assert
        actual.ShouldBeEmpty();
    }

    // 테스트 시나리오: Exists는 조건을 만족하는 상품이 있으면 true를 반환해야 한다
    [Fact]
    public void Exists_ShouldReturnTrue_WhenMatchingProductExists()
    {
        // Arrange
        var spec = new CategorySpec("전자제품") & new InStockSpec();

        // Act
        var actual = _sut.Exists(spec);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: Exists는 조건을 만족하는 상품이 없으면 false를 반환해야 한다
    [Fact]
    public void Exists_ShouldReturnFalse_WhenNoMatchingProductExists()
    {
        // Arrange
        var spec = new CategorySpec("가구") & new PriceRangeSpec(0, 10_000);

        // Act
        var actual = _sut.Exists(spec);

        // Assert
        actual.ShouldBeFalse();
    }
}
