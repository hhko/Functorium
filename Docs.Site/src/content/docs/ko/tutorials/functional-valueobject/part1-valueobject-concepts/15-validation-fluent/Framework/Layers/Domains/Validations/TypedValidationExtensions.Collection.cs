using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Framework.Layers.Domains.Validations;

public static partial class TypedValidationExtensions
{
    /// <summary>
    /// 배열이 비어있지 않은지 체인으로 검증합니다.
    /// </summary>
    /// <typeparam name="TValueObject">값 객체 타입</typeparam>
    /// <typeparam name="TElement">배열 요소 타입</typeparam>
    /// <param name="validation">이전 검증 결과</param>
    /// <returns>검증 결과</returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, TElement[]> ThenNotEmptyArray<TValueObject, TElement>(
        this TypedValidation<TValueObject, TElement[]> validation) =>
        new(validation.Value.Bind(v => ValidationRules<TValueObject>.NotEmptyArrayInternal<TElement>(v)));
}
