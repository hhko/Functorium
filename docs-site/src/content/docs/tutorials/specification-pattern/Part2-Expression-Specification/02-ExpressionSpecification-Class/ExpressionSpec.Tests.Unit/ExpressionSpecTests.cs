using ExpressionSpec;
using ExpressionSpec.Specifications;

namespace ExpressionSpec.Tests.Unit;

/// <summary>
/// ExpressionSpecification 클래스 테스트
///
/// 테스트 목적:
/// 1. IsSatisfiedBy가 Expression 컴파일 결과를 올바르게 반환하는지 검증
/// 2. ToExpression이 유효한 Expression을 반환하는지 검증
/// 3. 델리게이트 캐싱이 일관된 결과를 보장하는지 검증
/// </summary>
[Trait("Part2-Ch06-ExpressionSpecification-Class", "ExpressionSpecTests")]
public class ExpressionSpecTests
{
    private static readonly Product InStockProduct = new("노트북", 1_500_000, 10, "전자제품");
    private static readonly Product OutOfStockProduct = new("품절 키보드", 80_000, 0, "전자제품");
    private static readonly Product CheapProduct = new("볼펜", 500, 100, "문구류");

    // 테스트 시나리오: 재고가 있는 상품은 ProductInStockSpec을 만족해야 한다
    [Fact]
    public void ProductInStockSpec_ShouldBeSatisfied_WhenStockIsPositive()
    {
        // Arrange
        var spec = new ProductInStockSpec();

        // Act & Assert
        spec.IsSatisfiedBy(InStockProduct).ShouldBeTrue();
        spec.IsSatisfiedBy(OutOfStockProduct).ShouldBeFalse();
    }

    // 테스트 시나리오: 가격 범위 내 상품은 ProductPriceRangeSpec을 만족해야 한다
    [Fact]
    public void ProductPriceRangeSpec_ShouldBeSatisfied_WhenPriceInRange()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(1_000, 100_000);

        // Act & Assert
        spec.IsSatisfiedBy(OutOfStockProduct).ShouldBeTrue();  // 80,000원
        spec.IsSatisfiedBy(InStockProduct).ShouldBeFalse();     // 1,500,000원
        spec.IsSatisfiedBy(CheapProduct).ShouldBeFalse();       // 500원
    }

    // 테스트 시나리오: 특정 카테고리 상품은 ProductCategorySpec을 만족해야 한다
    [Fact]
    public void ProductCategorySpec_ShouldBeSatisfied_WhenCategoryMatches()
    {
        // Arrange
        var spec = new ProductCategorySpec("전자제품");

        // Act & Assert
        spec.IsSatisfiedBy(InStockProduct).ShouldBeTrue();
        spec.IsSatisfiedBy(CheapProduct).ShouldBeFalse();
    }

    // 테스트 시나리오: ToExpression이 유효한 Expression을 반환해야 한다
    [Fact]
    public void ToExpression_ShouldReturnValidExpression()
    {
        // Arrange
        var spec = new ProductInStockSpec();

        // Act
        var expr = spec.ToExpression();

        // Assert
        expr.ShouldNotBeNull();
        expr.Parameters.Count.ShouldBe(1);
        expr.Parameters[0].Type.ShouldBe(typeof(Product));
    }

    // 테스트 시나리오: ToExpression 결과로 AsQueryable 필터링이 가능해야 한다
    [Fact]
    public void ToExpression_ShouldWorkWithAsQueryable()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(0, 50_000);
        var products = new List<Product> { InStockProduct, OutOfStockProduct, CheapProduct };

        // Act
        var expr = spec.ToExpression();
        var results = products.AsQueryable().Where(expr).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("볼펜");
    }

    // 테스트 시나리오: IsSatisfiedBy를 여러 번 호출해도 캐싱으로 일관된 결과를 반환해야 한다
    [Fact]
    public void IsSatisfiedBy_ShouldReturnConsistentResults_WhenCalledMultipleTimes()
    {
        // Arrange
        var spec = new ProductInStockSpec();

        // Act & Assert - 캐싱된 델리게이트가 일관된 결과를 반환하는지 확인
        spec.IsSatisfiedBy(InStockProduct).ShouldBeTrue();
        spec.IsSatisfiedBy(InStockProduct).ShouldBeTrue();
        spec.IsSatisfiedBy(OutOfStockProduct).ShouldBeFalse();
        spec.IsSatisfiedBy(OutOfStockProduct).ShouldBeFalse();
    }
}
