using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

namespace Functorium.Applications.Errors;

/// <summary>
/// 도메인 이벤트의 오류 생성을 위한 헬퍼 클래스.
/// 에러 코드를 자동으로 "ApplicationErrors.{PublisherName}.{ErrorName}" 형식으로 생성.
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Applications.Errors.EventErrorType;
///
/// EventError.For&lt;DomainEventPublisher&gt;(new PublishFailed(), eventType, "Failed to publish event");
/// EventError.For&lt;ObservableDomainEventPublisher&gt;(new HandlerFailed(), eventType, "Event handler threw exception");
/// EventError.FromException&lt;DomainEventPublisher&gt;(exception);
/// </code>
/// </remarks>
public static class EventError
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TPublisher>(
        EventErrorType errorType,
        string currentValue,
        string message) =>
        LayerErrorCore.Create<TPublisher>(ErrorType.ApplicationErrorsPrefix, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TPublisher, TValue>(
        EventErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        LayerErrorCore.Create<TPublisher, TValue>(ErrorType.ApplicationErrorsPrefix, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error FromException<TPublisher>(Exception exception) =>
        LayerErrorCore.FromException<TPublisher>(ErrorType.ApplicationErrorsPrefix, new EventErrorType.PublishFailed(), exception);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error FromException<TPublisher>(
        EventErrorType errorType,
        Exception exception) =>
        LayerErrorCore.FromException<TPublisher>(ErrorType.ApplicationErrorsPrefix, errorType, exception);
}
