using Functorium.Abstractions.Errors;

namespace Functorium.Applications.Errors;

/// <summary>
/// 도메인 이벤트 레이어 에러 타입.
/// sealed record 계층으로 타입 안전한 에러 정의 제공.
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Applications.Errors.EventErrorType;
///
/// EventError.For&lt;DomainEventPublisher&gt;(new PublishFailed(), eventType, "Failed to publish event");
/// EventError.For&lt;ObservableDomainEventPublisher&gt;(new HandlerFailed(), eventType, "Event handler failed");
/// </code>
/// </remarks>
public abstract record EventErrorType : ErrorType
{
    /// <summary>
    /// 이벤트 발행 실패.
    /// </summary>
    public sealed record PublishFailed : EventErrorType;

    /// <summary>
    /// 이벤트 핸들러 실행 실패.
    /// </summary>
    public sealed record HandlerFailed : EventErrorType;

    /// <summary>
    /// 이벤트 타입이 유효하지 않음.
    /// </summary>
    public sealed record InvalidEventType : EventErrorType;

    /// <summary>
    /// 이벤트 발행 취소됨.
    /// </summary>
    public sealed record PublishCancelled : EventErrorType;

    /// <summary>
    /// 이벤트 특화 커스텀 에러 (표준 에러에 해당하지 않는 경우).
    /// </summary>
    /// <param name="Name">커스텀 에러 이름</param>
    public sealed record Custom(string Name) : EventErrorType
    {
        public override string ErrorName => Name;
    }
}
