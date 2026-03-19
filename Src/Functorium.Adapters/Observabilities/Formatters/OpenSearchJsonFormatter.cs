using System.Buffers;
using System.Text;
using System.Text.Json;

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;

using Attrs = Functorium.Adapters.Observabilities.Naming.ObservabilityNaming.CustomAttributes;
using OTel  = Functorium.Adapters.Observabilities.Naming.ObservabilityNaming.OTelAttributes;

namespace Functorium.Adapters.Observabilities.Formatters;

/// <summary>
/// OpenSearch 최적화 JSON 로그 포맷터.
/// ECS 호환 필드 매핑 + ctx.* 네임스페이스를 적용합니다.
/// </summary>
/// <remarks>
/// <para>변환 규칙:</para>
/// <list type="bullet">
///   <item>Properties 래퍼 제거 → 플랫 JSON</item>
///   <item>@timestamp (UTC ISO 8601), log.level (ECS 호환)</item>
///   <item>message (렌더링된 메시지), message.template (원본 템플릿)</item>
///   <item>EventId → event.id + event.name (플랫)</item>
///   <item>SourceContext → log.logger</item>
///   <item>request.message, response.message → JSON 문자열 (매핑 폭발 방지)</item>
///   <item>error 객체 → error.detail (JSON 문자열), error.type/error.code 보존</item>
///   <item>ctx.* 프로퍼티 → 그대로 출력</item>
///   <item>미식별 PascalCase → ctx.snake_case (안전망)</item>
///   <item>_typeTag, $type, Renderings 제거</item>
/// </list>
/// </remarks>
public sealed class OpenSearchJsonFormatter : ITextFormatter
{
    // ── ECS 표준 필드 키 ──
    private const string EcsTimestamp      = "@timestamp";
    private const string EcsLogLevel       = "log.level";
    private const string EcsMessage        = "message";
    private const string EcsMessageTemplate = "message.template";
    private const string EcsEventId        = "event.id";
    private const string EcsEventName      = "event.name";
    private const string EcsLogLogger      = "log.logger";
    private const string EcsErrorStackTrace = "error.stack_trace";

    // ── Serilog 내부 프로퍼티 키 ──
    private const string SerilogEventId      = "EventId";
    private const string SerilogSourceContext = "SourceContext";
    private const string SerilogTypeTag      = "_typeTag";
    private const string SerilogDollarType   = "$type";

    // ── Formatter 고유 키 ──
    private const string ErrorProperty       = "error";
    private const string ErrorDetailProperty = "error.detail";
    private const string CtxPrefix           = "ctx.";

    // ── EventId 구조체 내부 필드 ──
    private const string EventIdField   = "Id";
    private const string EventNameField = "Name";

    /// <summary>
    /// MessageTemplate 파라미터 및 프레임워크 표준 필드.
    /// 이 목록에 해당하는 필드는 ctx.* 네임스페이스 변환 없이 그대로 출력합니다.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> StandardFields =
    [
        // Request
        Attrs.RequestLayer,
        Attrs.RequestCategory,
        Attrs.RequestCategoryType,
        Attrs.RequestHandler,
        Attrs.RequestHandlerMethod,
        Attrs.RequestMessage,

        // Response
        Attrs.ResponseStatus,
        Attrs.ResponseElapsed,
        Attrs.ResponseMessage,

        // Error (스칼라)
        OTel.ErrorType,
        Attrs.ErrorCode,

        // Error (구조화 객체 → error.detail로 변환)
        ErrorProperty,

        // Event
        Attrs.RequestEventType,
        Attrs.RequestEventId,
        Attrs.RequestEventCount,
        Attrs.ResponseEventSuccessCount,
        Attrs.ResponseEventFailureCount
    ];

    /// <summary>
    /// 별도 처리 또는 제거 대상 필드.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> SkipFields =
    [
        SerilogEventId,        // → event.id + event.name 으로 변환
        SerilogSourceContext,   // → log.logger 로 변환
        SerilogTypeTag,        // Serilog 내부 태그
        SerilogDollarType      // Serilog 내부 태그
    ];

    /// <summary>
    /// 구조화 객체를 JSON 문자열로 직렬화하는 필드.
    /// 배열 객체 상관관계 소실 방지 + 매핑 폭발 방지.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> JsonStringifyFields =
    [
        Attrs.RequestMessage,
        Attrs.ResponseMessage
    ];

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            // @timestamp (UTC ISO 8601)
            writer.WriteString(EcsTimestamp, logEvent.Timestamp.UtcDateTime);

            // log.level (ECS 호환)
            writer.WriteString(EcsLogLevel, logEvent.Level.ToString());

            // message (렌더링된 메시지 — literal string + JSON structure)
            writer.WriteString(EcsMessage, RenderMessageLiteralJson(logEvent));

            // message.template (원본 템플릿)
            writer.WriteString(EcsMessageTemplate, logEvent.MessageTemplate.Text);

            // EventId → event.id + event.name
            WriteEventId(writer, logEvent.Properties);

            // 표준 필드 + ctx.* + 안전망 변환
            WriteProperties(writer, logEvent.Properties);

            // SourceContext → log.logger
            WriteLogLogger(writer, logEvent.Properties);

            // Exception
            if (logEvent.Exception is not null)
                writer.WriteString(EcsErrorStackTrace, logEvent.Exception.ToString());

