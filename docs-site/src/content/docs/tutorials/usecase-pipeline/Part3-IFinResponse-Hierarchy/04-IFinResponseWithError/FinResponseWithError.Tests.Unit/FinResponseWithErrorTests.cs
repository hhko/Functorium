using LanguageExt.Common;
using Shouldly;

namespace FinResponseWithError.Tests.Unit;

public class FinResponseWithErrorTests
{
    [Fact]
    public void LogResponse_ReturnsSuccess_WhenSucc()
    {
        // Arrange
        var response = ErrorAccessResponse<string>.CreateSucc("Hello");

        // Act
        var actual = LoggingPipelineExample.LogResponse(response);

        // Assert
        actual.ShouldBe("Success");
    }

    [Fact]
    public void LogResponse_ReturnsFail_WhenFail()
    {
        // Arrange
        var response = ErrorAccessResponse<string>.CreateFail(Error.New("bad request"));

        // Act
        var actual = LoggingPipelineExample.LogResponse(response);

        // Assert
        actual.ShouldStartWith("Fail:");
        actual.ShouldContain("bad request");
    }

    [Fact]
    public void SuccResponse_DoesNotImplementIFinResponseWithError()
    {
        // Arrange
        var response = ErrorAccessResponse<string>.CreateSucc("Hello");

        // Act & Assert
        (response is IFinResponseWithError).ShouldBeFalse();
    }

    [Fact]
    public void FailResponse_ImplementsIFinResponseWithError()
    {
        // Arrange
        var response = ErrorAccessResponse<string>.CreateFail(Error.New("error"));

        // Act & Assert
        (response is IFinResponseWithError).ShouldBeTrue();
    }
}
