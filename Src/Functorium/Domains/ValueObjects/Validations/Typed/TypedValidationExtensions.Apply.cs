using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations.Typed;

/// <summary>
/// TypedValidation Tuple Apply 확장 메서드
/// 병렬 검증 패턴에서 .As() 없이 사용 가능
/// </summary>
public static partial class TypedValidationExtensions
{
    // =========================================================================
    // 2-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 2-tuple Apply: 모두 TypedValidation
    /// </summary>
    /// <example>
    /// (ValidationRules&lt;Money&gt;.NonNegative(amount),
    ///  ValidationRules&lt;Money&gt;.NotEmpty(currency))
    ///     .Apply((a, c) => new Money(a, c));
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV1, T1, TV2, T2, R>(
        this (TypedValidation<TV1, T1> v1, TypedValidation<TV2, T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1.Value, tuple.v2.Value).Apply(f).As();

    /// <summary>
    /// 2-tuple Apply: TypedValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV, T1, T2, R>(
        this (TypedValidation<TV, T1> v1, Validation<Error, T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1.Value, tuple.v2).Apply(f).As();

    /// <summary>
    /// 2-tuple Apply: Validation + TypedValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, TV, T2, R>(
        this (Validation<Error, T1> v1, TypedValidation<TV, T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1, tuple.v2.Value).Apply(f).As();

    // =========================================================================
    // 3-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 3-tuple Apply: 모두 TypedValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV1, T1, TV2, T2, TV3, T3, R>(
        this (TypedValidation<TV1, T1> v1, TypedValidation<TV2, T2> v2, TypedValidation<TV3, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2.Value, tuple.v3.Value).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: Validation + Validation + TypedValidation
    /// ExchangeRate.Validate 패턴 지원
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, TV, T3, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, TypedValidation<TV, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1, tuple.v2, tuple.v3.Value).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: TypedValidation + Validation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV, T1, T2, T3, R>(
        this (TypedValidation<TV, T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2, tuple.v3).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: Validation + TypedValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, TV, T2, T3, R>(
        this (Validation<Error, T1> v1, TypedValidation<TV, T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1, tuple.v2.Value, tuple.v3).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: TypedValidation + TypedValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV1, T1, TV2, T2, T3, R>(
        this (TypedValidation<TV1, T1> v1, TypedValidation<TV2, T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2.Value, tuple.v3).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: TypedValidation + Validation + TypedValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV1, T1, T2, TV2, T3, R>(
        this (TypedValidation<TV1, T1> v1, Validation<Error, T2> v2, TypedValidation<TV2, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2, tuple.v3.Value).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: Validation + TypedValidation + TypedValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, TV1, T2, TV2, T3, R>(
        this (Validation<Error, T1> v1, TypedValidation<TV1, T2> v2, TypedValidation<TV2, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1, tuple.v2.Value, tuple.v3.Value).Apply(f).As();

    // =========================================================================
    // 4-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 4-tuple Apply: 모두 TypedValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV1, T1, TV2, T2, TV3, T3, TV4, T4, R>(
        this (TypedValidation<TV1, T1> v1, TypedValidation<TV2, T2> v2, TypedValidation<TV3, T3> v3, TypedValidation<TV4, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1.Value, tuple.v2.Value, tuple.v3.Value, tuple.v4.Value).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: 모두 Validation 중 하나가 TypedValidation (첫 번째 위치)
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<TV, T1, T2, T3, T4, R>(
        this (TypedValidation<TV, T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1.Value, tuple.v2, tuple.v3, tuple.v4).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: 모두 Validation 중 하나가 TypedValidation (두 번째 위치)
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, TV, T2, T3, T4, R>(
        this (Validation<Error, T1> v1, TypedValidation<TV, T2> v2, Validation<Error, T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1, tuple.v2.Value, tuple.v3, tuple.v4).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: 모두 Validation 중 하나가 TypedValidation (세 번째 위치)
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, TV, T3, T4, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, TypedValidation<TV, T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1, tuple.v2, tuple.v3.Value, tuple.v4).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: 모두 Validation 중 하나가 TypedValidation (네 번째 위치)
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, TV, T4, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3, TypedValidation<TV, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1, tuple.v2, tuple.v3, tuple.v4.Value).Apply(f).As();
}
