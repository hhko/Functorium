namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 파라미터 이름 충돌을 해결하는 유틸리티 클래스
/// </summary>
internal static class ParameterNameResolver
{
    /// <summary>
    /// 예약된 이름과 충돌하는 경우 새로운 이름을 반환합니다.
    /// </summary>
    /// <param name="parameterName">원래 파라미터 이름</param>
    /// <returns>충돌하지 않는 파라미터 이름</returns>
    public static string ResolveName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return parameterName;
        }

        // 언더스코어로 시작하는 경우 필드명과 충돌 가능성이 있으므로 처리
        if (parameterName.StartsWith("_"))
        {
            // _logger -> baseLogger (언더스코어 제거 + 접두사)
            string nameWithoutUnderscore = parameterName.Substring(1);
            return $"{PipelineGeneratorConstants.NameConflictPrefix}{char.ToUpper(nameWithoutUnderscore[0])}{nameWithoutUnderscore.Substring(1)}";
        }

        if (PipelineGeneratorConstants.ReservedParameterNames.Contains(parameterName))
        {
            // 예: logger -> baseLogger, adapterTrace -> baseAdapterTrace
            return $"{PipelineGeneratorConstants.NameConflictPrefix}{char.ToUpper(parameterName[0])}{parameterName.Substring(1)}";
        }

        return parameterName;
    }

    /// <summary>
    /// 파라미터 목록의 이름들을 충돌 없이 해결합니다.
    /// </summary>
    public static List<(ParameterInfo Original, string ResolvedName)> ResolveNames(List<ParameterInfo> parameters)
    {
        return parameters
            .Select(p => (Original: p, ResolvedName: ResolveName(p.Name)))
            .ToList();
    }
}
