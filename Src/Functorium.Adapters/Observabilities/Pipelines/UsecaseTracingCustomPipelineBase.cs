using System.Diagnostics;

using Functorium.Adapters.Observabilities.Naming;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Usecase별 커스텀 Tracing을 생성하기 위한 베이스 클래스.
/// ActivitySource를 통해 커스텀 Activity(Span)를 생성하고 표준 태그를 설정합니다.
/// TRequest 타입으로부터 CQRS 타입(Query/Command)과 Handler 이름을 자동으로 식별합니다.
/// </summary>
/// <typeparam name="TRequest">Request 타입 (IQueryRequest 또는 ICommandRequest 구현)</typeparam>
public abstract class UsecaseTracingCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
{
    protected readonly ActivitySource _activitySource;
    private readonly string _activityNamePrefix;

    protected UsecaseTracingCustomPipelineBase(ActivitySource activitySource)
    {
        _activitySource = activitySource;
        string categoryType = GetRequestCategoryType(typeof(TRequest));
        string handler = GetRequestHandler();
        _activityNamePrefix = $"{ObservabilityNaming.Layers.Application} {ObservabilityNaming.Categories.Usecase}.{categoryType} {handler}";
    }

    /// <summary>
    /// 커스텀 Activity를 생성합니다.
    /// 형식: "{prefix}.{operationName}"
    /// 부모 Activity.Current가 존재하면 자식 span으로 생성됩니다.
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <param name="kind">Activity 종류 (기본값: Internal)</param>
    /// <returns>생성된 Activity 또는 리스너 없을 경우 null</returns>
    protected Activity? StartCustomActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        string activityName = GetActivityName(operationName);
        Activity? parentActivity = Activity.Current;

        return parentActivity != null
            ? _activitySource.StartActivity(activityName, kind, parentActivity.Context)
            : _activitySource.StartActivity(activityName, kind);
    }

    /// <summary>
    /// Activity 이름을 조회합니다.
    /// 형식: "{prefix}.{operationName}"
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <returns>전체 Activity 이름</returns>
    protected string GetActivityName(string operationName)
    {
        return $"{_activityNamePrefix}.{operationName}";
    }

    /// <summary>
    /// Activity에 표준 요청 태그 5종을 설정합니다.
    /// </summary>
    /// <param name="activity">대상 Activity</param>
    /// <param name="method">핸들러 메서드 이름</param>
    protected static void SetStandardRequestTags(Activity activity, string method)
    {
        string categoryType = GetRequestCategoryType(typeof(TRequest));
        string handler = GetRequestHandler();

        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Usecase);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryType, categoryType);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestHandler, handler);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, method);
    }
}
