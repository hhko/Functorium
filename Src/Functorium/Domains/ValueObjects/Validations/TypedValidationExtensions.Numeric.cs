using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 숫자가 0이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenNotZero<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.NotZeroInternal<T>(v)));

    /// <summary>
    /// 숫자가 음수가 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenNonNegative<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.NonNegativeInternal<T>(v)));

    /// <summary>
    /// 숫자가 양수인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenPositive<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.PositiveInternal<T>(v)));

    /// <summary>
    /// 숫자가 지정된 범위 내에 있는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenBetween<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        T min,
        T max)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.BetweenInternal<T>(v, min, max)));

    /// <summary>
    /// 숫자가 최대값을 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="max">최대값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenAtMost<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        T max)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.AtMostInternal<T>(v, max)));

    /// <summary>
    /// 숫자가 최소값 이상인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">숫자 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소값</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenAtLeast<TValueObject, T>(
        this TypedValidation<TValueObject, T> validation,
        T min)
        where T : notnull, INumber<T> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.AtLeastInternal<T>(v, min)));
}
