using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;

namespace Functorium.Tests.Unit.DomainsTests.Specifications;

file sealed class IsPositiveExprSpec : ExpressionSpecification<int>
{
    public override Expression<Func<int, bool>> ToExpression() => x => x > 0;
}

file sealed class IsEvenExprSpec : ExpressionSpecification<int>
{
    public override Expression<Func<int, bool>> ToExpression() => x => x % 2 == 0;
}

file sealed class NonExpressionSpec : Specification<int>
{
    public override bool IsSatisfiedBy(int entity) => entity > 0;
}

public class SpecificationExpressionResolverTests
{
    [Fact]
    public void TryResolve_ReturnsExpression_WhenExpressionSpecification()
    {
        // Arrange
        var spec = new IsPositiveExprSpec();

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
        actual!.Compile()(5).ShouldBeTrue();
        actual.Compile()(-1).ShouldBeFalse();
    }

    [Fact]
    public void TryResolve_ReturnsNull_WhenNonExpressionSpecification()
    {
        // Arrange
        var spec = new NonExpressionSpec();

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldBeNull();
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(-2, false)]
    [InlineData(0, false)]
    public void TryResolve_CombinesAndExpressions_WhenAndSpecification(int value, bool expected)
    {
        // Arrange: 양수 AND 짝수
        var spec = new IsPositiveExprSpec().And(new IsEvenExprSpec());

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
        actual!.Compile()(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(-2, true)]
    [InlineData(-1, false)]
    public void TryResolve_CombinesOrExpressions_WhenOrSpecification(int value, bool expected)
    {
        // Arrange: 양수 OR 짝수
        var spec = new IsPositiveExprSpec().Or(new IsEvenExprSpec());

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
        actual!.Compile()(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(-1, true)]
    public void TryResolve_NegatesExpression_WhenNotSpecification(int value, bool expected)
    {
        // Arrange: NOT 양수
        var spec = new IsPositiveExprSpec().Not();

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
        actual!.Compile()(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    [InlineData(-1, false)]
    public void TryResolve_CombinesComplexComposition_WhenNestedAndOrNot(int value, bool expected)
    {
        // Arrange: 양수 AND NOT 짝수
        var spec = new IsPositiveExprSpec() & !new IsEvenExprSpec();

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldNotBeNull();
        actual!.Compile()(value).ShouldBe(expected);
    }

    [Fact]
    public void TryResolve_ReturnsNull_WhenAndContainsNonExpressionSpec()
    {
        // Arrange
        var spec = new IsPositiveExprSpec().And(new NonExpressionSpec());

        // Act
        var actual = SpecificationExpressionResolver.TryResolve(spec);

        // Assert
        actual.ShouldBeNull();
    }
}
