namespace Functorium.SourceGenerators.Generators.CtxEnricherGenerator;

/// <summary>
/// CtxEnricher 생성에 필요한 속성별 메타데이터.
/// </summary>
public readonly record struct CtxPropertyInfo
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
    /// [CtxRoot] 어트리뷰트 또는 루트 인터페이스로 인해
    /// ctx 루트 레벨(ctx.{field})로 승격된 속성 여부.
    /// </summary>
    public readonly bool IsRoot;

    /// <summary>
    /// 값 객체 등 복합 타입에서 .ToString() 호출이 필요한지 여부.
    /// DomainEventCtxEnricher에서 값 객체를 keyword로 변환할 때 사용됩니다.
    /// </summary>
    public readonly bool NeedsToString;

    /// <summary>
    /// 대상 Pillar 플래그 값 (CtxPillar enum의 정수 값).
    /// 기본값: 3 (Logging | Tracing = CtxPillar.Default).
    /// </summary>
    public readonly int TargetPillars;

    public CtxPropertyInfo(
        string propertyName,
        string ctxFieldName,
        string typeFullName,
        bool isCollection,
        string? countExpression,
        string openSearchTypeGroup,
        bool isRoot = false,
        bool needsToString = false,
        int targetPillars = 3) // CtxPillar.Default = Logging(1) | Tracing(2) = 3
    {
        PropertyName = propertyName;
        CtxFieldName = ctxFieldName;
        TypeFullName = typeFullName;
        IsCollection = isCollection;
        CountExpression = countExpression;
        OpenSearchTypeGroup = openSearchTypeGroup;
        IsRoot = isRoot;
        NeedsToString = needsToString;
        TargetPillars = targetPillars;
    }

    // CtxPillar flag constants (Source Generator는 Functorium.Applications를 참조하지 않으므로 상수로 정의)
    public const int PillarLogging = 1;
    public const int PillarTracing = 2;
    public const int PillarMetricsTag = 4;
    public const int PillarMetricsValue = 8;
    public const int PillarDefault = PillarLogging | PillarTracing; // 3
    public const int PillarAll = PillarLogging | PillarTracing | PillarMetricsTag; // 7

    public bool HasLogging => (TargetPillars & PillarLogging) != 0;
    public bool HasTracing => (TargetPillars & PillarTracing) != 0;
    public bool HasMetricsTag => (TargetPillars & PillarMetricsTag) != 0;
    public bool HasMetricsValue => (TargetPillars & PillarMetricsValue) != 0;
    public bool IsDefault => TargetPillars == PillarDefault;
}
