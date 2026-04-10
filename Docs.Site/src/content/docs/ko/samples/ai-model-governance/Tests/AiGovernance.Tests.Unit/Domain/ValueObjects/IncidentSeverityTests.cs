using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class IncidentSeverityTests
{
    [Theory]
    [InlineData("Critical")]
    [InlineData("High")]
    [InlineData("Medium")]
    [InlineData("Low")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = IncidentSeverity.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = IncidentSeverity.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Critical", true)]
    [InlineData("High", true)]
    [InlineData("Medium", false)]
    [InlineData("Low", false)]
    public void RequiresQuarantine_ShouldReturnExpected(string value, bool expected)
    {
        // Arrange
        var sut = IncidentSeverity.CreateFromValidated(value);

        // Act
        var actual = sut.RequiresQuarantine;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => IncidentSeverity.CreateFromValidated("Invalid"));
    }
}
