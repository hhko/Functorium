using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public static partial class ContextualValidationExtensions
{
    /// <summary>
    /// 숫자가 0이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenNotZero<T>(
        this ContextualValidation<T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => NotZeroInternal(v, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 숫자가 음수가 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenNonNegative<T>(
        this ContextualValidation<T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => NonNegativeInternal(v, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 숫자가 양수인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenPositive<T>(
        this ContextualValidation<T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => PositiveInternal(v, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 숫자가 지정된 범위 내에 있는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenBetween<T>(
        this ContextualValidation<T> validation,
        T min,
        T max)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => BetweenInternal(v, min, max, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenAtMost<T>(
        this ContextualValidation<T> validation,
        T max)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => AtMostInternal(v, max, validation.ContextName)), validation.ContextName);

    /// <summary>
    /// 숫자가 최소값 이상인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ContextualValidation<T> ThenAtLeast<T>(
        this ContextualValidation<T> validation,
        T min)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => AtLeastInternal(v, min, validation.ContextName)), validation.ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> NotZeroInternal<T>(T value, string contextName)
        where T : notnull, INumber<T> =>
        value != T.Zero
            ? value
            : DomainError.ForContext<T>(
                contextName,
                new Zero(),
                value,
                $"{contextName} cannot be zero. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> NonNegativeInternal<T>(T value, string contextName)
        where T : notnull, INumber<T> =>
        value >= T.Zero
            ? value
            : DomainError.ForContext<T>(
                contextName,
                new Negative(),
                value,
                $"{contextName} cannot be negative. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> PositiveInternal<T>(T value, string contextName)
        where T : notnull, INumber<T> =>
        value > T.Zero
            ? value
            : DomainError.ForContext<T>(
                contextName,
                new NotPositive(),
                value,
                $"{contextName} must be positive. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> BetweenInternal<T>(T value, T min, T max, string contextName)
        where T : notnull, INumber<T> =>
        value >= min && value <= max
            ? value
            : DomainError.ForContext<T>(
                contextName,
                new OutOfRange(min.ToString(), max.ToString()),
                value,
                $"{contextName} must be between {min} and {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> AtMostInternal<T>(T value, T max, string contextName)
        where T : notnull, INumber<T> =>
        value <= max
            ? value
            : DomainError.ForContext<T>(
                contextName,
                new AboveMaximum(max.ToString()),
                value,
                $"{contextName} cannot exceed {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Validation<Error, T> AtLeastInternal<T>(T value, T min, string contextName)
        where T : notnull, INumber<T> =>
        value >= min
            ? value
            : DomainError.ForContext<T>(
                contextName,
                new BelowMinimum(min.ToString()),
                value,
                $"{contextName} must be at least {min}. Current value: '{value}'");
}
