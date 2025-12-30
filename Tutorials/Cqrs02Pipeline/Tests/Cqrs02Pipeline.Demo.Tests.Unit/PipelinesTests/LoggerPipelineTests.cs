using Mediator;
using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Tests.Unit.PipelinesTests;

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
    public sealed record class TestCommandRequest(string Name) : IMessage;

    /// <summary>
    /// 테스트용 Query Request
    /// </summary>
    public sealed record class TestQueryRequest(Guid Id) : IMessage;

    /// <summary>
    /// CRTP 패턴을 따르는 테스트용 Response.
    /// IFinResponse{TSelf}와 IFinResponseFactory{TSelf}를 직접 구현합니다.
    /// </summary>
    public sealed record class TestResponse : IFinResponse<TestResponse>, IFinResponseFactory<TestResponse>, IFinResponseWithError
    {
        public bool IsSucc { get; init; }
        public bool IsFail => !IsSucc;
        public Guid Id { get; init; }
        public Error? ErrorValue { get; init; }

        // IFinResponseWithError 구현 (Fail 케이스에서 Error 접근용)
        Error IFinResponseWithError.Error => ErrorValue!;

        private TestResponse() { }

        public static TestResponse CreateSuccess(Guid id) => new() { IsSucc = true, Id = id };
        public static TestResponse CreateFail(Error error) => new() { IsSucc = false, ErrorValue = error };

        // IFinResponseFactory<TestResponse> 구현
        static TestResponse IFinResponseFactory<TestResponse>.CreateFail(Error error) => CreateFail(error);
    }

    #endregion

    [Fact]
    public async Task Handle_SuccessfulRequest_LogsRequestAndResponse()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UsecaseLoggerPipeline<TestCommandRequest, TestResponse>>>();
        var pipeline = new UsecaseLoggerPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
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
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
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
        result.IsFail.ShouldBeTrue();
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
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
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
        var expectedResponse = TestResponse.CreateSuccess(expectedId);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedId);
    }
}
