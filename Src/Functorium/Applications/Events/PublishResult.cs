using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Applications.Events;

/// <summary>
/// 도메인 이벤트 발행 결과.
/// 다중 이벤트 발행 시 부분 성공/실패를 추적합니다.
/// </summary>
/// <param name="SuccessfulEvents">성공적으로 발행된 이벤트 목록</param>
/// <param name="FailedEvents">발행 실패한 이벤트와 에러 목록</param>
public sealed record PublishResult(
    Seq<IDomainEvent> SuccessfulEvents,
    Seq<(IDomainEvent Event, Error Error)> FailedEvents)
{
    /// <summary>
    /// 모든 이벤트가 성공적으로 발행되었는지 확인합니다.
    /// </summary>
    public bool IsAllSuccessful => FailedEvents.IsEmpty;

    /// <summary>
    /// 발행 실패한 이벤트가 있는지 확인합니다.
    /// </summary>
    public bool HasFailures => !FailedEvents.IsEmpty;

    /// <summary>
    /// 발행된 총 이벤트 수.
    /// </summary>
    public int TotalCount => SuccessfulEvents.Count + FailedEvents.Count;

    /// <summary>
    /// 성공한 이벤트 수.
    /// </summary>
    public int SuccessCount => SuccessfulEvents.Count;

    /// <summary>
    /// 실패한 이벤트 수.
    /// </summary>
    public int FailureCount => FailedEvents.Count;

    /// <summary>
    /// 빈 결과를 생성합니다.
    /// </summary>
    public static PublishResult Empty => new(LanguageExt.Seq<IDomainEvent>.Empty, LanguageExt.Seq<(IDomainEvent, Error)>.Empty);

    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <param name="events">성공적으로 발행된 이벤트</param>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PublishResult Success(Seq<IDomainEvent> events) =>
        new(events, LanguageExt.Seq<(IDomainEvent, Error)>.Empty);

    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="failures">실패한 이벤트와 에러</param>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PublishResult Failure(Seq<(IDomainEvent Event, Error Error)> failures) =>
        new(LanguageExt.Seq<IDomainEvent>.Empty, failures);
}
