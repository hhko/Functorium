using LanguageExt.Common;

namespace FinResponseWrapperLimitation.Tests.Unit;

public class FinResponseWrapperTests
{
    [Fact]
    public void IsSucc_ReturnsTrue_WhenCreatedWithSuccess()
    {
        // Arrange
        var response = ResponseWrapper<TestResponse>.Success(new TestResponse("OK"));

        // Act & Assert
        response.IsSucc.ShouldBeTrue();
        response.IsFail.ShouldBeFalse();
    }

    [Fact]
    public void IsFail_ReturnsTrue_WhenCreatedWithFail()
    {
        // Arrange
        var response = ResponseWrapper<TestResponse>.Fail(Error.New("error"));

        // Act & Assert
        response.IsFail.ShouldBeTrue();
        response.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public void ProcessResponse_ReturnsSuccess_WhenWrapperIsSucc()
    {
        // Arrange
        var response = ResponseWrapper<TestResponse>.Success(new TestResponse("OK"));

        // Act
        var actual = WrapperPipelineExample.ProcessResponse(response);

        // Assert
        actual.ShouldBe("Success");
    }

    [Fact]
    public void ProcessResponse_ReturnsFail_WhenWrapperIsFail()
    {
        // Arrange
        var response = ResponseWrapper<TestResponse>.Fail(Error.New("bad request"));

        // Act
        var actual = WrapperPipelineExample.ProcessResponse(response);

        // Assert
        actual.ShouldStartWith("Fail:");
    }

    [Fact]
    public void ProcessResponse_ReturnsUnknown_WhenNotAWrapper()
    {
        // Arrange
        var response = "plain string";

        // Act
        var actual = WrapperPipelineExample.ProcessResponse(response);

        // Assert
        actual.ShouldBe("Unknown");
    }
}

file record TestResponse(string Message) : IResponse;
