using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

public readonly partial struct ValidationContext
{
    /// <summary>
    /// 숫자가 0이 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> NotZero<T>(T value)
        where T : notnull, INumber<T> =>
        new(NotZeroInternal<T>(value), ContextName);

    /// <summary>
    /// 숫자가 음수가 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> NonNegative<T>(T value)
        where T : notnull, INumber<T> =>
        new(NonNegativeInternal<T>(value), ContextName);

    /// <summary>
    /// 숫자가 양수인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> Positive<T>(T value)
        where T : notnull, INumber<T> =>
        new(PositiveInternal<T>(value), ContextName);

    /// <summary>
    /// 숫자가 지정된 범위 내에 있는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> Between<T>(T value, T min, T max)
        where T : notnull, INumber<T> =>
        new(BetweenInternal<T>(value, min, max), ContextName);

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> AtMost<T>(T value, T max)
        where T : notnull, INumber<T> =>
        new(AtMostInternal<T>(value, max), ContextName);

    /// <summary>
    /// 숫자가 최소값 이상인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContextualValidation<T> AtLeast<T>(T value, T min)
        where T : notnull, INumber<T> =>
        new(AtLeastInternal<T>(value, min), ContextName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> NotZeroInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value != T.Zero
            ? value
            : DomainError.ForContext<T>(
                ContextName,
                new Zero(),
                value,
                $"{ContextName} cannot be zero. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> NonNegativeInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value >= T.Zero
            ? value
            : DomainError.ForContext<T>(
                ContextName,
                new Negative(),
                value,
                $"{ContextName} cannot be negative. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> PositiveInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value > T.Zero
            ? value
            : DomainError.ForContext<T>(
                ContextName,
                new NotPositive(),
                value,
                $"{ContextName} must be positive. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> BetweenInternal<T>(T value, T min, T max)
        where T : notnull, INumber<T> =>
        value >= min && value <= max
            ? value
            : DomainError.ForContext<T>(
                ContextName,
                new OutOfRange(min.ToString(), max.ToString()),
                value,
                $"{ContextName} must be between {min} and {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> AtMostInternal<T>(T value, T max)
        where T : notnull, INumber<T> =>
        value <= max
            ? value
            : DomainError.ForContext<T>(
                ContextName,
                new AboveMaximum(max.ToString()),
                value,
                $"{ContextName} cannot exceed {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Validation<Error, T> AtLeastInternal<T>(T value, T min)
        where T : notnull, INumber<T> =>
        value >= min
            ? value
            : DomainError.ForContext<T>(
                ContextName,
                new BelowMinimum(min.ToString()),
                value,
                $"{ContextName} must be at least {min}. Current value: '{value}'");
}
