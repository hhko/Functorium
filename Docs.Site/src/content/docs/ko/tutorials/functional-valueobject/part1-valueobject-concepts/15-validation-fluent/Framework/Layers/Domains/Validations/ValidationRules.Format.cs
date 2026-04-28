using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using static Framework.Layers.Domains.DomainErrorKind;

namespace Framework.Layers.Domains.Validations;

public static partial class ValidationRules<TValueObject>
{
    /// <summary>
    /// 문자열이 정규식 패턴과 일치하는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="pattern">정규식 패턴</param>
    /// <param name="message">오류 메시지 (선택적)</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> Matches(
        string value,
        Regex pattern,
        string? message = null) =>
        new(MatchesInternal(value, pattern, message));

    /// <summary>
    /// 문자열이 대문자인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> IsUpperCase(string value) =>
        new(IsUpperCaseInternal(value));

    /// <summary>
    /// 문자열이 소문자인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> IsLowerCase(string value) =>
        new(IsLowerCaseInternal(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> MatchesInternal(
        string value,
        Regex pattern,
        string? message = null) =>
        pattern.IsMatch(value)
            ? value
            : DomainError.For<TValueObject>(
                new InvalidFormat(pattern.ToString()),
                value,
                message ?? $"Invalid {typeof(TValueObject).Name} format. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> IsUpperCaseInternal(string value) =>
        value == value.ToUpperInvariant()
            ? value
            : DomainError.For<TValueObject>(
                new NotUpperCase(),
                value,
                $"{typeof(TValueObject).Name} must be uppercase. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> IsLowerCaseInternal(string value) =>
        value == value.ToLowerInvariant()
            ? value
            : DomainError.For<TValueObject>(
                new NotLowerCase(),
                value,
                $"{typeof(TValueObject).Name} must be lowercase. Current value: '{value}'");
}
