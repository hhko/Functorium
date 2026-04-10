using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class AssessmentStatusTests
{
    [Theory]
    [InlineData("Initiated", "InProgress", true)]
    [InlineData("InProgress", "Passed", true)]
    [InlineData("InProgress", "Failed", true)]
    [InlineData("InProgress", "RequiresRemediation", true)]
    [InlineData("Initiated", "Passed", false)]
    [InlineData("Initiated", "Failed", false)]
    [InlineData("Passed", "InProgress", false)]
    [InlineData("Failed", "Initiated", false)]
    public void CanTransitionTo_ShouldReturnExpected(string from, string to, bool expected)
    {
        // Arrange
        var fromStatus = AssessmentStatus.CreateFromValidated(from);
        var toStatus = AssessmentStatus.CreateFromValidated(to);

        // Act
        var actual = fromStatus.CanTransitionTo(toStatus);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Initiated")]
    [InlineData("InProgress")]
    [InlineData("Passed")]
    [InlineData("Failed")]
    [InlineData("RequiresRemediation")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = AssessmentStatus.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = AssessmentStatus.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => AssessmentStatus.CreateFromValidated("Invalid"));
    }
}
