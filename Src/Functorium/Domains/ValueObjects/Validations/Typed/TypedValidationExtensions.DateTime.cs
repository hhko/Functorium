using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Domains.ValueObjects.Validations.Typed;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 날짜가 기본값(DateTime.MinValue)이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenNotDefault<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.NotDefaultInternal(v)));

    /// <summary>
    /// 날짜가 과거인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenInPast<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.InPastInternal(v)));

    /// <summary>
    /// 날짜가 미래인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenInFuture<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.InFutureInternal(v)));

    /// <summary>
    /// 날짜가 특정 기준 날짜 이전인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenBefore<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation,
        DateTime boundary) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.BeforeInternal(v, boundary)));

    /// <summary>
    /// 날짜가 특정 기준 날짜 이후인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="boundary">기준 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenAfter<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation,
        DateTime boundary) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.AfterInternal(v, boundary)));

    /// <summary>
    /// 날짜가 지정된 범위 내에 있는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="min">최소 날짜</param>
    /// <param name="max">최대 날짜</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, DateTime> ThenDateBetween<TValueObject>(
        this TypedValidation<TValueObject, DateTime> validation,
        DateTime min,
        DateTime max) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.DateBetweenInternal(v, min, max)));
}
