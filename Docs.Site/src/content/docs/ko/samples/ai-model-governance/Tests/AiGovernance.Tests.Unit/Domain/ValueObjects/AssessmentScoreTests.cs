using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class AssessmentScoreTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(70)]
    [InlineData(100)]
    public void Create_ShouldSucceed_WhenValueIsInRange(int value)
    {
        // Act
        var actual = AssessmentScore.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((int)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(-100)]
    [InlineData(200)]
    public void Create_ShouldFail_WhenValueIsOutOfRange(int value)
    {
        // Act
        var actual = AssessmentScore.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData(70, true)]
    [InlineData(80, true)]
    [InlineData(100, true)]
    [InlineData(69, false)]
    [InlineData(0, false)]
    [InlineData(50, false)]
    public void IsPassing_ShouldReturnExpected(int value, bool expected)
    {
        // Arrange
        var sut = AssessmentScore.CreateFromValidated(value);

        // Act
        var actual = sut.IsPassing;

        // Assert
        actual.ShouldBe(expected);
    }
}
