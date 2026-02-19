using Ardalis.SmartEnum;
using SortDirection = Functorium.Applications.Queries.SortDirection;

namespace Functorium.Tests.Unit.ApplicationsTests.Queries;

public class SortDirectionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Parse_ReturnsAscending_WhenValueIsNullOrEmpty(string? value)
    {
        // Act
        var actual = SortDirection.Parse(value);

        // Assert
        actual.ShouldBe(SortDirection.Ascending);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("ASC")]
    [InlineData("Asc")]
    public void Parse_ReturnsAscending_WhenValueIsAsc(string value)
    {
        // Act
        var actual = SortDirection.Parse(value);

        // Assert
        actual.ShouldBe(SortDirection.Ascending);
    }

    [Theory]
    [InlineData("desc")]
    [InlineData("DESC")]
    [InlineData("Desc")]
    public void Parse_ReturnsDescending_WhenValueIsDesc(string value)
    {
        // Act
        var actual = SortDirection.Parse(value);

        // Assert
        actual.ShouldBe(SortDirection.Descending);
    }

    [Fact]
    public void Parse_ThrowsSmartEnumNotFoundException_WhenValueIsInvalid()
    {
        // Act & Assert
        Should.Throw<SmartEnumNotFoundException>(() => SortDirection.Parse("invalid"));
    }

    [Fact]
    public void Ascending_HasExpectedValue()
    {
        // Assert
        SortDirection.Ascending.Value.ShouldBe("asc");
        SortDirection.Ascending.Name.ShouldBe("Ascending");
    }

    [Fact]
    public void Descending_HasExpectedValue()
    {
        // Assert
        SortDirection.Descending.Value.ShouldBe("desc");
        SortDirection.Descending.Name.ShouldBe("Descending");
    }
}
