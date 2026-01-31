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
    /// <summary>
    /// EventErrorType record를 사용하여 에러를 생성합니다.
    /// </summary>
    /// <typeparam name="TPublisher">발행자 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TPublisher>(
        EventErrorType errorType,
        string currentValue,
        string message) =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TPublisher).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// EventErrorType record를 사용하여 에러를 생성합니다. (제네릭 값 타입)
    /// </summary>
    /// <typeparam name="TPublisher">발행자 타입</typeparam>
    /// <typeparam name="TValue">현재 값의 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="currentValue">현재 값</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TPublisher, TValue>(
        EventErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TPublisher).Name}.{errorType.ErrorName}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// 예외로부터 Exceptional 에러를 생성합니다.
    /// </summary>
    /// <typeparam name="TPublisher">발행자 타입</typeparam>
    /// <param name="exception">발생한 예외</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error FromException<TPublisher>(Exception exception) =>
        ErrorCodeFactory.CreateFromException(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TPublisher).Name}.{new EventErrorType.PublishFailed().ErrorName}",
            exception: exception);

    /// <summary>
    /// 예외로부터 특정 에러 타입의 Exceptional 에러를 생성합니다.
    /// </summary>
    /// <typeparam name="TPublisher">발행자 타입</typeparam>
    /// <param name="errorType">에러 타입 record</param>
    /// <param name="exception">발생한 예외</param>
    /// <returns>생성된 Error</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error FromException<TPublisher>(
        EventErrorType errorType,
        Exception exception) =>
        ErrorCodeFactory.CreateFromException(
            errorCode: $"{ErrorType.ApplicationErrorsPrefix}.{typeof(TPublisher).Name}.{errorType.ErrorName}",
            exception: exception);
}
