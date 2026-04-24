namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 외부 서비스 사용 불가
    /// </summary>
    /// <param name="ServiceName">서비스 이름 (선택적)</param>
    public sealed record ExternalServiceUnavailable(string? ServiceName = null) : AdapterErrorKind;

    /// <summary>
    /// 연결 실패
    /// </summary>
    /// <param name="Target">연결 대상 (선택적)</param>
    public sealed record ConnectionFailed(string? Target = null) : AdapterErrorKind;

    /// <summary>
    /// 타임아웃
    /// </summary>
    /// <param name="Duration">타임아웃 시간 (선택적)</param>
    public sealed record Timeout(TimeSpan? Duration = null) : AdapterErrorKind;
}
