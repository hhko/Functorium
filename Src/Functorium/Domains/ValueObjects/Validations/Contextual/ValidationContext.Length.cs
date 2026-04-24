using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorKind;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public readonly partial struct ValidationContext
{
    /// <summary>
    /// 문자열이 비어있지 않은지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> NotEmpty(string value) =>
        new(NotEmptyInternal(value), ContextName);

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> MinLength(string value, int minLength) =>
        new(MinLengthInternal(value, minLength), ContextName);

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> MaxLength(string value, int maxLength) =>
        new(MaxLengthInternal(value, maxLength), ContextName);

    /// <summary>
    /// 문자열이 정확한 길이인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<string> ExactLength(string value, int length) =>
        new(ExactLengthInternal(value, length), ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> NotEmptyInternal(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.ForContext(
                ContextName,
                new Empty(),
                value,
                $"{ContextName} cannot be empty. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> MinLengthInternal(string value, int minLength) =>
        value.Length >= minLength
            ? value
            : DomainError.ForContext(
                ContextName,
                new TooShort(minLength),
                value,
                $"{ContextName} must be at least {minLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> MaxLengthInternal(string value, int maxLength) =>
        value.Length <= maxLength
            ? value
            : DomainError.ForContext(
                ContextName,
                new TooLong(maxLength),
                value,
                $"{ContextName} must not exceed {maxLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, string> ExactLengthInternal(string value, int length) =>
        value.Length == length
            ? value
            : DomainError.ForContext(
                ContextName,
                new WrongLength(length),
                value,
                $"{ContextName} must be exactly {length} characters. Current length: {value.Length}");
}
