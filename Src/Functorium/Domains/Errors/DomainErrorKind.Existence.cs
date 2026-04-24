namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorKind
{
    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : DomainErrorKind;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : DomainErrorKind;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : DomainErrorKind;

    /// <summary>
    /// 값이 일치하지 않음 (예: 비밀번호 확인)
    /// </summary>
    public sealed record Mismatch : DomainErrorKind;
}
