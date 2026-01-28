using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 사용자 정의 조건으로 값을 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="predicate">검증 조건</param>
    /// <param name="errorType">에러 타입</param>
    /// <param name="message">오류 메시지</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenMust<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        Func<T, bool> predicate,
        DomainErrorType errorType,
        string message)
        where T : notnull =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.MustInternal(v, predicate, errorType, message)));

    /// <summary>
    /// 사용자 정의 조건으로 값을 체인으로 검증합니다. (메시지 생성 함수 사용)
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="predicate">검증 조건</param>
    /// <param name="errorType">에러 타입</param>
    /// <param name="messageFactory">오류 메시지 생성 함수</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenMust<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        Func<T, bool> predicate,
        DomainErrorType errorType,
        Func<T, string> messageFactory)
        where T : notnull =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.MustInternal(v, predicate, errorType, messageFactory(v))));
}
