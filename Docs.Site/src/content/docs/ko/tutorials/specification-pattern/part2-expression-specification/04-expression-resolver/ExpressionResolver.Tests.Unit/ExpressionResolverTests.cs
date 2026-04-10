using ExpressionResolver;
using ExpressionResolver.Specifications;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;

namespace ExpressionResolver.Tests.Unit;

/// <summary>
/// SpecificationExpressionResolver 테스트
///
/// 테스트 목적:
/// 1. 단일 ExpressionSpecification에서 Expression 추출 검증
/// 2. And/Or/Not 복합 Specification에서 합성된 Expression 추출 검증
/// 3. Non-expression Specification에서 null 반환 검증
/// </summary>
[Trait("Part2-Ch08-Expression-Resolver", "ExpressionResolverTests")]
public class ExpressionResolverTests
{
    private static readonly Product InStockCheap = new("마우스", 25_000, 50, "전자제품");
    private static readonly Product InStockExpensive = new("노트북", 1_500_000, 10, "전자제품");
    private static readonly Product OutOfStock = new("품절 키보드", 80_000, 0, "전자제품");

    // 테스트 시나리오: 단일 ExpressionSpec에서 Expression을 추출해야 한다
    [Fact]
    public void TryResolve_ShouldReturnExpression_WhenSingleExpressionSpec()
    {
        // Arrange
        var spec = new ProductInStockSpec();

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        expr.ShouldNotBeNull();
        var compiled = expr.Compile();
        compiled(InStockCheap).ShouldBeTrue();
        compiled(OutOfStock).ShouldBeFalse();
    }

    // 테스트 시나리오: And 복합 Specification에서 합성된 Expression을 추출해야 한다
    [Fact]
    public void TryResolve_ShouldReturnCombinedExpression_WhenAndComposite()
    {
        // Arrange
        Specification<Product> spec = new ProductInStockSpec() & new ProductPriceRangeSpec(0, 50_000);

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        expr.ShouldNotBeNull();
        var compiled = expr.Compile();
        compiled(InStockCheap).ShouldBeTrue();         // 재고 있고 5만원 이하
        compiled(InStockExpensive).ShouldBeFalse();    // 재고 있지만 5만원 초과
        compiled(OutOfStock).ShouldBeFalse();          // 재고 없음
    }

    // 테스트 시나리오: Or 복합 Specification에서 합성된 Expression을 추출해야 한다
    [Fact]
    public void TryResolve_ShouldReturnCombinedExpression_WhenOrComposite()
    {
        // Arrange
        Specification<Product> spec = new ProductInStockSpec() | new ProductPriceRangeSpec(0, 50_000);

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        expr.ShouldNotBeNull();
        var compiled = expr.Compile();
        compiled(InStockCheap).ShouldBeTrue();         // 재고 있음 (둘 다 만족)
        compiled(InStockExpensive).ShouldBeTrue();     // 재고 있음
        compiled(OutOfStock).ShouldBeFalse();          // 재고 없고 5만원 초과
    }

    // 테스트 시나리오: Not Specification에서 부정된 Expression을 추출해야 한다
    [Fact]
    public void TryResolve_ShouldReturnNegatedExpression_WhenNotSpec()
    {
        // Arrange
        Specification<Product> spec = !new ProductPriceRangeSpec(50_000, decimal.MaxValue);

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        expr.ShouldNotBeNull();
        var compiled = expr.Compile();
        compiled(InStockCheap).ShouldBeTrue();         // 25,000원 → 5만원 이상이 아님
        compiled(InStockExpensive).ShouldBeFalse();    // 1,500,000원 → 5만원 이상
    }

    // 테스트 시나리오: Non-expression Specification은 null을 반환해야 한다
    [Fact]
    public void TryResolve_ShouldReturnNull_WhenNonExpressionSpec()
    {
        // Arrange
        var spec = new ProductInStockPlainSpec();

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        expr.ShouldBeNull();
    }

    // 테스트 시나리오: Expression/Non-expression 혼합 시 null을 반환해야 한다
    [Fact]
    public void TryResolve_ShouldReturnNull_WhenMixedWithNonExpressionSpec()
    {
        // Arrange
        Specification<Product> spec = new ProductInStockPlainSpec() & new ProductPriceRangeSpec(0, 50_000);

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        expr.ShouldBeNull();
    }

    // 테스트 시나리오: 추출된 Expression으로 AsQueryable 필터링이 가능해야 한다
    [Fact]
    public void TryResolve_ShouldWorkWithAsQueryable()
    {
        // Arrange
        Specification<Product> spec = new ProductInStockSpec() & new ProductPriceRangeSpec(0, 50_000);
        var products = new List<Product> { InStockCheap, InStockExpensive, OutOfStock };

        // Act
        var expr = SpecificationExpressionResolver.TryResolve(spec);
        var results = products.AsQueryable().Where(expr!).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("마우스");
    }
}
