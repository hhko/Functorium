namespace Functorium.Testing.Assertions.Logging;

/// <summary>
/// LogEventPropertyValue를 Verify 스냅샷 테스트용 익명 객체로 변환하는 유틸리티
/// </summary>
public static class LogEventPropertyValueConverter
{
    /// <summary>
    /// LogEventPropertyValue를 익명 객체로 재귀 변환합니다.
    /// </summary>
    public static object ToAnonymousObject(Serilog.Events.LogEventPropertyValue value)
    {
        return value switch
        {
            Serilog.Events.StructureValue sv => sv.Properties.ToDictionary(
                p => p.Name,
                p => ToAnonymousObject(p.Value)),
            Serilog.Events.SequenceValue seq => seq.Elements.Select(ToAnonymousObject).ToArray(),
            Serilog.Events.ScalarValue scalar => scalar.Value!,
            _ => value.ToString()
        };
    }
}
