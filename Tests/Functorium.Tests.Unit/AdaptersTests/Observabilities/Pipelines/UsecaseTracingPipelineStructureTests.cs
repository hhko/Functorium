using System.Diagnostics;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Usecases;

using LanguageExt.Common;

using Mediator;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseTracingPipeline의 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 Tracing 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// Activity 태그 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+
/// | Tag Key                  | Success           | Failure           |
/// +--------------------------+-------------------+-------------------+
/// | request.layer            | "application"     | "application"     |
/// | request.category         | "usecase"         | "usecase"         |
/// | request.category_type     | "command"/"query" | "command"/"query" |
/// | request.handler          | handler name      | handler name      |
/// | request.handler_method   | "Handle"          | "Handle"          |
/// | response.elapsed         | elapsed seconds   | elapsed seconds   |
/// | response.status          | "success"         | "failure"         |
/// | error.type               | (none)            | "expected"/       |
/// |                          |                   | "exceptional"/    |
/// |                          |                   | "aggregate"       |
/// | error.code               | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+
/// | Total Tags               | 7                 | 9                 |
/// +--------------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class UsecaseTracingPipelineStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private Activity? _capturedActivity;

    public UsecaseTracingPipelineStructureTests()
    {
        _activitySource = new ActivitySource("Test.TracingPipelineTagStructure");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _capturedActivity = activity
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
    }

    #region Span 이름 검증

    /// <summary>
    /// Command 요청 시 Span 이름이 올바른 패턴을 따라야 합니다.
    /// 패턴: "{layer} {category}.{cqrs} {handler}.{method}"
    /// </summary>
    [Fact]
    public async Task Handle_Command_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.DisplayName.ShouldStartWith("application usecase.command");
        _capturedActivity.DisplayName.ShouldContain(".Handle");
    }

    /// <summary>
    /// Query 요청 시 Span 이름이 올바른 패턴을 따라야 합니다.
    /// 패턴: "{layer} {category}.{cqrs} {handler}.{method}"
    /// </summary>
    [Fact]
    public async Task Handle_Query_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestQueryRequest, TestResponse>(_activitySource);
        var request = new TestQueryRequest();

        // Act
        await sut.Handle(request, NextSuccessQuery, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.DisplayName.ShouldStartWith("application usecase.query");
        _capturedActivity.DisplayName.ShouldContain(".Handle");
    }

    #endregion

    #region Response 태그 검증 - Success

    /// <summary>
    /// 성공 시 7개 태그를 가져야 합니다.
    /// (request 5개 + response.elapsed 1개 + response.status 1개)
    /// </summary>
    [Fact]
    public async Task Handle_Success_ShouldHaveSevenTags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(7);
    }

    /// <summary>
    /// 성공 시 ActivityStatus가 Ok여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Success_ShouldSetActivityStatusOk()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    #endregion

    #region Response 태그 검증 - Failure

    /// <summary>
    /// 실패 시 9개 태그를 가져야 합니다.
    /// (request 5개 + response.elapsed 1개 + response.status 1개 + error.type 1개 + error.code 1개)
    /// </summary>
    [Fact]
    public async Task Handle_Failure_ShouldHaveNineTags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExpectedError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(9);
    }

    /// <summary>
    /// 실패 시 ActivityStatus가 Error여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Failure_ShouldSetActivityStatusError()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExpectedError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Command Success 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Command_SuccessTags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Query Success 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Query_SuccessTags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestQueryRequest, TestResponse>(_activitySource);
        var request = new TestQueryRequest();

        // Act
        await sut.Handle(request, NextSuccessQuery, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExpectedError, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExceptionalError, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Aggregate Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_AggregateError_Tags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithAggregateError, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Generic Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_GenericError_Tags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithGenericError, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 캡처된 Activity에서 태그를 추출하고 정렬된 Dictionary로 반환합니다.
    /// </summary>
    private Dictionary<string, string?> ExtractActivityTags()
    {
        _capturedActivity.ShouldNotBeNull();
        return _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());
    }

    private static ValueTask<TestResponse> NextSuccess(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestResponse.CreateSuccess());
    }

    private static ValueTask<TestResponse> NextSuccessQuery(
        TestQueryRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestResponse.CreateSuccess());
    }

    private static ValueTask<TestResponse> NextFailWithExpectedError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        var error = new ErrorCodeExpected("Test.ExpectedError", "currentValue", "Expected error occurred");
        return ValueTask.FromResult(TestResponse.CreateFail(error));
    }

    private static ValueTask<TestResponse> NextFailWithExceptionalError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        var exception = new InvalidOperationException("Exceptional error occurred");
        var error = new ErrorCodeExceptional("Test.ExceptionalError", exception);
        return ValueTask.FromResult(TestResponse.CreateFail(error));
    }

    private static ValueTask<TestResponse> NextFailWithAggregateError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Error[]
        {
            new ErrorCodeExpected("Test.Error1", "value1", "First error"),
            new ErrorCodeExpected("Test.Error2", "value2", "Second error")
        };
        var error = Error.Many(errors);
        return ValueTask.FromResult(TestResponse.CreateFail(error));
    }

    private static ValueTask<TestResponse> NextFailWithGenericError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        var error = new ErrorCodeExpected<int>("Test.GenericError", 42, "Generic error occurred");
        return ValueTask.FromResult(TestResponse.CreateFail(error));
    }

    #endregion

    #region Test Types

    [LogEnricherIgnore]
    private sealed record TestCommandRequest : ICommandRequest<TestSuccessData>;

    [LogEnricherIgnore]
    private sealed record TestQueryRequest : IQueryRequest<TestSuccessData>;

    private sealed record TestSuccessData(Guid Id, string Name);

    private sealed record TestResponse : IFinResponse, IFinResponseFactory<TestResponse>, IFinResponseWithError
    {
        private readonly bool _isSucc;
        private readonly Error? _error;

        private TestResponse(bool isSucc, Error? error = null)
        {
            _isSucc = isSucc;
            _error = error;
        }

        public bool IsSucc => _isSucc;
        public bool IsFail => !_isSucc;
        public Error Error => _error ?? Error.New("Unknown error");

        public static TestResponse CreateSuccess() => new(true);
        public static TestResponse CreateFail(Error error) => new(false, error);
    }

    #endregion
}
