namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorType
{
    /// <summary>
    /// 파이프라인 검증 실패
    /// </summary>
    /// <param name="PropertyName">검증 실패한 속성 이름 (선택적)</param>
    public sealed record PipelineValidation(string? PropertyName = null) : AdapterErrorType;

    /// <summary>
    /// 파이프라인 예외 발생
    /// </summary>
    public sealed record PipelineException : AdapterErrorType;
}
