using System.Diagnostics;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Cqrs;

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
/// ┌──────────────────────────┬─────────────────────────┬─────────────────────────┐
/// │ Tag Key                  │ Success                 │ Failure                 │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ request.layer            │ "application"           │ "application"           │
/// │ request.category         │ "usecase"               │ "usecase"               │
/// │ request.handler.cqrs     │ "command"/"query"       │ "command"/"query"       │
/// │ request.handler          │ handler name            │ handler name            │
/// │ request.handler.method   │ "Handle"                │ "Handle"                │
/// │ response.elapsed         │ elapsed seconds         │ elapsed seconds         │
/// │ response.status          │ "success"               │ "failure"               │
/// │ error.type               │ (none)                  │ "expected"/"exceptional"│
/// │ error.code               │ (none)                  │ error code              │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ Total Tags               │ 7                       │ 9                       │
/// └──────────────────────────┴─────────────────────────┴─────────────────────────┘
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class UsecaseTracingPipelineTagStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private Activity? _capturedActivity;

    public UsecaseTracingPipelineTagStructureTests()
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

    #region Request 태그 검증

    /// <summary>
    /// Activity는 기본 Request 태그 5개를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_RequestTags_ShouldContainBaseTags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.RequestLayer);
        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.RequestCategory);
        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.RequestHandlerCqrs);
        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.RequestHandler);
        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.RequestHandlerMethod);
    }

    /// <summary>
    /// Request 태그는 올바른 값을 가져야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_RequestTags_ShouldHaveCorrectValues()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.CustomAttributes.RequestLayer].ShouldBe(ObservabilityNaming.Layers.Application);
        tags[ObservabilityNaming.CustomAttributes.RequestCategory].ShouldBe(ObservabilityNaming.Categories.Usecase);
        tags[ObservabilityNaming.CustomAttributes.RequestHandlerCqrs].ShouldBe(ObservabilityNaming.Cqrs.Command);
        tags[ObservabilityNaming.CustomAttributes.RequestHandlerMethod].ShouldBe(ObservabilityNaming.Methods.Handle);
    }

    #endregion

    #region Response 태그 검증 - Success

    /// <summary>
    /// 성공 시 response.status 태그가 "success"여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Success_ShouldSetSuccessStatusTag()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.CustomAttributes.ResponseStatus].ShouldBe(ObservabilityNaming.Status.Success);
    }

    /// <summary>
    /// 성공 시 response.elapsed 태그가 설정되어야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Success_ShouldSetElapsedTag()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextSuccess, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.ResponseElapsed);
        var elapsed = (double)tags[ObservabilityNaming.CustomAttributes.ResponseElapsed]!;
        elapsed.ShouldBeGreaterThanOrEqualTo(0);
    }

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
    /// 실패 시 response.status 태그가 "failure"여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Failure_ShouldSetFailureStatusTag()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExpectedError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.CustomAttributes.ResponseStatus].ShouldBe(ObservabilityNaming.Status.Failure);
    }

    /// <summary>
    /// 실패 시 error.type과 error.code 태그가 설정되어야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Failure_ShouldSetErrorTags()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExpectedError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags.ShouldContainKey(ObservabilityNaming.OTelAttributes.ErrorType);
        tags.ShouldContainKey(ObservabilityNaming.CustomAttributes.ErrorCode);
    }

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

    #region Error 타입별 검증

    /// <summary>
    /// Expected 에러 시 error.type이 "expected"여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ExpectedError_ShouldSetExpectedErrorType()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExpectedError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.OTelAttributes.ErrorType].ShouldBe(ObservabilityNaming.ErrorTypes.Expected);
    }

    /// <summary>
    /// Exceptional 에러 시 error.type이 "exceptional"이어야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ExceptionalError_ShouldSetExceptionalErrorType()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExceptionalError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.OTelAttributes.ErrorType].ShouldBe(ObservabilityNaming.ErrorTypes.Exceptional);
    }

    /// <summary>
    /// Aggregate 에러 시 error.type이 "aggregate"여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_AggregateError_ShouldSetAggregateErrorType()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithAggregateError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.OTelAttributes.ErrorType].ShouldBe(ObservabilityNaming.ErrorTypes.Aggregate);
    }

    /// <summary>
    /// Generic ErrorCodeExpected&lt;T&gt; 타입도 올바른 error.code를 가져야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_GenericErrorCodeExpected_ShouldHaveCorrectErrorCode()
    {
        // Arrange
        var sut = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithGenericError, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects.ToDictionary(t => t.Key, t => t.Value);

        tags[ObservabilityNaming.OTelAttributes.ErrorType].ShouldBe(ObservabilityNaming.ErrorTypes.Expected);
        tags[ObservabilityNaming.CustomAttributes.ErrorCode].ShouldBe("Test.GenericError");
    }

    #endregion

    #region Helper Methods

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

    private sealed record TestCommandRequest : ICommandRequest<TestSuccessData>;

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
