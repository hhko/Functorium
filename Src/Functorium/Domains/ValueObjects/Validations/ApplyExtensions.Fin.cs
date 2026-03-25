using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations;

/// <summary>
/// Fin&lt;T&gt; Tuple Apply 확장 메서드.
/// 여러 Fin&lt;T&gt; 결과를 applicative로 합성하여 모든 에러를 누적합니다.
/// </summary>
/// <remarks>
/// <para>ApplyExtensions.FinT의 ApplyT 메서드가 내부적으로 사용합니다.</para>
/// <para>Application Layer에서는 ApplyT를 사용하십시오.</para>
/// </remarks>
public static partial class ApplyExtensions
{
    // =========================================================================
    // 2-Tuple Apply (Fin)
    // =========================================================================

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, R>(
        this (Fin<T1> v1, Fin<T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // 3-Tuple Apply (Fin)
    // =========================================================================

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, T3, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation(), tuple.v3.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // 4-Tuple Apply (Fin)
    // =========================================================================

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, T3, T4, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation(),
         tuple.v3.ToValidation(), tuple.v4.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // 5-Tuple Apply (Fin)
    // =========================================================================

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, T3, T4, T5, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4, Fin<T5> v5) tuple,
        Func<T1, T2, T3, T4, T5, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation(),
         tuple.v3.ToValidation(), tuple.v4.ToValidation(),
         tuple.v5.ToValidation())
            .Apply(f).ToFin();
}
