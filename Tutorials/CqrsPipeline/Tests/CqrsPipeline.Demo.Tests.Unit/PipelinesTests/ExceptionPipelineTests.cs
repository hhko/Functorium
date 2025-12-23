using Mediator;

namespace CqrsPipeline.Demo.Tests.Unit.PipelinesTests;

/// <summary>
/// UsecaseExceptionPipeline 테스트
/// 예외를 ErrorCodeExceptional로 변환하는 파이프라인 테스트
/// </summary>
public sealed class ExceptionPipelineTests
{
    #region Test Fixtures

    /// <summary>
    /// 테스트용 Request
    /// </summary>
    public sealed record class TestRequest(string Name) : ICommandRequest<TestResponse>;

    /// <summary>
    /// 테스트용 Response
    /// </summary>
    public sealed record class TestResponse(Guid Id) : IResponse;

    #endregion

    [Fact]
    public async Task Handle_NoException_ReturnsResponse()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, IFinResponse<TestResponse>>();
        var request = new TestRequest("Test");
        var expectedResponse = new FinResponse<TestResponse>(Fin.Succ(new TestResponse(Guid.NewGuid())));

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => ValueTask.FromResult<IFinResponse<TestResponse>>(expectedResponse);

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Value.ShouldBe(expectedResponse.Value);
    }

    [Fact]
    public async Task Handle_Exception_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, IFinResponse<TestResponse>>();
        var request = new TestRequest("Test");
        var expectedException = new InvalidOperationException("Test exception");

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => throw expectedException;

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Exception_PreservesExceptionMessage()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, IFinResponse<TestResponse>>();
        var request = new TestRequest("Test");
        var expectedMessage = "Custom exception message for testing";
        var expectedException = new ArgumentException(expectedMessage);

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => throw expectedException;

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.Message.ShouldContain(expectedMessage);
    }

    [Fact]
    public async Task Handle_NullReferenceException_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, IFinResponse<TestResponse>>();
        var request = new TestRequest("Test");

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            (_, _) => throw new NullReferenceException("Object reference not set");

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_AsyncException_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, IFinResponse<TestResponse>>();
        var request = new TestRequest("Test");

        MessageHandlerDelegate<TestRequest, IFinResponse<TestResponse>> next =
            async (_, _) =>
            {
                await Task.Delay(1);
                throw new TimeoutException("Async operation timed out");
            };

        // Act
        IFinResponse<TestResponse> result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeFalse();
        result.Error.IsExceptional.ShouldBeTrue();
        result.Error.Message.ShouldContain("timed out");
    }
}
