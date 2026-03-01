using Functorium.Applications.Queries;

namespace Functorium.Tests.Unit.ApplicationsTests.Queries;

public class PagedResultTests
{
    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    public void TotalPages_ReturnsCorrectValue_WhenTotalCountAndPageSizeProvided(
        int totalCount, int pageSize, int expectedTotalPages)
    {
        // Act
        var actual = new PagedResult<string>([], totalCount, 1, pageSize);

        // Assert
        actual.TotalPages.ShouldBe(expectedTotalPages);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void HasPreviousPage_ReturnsCorrectValue_WhenPageProvided(int page, bool expected)
    {
        // Act
        var actual = new PagedResult<string>([], 100, page, 10);

        // Assert
        actual.HasPreviousPage.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 10, 100, true)]
    [InlineData(10, 10, 100, false)]
    [InlineData(9, 10, 100, true)]
    [InlineData(1, 10, 5, false)]
    public void HasNextPage_ReturnsCorrectValue_WhenPageAndTotalProvided(
        int page, int pageSize, int totalCount, bool expected)
    {
        // Act
        var actual = new PagedResult<string>([], totalCount, page, pageSize);

        // Assert
        actual.HasNextPage.ShouldBe(expected);
    }
}
