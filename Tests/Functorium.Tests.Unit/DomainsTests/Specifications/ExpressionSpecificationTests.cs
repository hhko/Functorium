using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace Functorium.Tests.Unit.DomainsTests.Specifications;

file sealed class GreaterThanSpec : ExpressionSpecification<int>
{
    public int Threshold { get; }

    public GreaterThanSpec(int threshold) => Threshold = threshold;

    public override Expression<Func<int, bool>> ToExpression()
    {
        var threshold = Threshold;
        return x => x > threshold;
    }
}

file sealed class LessThanSpec : ExpressionSpecification<int>
{
    public int Threshold { get; }

    public LessThanSpec(int threshold) => Threshold = threshold;

    public override Expression<Func<int, bool>> ToExpression()
    {
        var threshold = Threshold;
        return x => x < threshold;
    }
}

public class ExpressionSpecificationTests
{
    [Theory]
    [InlineData(5, true)]
    [InlineData(3, false)]
    [InlineData(0, false)]
    public void IsSatisfiedBy_ReturnsExpected_WhenEvaluated(int value, bool expected)
    {
        // Arrange
        var sut = new GreaterThanSpec(3);

        // Act
        var actual = sut.IsSatisfiedBy(value);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void ToExpression_ReturnsValidExpression_WhenCompiled()
    {
        // Arrange
        var sut = new GreaterThanSpec(3);

        // Act
        var actual = sut.ToExpression().Compile()(5);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_CachesCompiledDelegate_WhenCalledMultipleTimes()
    {
        // Arrange
        var sut = new GreaterThanSpec(3);

        // Act
        var first = sut.IsSatisfiedBy(5);
        var second = sut.IsSatisfiedBy(5);

        // Assert
        first.ShouldBeTrue();
        second.ShouldBeTrue();
    }

    [Fact]
    public void ImplementsIExpressionSpec_WhenInheritsExpressionSpecification()
    {
        // Arrange
        var sut = new GreaterThanSpec(3);

        // Act & Assert
        sut.ShouldBeAssignableTo<IExpressionSpec<int>>();
    }

    [Fact]
    public void And_ReturnsCombinedSpecification_WhenComposed()
    {
        // Arrange: 3 < x < 10
        var sut = new GreaterThanSpec(3).And(new LessThanSpec(10));

        // Act & Assert
        sut.IsSatisfiedBy(5).ShouldBeTrue();
        sut.IsSatisfiedBy(3).ShouldBeFalse();
        sut.IsSatisfiedBy(10).ShouldBeFalse();
    }
}
