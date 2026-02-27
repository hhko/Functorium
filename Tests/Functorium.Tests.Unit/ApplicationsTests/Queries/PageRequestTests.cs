using Functorium.Applications.Queries;

namespace Functorium.Tests.Unit.ApplicationsTests.Queries;

public class PageRequestTests
{
    [Fact]
    public void Ctor_UsesDefaults_WhenNoParametersProvided()
    {
        // Act
        var actual = new PageRequest();

        // Assert
        actual.Page.ShouldBe(1);
        actual.PageSize.ShouldBe(PageRequest.DefaultPageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Ctor_ClampsPageToOne_WhenPageIsLessThanOne(int page)
    {
        // Act
        var actual = new PageRequest(page: page);

        // Assert
        actual.Page.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Ctor_UsesDefaultPageSize_WhenPageSizeIsLessThanOne(int pageSize)
    {
        // Act
        var actual = new PageRequest(pageSize: pageSize);

        // Assert
        actual.PageSize.ShouldBe(PageRequest.DefaultPageSize);
    }

    [Fact]
    public void Ctor_ClampsPageSizeToMax_WhenPageSizeExceedsMax()
    {
        // Act
        var actual = new PageRequest(pageSize: 20_000);

        // Assert
        actual.PageSize.ShouldBe(PageRequest.MaxPageSize);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 20, 40)]
    [InlineData(5, 15, 60)]
    public void Skip_ReturnsCorrectOffset_WhenPageAndSizeProvided(int page, int pageSize, int expectedSkip)
    {
        // Act
        var actual = new PageRequest(page, pageSize);

        // Assert
        actual.Skip.ShouldBe(expectedSkip);
    }
}
