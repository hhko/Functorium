using Functorium.Adapters.Pipelines;
using Mediator;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseExceptionPipeline 테스트
/// 예외를 ExceptionalError로 변환하는 파이프라인 테스트
/// </summary>
public sealed class UsecaseExceptionPipelineTests
{
    [Fact]
    public async Task Handle_NoException_ReturnsResponse()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
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
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");
        var expectedException = new InvalidOperationException("Test exception");

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
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
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");
        var expectedMessage = "Custom exception message for testing";
        var expectedException = new ArgumentException(expectedMessage);

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
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
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
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
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
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

    [Fact]
    public async Task Handle_AggregateException_ReturnsFailure()
    {
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");
        var innerException = new InvalidOperationException("Inner exception");
        var aggregateException = new AggregateException("Aggregate exception", innerException);

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => throw aggregateException;

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.IsExceptional.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_OperationCanceledException_Propagates()
    {
        // 취소 예외는 FinResponse.Fail로 변환되지 않고 상위로 전파되어야 한다.
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, token) =>
            {
                token.ThrowIfCancellationRequested();
                return ValueTask.FromResult(TestResponse.CreateSuccess(Guid.NewGuid()));
            };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await pipeline.Handle(request, next, cts.Token));
    }

    [Fact]
    public async Task Handle_TaskCanceledException_Propagates()
    {
        // TaskCanceledException은 OperationCanceledException의 서브타입이므로
        // 동일 catch 분기로 전파되어야 한다.
        // Arrange
        var pipeline = new UsecaseExceptionPipeline<SimpleTestRequest, TestResponse>();
        var request = new SimpleTestRequest("Test");

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => throw new TaskCanceledException("downstream cancelled");

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(
            async () => await pipeline.Handle(request, next, CancellationToken.None));
    }
}
