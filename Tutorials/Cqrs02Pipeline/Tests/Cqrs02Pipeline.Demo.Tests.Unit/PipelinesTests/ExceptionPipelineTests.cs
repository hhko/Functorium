using Mediator;

namespace Cqrs02Pipeline.Demo.Tests.Unit.PipelinesTests;

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
    public sealed record class TestRequest(string Name) : IMessage;

    /// <summary>
    /// CRTP 패턴을 따르는 테스트용 Response.
    /// IFinResponse{TSelf}와 IFinResponseFactory{TSelf}를 직접 구현합니다.
    /// </summary>
    public sealed record class TestResponse : IFinResponse<TestResponse>, IFinResponseFactory<TestResponse>
    {
        public bool IsSucc { get; init; }
        public bool IsFail => !IsSucc;
        public Guid Id { get; init; }
        public Error? Error { get; init; }

        private TestResponse() { }

        public static TestResponse CreateSuccess(Guid id) => new() { IsSucc = true, Id = id };
        public static TestResponse CreateFail(Error error) => new() { IsSucc = false, Error = error };

        // IFinResponseFactory<TestResponse> 구현
        static TestResponse IFinResponseFactory<TestResponse>.CreateFail(Error error) => CreateFail(error);
    }

    #endregion

    [Fact]
    public async Task Handle_NoException_ReturnsResponse()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, TestResponse>();
        var request = new TestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_Exception_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, TestResponse>();
        var request = new TestRequest("Test");
        var expectedException = new InvalidOperationException("Test exception");

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => throw expectedException;

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Exception_PreservesExceptionMessage()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, TestResponse>();
        var request = new TestRequest("Test");
        var expectedMessage = "Custom exception message for testing";
        var expectedException = new ArgumentException(expectedMessage);

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => throw expectedException;

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.Message.ShouldContain(expectedMessage);
    }

    [Fact]
    public async Task Handle_NullReferenceException_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, TestResponse>();
        var request = new TestRequest("Test");

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => throw new NullReferenceException("Object reference not set");

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_AsyncException_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<TestRequest, TestResponse>();
        var request = new TestRequest("Test");

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(1);
                throw new TimeoutException("Async operation timed out");
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.IsExceptional.ShouldBeTrue();
        result.Error!.Message.ShouldContain("timed out");
    }
}
