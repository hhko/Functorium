namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// Pipeline 생성기에서 사용하는 상수들
/// </summary>
internal static class PipelineGeneratorConstants
{
    /// <summary>
    /// Pipeline 클래스의 고정 파라미터 이름들
    /// 타겟 클래스의 생성자 파라미터와 이름 충돌을 방지하기 위해 사용
    /// </summary>
    public static readonly HashSet<string> ReservedParameterNames = new()
    {
        "parentContext",
        "logger",
        "adapterTrace",
        "adapterMetric"
    };

    /// <summary>
    /// 예약된 이름과 충돌할 때 사용할 접두사
    /// </summary>
    public const string NameConflictPrefix = "base";

    /// <summary>
    /// 파라미터 없을 때 로깅에 사용할 빈 객체 표현
    /// </summary>
    public const string EmptyRequestObjectLiteral = "new {  }";
}
