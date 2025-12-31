using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// StartupLogger에서 자동으로 설정 정보를 출력할 수 있는 Options 인터페이스
/// 이 인터페이스를 구현한 Options는 애플리케이션 시작 시 자동으로 로깅됩니다.
/// </summary>
public interface IStartupOptionsLogger
{
    void LogConfiguration(ILogger logger);
}
