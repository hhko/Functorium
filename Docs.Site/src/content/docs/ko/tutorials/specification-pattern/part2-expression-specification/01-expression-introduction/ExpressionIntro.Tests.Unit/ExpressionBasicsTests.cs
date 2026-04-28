using System.Linq.Expressions;
using ExpressionIntro;

namespace ExpressionIntro.Tests.Unit;

/// <summary>
/// Expression Tree 기초 테스트
///
/// 테스트 목적:
/// 1. Expression Body 타입 검증
/// 2. Expression 컴파일 결과 검증
/// 3. 동일 Expression 반복 컴파일 일관성 검증
/// </summary>
[Trait("Part2-Ch05-Expression-Introduction", "ExpressionBasicsTests")]
public class ExpressionBasicsTests
{
    private static readonly Product Laptop = new("노트북", 1_500_000, 10, "전자제품");
    private static readonly Product Pen = new("볼펜", 500, 100, "문구류");

    // 테스트 시나리오: Expression Body가 BinaryExpression(비교 연산)이어야 한다
    [Fact]
    public void Expression_ShouldHaveBinaryBody_WhenUsingComparisonOperator()
    {
        // Arrange
        Expression<Func<Product, bool>> expr = p => p.Price > 1000;

        // Act
        var body = expr.Body;

        // Assert
        body.NodeType.ShouldBe(ExpressionType.GreaterThan);
        body.ShouldBeAssignableTo<BinaryExpression>();
    }

    // 테스트 시나리오: Expression을 컴파일하면 올바른 결과를 반환해야 한다
    [Fact]
    public void CompiledExpression_ShouldReturnCorrectResult()
    {
        // Arrange
        Expression<Func<Product, bool>> expr = p => p.Price > 1000;

        // Act
        var compiled = expr.Compile();

        // Assert
        compiled(Laptop).ShouldBeTrue();
        compiled(Pen).ShouldBeFalse();
    }

    // 테스트 시나리오: 같은 Expression을 두 번 컴파일해도 동일한 결과를 반환해야 한다
    [Fact]
    public void CompiledExpression_ShouldReturnSameResults_WhenCompiledTwice()
    {
        // Arrange
        Expression<Func<Product, bool>> expr = p => p.Price > 1000;

        // Act
        var compiled1 = expr.Compile();
        var compiled2 = expr.Compile();

        // Assert
        compiled1(Laptop).ShouldBe(compiled2(Laptop));
        compiled1(Pen).ShouldBe(compiled2(Pen));
    }

    // 테스트 시나리오: Expression의 Parameters에 접근할 수 있어야 한다
    [Fact]
    public void Expression_ShouldExposeParameters()
    {
        // Arrange
        Expression<Func<Product, bool>> expr = p => p.Price > 1000;

        // Act & Assert
        expr.Parameters.Count.ShouldBe(1);
        expr.Parameters[0].Type.ShouldBe(typeof(Product));
    }

    // 테스트 시나리오: AsQueryable에서 Expression이 필터링에 사용될 수 있어야 한다
    [Fact]
    public void Expression_ShouldWorkWithAsQueryable()
    {
        // Arrange
        Expression<Func<Product, bool>> expr = p => p.Price > 1000;
        var products = new List<Product> { Laptop, Pen };

        // Act
        var results = products.AsQueryable().Where(expr).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("노트북");
    }
}
