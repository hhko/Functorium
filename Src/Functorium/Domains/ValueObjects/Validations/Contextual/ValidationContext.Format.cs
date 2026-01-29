using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public readonly partial struct ValidationContext
{
    /// <summary>
    /// 문자열이 정규식 패턴과 일치하는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="pattern">정규식 패턴</param>
    /// <param name="message">오류 메시지 (선택적)</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> Matches(
        string value,
        Regex pattern,
        string? message = null) =>
        new(MatchesInternal(value, pattern, message), ContextName);

    /// <summary>
    /// 문자열이 대문자인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> IsUpperCase(string value) =>
        new(IsUpperCaseInternal(value), ContextName);

    /// <summary>
    /// 문자열이 소문자인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> IsLowerCase(string value) =>
        new(IsLowerCaseInternal(value), ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> MatchesInternal(
        string value,
        Regex pattern,
        string? message = null) =>
        pattern.IsMatch(value)
            ? value
            : DomainError.ForContext(
                ContextName,
                new InvalidFormat(pattern.ToString()),
                value,
                message ?? $"Invalid {ContextName} format. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> IsUpperCaseInternal(string value) =>
        value == value.ToUpperInvariant()
            ? value
            : DomainError.ForContext(
                ContextName,
                new NotUpperCase(),
                value,
                $"{ContextName} must be uppercase. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> IsLowerCaseInternal(string value) =>
        value == value.ToLowerInvariant()
            ? value
            : DomainError.ForContext(
                ContextName,
                new NotLowerCase(),
                value,
                $"{ContextName} must be lowercase. Current value: '{value}'");
}
