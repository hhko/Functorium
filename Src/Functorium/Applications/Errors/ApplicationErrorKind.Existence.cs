namespace Functorium.Applications.Errors;

public abstract partial record ApplicationErrorKind
{
    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : ApplicationErrorKind;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : ApplicationErrorKind;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : ApplicationErrorKind;

    /// <summary>
    /// 유효하지 않은 상태
    /// </summary>
    public sealed record InvalidState : ApplicationErrorKind;
}
