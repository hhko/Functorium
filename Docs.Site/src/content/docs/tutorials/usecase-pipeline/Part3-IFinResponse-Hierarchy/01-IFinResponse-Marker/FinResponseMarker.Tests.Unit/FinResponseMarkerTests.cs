using LanguageExt.Common;
using Shouldly;

namespace FinResponseMarker.Tests.Unit;

public class FinResponseMarkerTests
{
    [Fact]
    public void IsSucc_ReturnsTrue_WhenCreatedWithSucc()
    {
        // Arrange
        var sut = SimpleResponse<string>.Succ("Hello");

        // Act & Assert
        sut.IsSucc.ShouldBeTrue();
        sut.IsFail.ShouldBeFalse();
    }

    [Fact]
    public void IsFail_ReturnsTrue_WhenCreatedWithFail()
    {
        // Arrange
        var sut = SimpleResponse<string>.Fail(Error.New("error"));

        // Act & Assert
        sut.IsFail.ShouldBeTrue();
        sut.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public void LogResponse_ReturnsSuccess_WhenResponseIsSucc()
    {
        // Arrange
        var response = SimpleResponse<string>.Succ("Hello");

        // Act
        var actual = PipelineExample.LogResponse(response);

        // Assert
        actual.ShouldBe("Success");
    }

    [Fact]
    public void LogResponse_ReturnsFail_WhenResponseIsFail()
    {
        // Arrange
        var response = SimpleResponse<string>.Fail(Error.New("error"));

        // Act
        var actual = PipelineExample.LogResponse(response);

        // Assert
        actual.ShouldBe("Fail");
    }
}
