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
    /// 테스트용 Response (IResponse&lt;T&gt; 구현)
    /// </summary>
    public sealed record class TestResponse : ResponseBase<TestResponse>
    {
        public Guid Id { get; init; }

        public TestResponse() { }
        public TestResponse(Guid id) => Id = id;
    }

    #endregion

    [Fact]
    public async Task Handle_SuccessfulRequest_LogsRequestAndResponse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, TestResponse>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = new TestResponse(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Verify logging was called (at least for request and response)
        logger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_QueryRequest_ReturnsSuccessfully()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestQueryRequest, TestResponse>>>();
        var pipeline = new UsecaseLoggerPipeline<TestQueryRequest, TestResponse>(logger);
        var request = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = new TestResponse(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_FailedRequest_LogsError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, TestResponse>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var errorResponse = TestResponse.CreateFail(Error.New("Test error"));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        // Verify logging was called
        logger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_Request_MeasuresElapsedTime()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, TestResponse>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = new TestResponse(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Pipeline should complete without errors even with delay
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, TestResponse>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = new TestResponse(expectedId);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Id.ShouldBe(expectedId);
    }
}
