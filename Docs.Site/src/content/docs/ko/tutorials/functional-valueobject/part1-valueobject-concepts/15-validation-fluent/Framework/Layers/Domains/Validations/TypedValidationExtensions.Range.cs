using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Framework.Layers.Domains.Validations;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 범위가 유효한지 체인으로 검증합니다 (min &lt;= max).
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="TValue">비교 가능한 값 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과 (유효한 경우 (min, max) 튜플 반환)</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, (TValue Min, TValue Max)> ThenValidRange<TValueObject, TValue>(
        this TypedValidation<TValueObject, (TValue Min, TValue Max)> validation)
        where TValue : notnull, IComparable<TValue> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.ValidRangeInternal<TValue>(v.Min, v.Max)));

    /// <summary>
    /// 엄격한 범위가 유효한지 체인으로 검증합니다 (min &lt; max).
    /// 시작과 끝이 같은 경우 빈 범위로 간주하여 오류 처리합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="TValue">비교 가능한 값 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과 (유효한 경우 (min, max) 튜플 반환)</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, (TValue Min, TValue Max)> ThenValidStrictRange<TValueObject, TValue>(
        this TypedValidation<TValueObject, (TValue Min, TValue Max)> validation)
        where TValue : notnull, IComparable<TValue> =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.ValidStrictRangeInternal<TValue>(v.Min, v.Max)));
}
