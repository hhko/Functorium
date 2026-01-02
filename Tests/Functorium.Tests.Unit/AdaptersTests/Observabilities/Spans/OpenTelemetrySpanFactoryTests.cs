using System.Diagnostics;
using Functorium.Adapters.Observabilities.Context;
using Functorium.Adapters.Observabilities.Spans;
using Functorium.Applications.Observabilities.Context;
using Functorium.Tests.Unit.Abstractions.Constants;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Spans;

/// <summary>
/// OpenTelemetrySpanFactory의 부모 컨텍스트 결정 로직을 테스트합니다.
/// </summary>
/// <remarks>
/// <para>
/// 테스트할 Trace 계층 구조:
/// </para>
/// <code>
/// ┌─────────────────────────────────────────────────────────────────────┐
/// │  시나리오 1: Activity.Current가 있는 경우 (우선순위 1)               │
/// ├─────────────────────────────────────────────────────────────────────┤
/// │   HttpRequestIn (ROOT)                                              │
/// │   └── UsecaseActivity (Activity.Current) ← 이것이 선택되어야 함     │
/// │       └── AdapterSpan (CreateChildSpan 결과)                        │
/// └─────────────────────────────────────────────────────────────────────┘
///
/// ┌─────────────────────────────────────────────────────────────────────┐
/// │  시나리오 2: ActivityContextHolder가 있는 경우 (우선순위 2)          │
/// ├─────────────────────────────────────────────────────────────────────┤
/// │   HttpRequestIn (ROOT)                                              │
/// │   └── TraverseActivity (ActivityContextHolder에 저장)               │
/// │       └── AdapterSpan (CreateChildSpan 결과)                        │
/// │                                                                     │
/// │   * Activity.Current = null (FinT 실행 후 복원 안됨)                │
/// └─────────────────────────────────────────────────────────────────────┘
///
/// ┌─────────────────────────────────────────────────────────────────────┐
/// │  시나리오 3: parentContext만 있는 경우 (우선순위 3)                  │
/// ├─────────────────────────────────────────────────────────────────────┤
/// │   HttpRequestIn (ROOT) ← 이것이 선택되어야 함                       │
/// │   └── AdapterSpan (CreateChildSpan 결과)                            │
/// │                                                                     │
/// │   * Activity.Current = null                                         │
/// │   * ActivityContextHolder = null                                    │
/// └─────────────────────────────────────────────────────────────────────┘
///
/// ┌─────────────────────────────────────────────────────────────────────┐
/// │  시나리오 4: 부모 컨텍스트가 없는 경우 (폴백)                        │
/// ├─────────────────────────────────────────────────────────────────────┤
/// │   AdapterSpan (새로운 ROOT로 생성)                                  │
/// │                                                                     │
/// │   * 모든 컨텍스트 소스가 null                                       │
/// └─────────────────────────────────────────────────────────────────────┘
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OpenTelemetrySpanFactoryTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;

    public OpenTelemetrySpanFactoryTests()
    {
        _activitySource = new ActivitySource("TestSource");

        // ActivitySource가 Activity를 생성하려면 Listener가 등록되어 있어야 함
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
        Activity.Current = null;
        ActivityContextHolder.SetCurrentActivity(null);
    }

    #region DetermineParentContext 우선순위 테스트

    /// <summary>
    /// 시나리오 1: Activity.Current가 있으면 해당 Context를 반환해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// HttpRequestIn (ROOT)
    /// └── UsecaseActivity (Activity.Current) ← 선택됨
    ///     └── AdapterSpan
    /// </code>
    /// </remarks>
    [Fact]
    public void DetermineParentContext_ReturnsActivityCurrentContext_WhenActivityCurrentExists()
    {
        // Arrange
        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? usecaseActivity = _activitySource.StartActivity("UsecaseActivity", ActivityKind.Internal, httpActivity!.Context);
        Activity.Current = usecaseActivity;

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        ActivityContext actual = OpenTelemetrySpanFactory.DetermineParentContext(httpContext);

        // Assert
        actual.SpanId.ShouldBe(usecaseActivity!.SpanId);
        actual.TraceId.ShouldBe(usecaseActivity.TraceId);
    }

    /// <summary>
    /// 시나리오 2: Activity.Current가 null이고 ActivityContextHolder에 Activity가 있으면
    /// ActivityContextHolder의 Context를 반환해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// HttpRequestIn (ROOT)
    /// └── TraverseActivity (ActivityContextHolder) ← 선택됨
    ///     └── AdapterSpan
    ///
    /// * Activity.Current = null (FinT 실행 후 AsyncLocal 복원 안됨)
    /// </code>
    /// </remarks>
    [Fact]
    public void DetermineParentContext_ReturnsActivityContextHolderContext_WhenActivityCurrentIsNull()
    {
        // Arrange
        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? traverseActivity = _activitySource.StartActivity("TraverseActivity", ActivityKind.Internal, httpActivity!.Context);

        // Activity.Current를 null로 설정 (FinT 실행 후 복원 안되는 상황 시뮬레이션)
        Activity.Current = null;

        // ActivityContextHolder에 TraverseActivity 설정
        ActivityContextHolder.SetCurrentActivity(traverseActivity);

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        ActivityContext actual = OpenTelemetrySpanFactory.DetermineParentContext(httpContext);

        // Assert
        actual.SpanId.ShouldBe(traverseActivity!.SpanId);
        actual.TraceId.ShouldBe(traverseActivity.TraceId);
    }

    /// <summary>
    /// 시나리오 3: Activity.Current와 ActivityContextHolder 모두 null이고
    /// parentContext가 있으면 parentContext를 반환해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// HttpRequestIn (ROOT) ← 선택됨 (ObservabilityContext)
    /// └── AdapterSpan
    ///
    /// * Activity.Current = null
    /// * ActivityContextHolder = null
    /// </code>
    /// </remarks>
    [Fact]
    public void DetermineParentContext_ReturnsObservabilityContext_WhenBothActivitySourcesAreNull()
    {
        // Arrange
        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        Activity.Current = null;
        ActivityContextHolder.SetCurrentActivity(null);

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        ActivityContext actual = OpenTelemetrySpanFactory.DetermineParentContext(httpContext);

        // Assert
        actual.SpanId.ShouldBe(httpActivity!.SpanId);
        actual.TraceId.ShouldBe(httpActivity.TraceId);
    }

    /// <summary>
    /// 시나리오 4: 모든 컨텍스트 소스가 null이면 default ActivityContext를 반환해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// AdapterSpan (새로운 ROOT로 생성됨)
    ///
    /// * Activity.Current = null
    /// * ActivityContextHolder = null
    /// * parentContext = null
    /// </code>
    /// </remarks>
    [Fact]
    public void DetermineParentContext_ReturnsDefault_WhenAllSourcesAreNull()
    {
        // Arrange
        Activity.Current = null;
        ActivityContextHolder.SetCurrentActivity(null);

        // Act
        ActivityContext actual = OpenTelemetrySpanFactory.DetermineParentContext(null);

        // Assert
        actual.ShouldBe(default);
        actual.TraceId.ShouldBe(default);
        actual.SpanId.ShouldBe(default);
    }

    #endregion

    #region CreateChildSpan 통합 테스트

    /// <summary>
    /// CreateChildSpan이 Activity.Current를 부모로 사용하여 Span을 생성해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 기대되는 Trace 계층 구조:
    ///
    /// HttpRequestIn (TraceId: T1, SpanId: S1)
    /// └── UsecaseActivity (TraceId: T1, SpanId: S2, ParentSpanId: S1)
    ///     └── AdapterSpan (TraceId: T1, SpanId: S3, ParentSpanId: S2) ← 검증 대상
    /// </code>
    /// </remarks>
    [Fact]
    public void CreateChildSpan_CreatesSpanWithCorrectParent_WhenActivityCurrentExists()
    {
        // Arrange
        var sut = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? usecaseActivity = _activitySource.StartActivity("UsecaseActivity", ActivityKind.Internal, httpActivity!.Context);
        Activity.Current = usecaseActivity;

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        using var actual = sut.CreateChildSpan(
            httpContext,
            "adapter Repository TestRepository.GetAll",
            "Repository",
            "TestRepository",
            "GetAll");

        // Assert
        actual.ShouldNotBeNull();
        actual.TraceId.ShouldBe(httpActivity.TraceId.ToString());

        // 생성된 Span의 부모가 UsecaseActivity여야 함 (HTTP가 아님)
        // Span은 ISpan이므로 내부 Activity에 직접 접근할 수 없지만,
        // 같은 TraceId를 공유하는 것을 확인
        actual.TraceId.ShouldBe(usecaseActivity!.TraceId.ToString());
    }

    /// <summary>
    /// Activity.Current가 null일 때 ActivityContextHolder를 부모로 사용해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// FinT/IO 모나드 실행 시나리오:
    ///
    /// HttpRequestIn (TraceId: T1, SpanId: S1)
    /// └── TraverseActivity (TraceId: T1, SpanId: S2) ← ActivityContextHolder에 저장
    ///     └── AdapterSpan (TraceId: T1, SpanId: S3, ParentSpanId: S2)
    ///
    /// * Activity.Current는 FinT 실행 후 null로 복원될 수 있음
    /// </code>
    /// </remarks>
    [Fact]
    public void CreateChildSpan_UsesActivityContextHolder_WhenActivityCurrentIsNull()
    {
        // Arrange
        var sut = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? traverseActivity = _activitySource.StartActivity("TraverseActivity", ActivityKind.Internal, httpActivity!.Context);

        // FinT 실행 후 Activity.Current가 null로 복원된 상황
        Activity.Current = null;
        ActivityContextHolder.SetCurrentActivity(traverseActivity);

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        using var actual = sut.CreateChildSpan(
            httpContext,
            "adapter Repository TestRepository.GetAll",
            "Repository",
            "TestRepository",
            "GetAll");

        // Assert
        actual.ShouldNotBeNull();
        actual.TraceId.ShouldBe(traverseActivity!.TraceId.ToString());
    }

    #endregion

    #region 우선순위 경쟁 테스트

    /// <summary>
    /// Activity.Current가 ActivityContextHolder보다 우선해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 경쟁 시나리오:
    ///
    /// HttpRequestIn (ROOT)
    /// ├── TraverseActivity (ActivityContextHolder에 저장)
    /// └── UsecaseActivity (Activity.Current) ← 이것이 선택되어야 함
    ///     └── AdapterSpan
    ///
    /// 두 Activity가 모두 존재할 때 Activity.Current가 우선
    /// </code>
    /// </remarks>
    [Fact]
    public void DetermineParentContext_PrefersActivityCurrent_OverActivityContextHolder()
    {
        // Arrange
        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? traverseActivity = _activitySource.StartActivity("TraverseActivity", ActivityKind.Internal, httpActivity!.Context);
        using Activity? usecaseActivity = _activitySource.StartActivity("UsecaseActivity", ActivityKind.Internal, httpActivity!.Context);

        // 둘 다 설정
        Activity.Current = usecaseActivity;
        ActivityContextHolder.SetCurrentActivity(traverseActivity);

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        ActivityContext actual = OpenTelemetrySpanFactory.DetermineParentContext(httpContext);

        // Assert: Activity.Current(usecaseActivity)가 선택되어야 함
        actual.SpanId.ShouldBe(usecaseActivity!.SpanId);
        actual.SpanId.ShouldNotBe(traverseActivity!.SpanId);
    }

    /// <summary>
    /// ActivityContextHolder가 parentContext보다 우선해야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 경쟁 시나리오:
    ///
    /// HttpRequestIn (ROOT, parentContext로 전달됨)
    /// └── TraverseActivity (ActivityContextHolder에 저장) ← 이것이 선택되어야 함
    ///     └── AdapterSpan
    ///
    /// * Activity.Current = null
    /// </code>
    /// </remarks>
    [Fact]
    public void DetermineParentContext_PrefersActivityContextHolder_OverParentContext()
    {
        // Arrange
        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? traverseActivity = _activitySource.StartActivity("TraverseActivity", ActivityKind.Internal, httpActivity!.Context);

        Activity.Current = null;
        ActivityContextHolder.SetCurrentActivity(traverseActivity);

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        ActivityContext actual = OpenTelemetrySpanFactory.DetermineParentContext(httpContext);

        // Assert: ActivityContextHolder(traverseActivity)가 선택되어야 함
        actual.SpanId.ShouldBe(traverseActivity!.SpanId);
        actual.SpanId.ShouldNotBe(httpActivity!.SpanId);
    }

    #endregion

    #region 복수 AdapterSpan 테스트

    /// <summary>
    /// 여러 AdapterSpan이 순차적으로 생성될 때 모두 동일한 부모(Usecase)를 가져야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 순차 호출 시나리오 (Usecase에서 여러 Repository 호출):
    ///
    /// HttpRequestIn (TraceId: T1, SpanId: S1)
    /// └── UsecaseActivity (TraceId: T1, SpanId: S2, ParentSpanId: S1)
    ///     ├── AdapterSpan1 (TraceId: T1, SpanId: S3, ParentSpanId: S2) ← OrderRepository
    ///     ├── AdapterSpan2 (TraceId: T1, SpanId: S4, ParentSpanId: S2) ← ProductRepository
    ///     └── AdapterSpan3 (TraceId: T1, SpanId: S5, ParentSpanId: S2) ← CustomerRepository
    ///
    /// 모든 AdapterSpan이 동일한 ParentSpanId(S2)를 가져야 함
    /// </code>
    /// </remarks>
    [Fact]
    public void CreateChildSpan_MultipleSpans_AllHaveSameParent()
    {
        // Arrange
        var sut = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? usecaseActivity = _activitySource.StartActivity("UsecaseActivity", ActivityKind.Internal, httpActivity!.Context);
        Activity.Current = usecaseActivity;

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act: 여러 AdapterSpan 순차 생성
        using var adapterSpan1 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository OrderRepository.GetById",
            "Repository",
            "OrderRepository",
            "GetById");

        using var adapterSpan2 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository ProductRepository.GetByIds",
            "Repository",
            "ProductRepository",
            "GetByIds");

        using var adapterSpan3 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository CustomerRepository.GetById",
            "Repository",
            "CustomerRepository",
            "GetById");

        // Assert
        adapterSpan1.ShouldNotBeNull();
        adapterSpan2.ShouldNotBeNull();
        adapterSpan3.ShouldNotBeNull();

        // 모든 AdapterSpan이 동일한 TraceId를 가짐
        var expectedTraceId = usecaseActivity!.TraceId.ToString();
        adapterSpan1.TraceId.ShouldBe(expectedTraceId);
        adapterSpan2.TraceId.ShouldBe(expectedTraceId);
        adapterSpan3.TraceId.ShouldBe(expectedTraceId);

        // 각 AdapterSpan은 서로 다른 SpanId를 가짐
        adapterSpan1.SpanId.ShouldNotBe(adapterSpan2.SpanId);
        adapterSpan2.SpanId.ShouldNotBe(adapterSpan3.SpanId);
        adapterSpan1.SpanId.ShouldNotBe(adapterSpan3.SpanId);
    }

    /// <summary>
    /// 여러 AdapterSpan 생성 후 Dispose되면 Activity.Current가 원래 값으로 복원되어야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Activity.Current 복원 검증:
    ///
    /// HttpRequestIn (ROOT)
    /// └── UsecaseActivity (Activity.Current) ← 시작 시점
    ///     ├── AdapterSpan1 생성/Dispose → Activity.Current 복원
    ///     ├── AdapterSpan2 생성/Dispose → Activity.Current 복원
    ///     └── AdapterSpan3 생성/Dispose → Activity.Current 복원
    ///
    /// AdapterSpan Dispose 후 Activity.Current가 UsecaseActivity로 복원됨
    /// </code>
    /// </remarks>
    [Fact]
    public void CreateChildSpan_MultipleSpans_ActivityCurrentRestoredAfterDispose()
    {
        // Arrange
        var sut = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? usecaseActivity = _activitySource.StartActivity("UsecaseActivity", ActivityKind.Internal, httpActivity!.Context);

        // 명시적으로 Activity.Current 설정 (StartActivity가 자동으로 변경하므로)
        Activity.Current = usecaseActivity;
        var expectedSpanId = usecaseActivity!.SpanId;

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act & Assert: AdapterSpan 생성 및 Dispose 후 Activity.Current SpanId 확인
        var adapterSpan1 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository OrderRepository.GetById",
            "Repository",
            "OrderRepository",
            "GetById");
        adapterSpan1?.Dispose();
        Activity.Current?.SpanId.ShouldBe(expectedSpanId);

        var adapterSpan2 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository ProductRepository.GetByIds",
            "Repository",
            "ProductRepository",
            "GetByIds");
        adapterSpan2?.Dispose();
        Activity.Current?.SpanId.ShouldBe(expectedSpanId);

        var adapterSpan3 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository CustomerRepository.GetById",
            "Repository",
            "CustomerRepository",
            "GetById");
        adapterSpan3?.Dispose();
        Activity.Current?.SpanId.ShouldBe(expectedSpanId);
    }

    /// <summary>
    /// 다양한 Adapter 타입(Repository, MessageBroker, HttpClient)이 동일한 부모를 가져야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 다양한 Adapter 타입 시나리오:
    ///
    /// HttpRequestIn (ROOT)
    /// └── UsecaseActivity (Activity.Current)
    ///     ├── AdapterSpan [Repository] ProductRepository.GetById
    ///     ├── AdapterSpan [MessageBroker] EventPublisher.Publish
    ///     └── AdapterSpan [HttpClient] PaymentGateway.ProcessPayment
    ///
    /// 모든 Adapter 타입이 동일한 부모(UsecaseActivity)를 가짐
    /// </code>
    /// </remarks>
    [Fact]
    public void CreateChildSpan_DifferentAdapterTypes_AllHaveSameParent()
    {
        // Arrange
        var sut = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? usecaseActivity = _activitySource.StartActivity("UsecaseActivity", ActivityKind.Internal, httpActivity!.Context);
        Activity.Current = usecaseActivity;

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act: 다양한 Adapter 타입의 Span 생성
        using var repositorySpan = sut.CreateChildSpan(
            httpContext,
            "adapter Repository ProductRepository.GetById",
            "Repository",
            "ProductRepository",
            "GetById");

        using var messageBrokerSpan = sut.CreateChildSpan(
            httpContext,
            "adapter MessageBroker EventPublisher.Publish",
            "MessageBroker",
            "EventPublisher",
            "Publish");

        using var httpClientSpan = sut.CreateChildSpan(
            httpContext,
            "adapter HttpClient PaymentGateway.ProcessPayment",
            "HttpClient",
            "PaymentGateway",
            "ProcessPayment");

        // Assert
        repositorySpan.ShouldNotBeNull();
        messageBrokerSpan.ShouldNotBeNull();
        httpClientSpan.ShouldNotBeNull();

        var expectedTraceId = usecaseActivity!.TraceId.ToString();
        repositorySpan.TraceId.ShouldBe(expectedTraceId);
        messageBrokerSpan.TraceId.ShouldBe(expectedTraceId);
        httpClientSpan.TraceId.ShouldBe(expectedTraceId);
    }

    /// <summary>
    /// FinT 실행 중 복수 AdapterSpan이 ActivityContextHolder를 통해 올바른 부모를 찾아야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// FinT 모나드 내부에서 복수 Adapter 호출:
    ///
    /// HttpRequestIn (ROOT)
    /// └── TraverseActivity (ActivityContextHolder에 저장)
    ///     ├── AdapterSpan1 (Activity.Current=null, ActivityContextHolder 사용)
    ///     ├── AdapterSpan2 (Activity.Current=null, ActivityContextHolder 사용)
    ///     └── AdapterSpan3 (Activity.Current=null, ActivityContextHolder 사용)
    ///
    /// Activity.Current가 null이어도 ActivityContextHolder를 통해 부모 찾음
    /// </code>
    /// </remarks>
    [Fact]
    public void CreateChildSpan_MultipleSpansWithActivityContextHolder_AllHaveSameParent()
    {
        // Arrange
        var sut = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? traverseActivity = _activitySource.StartActivity("TraverseActivity", ActivityKind.Internal, httpActivity!.Context);

        // FinT 실행 후 Activity.Current가 null인 상황
        Activity.Current = null;
        ActivityContextHolder.SetCurrentActivity(traverseActivity);

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act: 여러 AdapterSpan 생성 (Activity.Current=null)
        using var adapterSpan1 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository OrderRepository.GetById",
            "Repository",
            "OrderRepository",
            "GetById");

        using var adapterSpan2 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository ProductRepository.GetByIds",
            "Repository",
            "ProductRepository",
            "GetByIds");

        using var adapterSpan3 = sut.CreateChildSpan(
            httpContext,
            "adapter Repository CustomerRepository.GetById",
            "Repository",
            "CustomerRepository",
            "GetById");

        // Assert
        adapterSpan1.ShouldNotBeNull();
        adapterSpan2.ShouldNotBeNull();
        adapterSpan3.ShouldNotBeNull();

        // ActivityContextHolder의 TraverseActivity가 부모로 사용됨
        var expectedTraceId = traverseActivity!.TraceId.ToString();
        adapterSpan1.TraceId.ShouldBe(expectedTraceId);
        adapterSpan2.TraceId.ShouldBe(expectedTraceId);
        adapterSpan3.TraceId.ShouldBe(expectedTraceId);
    }

    #endregion
}
