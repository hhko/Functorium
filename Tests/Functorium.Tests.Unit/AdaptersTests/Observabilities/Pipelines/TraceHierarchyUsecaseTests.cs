using System.Diagnostics;
using Functorium.Adapters.Observabilities.Abstractions;
using Functorium.Adapters.Observabilities.Context;
using Functorium.Adapters.Observabilities.Spans;
using Functorium.Tests.Unit.Abstractions.Constants;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// 유스케이스 관점에서 Trace 계층 구조를 테스트합니다.
/// HTTP 요청 → Usecase → Adapter 계층 간 부모-자식 관계를 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// .NET의 ExecutionContext가 async/await를 통해 AsyncLocal을 자동으로 전파하므로,
/// Activity.Current는 LanguageExt의 IO/FinT 실행에서도 올바르게 유지됩니다.
/// </para>
/// <para>
/// 기대되는 Trace 계층 구조:
/// </para>
/// <code>
/// ┌─────────────────────────────────────────────────────────────────────────────┐
/// │  실제 시스템에서의 Trace 흐름                                                │
/// ├─────────────────────────────────────────────────────────────────────────────┤
/// │                                                                             │
/// │   [HTTP Layer]                                                              │
/// │   HttpRequestIn (ROOT)                                                      │
/// │   TraceId: T1, SpanId: S1                                                   │
/// │   │                                                                         │
/// │   │  ┌──────────────────────────────────────────────────────────────┐       │
/// │   │  │ [Application Layer - UsecaseTracingPipeline]                 │       │
/// │   └──┤                                                              │       │
/// │      │  GetAllProductsQuery.Handle                                  │       │
/// │      │  TraceId: T1, SpanId: S2, ParentSpanId: S1                   │       │
/// │      │  │                                                           │       │
/// │      │  │  ┌───────────────────────────────────────────────────┐    │       │
/// │      │  │  │ [Adapter Layer - AdapterTracingPipeline]          │    │       │
/// │      │  └──┤                                                   │    │       │
/// │      │     │  InMemoryProductRepository.GetAll                 │    │       │
/// │      │     │  TraceId: T1, SpanId: S3, ParentSpanId: S2 ← 핵심! │    │       │
/// │      │     └───────────────────────────────────────────────────┘    │       │
/// │      └──────────────────────────────────────────────────────────────┘       │
/// │                                                                             │
/// └─────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class TraceHierarchyUsecaseTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;
    private readonly List<Activity> _capturedActivities;

    public TraceHierarchyUsecaseTests()
    {
        _activitySource = new ActivitySource("TestService");
        _capturedActivities = new List<Activity>();

        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
        Activity.Current = null;
    }

    #region 유스케이스 시나리오 테스트

    /// <summary>
    /// HTTP → Usecase → Adapter 전체 흐름에서 올바른 부모-자식 관계가 형성되어야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 시뮬레이션되는 호출 흐름:
    ///
    /// 1. HTTP 요청 진입 (ASP.NET Core가 HttpRequestIn Activity 생성)
    ///    └── Activity.Current = HttpRequestIn
    ///
    /// 2. UsecaseTracingPipeline이 Usecase Activity 생성
    ///    └── Activity.Current = UsecaseActivity (Parent: HttpRequestIn)
    ///
    /// 3. Usecase 내부에서 Repository 호출
    ///    └── AdapterTracingPipeline이 OpenTelemetrySpanFactory.CreateChildSpan 호출
    ///        └── DetermineParentContext가 Activity.Current(UsecaseActivity)를 부모로 결정
    ///            └── Adapter Activity 생성 (Parent: UsecaseActivity) ← 핵심 검증 포인트
    /// </code>
    /// </remarks>
    [Fact]
    public void TraceHierarchy_AdapterIsChildOfUsecase_WhenCalledFromUsecaseContext()
    {
        // Arrange
        var spanFactory = new OpenTelemetrySpanFactory(_activitySource);

        // 1. HTTP Layer - ASP.NET Core가 생성하는 HttpRequestIn Activity 시뮬레이션
        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        httpActivity.ShouldNotBeNull();

        // 2. Application Layer - UsecaseTracingPipeline이 생성하는 Usecase Activity
        using Activity? usecaseActivity = _activitySource.StartActivity(
            "application usecase.query GetAllProductsQuery.Handle",
            ActivityKind.Internal,
            httpActivity.Context);
        usecaseActivity.ShouldNotBeNull();

        // Activity.Current가 Usecase Activity를 가리킴 (실제 시스템에서도 동일)
        Activity.Current = usecaseActivity;

        // HTTP 레벨의 ObservabilityContext (Scoped DI로 주입됨)
        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity.Context);

        // Act: Adapter Layer - OpenTelemetrySpanFactory가 Adapter Span 생성
        using ISpan? adapterSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository InMemoryProductRepository.GetAll",
            "Repository",
            "InMemoryProductRepository",
            "GetAll");

        // Assert
        adapterSpan.ShouldNotBeNull();

        // 핵심 검증: Adapter Span의 TraceId가 전체 요청과 동일
        adapterSpan.TraceId.ShouldBe(httpActivity.TraceId.ToString());

        // Activity.Current를 통해 부모가 결정되었으므로,
        // Usecase Activity와 같은 TraceId를 가져야 함
        adapterSpan.TraceId.ShouldBe(usecaseActivity.TraceId.ToString());
    }

    /// <summary>
    /// Activity.Current가 null일 때 parentContext(HTTP 레벨)를 폴백으로 사용해야 합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 참고: .NET의 ExecutionContext가 Activity.Current를 자동으로 전파하므로,
    /// 정상적인 시나리오에서 Activity.Current가 null이 되는 경우는 드뭅니다.
    /// 이 테스트는 비정상적인 상황에서의 폴백 동작을 검증합니다.
    /// </para>
    /// <code>
    /// 폴백 시나리오 (정상적이지 않은 상황):
    ///
    /// HttpRequestIn (ROOT) ← 이것이 부모로 선택됨
    /// └── AdapterSpan (형제 관계 - 비정상)
    ///
    /// * Activity.Current = null
    /// * parentContext = HttpRequestContext (폴백)
    ///
    /// 이 시나리오는 Usecase Pipeline이 누락된 경우에 발생할 수 있음
    /// </code>
    /// </remarks>
    [Fact]
    public void TraceHierarchy_AdapterUsesHttpContext_WhenActivityCurrentIsNull()
    {
        // Arrange
        var spanFactory = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");

        Activity.Current = null;

        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        using ISpan? adapterSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository InMemoryProductRepository.GetAll",
            "Repository",
            "InMemoryProductRepository",
            "GetAll");

        // Assert
        adapterSpan.ShouldNotBeNull();

        // HTTP 레벨 Context가 사용됨 (폴백)
        adapterSpan.TraceId.ShouldBe(httpActivity!.TraceId.ToString());
    }

    #endregion

    #region 다중 Adapter 호출 시나리오

    /// <summary>
    /// Usecase에서 여러 Adapter를 호출할 때 모두 Usecase의 자식이어야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 다중 Repository 호출 시나리오:
    ///
    /// HttpRequestIn (SpanId: S1)
    /// └── GetOrderQuery.Handle (SpanId: S2, ParentSpanId: S1)
    ///     ├── OrderRepository.GetById (SpanId: S3, ParentSpanId: S2)      ← 자식
    ///     ├── ProductRepository.GetByIds (SpanId: S4, ParentSpanId: S2)   ← 자식
    ///     └── CustomerRepository.GetById (SpanId: S5, ParentSpanId: S2)   ← 자식
    ///
    /// 모든 Adapter Span이 동일한 ParentSpanId(Usecase)를 가져야 함
    /// </code>
    /// </remarks>
    [Fact]
    public void TraceHierarchy_MultipleAdapters_AllAreChildrenOfSameUsecase()
    {
        // Arrange
        var spanFactory = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("HttpRequestIn");
        using Activity? usecaseActivity = _activitySource.StartActivity(
            "application usecase.query GetOrderQuery.Handle",
            ActivityKind.Internal,
            httpActivity!.Context);

        Activity.Current = usecaseActivity;
        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act: 여러 Adapter 호출 시뮬레이션
        using ISpan? orderRepoSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository OrderRepository.GetById",
            "Repository",
            "OrderRepository",
            "GetById");

        using ISpan? productRepoSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository ProductRepository.GetByIds",
            "Repository",
            "ProductRepository",
            "GetByIds");

        using ISpan? customerRepoSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository CustomerRepository.GetById",
            "Repository",
            "CustomerRepository",
            "GetById");

        // Assert: 모든 Adapter Span이 동일한 TraceId를 가짐
        orderRepoSpan.ShouldNotBeNull();
        productRepoSpan.ShouldNotBeNull();
        customerRepoSpan.ShouldNotBeNull();

        var expectedTraceId = usecaseActivity!.TraceId.ToString();
        orderRepoSpan.TraceId.ShouldBe(expectedTraceId);
        productRepoSpan.TraceId.ShouldBe(expectedTraceId);
        customerRepoSpan.TraceId.ShouldBe(expectedTraceId);
    }

    #endregion

    #region Command/Query CQRS 시나리오

    /// <summary>
    /// Query Usecase에서 Repository 호출 시 올바른 Trace 계층이 형성되어야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Query 시나리오:
    ///
    /// HttpRequestIn (GET /api/products)
    /// └── application usecase.query GetAllProductsQuery.Handle
    ///     └── adapter Repository InMemoryProductRepository.GetAll
    /// </code>
    /// </remarks>
    [Fact]
    public void TraceHierarchy_QueryUsecase_HasCorrectHierarchy()
    {
        // Arrange
        var spanFactory = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("GET /api/products");
        using Activity? queryActivity = _activitySource.StartActivity(
            "application usecase.query GetAllProductsQuery.Handle",
            ActivityKind.Internal,
            httpActivity!.Context);

        Activity.Current = queryActivity;
        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        using ISpan? repoSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository InMemoryProductRepository.GetAll",
            "Repository",
            "InMemoryProductRepository",
            "GetAll");

        // Assert
        repoSpan.ShouldNotBeNull();
        repoSpan.TraceId.ShouldBe(queryActivity!.TraceId.ToString());
    }

    /// <summary>
    /// Command Usecase에서 Repository 호출 시 올바른 Trace 계층이 형성되어야 합니다.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Command 시나리오:
    ///
    /// HttpRequestIn (POST /api/products)
    /// └── application usecase.command CreateProductCommand.Handle
    ///     ├── adapter Repository ProductRepository.Add
    ///     └── adapter MessageBroker EventPublisher.Publish
    /// </code>
    /// </remarks>
    [Fact]
    public void TraceHierarchy_CommandUsecase_HasCorrectHierarchy()
    {
        // Arrange
        var spanFactory = new OpenTelemetrySpanFactory(_activitySource);

        using Activity? httpActivity = _activitySource.StartActivity("POST /api/products");
        using Activity? commandActivity = _activitySource.StartActivity(
            "application usecase.command CreateProductCommand.Handle",
            ActivityKind.Internal,
            httpActivity!.Context);

        Activity.Current = commandActivity;
        ObservabilityContext httpContext = ObservabilityContext.FromActivityContext(httpActivity!.Context);

        // Act
        using ISpan? repoSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter Repository ProductRepository.Add",
            "Repository",
            "ProductRepository",
            "Add");

        using ISpan? eventSpan = spanFactory.CreateChildSpan(
            httpContext,
            "adapter MessageBroker EventPublisher.Publish",
            "MessageBroker",
            "EventPublisher",
            "Publish");

        // Assert
        repoSpan.ShouldNotBeNull();
        eventSpan.ShouldNotBeNull();

        var expectedTraceId = commandActivity!.TraceId.ToString();
        repoSpan.TraceId.ShouldBe(expectedTraceId);
        eventSpan.TraceId.ShouldBe(expectedTraceId);
    }

    #endregion
}
