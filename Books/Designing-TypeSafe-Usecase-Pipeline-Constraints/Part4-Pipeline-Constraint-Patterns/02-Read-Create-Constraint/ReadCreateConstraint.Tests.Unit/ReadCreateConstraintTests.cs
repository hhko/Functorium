using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace ReadCreateConstraint.Tests.Unit;

public class ReadCreateConstraintTests
{
    [Fact]
    public void LogAndReturn_LogsSuccess_WhenResponseIsSucc()
    {
        // Arrange
        var sut = new SimpleLoggingPipeline<FinResponse<string>>();
        var response = FinResponse.Succ("OK");

        // Act
        sut.LogAndReturn(response);

        // Assert
        sut.Logs.ShouldContain("Success");
    }

    [Fact]
    public void LogAndReturn_LogsFail_WhenResponseIsFail()
    {
        // Arrange
        var sut = new SimpleLoggingPipeline<FinResponse<string>>();
        var response = FinResponse.Fail<string>(Error.New("bad request"));

        // Act
        sut.LogAndReturn(response);

        // Assert
        sut.Logs[0].ShouldStartWith("Fail:");
    }

    [Fact]
    public void TraceAndReturn_SetsOkStatus_WhenResponseIsSucc()
    {
        // Arrange
        var sut = new SimpleTracingPipeline<FinResponse<string>>();
        var response = FinResponse.Succ("OK");

        // Act
        sut.TraceAndReturn(response);

        // Assert
        sut.Tags.ShouldContain("status:ok");
    }

    [Fact]
    public void TraceAndReturn_SetsErrorStatus_WhenResponseIsFail()
    {
        // Arrange
        var sut = new SimpleTracingPipeline<FinResponse<string>>();
        var response = FinResponse.Fail<string>(Error.New("error"));

        // Act
        sut.TraceAndReturn(response);

        // Assert
        sut.Tags.ShouldContain("status:error");
        sut.Tags.Any(t => t.StartsWith("error.message:")).ShouldBeTrue();
    }
}
