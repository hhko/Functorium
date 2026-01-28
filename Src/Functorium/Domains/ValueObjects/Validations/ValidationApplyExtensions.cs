using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;

namespace Functorium.Domains.ValueObjects.Validations;

/// <summary>
/// Validation&lt;Error, T&gt; Tuple Apply 확장 메서드
/// .As() 없이 concrete Validation 반환
/// </summary>
/// <remarks>
/// LanguageExt의 generic Apply가 K&lt;Validation&lt;Error&gt;, T&gt;를 반환하므로
/// concrete Validation&lt;Error, T&gt; 튜플에 대한 오버로드를 제공하여
/// .As() 호출 없이 사용할 수 있게 합니다.
/// </remarks>
public static class ValidationApplyExtensions
{
    // =========================================================================
    // 2-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 2-tuple Apply: 모두 Validation&lt;Error, T&gt;
    /// </summary>
    /// <example>
    /// (ValidateAmount(amount), ValidateCurrency(currency))
    ///     .Apply((a, c) => new Money(a, c));
    /// </example>
    /// <remarks>
    /// K&lt;&gt; 인터페이스로 캐스팅하여 재귀 호출을 방지하고
    /// LanguageExt의 generic Apply를 호출합니다.
    /// </remarks>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2) tuple,
        Func<T1, T2, R> f) =>
        ((K<Validation<Error>, T1>)tuple.v1, (K<Validation<Error>, T2>)tuple.v2)
            .Apply(f).As();

    // =========================================================================
    // 3-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 3-tuple Apply: 모두 Validation&lt;Error, T&gt;
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2, Validation<Error, T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        ((K<Validation<Error>, T1>)tuple.v1,
         (K<Validation<Error>, T2>)tuple.v2,
         (K<Validation<Error>, T3>)tuple.v3)
            .Apply(f).As();

    // =========================================================================
    // 4-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 4-tuple Apply: 모두 Validation&lt;Error, T&gt;
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2,
              Validation<Error, T3> v3, Validation<Error, T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        ((K<Validation<Error>, T1>)tuple.v1,
         (K<Validation<Error>, T2>)tuple.v2,
         (K<Validation<Error>, T3>)tuple.v3,
         (K<Validation<Error>, T4>)tuple.v4)
            .Apply(f).As();

    // =========================================================================
    // 5-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 5-tuple Apply: 모두 Validation&lt;Error, T&gt;
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Validation<Error, R> Apply<T1, T2, T3, T4, T5, R>(
        this (Validation<Error, T1> v1, Validation<Error, T2> v2,
              Validation<Error, T3> v3, Validation<Error, T4> v4,
              Validation<Error, T5> v5) tuple,
        Func<T1, T2, T3, T4, T5, R> f) =>
        ((K<Validation<Error>, T1>)tuple.v1,
         (K<Validation<Error>, T2>)tuple.v2,
         (K<Validation<Error>, T3>)tuple.v3,
         (K<Validation<Error>, T4>)tuple.v4,
         (K<Validation<Error>, T5>)tuple.v5)
            .Apply(f).As();
}
