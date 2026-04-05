namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorType
{
    /// <summary>
    /// 이벤트 발행 실패
    /// </summary>
    public sealed record PublishFailed : AdapterErrorType;

    /// <summary>
    /// 이벤트 핸들러 실행 실패
    /// </summary>
    public sealed record HandlerFailed : AdapterErrorType;

    /// <summary>
    /// 이벤트 타입이 유효하지 않음
    /// </summary>
    public sealed record InvalidEventType : AdapterErrorType;

    /// <summary>
    /// 이벤트 발행 취소됨
    /// </summary>
    public sealed record PublishCancelled : AdapterErrorType;
}
