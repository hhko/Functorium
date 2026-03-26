using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class IncidentStatusTests
{
    [Theory]
    [InlineData("Reported", "Investigating", true)]
    [InlineData("Reported", "Escalated", true)]
    [InlineData("Investigating", "Resolved", true)]
    [InlineData("Investigating", "Escalated", true)]
    [InlineData("Reported", "Resolved", false)]
    [InlineData("Resolved", "Investigating", false)]
    [InlineData("Escalated", "Reported", false)]
    [InlineData("Resolved", "Escalated", false)]
    public void CanTransitionTo_ShouldReturnExpected(string from, string to, bool expected)
    {
        // Arrange
        var fromStatus = IncidentStatus.CreateFromValidated(from);
        var toStatus = IncidentStatus.CreateFromValidated(to);

        // Act
        var actual = fromStatus.CanTransitionTo(toStatus);

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Reported")]
    [InlineData("Investigating")]
    [InlineData("Resolved")]
    [InlineData("Escalated")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = IncidentStatus.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = IncidentStatus.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => IncidentStatus.CreateFromValidated("Invalid"));
    }
}
