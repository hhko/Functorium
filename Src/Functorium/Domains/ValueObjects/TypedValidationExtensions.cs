using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects;

/// <summary>
/// TypedValidation 체이닝을 위한 확장 메서드
/// </summary>
public static class TypedValidationExtensions
{
    #region String Extensions

    /// <summary>
    /// 문자열이 비어있지 않은지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenNotEmpty<TValueObject>(
        this TypedValidation<TValueObject, string> validation) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.NotEmptyInternal(v)));

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenMinLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int minLength) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.MinLengthInternal(v, minLength)));

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenMaxLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int maxLength) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.MaxLengthInternal(v, maxLength)));

    /// <summary>
    /// 문자열이 정확한 길이인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenExactLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int length) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.ExactLengthInternal(v, length)));

    /// <summary>
    /// 문자열이 정규식 패턴과 일치하는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="pattern">정규식 패턴</param>
    /// <param name="message">오류 메시지 (선택적)</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenMatches<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        Regex pattern,
        string? message = null) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.MatchesInternal(v, pattern, message)));

    /// <summary>
    /// 문자열을 변환(정규화)합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="normalize">변환 함수</param>
    /// <returns>변환된 문자열을 포함하는 검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenNormalize<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        Func<string, string> normalize) =>
        new(validation.Value.Map(normalize));

    #endregion

    #region Numeric Extensions

    /// <summary>
    /// 숫자가 0이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenNotZero<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => Validate<TValueObject>.NotZeroInternal<T>(v)));

    /// <summary>
    /// 숫자가 음수가 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenNonNegative<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => Validate<TValueObject>.NonNegativeInternal<T>(v)));

    /// <summary>
    /// 숫자가 양수인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenPositive<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => Validate<TValueObject>.PositiveInternal<T>(v)));

    /// <summary>
    /// 숫자가 지정된 범위 내에 있는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenBetween<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        T min,
        T max)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => Validate<TValueObject>.BetweenInternal<T>(v, min, max)));

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenAtMost<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        T max)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => Validate<TValueObject>.AtMostInternal<T>(v, max)));

    /// <summary>
    /// 숫자가 최소값 이상인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenAtLeast<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        T min)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => Validate<TValueObject>.AtLeastInternal<T>(v, min)));

    #endregion

    #region DateTime Extensions

    /// <summary>
    /// 날짜가 기본값(DateTime.MinValue)이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenNotDefault<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.NotDefaultInternal(v)));

    /// <summary>
    /// 날짜가 과거인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenInPast<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.InPastInternal(v)));

    /// <summary>
    /// 날짜가 미래인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenInFuture<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.InFutureInternal(v)));

    /// <summary>
    /// 날짜가 특정 기준 날짜 이전인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenBefore<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation,
        DateTime boundary) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.BeforeInternal(v, boundary)));

    /// <summary>
    /// 날짜가 특정 기준 날짜 이후인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenAfter<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation,
        DateTime boundary) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.AfterInternal(v, boundary)));

    /// <summary>
    /// 날짜가 지정된 범위 내에 있는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소 날짜</param>
    /// <param name="max">최대 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenDateBetween<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation,
        DateTime min,
        DateTime max) =>
        new(validation.Value.Bind(v => Validate<TValueObject>.DateBetweenInternal(v, min, max)));

    #endregion

    #region Generic Extensions

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
        new(validation.Value.Bind(v => Validate<TValueObject>.MustInternal(v, predicate, errorType, message)));

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
        new(validation.Value.Bind(v => Validate<TValueObject>.MustInternal(v, predicate, errorType, messageFactory(v))));

    #endregion
}
