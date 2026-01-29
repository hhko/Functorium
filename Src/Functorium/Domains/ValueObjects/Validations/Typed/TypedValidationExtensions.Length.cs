using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Domains.ValueObjects.Validations.Typed;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 문자열이 비어있지 않은지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenNotEmpty<TValueObject>(
        this TypedValidation<TValueObject, string> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.NotEmptyInternal(v)));

    /// <summary>
    /// 문자열이 최소 길이를 충족하는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="minLength">최소 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenMinLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int minLength) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.MinLengthInternal(v, minLength)));

    /// <summary>
    /// 문자열이 최대 길이를 초과하지 않는지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="maxLength">최대 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenMaxLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int maxLength) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.MaxLengthInternal(v, maxLength)));

    /// <summary>
    /// 문자열이 정확한 길이인지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="length">요구되는 길이</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenExactLength<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        int length) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.ExactLengthInternal(v, length)));

    /// <summary>
    /// 문자열을 변환(정규화)합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <param name="normalize">변환 함수</param>
    /// <returns>변환된 문자열을 포함하는 검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, string> ThenNormalize<TValueObject>(
        this TypedValidation<TValueObject, string> validation,
        Func<string, string> normalize) =>
        new(validation.Value.Map(normalize));
}
