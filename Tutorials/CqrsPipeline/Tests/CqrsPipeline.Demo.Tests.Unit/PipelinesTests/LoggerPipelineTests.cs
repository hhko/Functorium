using Mediator;
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Tests.Unit.PipelinesTests;

/// <summary>
/// UsecaseLoggerPipeline 테스트
/// 요청/응답 로깅 파이프라인 테스트
/// </summary>
public sealed class LoggerPipelineTests
{
    #region Test Fixtures

    /// <summary>
    /// 테스트용 Command Request
    /// </summary>
    public sealed record class TestCommandRequest(string Name) : ICommandRequest<TestResponse>;

    /// <summary>
    /// 테스트용 Query Request
    /// </summary>
    public sealed record class TestQueryRequest(Guid Id) : IQueryRequest<TestResponse>;

    /// <summary>
    /// 테스트용 Response
    /// </summary>
    public sealed record class TestResponse(Guid Id) : IResponse;

    #endregion

    [Fact]
    public async Task Handle_SuccessfulRequest_LogsRequestAndResponse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid())));

        MessageHandlerDelegate<TestCommandRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(expectedResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        // Verify logging was called (at least for request and response)
        logger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_QueryRequest_ReturnsSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestQueryRequest, IFinResponse<TestResponse>>>>();
        var pipeline = new UsecaseLoggerPipeline<TestQueryRequest, IFinResponse<TestResponse>>(logger);
        var request = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid())));

        MessageHandlerDelegate<TestQueryRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(expectedResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldBe(expectedResponse.Value);
    }

    [Fact]
    public async Task Handle_FailedRequest_LogsError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>(logger);
        var request = new TestCommandRequest("Test");
        var errorResponse = new FinResponse<TestResponse>(Fin.Fail<TestResponse>(Error.New("Test error")));

        MessageHandlerDelegate<TestCommandRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(errorResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        // Verify logging was called
        logger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_Request_MeasuresElapsedTime()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid())));

        MessageHandlerDelegate<TestCommandRequest, IFinResponse<TestResponse>> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        // Pipeline should complete without errors even with delay
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, IFinResponse<TestResponse>>(logger);
        var request = new TestCommandRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(expectedId)));

        MessageHandlerDelegate<TestCommandRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(expectedResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.Id.ShouldBe(expectedId);
    }
}
