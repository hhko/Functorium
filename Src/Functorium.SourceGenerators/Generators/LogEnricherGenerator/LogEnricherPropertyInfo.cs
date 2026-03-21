namespace Functorium.SourceGenerators.Generators.LogEnricherGenerator;

/// <summary>
/// LogEnricher 생성에 필요한 속성별 메타데이터.
/// </summary>
public readonly record struct LogEnricherPropertyInfo
{
    /// <summary>
    /// 속성 이름 (PascalCase). 예: "CustomerId"
    /// </summary>
    public readonly string PropertyName;

    /// <summary>
    /// ctx 필드 이름 (snake_case). 예: "ctx.customer_id"
    /// </summary>
    public readonly string CtxFieldName;

    /// <summary>
    /// 타입의 전체 이름. 예: "string", "System.Collections.Generic.List&lt;OrderLine&gt;"
    /// </summary>
    public readonly string TypeFullName;

    /// <summary>
    /// 컬렉션 타입 여부.
    /// </summary>
    public readonly bool IsCollection;

    /// <summary>
    /// 컬렉션일 때 Count 접근 표현식. 예: "?.Count ?? 0", "?.Length ?? 0"
    /// </summary>
    public readonly string? CountExpression;

    /// <summary>
    /// OpenSearch 동적 매핑 타입 그룹. 예: "keyword", "long", "double", "boolean"
    /// 같은 ctx 필드명에 서로 다른 그룹이 할당되면 매핑 충돌이 발생합니다.
    /// </summary>
    public readonly string OpenSearchTypeGroup;

    /// <summary>
    /// [LogEnricherRoot] 어트리뷰트 또는 루트 인터페이스로 인해
    /// ctx 루트 레벨(ctx.{field})로 승격된 속성 여부.
    /// </summary>
    public readonly bool IsRoot;

    /// <summary>
    /// 값 객체 등 복합 타입에서 .ToString() 호출이 필요한지 여부.
    /// DomainEventLogEnricher에서 값 객체를 keyword로 변환할 때 사용됩니다.
    /// </summary>
    public readonly bool NeedsToString;

    public LogEnricherPropertyInfo(
        string propertyName,
        string ctxFieldName,
        string typeFullName,
        bool isCollection,
        string? countExpression,
        string openSearchTypeGroup,
        bool isRoot = false,
        bool needsToString = false)
    {
        PropertyName = propertyName;
        CtxFieldName = ctxFieldName;
        TypeFullName = typeFullName;
        IsCollection = isCollection;
        CountExpression = countExpression;
        OpenSearchTypeGroup = openSearchTypeGroup;
        IsRoot = isRoot;
        NeedsToString = needsToString;
    }
}
