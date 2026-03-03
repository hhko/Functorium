using ValueObjectConversion;
using ValueObjectConversion.Specifications;

namespace ValueObjectConversion.Tests.Unit;

/// <summary>
/// Value Object Primitive 변환 테스트
///
/// 테스트 목적:
/// 1. Value Object 속성을 가진 Product에 대해 각 Specification이 올바르게 동작하는지 검증
/// 2. Expression Tree에서 Value Object → primitive 변환이 정상 동작하는지 검증
/// </summary>
[Trait("Part2-Ch07-ValueObject-Conversion", "ValueObjectConversionTests")]
public class ValueObjectConversionTests
{
    private static readonly Product Laptop = new(
        new ProductName("노트북"), new Money(1_500_000), new Quantity(10), "전자제품");
    private static readonly Product Mouse = new(
        new ProductName("마우스"), new Money(25_000), new Quantity(50), "전자제품");
    private static readonly Product OutOfStock = new(
        new ProductName("품절 키보드"), new Money(80_000), new Quantity(0), "전자제품");
    private static readonly Product Pen = new(
        new ProductName("볼펜"), new Money(500), new Quantity(3), "문구류");

    // 테스트 시나리오: 이름이 일치하는 상품은 ProductNameSpec을 만족해야 한다
    [Fact]
    public void ProductNameSpec_ShouldBeSatisfied_WhenNameMatches()
    {
        // Arrange
        var spec = new ProductNameSpec(new ProductName("노트북"));

        // Act & Assert
        spec.IsSatisfiedBy(Laptop).ShouldBeTrue();
        spec.IsSatisfiedBy(Mouse).ShouldBeFalse();
    }

    // 테스트 시나리오: 가격 범위 내 상품은 ProductPriceRangeSpec을 만족해야 한다
    [Fact]
    public void ProductPriceRangeSpec_ShouldBeSatisfied_WhenPriceInRange()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(10_000), new Money(100_000));

        // Act & Assert
        spec.IsSatisfiedBy(Mouse).ShouldBeTrue();       // 25,000원
        spec.IsSatisfiedBy(OutOfStock).ShouldBeTrue();   // 80,000원
        spec.IsSatisfiedBy(Laptop).ShouldBeFalse();      // 1,500,000원
        spec.IsSatisfiedBy(Pen).ShouldBeFalse();          // 500원
    }

    // 테스트 시나리오: 재고가 임계값 이하인 상품은 ProductLowStockSpec을 만족해야 한다
    [Fact]
    public void ProductLowStockSpec_ShouldBeSatisfied_WhenStockBelowThreshold()
    {
        // Arrange
        var spec = new ProductLowStockSpec(new Quantity(5));

        // Act & Assert
        spec.IsSatisfiedBy(OutOfStock).ShouldBeTrue();   // 재고 0
        spec.IsSatisfiedBy(Pen).ShouldBeTrue();           // 재고 3
        spec.IsSatisfiedBy(Laptop).ShouldBeFalse();      // 재고 10
        spec.IsSatisfiedBy(Mouse).ShouldBeFalse();       // 재고 50
    }

    // 테스트 시나리오: ToExpression이 AsQueryable에서 사용 가능해야 한다
    [Fact]
    public void ProductNameSpec_ToExpression_ShouldWorkWithAsQueryable()
    {
        // Arrange
        var spec = new ProductNameSpec(new ProductName("마우스"));
        var products = new List<Product> { Laptop, Mouse, OutOfStock, Pen };

        // Act
        var results = products.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.Value.ShouldBe("마우스");
    }

    // 테스트 시나리오: ProductPriceRangeSpec의 ToExpression이 AsQueryable에서 사용 가능해야 한다
    [Fact]
    public void ProductPriceRangeSpec_ToExpression_ShouldWorkWithAsQueryable()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(10_000), new Money(100_000));
        var products = new List<Product> { Laptop, Mouse, OutOfStock, Pen };

        // Act
        var results = products.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        results.Count.ShouldBe(2);
    }

    // 테스트 시나리오: ProductLowStockSpec의 ToExpression이 유효한 Expression을 반환해야 한다
    [Fact]
    public void ProductLowStockSpec_ToExpression_ShouldReturnValidExpression()
    {
        // Arrange
        var spec = new ProductLowStockSpec(new Quantity(5));

        // Act
        var expr = spec.ToExpression();

        // Assert
        expr.ShouldNotBeNull();
        expr.Parameters.Count.ShouldBe(1);
        expr.Parameters[0].Type.ShouldBe(typeof(Product));
    }
}
