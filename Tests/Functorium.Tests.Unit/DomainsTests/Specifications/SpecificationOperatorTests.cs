using Functorium.Domains.Specifications;
using System.Linq.Expressions;

namespace Functorium.Tests.Unit.DomainsTests.Specifications;

/// <summary>
/// 양수 조건 테스트 Specification
/// </summary>
file sealed class IsPositiveSpec : Specification<int>
{
    public override bool IsSatisfiedBy(int entity) => entity > 0;
}

/// <summary>
/// 짝수 조건 테스트 Specification
/// </summary>
file sealed class IsEvenSpec : Specification<int>
{
    public override bool IsSatisfiedBy(int entity) => entity % 2 == 0;
}

public class SpecificationOperatorTests
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    public void AndOperator_ReturnsExpected_WhenEvaluated(int value, bool expected)
    {
        // Arrange
        var sut = new IsPositiveSpec() & new IsEvenSpec();

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(-2, true)]
    [InlineData(-1, false)]
    public void OrOperator_ReturnsExpected_WhenEvaluated(int value, bool expected)
    {
        // Arrange
        var sut = new IsPositiveSpec() | new IsEvenSpec();

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void NotOperator_ReturnsInverted_WhenEvaluated(int value, bool expected)
    {
        // Arrange
        var sut = !new IsPositiveSpec();

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    [InlineData(-1, false)]
    public void Composite_AndNot_ReturnsExpected_WhenEvaluated(int value, bool expected)
    {
        // Arrange: 양수이면서 짝수가 아닌 수
        var sut = new IsPositiveSpec() & !new IsEvenSpec();

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    public void Operator_ReturnsSameResult_AsMethodEquivalent(int value, bool expected)
    {
        // Arrange
        var operatorResult = new IsPositiveSpec() & new IsEvenSpec();
        var methodResult = new IsPositiveSpec().And(new IsEvenSpec());

        // Act
        var actualOperator = operatorResult.IsSatisfiedBy(value);
        var actualMethod = methodResult.IsSatisfiedBy(value);

        // Assert
        actualOperator.ShouldBe(expected);
        actualMethod.ShouldBe(expected);
        actualOperator.ShouldBe(actualMethod);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void All_IsSatisfiedBy_ReturnsTrue_WhenAnyValue(int value)
    {
        // Arrange
        var sut = Specification<int>.All;

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void All_IsAll_ReturnsTrue()
    {
        // Arrange
        var sut = Specification<int>.All;

        // Act & Assert
        sut.IsAll.ShouldBeTrue();
    }

    [Fact]
    public void All_ToExpression_ReturnsTrueForAll()
    {
        // Arrange
        var sut = (IExpressionSpec<int>)Specification<int>.All;

        // Act
        Expression<Func<int, bool>> expr = sut.ToExpression();
        var compiled = expr.Compile();

        // Assert
        compiled(0).ShouldBeTrue();
        compiled(-1).ShouldBeTrue();
        compiled(int.MaxValue).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void AllAndSpec_ReturnsSpec_WhenLeftIsAll(int value, bool expected)
    {
        // Arrange: All & X = X (항등원 — 좌측)
        var sut = Specification<int>.All & new IsPositiveSpec();

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void SpecAndAll_ReturnsSpec_WhenRightIsAll(int value, bool expected)
    {
        // Arrange: X & All = X (항등원 — 우측)
        var sut = new IsPositiveSpec() & Specification<int>.All;

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void AllAndSpec_ReturnsOriginalSpec_NotAndSpecification()
    {
        // Arrange & Act: All & X should return X itself, not wrapped in AndSpecification
        var spec = new IsPositiveSpec();
        var result = Specification<int>.All & spec;

        // Assert
        result.ShouldBeSameAs(spec);
    }

    [Fact]
    public void SpecAndAll_ReturnsOriginalSpec_NotAndSpecification()
    {
        // Arrange & Act: X & All should return X itself, not wrapped in AndSpecification
        var spec = new IsPositiveSpec();
        var result = spec & Specification<int>.All;

        // Assert
        result.ShouldBeSameAs(spec);
    }
}
