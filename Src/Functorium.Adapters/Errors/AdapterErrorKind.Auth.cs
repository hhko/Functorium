namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 필수 설정이 누락됨
    /// </summary>
    public sealed record NotConfigured : AdapterErrorKind;

    /// <summary>
    /// 지원되지 않는 연산
    /// </summary>
    public sealed record NotSupported : AdapterErrorKind;

    /// <summary>
    /// 인증되지 않음
    /// </summary>
    public sealed record Unauthorized : AdapterErrorKind;

    /// <summary>
    /// 접근 금지
    /// </summary>
    public sealed record Forbidden : AdapterErrorKind;
}
