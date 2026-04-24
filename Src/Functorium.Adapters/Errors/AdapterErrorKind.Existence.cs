namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : AdapterErrorKind;

    /// <summary>
    /// 요청한 ID 중 일부를 찾을 수 없음
    /// </summary>
    public sealed record PartialNotFound : AdapterErrorKind;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : AdapterErrorKind;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : AdapterErrorKind;

    /// <summary>
    /// 유효하지 않은 상태
    /// </summary>
    public sealed record InvalidState : AdapterErrorKind;
}
