using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

/// <summary>
/// ContextualValidation Apply 확장 메서드
/// 병렬 검증 패턴에서 여러 검증을 결합
/// </summary>
public static partial class ContextualValidationExtensions
{
    // =========================================================================
    // 2-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 2-tuple Apply: 모두 ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, R>(
        this (ContextualValidation<T1> v1, ContextualValidation<T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1.Value, tuple.v2.Value).Apply(f).As();

    /// <summary>
    /// 2-tuple Apply: ContextualValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, R>(
        this (ContextualValidation<T1> v1, Validation<Error, T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1.Value, tuple.v2).Apply(f).As();

    /// <summary>
    /// 2-tuple Apply: Validation + ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, R>(
        this (Validation<Error, T1> v1, ContextualValidation<T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1, tuple.v2.Value).Apply(f).As();

    // =========================================================================
    // 3-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 3-tuple Apply: 모두 ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (ContextualValidation<T1> v1, ContextualValidation<T2> v2, ContextualValidation<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2.Value, tuple.v3.Value).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: Validation + Validation + ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, ContextualValidation<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1, tuple.v2, tuple.v3.Value).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: ContextualValidation + Validation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (ContextualValidation<T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2, tuple.v3).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: Validation + ContextualValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (Validation<Error, T1> v1, ContextualValidation<T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1, tuple.v2.Value, tuple.v3).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: ContextualValidation + ContextualValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (ContextualValidation<T1> v1, ContextualValidation<T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2.Value, tuple.v3).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: ContextualValidation + Validation + ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (ContextualValidation<T1> v1, Validation<Error, T2> v2, ContextualValidation<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.Value, tuple.v2, tuple.v3.Value).Apply(f).As();

    /// <summary>
    /// 3-tuple Apply: Validation + ContextualValidation + ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (Validation<Error, T1> v1, ContextualValidation<T2> v2, ContextualValidation<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1, tuple.v2.Value, tuple.v3.Value).Apply(f).As();

    // =========================================================================
    // 4-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 4-tuple Apply: 모두 ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, R>(
        this (ContextualValidation<T1> v1, ContextualValidation<T2> v2, ContextualValidation<T3> v3, ContextualValidation<T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1.Value, tuple.v2.Value, tuple.v3.Value, tuple.v4.Value).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: ContextualValidation + Validation + Validation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, R>(
        this (ContextualValidation<T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1.Value, tuple.v2, tuple.v3, tuple.v4).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: Validation + ContextualValidation + Validation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, R>(
        this (Validation<Error, T1> v1, ContextualValidation<T2> v2, Validation<Error, T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1, tuple.v2.Value, tuple.v3, tuple.v4).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: Validation + Validation + ContextualValidation + Validation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, ContextualValidation<T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1, tuple.v2, tuple.v3.Value, tuple.v4).Apply(f).As();

    /// <summary>
    /// 4-tuple Apply: Validation + Validation + Validation + ContextualValidation
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3, ContextualValidation<T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1, tuple.v2, tuple.v3, tuple.v4.Value).Apply(f).As();
}
