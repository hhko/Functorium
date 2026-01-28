using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class TypedValidationExtensions
{
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
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.MatchesInternal(v, pattern, message)));

    /// <summary>
    /// 문자열이 대문자인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenIsUpperCase<TValueObject>(
        this TypedValidation<TValueObject, string> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.IsUpperCaseInternal(v)));

    /// <summary>
    /// 문자열이 소문자인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenIsLowerCase<TValueObject>(
        this TypedValidation<TValueObject, string> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.IsLowerCaseInternal(v)));
}
