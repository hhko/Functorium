namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : DomainErrorType;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : DomainErrorType;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : DomainErrorType;

    /// <summary>
    /// 값이 일치하지 않음 (예: 비밀번호 확인)
    /// </summary>
    public sealed record Mismatch : DomainErrorType;
}
