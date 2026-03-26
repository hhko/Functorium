using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// Observable 래퍼가 base.Method() 호출 전에 설정하는 AsyncLocal 스코프.
/// ObservableSignal 호출 시 공통 필드(layer, category, handler, method)와 ILogger를 자동으로 제공합니다.
/// </summary>
public sealed class ObservableSignalScope : IDisposable
{
    private static readonly AsyncLocal<ObservableSignalScope?> _current = new();
    private readonly ObservableSignalScope? _parent;

    /// <summary>
    /// 현재 AsyncLocal 컨텍스트의 ObservableSignalScope.
    /// Observable 래퍼 외부에서 호출 시 null.
    /// </summary>
    internal static ObservableSignalScope? Current => _current.Value;

    /// <summary>
    /// 로거 인스턴스 (Observable 래퍼의 ILogger)
    /// </summary>
    internal ILogger Logger { get; }

    /// <summary>
    /// 요청 레이어 (항상 "adapter")
    /// </summary>
    internal string Layer { get; }

    /// <summary>
    /// 요청 카테고리 (예: "repository", "externalapi", "messaging")
    /// </summary>
    internal string Category { get; }

    /// <summary>
    /// 핸들러 이름 (Adapter 클래스명)
    /// </summary>
    internal string Handler { get; }

    /// <summary>
    /// 핸들러 메서드명
    /// </summary>
    internal string Method { get; }

    private ObservableSignalScope(ILogger logger, string layer, string category, string handler, string method)
    {
        Logger = logger;
        Layer = layer;
        Category = category;
        Handler = handler;
        Method = method;
        _parent = _current.Value;
        _current.Value = this;
    }

    /// <summary>
    /// 새 ObservableSignalScope를 시작합니다. Observable 래퍼의 Generated 코드에서 호출됩니다.
    /// </summary>
    public static ObservableSignalScope Begin(ILogger logger, string layer, string category, string handler, string method)
        => new(logger, layer, category, handler, method);

    /// <summary>
    /// 스코프를 종료하고 이전 스코프를 복원합니다.
    /// </summary>
    public void Dispose()
    {
        _current.Value = _parent;
    }
}
