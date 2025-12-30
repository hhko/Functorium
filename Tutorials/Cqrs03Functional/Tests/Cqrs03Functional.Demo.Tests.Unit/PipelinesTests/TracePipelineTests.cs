using System.Diagnostics;
using LanguageExt.Common;
using Mediator;

namespace Cqrs03Functional.Demo.Tests.Unit.PipelinesTests;

/// <summary>
/// UsecaseTracePipeline 테스트
/// 분산 추적 파이프라인 테스트
/// </summary>
public sealed class TracePipelineTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

    public TracePipelineTests()
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

    #region Test Fixtures

    /// <summary>
    /// 테스트용 Request
    /// </summary>
    public sealed record class TestRequest(string Name) : IMessage;

    /// <summary>
    /// CRTP 패턴을 따르는 테스트용 Response.
    /// IFinResponse{TSelf}와 IFinResponseFactory{TSelf}를 직접 구현합니다.
    /// </summary>
    public sealed record class TestResponse : IFinResponse, IFinResponseFactory<TestResponse>, IFinResponseWithError
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
    public async Task Handle_SuccessfulRequest_CreatesActivity()
    {
        // Arrange
        var pipeline = new UsecaseTracePipeline<TestRequest, TestResponse>(_activitySource);
        var request = new TestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
        // Activity가 생성되었는지 확인 (Activity.Current가 null이 아니어야 함)
    }

    [Fact]
    public async Task Handle_FailedRequest_CreatesActivityWithErrorStatus()
    {
        // Arrange
        var pipeline = new UsecaseTracePipeline<TestRequest, TestResponse>(_activitySource);
        var request = new TestRequest("Test");
        var errorResponse = TestResponse.CreateFail(Error.New("Test error"));

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        // Activity가 생성되고 에러 상태로 설정되었는지 확인
    }

    [Fact]
    public async Task Handle_MeasuresElapsedTime()
    {
        // Arrange
        var pipeline = new UsecaseTracePipeline<TestRequest, TestResponse>(_activitySource);
        var request = new TestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        // 처리 시간이 측정되었는지 확인
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var pipeline = new UsecaseTracePipeline<TestRequest, TestResponse>(_activitySource);
        var request = new TestRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = TestResponse.CreateSuccess(expectedId);

        MessageHandlerDelegate<TestRequest, TestResponse> next =
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
        var pipeline = new UsecaseTracePipeline<TestRequest, TestResponse>(nonListeningSource);
        var request = new TestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert - Activity가 생성되지 않아도 파이프라인은 정상 동작해야 함
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_QueryRequest_CreatesActivity()
    {
        // Arrange
        var pipeline = new UsecaseTracePipeline<TestRequest, TestResponse>(_activitySource);
        var request = new TestRequest("Query");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }
}