            writer.WriteEndObject();
        }

        output.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
        output.WriteLine();
    }

    /// <summary>
    /// EventId 구조체를 event.id + event.name 플랫 필드로 변환합니다.
    /// </summary>
    private static void WriteEventId(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        if (!properties.TryGetValue(SerilogEventId, out var value)
            || value is not StructureValue eventIdStruct)
            return;

        foreach (var prop in eventIdStruct.Properties)
        {
            if (prop.Name == EventIdField && prop.Value is ScalarValue { Value: int id })
                writer.WriteNumber(EcsEventId, id);
            else if (prop.Name == EventNameField && prop.Value is ScalarValue { Value: string name })
                writer.WriteString(EcsEventName, name);
        }
    }

    /// <summary>
    /// LogEvent.Properties를 규칙에 따라 출력합니다.
    /// </summary>
    private static void WriteProperties(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        foreach (var (key, value) in properties)
        {
            // 별도 처리 또는 제거 대상
            if (SkipFields.Contains(key))
                continue;

            // request.message, response.message → JSON 문자열
            if (JsonStringifyFields.Contains(key))
            {
                writer.WriteString(key, SerializeToJsonString(value));
                continue;
            }

            // error 구조화 객체 → error.detail (JSON 문자열)
            if (key == ErrorProperty && value is not ScalarValue)
            {
                writer.WriteString(ErrorDetailProperty, SerializeToJsonString(value));
                continue;
            }

            // 표준 필드 → 그대로 출력
            if (StandardFields.Contains(key))
            {
                WriteProperty(writer, key, value);
                continue;
            }

            // ctx.* → 그대로 출력
            if (key.StartsWith(CtxPrefix, StringComparison.Ordinal))
            {
                WriteProperty(writer, key, value);
                continue;
            }

            // 안전망: PascalCase → ctx.snake_case
            WriteProperty(writer, CtxPrefix + ToSnakeCase(key), value);
        }
    }

    /// <summary>
    /// SourceContext → log.logger (ECS 호환)
    /// </summary>
    private static void WriteLogLogger(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        if (properties.TryGetValue(SerilogSourceContext, out var value)
            && value is ScalarValue { Value: string context })
        {
            writer.WriteString(EcsLogLogger, context);
        }
    }

    private static void WriteProperty(Utf8JsonWriter writer, string name, LogEventPropertyValue value)
    {
        writer.WritePropertyName(name);
        WriteValue(writer, value);
    }

    /// <summary>
    /// LogEventPropertyValue를 JSON 값으로 출력합니다.
    /// _typeTag, $type 프로퍼티는 제거합니다.
    /// </summary>
    private static void WriteValue(Utf8JsonWriter writer, LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalar:
                WriteScalar(writer, scalar);
                break;

            case StructureValue structure:
                writer.WriteStartObject();
                foreach (var prop in structure.Properties)
                {
                    if (prop.Name is SerilogTypeTag or SerilogDollarType)
                        continue;
                    writer.WritePropertyName(prop.Name);
                    WriteValue(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case SequenceValue sequence:
                writer.WriteStartArray();
                foreach (var element in sequence.Elements)
                    WriteValue(writer, element);
                writer.WriteEndArray();
                break;

            case DictionaryValue dict:
                writer.WriteStartObject();
                foreach (var kvp in dict.Elements)
                {
                    writer.WritePropertyName(kvp.Key.Value?.ToString() ?? "");
                    WriteValue(writer, kvp.Value);
                }
                writer.WriteEndObject();
                break;
        }
    }

    private static void WriteScalar(Utf8JsonWriter writer, ScalarValue scalar)
    {
        switch (scalar.Value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case decimal m:
                writer.WriteNumberValue(m);
                break;
            case DateTime dt:
                writer.WriteStringValue(dt);
                break;
            case DateTimeOffset dto:
                writer.WriteStringValue(dto);
                break;
            default:
                writer.WriteStringValue(scalar.Value.ToString());
                break;
        }
    }

    /// <summary>
    /// LogEventPropertyValue를 JSON 문자열로 직렬화합니다.
    /// 구조화 객체를 플랫 JSON 문자열로 변환하여 매핑 폭발을 방지합니다.
    /// </summary>
    private static string SerializeToJsonString(LogEventPropertyValue value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteValue(writer, value);
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// MessageTemplate을 literal string + JSON structure 모드로 렌더링합니다.
    /// Serilog outputTemplate의 {Message:lj}와 동일한 결과를 생성합니다.
    /// - 문자열 스칼라: 따옴표 없이 출력 (literal)
    /// - 구조화 객체: JSON 형식으로 출력
    /// - 숫자/포맷 지정 스칼라: 원본 포맷 유지
    /// </summary>
    private static string RenderMessageLiteralJson(LogEvent logEvent)
    {
        using var output = new StringWriter();
        foreach (var token in logEvent.MessageTemplate.Tokens)
        {
            if (token is PropertyToken pt
                && logEvent.Properties.TryGetValue(pt.PropertyName, out var val))
            {
                if (val is ScalarValue { Value: string } && string.IsNullOrEmpty(pt.Format))
                    val.Render(output, "l");       // literal: 따옴표 없이
                else if (val is not ScalarValue)
                    val.Render(output, "j");        // JSON: 구조화 객체
                else
                    val.Render(output, pt.Format);  // 원본 포맷 유지 (숫자 등)
            }
            else
            {
                token.Render(logEvent.Properties, output);
            }
        }
        return output.ToString();
    }

    /// <summary>
    /// PascalCase를 snake_case로 변환합니다.
    /// 안전망 프로퍼티 네이밍에 사용됩니다.
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length + 4);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
