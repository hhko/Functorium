using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations;

/// <summary>
/// TypedValidation LINQ 지원 확장 메서드
/// LINQ query expression (from...in...select) 사용 가능
/// </summary>
public static partial class TypedValidationExtensions
{
    /// <summary>
    /// SelectMany: TypedValidation → Validation 체이닝
    /// LINQ from...in 구문 지원
    /// </summary>
    /// <example>
    /// from validStartDate in ValidationRules&lt;DateRange&gt;.NotDefault(startDate)
    /// from validEndDate in ValidationRules&lt;DateRange&gt;.NotDefault(endDate)
    /// select (validStartDate, validEndDate);
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, C> SelectMany<TValueObject, A, B, C>(
        this TypedValidation<TValueObject, A> source,
        Func<A, Validation<Error, B>> selector,
        Func<A, B, C> projector) =>
        source.Value.Bind(a => selector(a).Map(b => projector(a, b)));

    /// <summary>
    /// SelectMany: TypedValidation → TypedValidation 체이닝
    /// 동일 ValueObject 타입 내에서 체이닝
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, C> SelectMany<TValueObject, A, B, C>(
        this TypedValidation<TValueObject, A> source,
        Func<A, TypedValidation<TValueObject, B>> selector,
        Func<A, B, C> projector) =>
        new(source.Value.Bind(a => selector(a).Value.Map(b => projector(a, b))));

    /// <summary>
    /// Select: 값 변환 (Map)
    /// LINQ select 구문 지원
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypedValidation<TValueObject, B> Select<TValueObject, A, B>(
        this TypedValidation<TValueObject, A> source,
        Func<A, B> selector) =>
        new(source.Value.Map(selector));

    /// <summary>
    /// Validation으로 명시적 변환
    /// 캐스팅보다 가독성 좋은 대안
    /// </summary>
    /// <example>
    /// ValidationRules&lt;PriceRange&gt;.ValidRange(min, max)
    ///     .ToValidation()
    ///     .Map(_ => (MinPrice: minPrice, MaxPrice: maxPrice));
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, T> ToValidation<TValueObject, T>(
        this TypedValidation<TValueObject, T> typed) =>
        typed.Value;
}
