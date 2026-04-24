namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 어댑터 특화 커스텀 에러의 기본 클래스 (표준 에러에 해당하지 않는 경우)
    /// </summary>
    /// <remarks>
    /// 파생 sealed record로 정의하여 타입 안전하게 사용합니다.
    /// <code>
    /// public sealed record RateLimited : AdapterErrorKind.Custom;
    /// </code>
    /// </remarks>
    public abstract record Custom : AdapterErrorKind;
}
