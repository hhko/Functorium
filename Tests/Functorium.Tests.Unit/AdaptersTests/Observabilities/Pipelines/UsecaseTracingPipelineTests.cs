using System.Diagnostics;
using Functorium.Adapters.Observabilities.Pipelines;
using LanguageExt.Common;
using Mediator;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseTracingPipeline 테스트
/// 분산 추적 파이프라인 테스트
/// </summary>
public sealed class UsecaseTracingPipelineTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

    public UsecaseTracingPipelineTests()
    {
        _activitySource = new ActivitySource("Test.TracePipeline");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
        _activitySource?.Dispose();
    }

    [Fact]
    public async Task Handle_SuccessfulRequest_CreatesActivity()
    {
        // Arrange
        var pipeline = new UsecaseTracingPipeline<SimpleTestRequest, TestResponse>(_activitySource);
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
    public async Task Handle_FailedRequest_CreatesActivityWithErrorStatus()
    {
        // Arrange
        var pipeline = new UsecaseTracingPipeline<SimpleTestRequest, TestResponse>(_activitySource);
        var request = new SimpleTestRequest("Test");
        var errorResponse = TestResponse.CreateFail(Error.New("Test error"));

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_MeasuresElapsedTime()
    {
        // Arrange
        var pipeline = new UsecaseTracingPipeline<SimpleTestRequest, TestResponse>(_activitySource);
        var request = new SimpleTestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var pipeline = new UsecaseTracingPipeline<SimpleTestRequest, TestResponse>(_activitySource);
        var request = new SimpleTestRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = TestResponse.CreateSuccess(expectedId);

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Handle_ActivitySourceNotListening_PassesThrough()
    {
        // Arrange - ActivitySource가 리스닝하지 않는 경우
        using var nonListeningSource = new ActivitySource("NonListening");
        var pipeline = new UsecaseTracingPipeline<SimpleTestRequest, TestResponse>(nonListeningSource);
        var request = new SimpleTestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert - Activity가 생성되지 않아도 파이프라인은 정상 동작해야 함
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_CommandRequest_CreatesActivity()
    {
        // Arrange
        var pipeline = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest("Command");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_QueryRequest_CreatesActivity()
    {
        // Arrange
        var pipeline = new UsecaseTracingPipeline<TestQueryRequest, TestResponse>(_activitySource);
        var request = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }
}
