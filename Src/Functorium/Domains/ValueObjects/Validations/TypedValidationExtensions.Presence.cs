using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Domains.ValueObjects.Validations;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 값이 null이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">값의 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenNotNull<TValueObject, T>(
        this TypedValidation<TValueObject, T?> validation)
        where T : class =>
        new(validation.Value.Bind(v => Validate<TValueObject>.NotNullInternal<T>(v)));

    /// <summary>
    /// nullable 값 타입이 null이 아닌지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="T">값의 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, T> ThenNotNull<TValueObject, T>(
        this TypedValidation<TValueObject, T?> validation)
        where T : struct =>
        new(validation.Value.Bind(v => Validate<TValueObject>.NotNullStructInternal<T>(v)));
}
