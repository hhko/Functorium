using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class ResolutionNoteTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenValueIsValid()
    {
        // Act
        var actual = ResolutionNote.Create("Applied bias correction patch");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ResolutionNote.Create("  Applied patch  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("Applied patch");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ResolutionNote.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ResolutionNote.MaxLength + 1);

        // Act
        var actual = ResolutionNote.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
