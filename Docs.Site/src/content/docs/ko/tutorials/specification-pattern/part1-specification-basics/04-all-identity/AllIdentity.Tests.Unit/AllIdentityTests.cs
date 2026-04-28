using AllIdentity;
using AllIdentity.Specifications;
using Functorium.Domains.Specifications;

namespace AllIdentity.Tests.Unit;

/// <summary>
/// All 항등원 및 동적 필터 패턴 테스트
/// </summary>
[Trait("Part1-AllIdentity", "AllIdentityTests")]
public class AllIdentityTests
{
    // 테스트 시나리오: All은 어떤 엔터티에 대해서도 항상 true를 반환해야 한다
    [Fact]
    public void All_ShouldReturnTrue_ForAnyProduct()
    {
        // Arrange
        var all = Specification<Product>.All;
        var product = new Product("테스트", 1000m, 0, "카테고리");

        // Act
        var actual = all.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    // 테스트 시나리오: All & X 는 X와 동일한 참조를 반환해야 한다 (항등원)
    [Fact]
    public void AllAndSpec_ShouldReturnSameReference_AsOriginalSpec()
    {
        // Arrange
        var all = Specification<Product>.All;
        var inStock = new ProductInStockSpec();

        // Act
        var result = all & inStock;

        // Assert
        ReferenceEquals(result, inStock).ShouldBeTrue();
    }

    // 테스트 시나리오: X & All 도 X와 동일한 참조를 반환해야 한다
    [Fact]
    public void SpecAndAll_ShouldReturnSameReference_AsOriginalSpec()
    {
        // Arrange
        var all = Specification<Product>.All;
        var inStock = new ProductInStockSpec();

        // Act
        var result = inStock & all;

        // Assert
        ReferenceEquals(result, inStock).ShouldBeTrue();
    }

    // 테스트 시나리오: 동적 필터가 조건 없이 구성되면 모든 상품이 반환되어야 한다
    [Fact]
    public void DynamicFilter_ShouldReturnAllProducts_WhenNoConditionsAdded()
    {
        // Arrange
        var spec = Specification<Product>.All;
        var products = SampleProducts.All;

        // Act
        var actual = products.Where(p => spec.IsSatisfiedBy(p)).ToArray();

        // Assert
        actual.Length.ShouldBe(products.Length);
    }

    // 테스트 시나리오: 동적 필터에 조건을 추가하면 해당 조건에 맞는 상품만 반환
    [Fact]
    public void DynamicFilter_ShouldFilterCorrectly_WhenConditionsAdded()
    {
        // Arrange
        var spec = Specification<Product>.All;
        spec &= new ProductInStockSpec();
        spec &= new ProductCategorySpec("전자제품");

        // Act
        var actual = SampleProducts.All.Where(p => spec.IsSatisfiedBy(p)).ToArray();

        // Assert: 재고 있는 전자제품만
        actual.ShouldAllBe(p => p.Stock > 0);
        actual.ShouldAllBe(p => p.Category == "전자제품");
    }
}
