using Functorium.SourceGenerators.Generators.LogEnricherGenerator;

using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.DomainEventLogEnricherGenerator;

/// <summary>
/// DomainEventLogEnricher 생성에 필요한 이벤트 메타데이터.
/// IDomainEvent 구현체의 속성 정보와 Enricher 클래스 이름을 포함합니다.
/// </summary>
public readonly record struct DomainEventLogEnricherInfo
{
    /// <summary>
    /// 이벤트 타입의 네임스페이스. 예: "MyApp.Domain.Events"
    /// </summary>
    public readonly string Namespace;

    /// <summary>
    /// 포함 타입 이름 목록 (바깥 → 안쪽). 예: ["Order"] for Order.CreatedEvent
    /// </summary>
    public readonly string[] ContainingTypeNames;

    /// <summary>
    /// 이벤트 타입 이름. 예: "CreatedEvent"
    /// </summary>
    public readonly string EventTypeName;

    /// <summary>
    /// 이벤트 타입의 전체 한정 이름. 예: "global::MyApp.Domain.Events.Order.CreatedEvent"
    /// </summary>
    public readonly string EventTypeFullName;

    /// <summary>
    /// 이벤트 속성 목록 (IDomainEvent 기본 속성 제외).
    /// </summary>
    public readonly LogEnricherPropertyInfo[] EventProperties;

    /// <summary>
    /// 생성될 Enricher 클래스 이름. 예: "OrderCreatedEventLogEnricher"
    /// </summary>
    public readonly string EnricherClassName;

    /// <summary>
    /// 원본 소스 위치 (진단용).
    /// </summary>
    public readonly Location? Location;

    /// <summary>
    /// 코드 생성을 건너뛰는 이유. 비어 있으면 정상 생성 대상.
    /// </summary>
    public readonly string SkipReason;

    /// <summary>
    /// 빈 값을 나타내는 상수.
    /// </summary>
    public static readonly DomainEventLogEnricherInfo None = new(
        string.Empty, [], string.Empty, string.Empty, [], string.Empty, null, string.Empty);

    public DomainEventLogEnricherInfo(
        string @namespace,
        string[] containingTypeNames,
        string eventTypeName,
        string eventTypeFullName,
        LogEnricherPropertyInfo[] eventProperties,
        string enricherClassName,
        Location? location,
        string skipReason = "")
    {
        Namespace = @namespace;
        ContainingTypeNames = containingTypeNames;
        EventTypeName = eventTypeName;
        EventTypeFullName = eventTypeFullName;
        EventProperties = eventProperties;
        EnricherClassName = enricherClassName;
        Location = location;
        SkipReason = skipReason;
    }

    /// <summary>
    /// 접근 불가능한 타입에 대한 진단 전용 엔트리를 생성합니다.
    /// </summary>
    public static DomainEventLogEnricherInfo Inaccessible(
        string eventTypeDisplayName,
        string inaccessibleTypeName,
        string accessibility,
        Location? location)
    {
        return new DomainEventLogEnricherInfo(
            @namespace: string.Empty,
            containingTypeNames: [],
            eventTypeName: "SKIP",
            eventTypeFullName: inaccessibleTypeName,
            eventProperties: [],
            enricherClassName: string.Empty,
            location: location,
            skipReason: accessibility);
    }
}
