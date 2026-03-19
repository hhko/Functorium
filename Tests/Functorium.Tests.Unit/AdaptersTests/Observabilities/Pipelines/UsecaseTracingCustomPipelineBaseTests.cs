using System.Diagnostics;

using Functorium.Adapters.Observabilities.Naming;
using Functorium.Adapters.Observabilities.Pipelines;

using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseTracingCustomPipelineBase 테스트
/// 커스텀 Tracing 베이스 클래스의 Activity 생성 및 태그 설정 검증
/// </summary>
public sealed class UsecaseTracingCustomPipelineBaseTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;

    public UsecaseTracingCustomPipelineBaseTests()
    {
        _activitySource = new ActivitySource("Test.TracingCustomBase");
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
    public void StartCustomActivity_CreatesActivityWithCorrectName()
    {
        // Arrange
        var sut = new TestTracingCustomPipeline<TestCommandRequest>(_activitySource);

        // Act
        using Activity? actual = sut.InvokeStartCustomActivity("DoSomething");

        // Assert
        actual.ShouldNotBeNull();
        actual.DisplayName.ShouldContain("application usecase.command");
        actual.DisplayName.ShouldContain("TestFixtures.DoSomething");
    }

    [Fact]
    public void GetActivityName_ReturnsCorrectFormat()
    {
        // Arrange
        var sut = new TestTracingCustomPipeline<TestCommandRequest>(_activitySource);

        // Act
        string actual = sut.InvokeGetActivityName("CustomOp");

        // Assert
        actual.ShouldContain("application usecase.command");
        actual.ShouldContain("TestFixtures.CustomOp");
    }

    [Fact]
    public void SetStandardRequestTags_SetsAllFiveTags()
    {
        // Arrange
        var sut = new TestTracingCustomPipeline<TestCommandRequest>(_activitySource);
        using Activity? activity = sut.InvokeStartCustomActivity("TagTest");
        activity.ShouldNotBeNull();

        // Act
        TestTracingCustomPipeline<TestCommandRequest>.InvokeSetStandardRequestTags(activity, "Handle");

        // Assert
        activity.GetTagItem(ObservabilityNaming.CustomAttributes.RequestLayer).ShouldBe(ObservabilityNaming.Layers.Application);
        activity.GetTagItem(ObservabilityNaming.CustomAttributes.RequestCategory).ShouldBe(ObservabilityNaming.Categories.Usecase);
        activity.GetTagItem(ObservabilityNaming.CustomAttributes.RequestCategoryType).ShouldBe(ObservabilityNaming.CategoryTypes.Command);
        activity.GetTagItem(ObservabilityNaming.CustomAttributes.RequestHandler).ShouldNotBeNull();
        activity.GetTagItem(ObservabilityNaming.CustomAttributes.RequestHandlerMethod).ShouldBe("Handle");
    }

    [Fact]
    public void StartCustomActivity_NoListener_ReturnsNull()
    {
        // Arrange — 클래스 리스너를 Dispose하여 리스닝 중지
        _activityListener.Dispose();

        using var nonListeningSource = new ActivitySource("NonListening.TracingCustomBase");
        var sut = new TestTracingCustomPipeline<TestCommandRequest>(nonListeningSource);

        // Act
        Activity? actual = sut.InvokeStartCustomActivity("NoOp");

        // Assert
        actual.ShouldBeNull();
    }

    [Fact]
    public void StartCustomActivity_QueryRequest_IncludesQueryCategoryType()
    {
        // Arrange
        var sut = new TestTracingCustomPipeline<TestQueryRequest>(_activitySource);

        // Act
        using Activity? actual = sut.InvokeStartCustomActivity("QueryOp");

        // Assert
        actual.ShouldNotBeNull();
        actual.DisplayName.ShouldContain("usecase.query");
    }

    /// <summary>
    /// protected 멤버 접근을 위한 concrete 테스트 구현체
    /// </summary>
    private sealed class TestTracingCustomPipeline<TRequest> : UsecaseTracingCustomPipelineBase<TRequest>
    {
        public TestTracingCustomPipeline(ActivitySource activitySource) : base(activitySource)
        {
        }

        public Activity? InvokeStartCustomActivity(string operationName)
            => StartCustomActivity(operationName);

        public string InvokeGetActivityName(string operationName)
            => GetActivityName(operationName);

        public static void InvokeSetStandardRequestTags(Activity activity, string method)
            => SetStandardRequestTags(activity, method);
    }
}
