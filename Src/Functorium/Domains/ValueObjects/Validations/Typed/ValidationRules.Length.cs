using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorKind;

namespace Functorium.Domains.ValueObjects.Validations.Typed;

public static partial class ValidationRules<TValueObject>
{
    /// <summary>
    /// 문자열이 비어있지 않은지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> NotEmpty(string value) =>
        new(NotEmptyInternal(value));

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> MinLength(string value, int minLength) =>
        new(MinLengthInternal(value, minLength));

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> MaxLength(string value, int maxLength) =>
        new(MaxLengthInternal(value, maxLength));

    /// <summary>
    /// 문자열이 정확한 길이인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ExactLength(string value, int length) =>
        new(ExactLengthInternal(value, length));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> NotEmptyInternal(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<TValueObject>(
                new Empty(),
                value,
                $"{typeof(TValueObject).Name} cannot be empty. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> MinLengthInternal(string value, int minLength) =>
        value.Length >= minLength
            ? value
            : DomainError.For<TValueObject>(
                new TooShort(minLength),
                value,
                $"{typeof(TValueObject).Name} must be at least {minLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> MaxLengthInternal(string value, int maxLength) =>
        value.Length <= maxLength
            ? value
            : DomainError.For<TValueObject>(
                new TooLong(maxLength),
                value,
                $"{typeof(TValueObject).Name} must not exceed {maxLength} characters. Current length: {value.Length}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, string> ExactLengthInternal(string value, int length) =>
        value.Length == length
            ? value
            : DomainError.For<TValueObject>(
                new WrongLength(length),
                value,
                $"{typeof(TValueObject).Name} must be exactly {length} characters. Current length: {value.Length}");
}
