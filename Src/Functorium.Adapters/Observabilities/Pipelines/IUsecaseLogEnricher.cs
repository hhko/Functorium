namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Usecase 로그에 비즈니스 컨텍스트 필드를 추가하는 Enricher 인터페이스.
/// 내장 UsecaseLoggingPipeline이 Request/Response 로그 출력 시
/// Serilog LogContext에 커스텀 속성을 자동으로 Push합니다.
/// </summary>
/// <typeparam name="TRequest">대상 Request 타입</typeparam>
public interface IUsecaseLogEnricher<in TRequest>
{
    /// <summary>
    /// Request 로그 출력 전에 호출됩니다.
    /// LogContext.PushProperty로 추가 속성을 Push하고 IDisposable을 반환하세요.
    /// </summary>
    IDisposable? EnrichRequestLog(TRequest request);

    /// <summary>
    /// Response 로그 출력 전에 호출됩니다.
    /// LogContext.PushProperty로 추가 속성을 Push하고 IDisposable을 반환하세요.
    /// </summary>
    IDisposable? EnrichResponseLog(TRequest request);
}
