using Microsoft.Extensions.Logging;

namespace Functorium.Domains.Observabilities;

/// <summary>
/// ObservableSignal의 실제 로깅 구현을 제공하는 팩토리 인터페이스.
/// Adapter 레이어에서 구현하여 ILogger + Activity 연동을 처리합니다.
/// </summary>
public interface IObservableSignalFactory
{
    /// <summary>
    /// 구조화된 로그를 출력합니다.
    /// 공통 필드(request.layer, request.category.name, request.handler.name, request.handler.method)는
    /// ObservableSignalScope에서 자동으로 포함됩니다.
    /// </summary>
    /// <param name="level">로그 수준 (Debug, Warning, Error)</param>
    /// <param name="message">로그 메시지</param>
    /// <param name="context">부가 컨텍스트 필드 (adapter.* 프리픽스 권장)</param>
    /// <param name="exception">예외 (Error 수준에서 선택적)</param>
    void Log(LogLevel level, string message, (string Key, object? Value)[]? context, Exception? exception);
}
