using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects;

/// <summary>
/// Validation 체이닝을 위한 확장 메서드
/// </summary>
public static class ValidationRulesExtensions
{
    /// <summary>
    /// 문자열이 비어있지 않은지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenNotEmpty<TValueObject>(
        this Validation<Error, string> validation) =>
        validation.Bind(v => ValidationRules.NotEmpty<TValueObject>(v));

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenMinLength<TValueObject>(
        this Validation<Error, string> validation,
        int minLength) =>
        validation.Bind(v => ValidationRules.MinLength<TValueObject>(v, minLength));

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenMaxLength<TValueObject>(
        this Validation<Error, string> validation,
        int maxLength) =>
        validation.Bind(v => ValidationRules.MaxLength<TValueObject>(v, maxLength));

    /// <summary>
    /// 문자열이 정확한 길이인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenExactLength<TValueObject>(
        this Validation<Error, string> validation,
        int length) =>
        validation.Bind(v => ValidationRules.ExactLength<TValueObject>(v, length));

    /// <summary>
    /// 문자열이 정규식 패턴과 일치하는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="pattern">정규식 패턴</param>
    /// <param name="message">오류 메시지 (선택적)</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenMatches<TValueObject>(
        this Validation<Error, string> validation,
        Regex pattern,
        string? message = null) =>
        validation.Bind(v => ValidationRules.Matches<TValueObject>(v, pattern, message));

    /// <summary>
    /// 문자열을 변환(정규화)합니다.
    /// </summary>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="normalize">변환 함수</param>
    /// <returns>변환된 문자열을 포함하는 검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenNormalize(
        this Validation<Error, string> validation,
        Func<string, string> normalize) =>
        validation.Map(normalize);

    /// <summary>
    /// 숫자가 음수가 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> ThenNonNegative<TValueObject, T>(
        this Validation<Error, T> validation)
        where T : notnull, INumber<T> =>
        validation.Bind(v => ValidationRules.NonNegative<TValueObject, T>(v));

    /// <summary>
    /// 숫자가 양수인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> ThenPositive<TValueObject, T>(
        this Validation<Error, T> validation)
        where T : notnull, INumber<T> =>
        validation.Bind(v => ValidationRules.Positive<TValueObject, T>(v));

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
    public static Validation<Error, T> ThenBetween<TValueObject, T>(
        this Validation<Error, T> validation,
        T min,
        T max)
        where T : notnull, INumber<T> =>
        validation.Bind(v => ValidationRules.Between<TValueObject, T>(v, min, max));

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> ThenAtMost<TValueObject, T>(
        this Validation<Error, T> validation,
        T max)
        where T : notnull, INumber<T> =>
        validation.Bind(v => ValidationRules.AtMost<TValueObject, T>(v, max));

    /// <summary>
    /// 숫자가 최소값 이상인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> ThenAtLeast<TValueObject, T>(
        this Validation<Error, T> validation,
        T min)
        where T : notnull, INumber<T> =>
        validation.Bind(v => ValidationRules.AtLeast<TValueObject, T>(v, min));

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
    public static Validation<Error, T> ThenMust<TValueObject, T>(
        this Validation<Error, T> validation,
        Func<T, bool> predicate,
        DomainErrorType errorType,
        string message)
        where T : notnull =>
        validation.Bind(v => ValidationRules.Must<TValueObject, T>(v, predicate, errorType, message));

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
    public static Validation<Error, T> ThenMust<TValueObject, T>(
        this Validation<Error, T> validation,
        Func<T, bool> predicate,
        DomainErrorType errorType,
        Func<T, string> messageFactory)
        where T : notnull =>
        validation.Bind(v => ValidationRules.Must<TValueObject, T>(v, predicate, errorType, messageFactory(v)));
}
