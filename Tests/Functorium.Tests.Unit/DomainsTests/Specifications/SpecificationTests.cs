using Functorium.Domains.Specifications;

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

public class SpecificationTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsSatisfiedBy_ReturnsExpected_WhenEvaluated(int value, bool expected)
    {
        // Arrange
        var sut = new IsPositiveSpec();

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
    public void And_ReturnsTrue_WhenBothSatisfied(int value, bool expected)
    {
        // Arrange
        var sut = new IsPositiveSpec().And(new IsEvenSpec());

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
    public void Or_ReturnsTrue_WhenEitherSatisfied(int value, bool expected)
    {
        // Arrange
        var sut = new IsPositiveSpec().Or(new IsEvenSpec());

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void Not_ReturnsInverted_WhenEvaluated(int value, bool expected)
    {
        // Arrange
        var sut = new IsPositiveSpec().Not();

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    [InlineData(-1, false)]
    public void Composite_And_Not_ReturnsExpected_WhenEvaluated(int value, bool expected)
    {
        // Arrange: 양수이면서 짝수가 아닌 수
        var sut = new IsPositiveSpec().And(new IsEvenSpec().Not());

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }
}
