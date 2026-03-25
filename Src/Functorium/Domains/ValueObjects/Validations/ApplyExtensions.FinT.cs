using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations;

/// <summary>
/// FinT&lt;IO, R&gt; 반환 Apply 확장 메서드.
/// Fin&lt;T&gt; tuple 합성 결과를 FinT&lt;IO, R&gt;로 리프팅하여
/// LINQ from 첫 구문에서 직접 사용할 수 있습니다.
/// </summary>
/// <example>
/// <code>
/// FinT&lt;IO, Response&gt; usecase =
///     from vos in (Name.Create(req.Name), Price.Create(req.Price))
///         .ApplyT((name, price) =&gt; (Name: name, Price: price))
///     let product = Product.Create(vos.Name, vos.Price)
///     from created in repo.Create(product)
///     select new Response(...);
/// </code>
/// </example>
public static partial class ApplyExtensions
{
    // =========================================================================
    // 2-Tuple ApplyT (FinT<IO, R>)
    // =========================================================================

    /// <summary>
    /// 2-tuple ApplyT: Fin 합성 + FinT&lt;IO, R&gt; 리프팅
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, R>(
        this (Fin<T1> v1, Fin<T2> v2) tuple,
        Func<T1, T2, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));

    // =========================================================================
    // 3-Tuple ApplyT (FinT<IO, R>)
    // =========================================================================

    /// <summary>
    /// 3-tuple ApplyT: Fin 합성 + FinT&lt;IO, R&gt; 리프팅
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, T3, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));

    // =========================================================================
    // 4-Tuple ApplyT (FinT<IO, R>)
    // =========================================================================

    /// <summary>
    /// 4-tuple ApplyT: Fin 합성 + FinT&lt;IO, R&gt; 리프팅
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, T3, T4, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));

    // =========================================================================
    // 5-Tuple ApplyT (FinT<IO, R>)
    // =========================================================================

    /// <summary>
    /// 5-tuple ApplyT: Fin 합성 + FinT&lt;IO, R&gt; 리프팅
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, T3, T4, T5, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4, Fin<T5> v5) tuple,
        Func<T1, T2, T3, T4, T5, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));
}
