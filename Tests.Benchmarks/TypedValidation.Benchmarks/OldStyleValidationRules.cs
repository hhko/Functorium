using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace TypedValidation.Benchmarks;

/// <summary>
/// Old style: ValidationRules static methods (requires type parameter on every call)
/// </summary>
public static class ValidationRules
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> NotEmpty<TValueObject>(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<TValueObject>(
                new Empty(),
                value,
                $"{typeof(TValueObject).Name} cannot be empty. Current value: '{value}'");

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> MaxLength<TValueObject>(string value, int maxLength) =>
        value.Length <= maxLength
            ? value
            : DomainError.For<TValueObject>(
                new TooLong(maxLength),
                value,
                $"{typeof(TValueObject).Name} must not exceed {maxLength} characters. Current length: {value.Length}");

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> MinLength<TValueObject>(string value, int minLength) =>
        value.Length >= minLength
            ? value
            : DomainError.For<TValueObject>(
                new TooShort(minLength),
                value,
                $"{typeof(TValueObject).Name} must be at least {minLength} characters. Current length: {value.Length}");

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> Matches<TValueObject>(
        string value,
        Regex pattern,
        string? message = null) =>
        pattern.IsMatch(value)
            ? value
            : DomainError.For<TValueObject>(
                new InvalidFormat(pattern.ToString()),
                value,
                message ?? $"Invalid {typeof(TValueObject).Name} format. Current value: '{value}'");

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> Positive<TValueObject, T>(T value)
        where T : notnull, INumber<T> =>
        value > T.Zero
            ? value
            : DomainError.For<TValueObject, T>(
                new NotPositive(),
                value,
                $"{typeof(TValueObject).Name} must be positive. Current value: '{value}'");

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> AtMost<TValueObject, T>(T value, T max)
        where T : notnull, INumber<T> =>
        value <= max
            ? value
            : DomainError.For<TValueObject, T>(
                new AboveMaximum(max.ToString()),
                value,
                $"{typeof(TValueObject).Name} cannot exceed {max}. Current value: '{value}'");
}

/// <summary>
/// Old style: Extension methods (requires type parameter on every call)
/// </summary>
public static class ValidationRulesExtensions
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenMaxLength<TValueObject>(
        this Validation<Error, string> validation, int maxLength) =>
        validation.Bind(v => ValidationRules.MaxLength<TValueObject>(v, maxLength));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenMinLength<TValueObject>(
        this Validation<Error, string> validation, int minLength) =>
        validation.Bind(v => ValidationRules.MinLength<TValueObject>(v, minLength));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenMatches<TValueObject>(
        this Validation<Error, string> validation,
        Regex pattern,
        string? message = null) =>
        validation.Bind(v => ValidationRules.Matches<TValueObject>(v, pattern, message));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> ThenAtMost<TValueObject, T>(
        this Validation<Error, T> validation, T max)
        where T : notnull, INumber<T> =>
        validation.Bind(v => ValidationRules.AtMost<TValueObject, T>(v, max));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, string> ThenNormalize(
        this Validation<Error, string> validation, Func<string, string> normalize) =>
        validation.Map(normalize);
}
