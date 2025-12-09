namespace Functorium.Testing.Assertions.Logging;

/// <summary>
/// Serilog LogEventPropertyValue에서 실제 값을 추출하는 유틸리티 클래스
/// </summary>
public static class LogEventPropertyExtractor
{
    /// <summary>
    /// LogEventPropertyValue에서 실제 값을 재귀적으로 추출합니다.
    /// 현대적인 패턴 매칭과 null 처리 연산자를 활용
    /// </summary>
    public static object ExtractValue(Serilog.Events.LogEventPropertyValue propertyValue) =>
        propertyValue switch
        {
            // ScalarValue: 단일 값 처리 (null 안전)
            Serilog.Events.ScalarValue scalar => scalar.Value ?? "null",

            // SequenceValue: 배열/리스트 처리
            Serilog.Events.SequenceValue sequence => sequence.Elements
                .Select(ExtractValue)
                .ToList(),

            // StructureValue: 객체 구조 처리
            Serilog.Events.StructureValue structure => structure.Properties
                .ToDictionary(
                    prop => prop.Name,
                    prop => ExtractValue(prop.Value)
                ),

            // DictionaryValue: 키-값 쌍 처리
            Serilog.Events.DictionaryValue dict => dict.Elements
                .ToDictionary(
                    kvp => kvp.Key.Value?.ToString() ?? "null",
                    kvp => ExtractValue(kvp.Value)
                ),

            // 처리되지 않은 타입들 (확장성을 위해 로깅)
            var unhandled => HandleUnhandledType(unhandled)
        };

    /// <summary>
    /// 처리되지 않은 타입을 처리합니다.
    /// </summary>
    private static object HandleUnhandledType(Serilog.Events.LogEventPropertyValue propertyValue)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[LogEventPropertyExtractor] Unhandled type: {propertyValue.GetType().Name} - {propertyValue}"
        );

        return propertyValue.ToString() ?? "null";
    }

    /// <summary>
    /// 여러 LogEvent에서 정보를 추출하여 익명 타입 리스트로 변환합니다.
    /// 컬렉션 식과 타입 추론 활용
    /// </summary>
    public static IEnumerable<object> ExtractLogData(IEnumerable<Serilog.Events.LogEvent> logEvents) =>
        logEvents.Select(CreateLogDataObject);

    /// <summary>
    /// 단일 LogEvent에서 정보를 추출하여 익명 타입으로 변환합니다.
    /// </summary>
    public static object ExtractLogData(Serilog.Events.LogEvent logEvent) =>
        CreateLogDataObject(logEvent);

    /// <summary>
    /// LogEvent에서 익명 타입 객체를 생성합니다.
    /// </summary>
    private static object CreateLogDataObject(Serilog.Events.LogEvent logEvent) =>
        new
        {
            Information = logEvent.MessageTemplate.Text,
            Properties = logEvent.Properties.ToDictionary(
                static p => p.Key,           // 정적 람다 (성능 최적화)
                static p => ExtractValue(p.Value)
            )
        };
}

