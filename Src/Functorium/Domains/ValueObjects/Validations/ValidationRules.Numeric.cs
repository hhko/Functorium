using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class ValidationRules<TValueObject>
{
    /// <summary>
    /// 숫자가 0이 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> NotZero<T>(T value)
        where T : notnull, INumber<T> =>
        new(NotZeroInternal<T>(value));

    /// <summary>
    /// 숫자가 음수가 아닌지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> NonNegative<T>(T value)
        where T : notnull, INumber<T> =>
        new(NonNegativeInternal<T>(value));

    /// <summary>
    /// 숫자가 양수인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> Positive<T>(T value)
        where T : notnull, INumber<T> =>
        new(PositiveInternal<T>(value));

    /// <summary>
    /// 숫자가 지정된 범위 내에 있는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> Between<T>(T value, T min, T max)
        where T : notnull, INumber<T> =>
        new(BetweenInternal<T>(value, min, max));

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> AtMost<T>(T value, T max)
        where T : notnull, INumber<T> =>
        new(AtMostInternal<T>(value, max));

    /// <summary>
    /// 숫자가 최소값 이상인지 검증합니다.
    /// </summary>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="value">검증할 값</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> AtLeast<T>(T value, T min)
        where T : notnull, INumber<T> =>
        new(AtLeastInternal<T>(value, min));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> NotZeroInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value != T.Zero
            ? value
            : DomainError.For<TValueObject, T>(
                new Zero(),
                value,
                $"{typeof(TValueObject).Name} cannot be zero. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> NonNegativeInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value >= T.Zero
            ? value
            : DomainError.For<TValueObject, T>(
                new Negative(),
                value,
                $"{typeof(TValueObject).Name} cannot be negative. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> PositiveInternal<T>(T value)
        where T : notnull, INumber<T> =>
        value > T.Zero
            ? value
            : DomainError.For<TValueObject, T>(
                new NotPositive(),
                value,
                $"{typeof(TValueObject).Name} must be positive. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> BetweenInternal<T>(T value, T min, T max)
        where T : notnull, INumber<T> =>
        value >= min && value <= max
            ? value
            : DomainError.For<TValueObject, T>(
                new OutOfRange(min.ToString(), max.ToString()),
                value,
                $"{typeof(TValueObject).Name} must be between {min} and {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> AtMostInternal<T>(T value, T max)
        where T : notnull, INumber<T> =>
        value <= max
            ? value
            : DomainError.For<TValueObject, T>(
                new AboveMaximum(max.ToString()),
                value,
                $"{typeof(TValueObject).Name} cannot exceed {max}. Current value: '{value}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, T> AtLeastInternal<T>(T value, T min)
        where T : notnull, INumber<T> =>
        value >= min
            ? value
            : DomainError.For<TValueObject, T>(
                new BelowMinimum(min.ToString()),
                value,
                $"{typeof(TValueObject).Name} must be at least {min}. Current value: '{value}'");
}
