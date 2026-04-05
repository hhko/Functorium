namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorType
{
    /// <summary>
    /// 값을 찾을 수 없음
    /// </summary>
    public sealed record NotFound : AdapterErrorType;

    /// <summary>
    /// 요청한 ID 중 일부를 찾을 수 없음
    /// </summary>
    public sealed record PartialNotFound : AdapterErrorType;

    /// <summary>
    /// 값이 이미 존재함
    /// </summary>
    public sealed record AlreadyExists : AdapterErrorType;

    /// <summary>
    /// 중복된 값
    /// </summary>
    public sealed record Duplicate : AdapterErrorType;

    /// <summary>
    /// 유효하지 않은 상태
    /// </summary>
    public sealed record InvalidState : AdapterErrorType;
}
