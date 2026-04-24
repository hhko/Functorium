namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 이벤트 발행 실패
    /// </summary>
    public sealed record PublishFailed : AdapterErrorKind;

    /// <summary>
    /// 이벤트 핸들러 실행 실패
    /// </summary>
    public sealed record HandlerFailed : AdapterErrorKind;

    /// <summary>
    /// 이벤트 타입이 유효하지 않음
    /// </summary>
    public sealed record InvalidEventType : AdapterErrorKind;

    /// <summary>
    /// 이벤트 발행 취소됨
    /// </summary>
    public sealed record PublishCancelled : AdapterErrorKind;
}
