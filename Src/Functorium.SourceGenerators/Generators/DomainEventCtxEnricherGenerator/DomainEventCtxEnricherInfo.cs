using Functorium.SourceGenerators.Generators.CtxEnricherGenerator;

using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.DomainEventCtxEnricherGenerator;

/// <summary>
/// DomainEventCtxEnricher 생성에 필요한 이벤트 메타데이터.
/// IDomainEvent 구현체의 속성 정보와 Enricher 클래스 이름을 포함합니다.
/// </summary>
public readonly record struct DomainEventCtxEnricherInfo
{
    public readonly string Namespace;
    public readonly string[] ContainingTypeNames;
    public readonly string EventTypeName;
    public readonly string EventTypeFullName;
    public readonly CtxPropertyInfo[] EventProperties;
    public readonly string EnricherClassName;
    public readonly Location? Location;
    public readonly string SkipReason;

    public static readonly DomainEventCtxEnricherInfo None = new(
        string.Empty, [], string.Empty, string.Empty, [], string.Empty, null, string.Empty);

    public DomainEventCtxEnricherInfo(
        string @namespace,
        string[] containingTypeNames,
        string eventTypeName,
        string eventTypeFullName,
        CtxPropertyInfo[] eventProperties,
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

    public static DomainEventCtxEnricherInfo Inaccessible(
        string eventTypeDisplayName,
        string inaccessibleTypeName,
        string accessibility,
        Location? location)
    {
        return new DomainEventCtxEnricherInfo(
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
