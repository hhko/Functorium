using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.LogEnricherGenerator;

/// <summary>
/// LogEnricher 생성에 필요한 메타데이터.
/// Request + Response 타입 정보와 양쪽 속성 목록을 포함합니다.
/// </summary>
public readonly record struct LogEnricherInfo
{
    /// <summary>
    /// Request 타입의 네임스페이스. 예: "ObservabilityHost.Usecases"
    /// </summary>
    public readonly string Namespace;

    /// <summary>
    /// 포함 타입 이름 목록 (바깥 → 안쪽). 예: ["PlaceOrderCommand"]
    /// </summary>
    public readonly string[] ContainingTypeNames;

    /// <summary>
    /// Request 타입 이름. 예: "Request"
    /// </summary>
    public readonly string RequestTypeName;

    /// <summary>
    /// Request 속성 목록.
    /// </summary>
    public readonly LogEnricherPropertyInfo[] RequestProperties;

    /// <summary>
    /// Response(TSuccess) 타입의 전체 이름. 예: "PlaceOrderCommand.Response"
    /// </summary>
    public readonly string ResponseTypeName;

    /// <summary>
    /// Response(TSuccess) 타입의 전체 한정 이름 (네임스페이스 포함).
    /// 예: "global::ObservabilityHost.Usecases.PlaceOrderCommand.Response"
    /// </summary>
    public readonly string ResponseTypeFullName;

    /// <summary>
    /// Response 속성 목록.
    /// </summary>
    public readonly LogEnricherPropertyInfo[] ResponseProperties;

    /// <summary>
    /// 생성될 Enricher 클래스 이름. 예: "PlaceOrderCommandRequestLogEnricher"
    /// </summary>
    public readonly string EnricherClassName;

    /// <summary>
    /// Request의 전체 한정 타입 이름 (네임스페이스 포함).
    /// 예: "global::ObservabilityHost.Usecases.PlaceOrderCommand.Request"
    /// </summary>
    public readonly string RequestTypeFullName;

    /// <summary>
    /// 원본 소스 위치 (진단용).
    /// </summary>
    public readonly Location? Location;

    /// <summary>
    /// 코드 생성을 건너뛰는 이유. 비어 있으면 정상 생성 대상.
    /// 비어 있지 않으면 진단 전용 엔트리 (예: "TestCommandRequest is private").
    /// </summary>
    public readonly string SkipReason;

    /// <summary>
    /// 빈 값을 나타내는 상수.
    /// </summary>
    public static readonly LogEnricherInfo None = new(
        string.Empty, [], string.Empty, [], string.Empty,
        string.Empty, [], string.Empty, string.Empty, null, string.Empty);

    public LogEnricherInfo(
        string @namespace,
        string[] containingTypeNames,
        string requestTypeName,
        LogEnricherPropertyInfo[] requestProperties,
        string responseTypeName,
        string responseTypeFullName,
        LogEnricherPropertyInfo[] responseProperties,
        string enricherClassName,
        string requestTypeFullName,
        Location? location,
        string skipReason = "")
    {
        Namespace = @namespace;
        ContainingTypeNames = containingTypeNames;
        RequestTypeName = requestTypeName;
        RequestProperties = requestProperties;
        ResponseTypeName = responseTypeName;
        ResponseTypeFullName = responseTypeFullName;
        ResponseProperties = responseProperties;
        EnricherClassName = enricherClassName;
        RequestTypeFullName = requestTypeFullName;
        Location = location;
        SkipReason = skipReason;
    }

    /// <summary>
    /// 접근 불가능한 타입에 대한 진단 전용 엔트리를 생성합니다.
    /// </summary>
    public static LogEnricherInfo Inaccessible(
        string requestTypeDisplayName,
        string inaccessibleTypeName,
        string accessibility,
        Location? location)
    {
        return new LogEnricherInfo(
            @namespace: string.Empty,
            containingTypeNames: [],
            requestTypeName: "SKIP",
            requestProperties: [],
            responseTypeName: string.Empty,
            responseTypeFullName: inaccessibleTypeName,
            responseProperties: [],
            enricherClassName: string.Empty,
            requestTypeFullName: requestTypeDisplayName,
            location: location,
            skipReason: accessibility);
    }
}
