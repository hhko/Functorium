using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

namespace Functorium.Domains.ValueObjects;

public static partial class Validate<TValueObject>
{
    /// <summary>
    /// 범위가 유효한지 검증합니다 (min &lt;= max).
    /// </summary>
    /// <typeparam name="TValue">비교 가능한 값 타입</typeparam>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과 (유효한 경우 (min, max) 튜플 반환)</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, (TValue Min, TValue Max)> ValidRange<TValue>(TValue min, TValue max)
        where TValue : notnull, IComparable<TValue> =>
        new(ValidRangeInternal<TValue>(min, max));

    /// <summary>
    /// 엄격한 범위가 유효한지 검증합니다 (min &lt; max).
    /// 시작과 끝이 같은 경우 빈 범위로 간주하여 오류 처리합니다.
    /// </summary>
    /// <typeparam name="TValue">비교 가능한 값 타입</typeparam>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과 (유효한 경우 (min, max) 튜플 반환)</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, (TValue Min, TValue Max)> ValidStrictRange<TValue>(TValue min, TValue max)
        where TValue : notnull, IComparable<TValue> =>
        new(ValidStrictRangeInternal<TValue>(min, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, (TValue Min, TValue Max)> ValidRangeInternal<TValue>(TValue min, TValue max)
        where TValue : notnull, IComparable<TValue> =>
        min.CompareTo(max) <= 0
            ? (Min: min, Max: max)
            : DomainError.For<TValueObject>(
                new RangeInverted(min.ToString(), max.ToString()),
                $"Min: {min}, Max: {max}",
                $"{typeof(TValueObject).Name} range is invalid. Minimum ({min}) cannot exceed maximum ({max}).");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Validation<Error, (TValue Min, TValue Max)> ValidStrictRangeInternal<TValue>(TValue min, TValue max)
        where TValue : notnull, IComparable<TValue>
    {
        var comparison = min.CompareTo(max);
        return comparison < 0
            ? (Min: min, Max: max)
            : comparison == 0
                ? DomainError.For<TValueObject>(
                    new RangeEmpty(min.ToString()),
                    $"Value: {min}",
                    $"{typeof(TValueObject).Name} range is empty. Start ({min}) equals end ({max}).")
                : DomainError.For<TValueObject>(
                    new RangeInverted(min.ToString(), max.ToString()),
                    $"Min: {min}, Max: {max}",
                    $"{typeof(TValueObject).Name} range is invalid. Minimum ({min}) cannot exceed maximum ({max}).");
    }
}
