using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class IncidentDescriptionTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenValueIsValid()
    {
        // Act
        var actual = IncidentDescription.Create("Model returned biased results");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = IncidentDescription.Create("  Model returned biased results  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("Model returned biased results");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = IncidentDescription.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', IncidentDescription.MaxLength + 1);

        // Act
        var actual = IncidentDescription.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
