using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations;

/// <summary>
/// Fin&lt;T&gt; Tuple Apply 확장 메서드
/// 여러 Fin&lt;T&gt; 결과를 applicative로 합성하여 모든 에러를 누적합니다.
/// </summary>
/// <remarks>
/// Fin&lt;T&gt;를 Validation&lt;Error, T&gt;로 변환한 뒤
/// ValidationApplyExtensions의 Apply를 사용하고
/// 결과를 다시 Fin&lt;R&gt;로 변환합니다.
/// </remarks>
public static class FinApplyExtensions
{
    // =========================================================================
    // 2-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 2-tuple Apply: 모두 Fin&lt;T&gt;
    /// </summary>
    /// <example>
    /// (PersonalName.Create("HyungHo", "Ko"),
    ///  EmailAddress.Create("user@example.com"))
    ///     .Apply((name, email) => Contact.Create(name, email, now));
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, R>(
        this (Fin<T1> v1, Fin<T2> v2) tuple,
        Func<T1, T2, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // 3-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 3-tuple Apply: 모두 Fin&lt;T&gt;
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, T3, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation(), tuple.v3.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // 4-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 4-tuple Apply: 모두 Fin&lt;T&gt;
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, T3, T4, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation(),
         tuple.v3.ToValidation(), tuple.v4.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // 5-Tuple Apply
    // =========================================================================

    /// <summary>
    /// 5-tuple Apply: 모두 Fin&lt;T&gt;
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fin<R> Apply<T1, T2, T3, T4, T5, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4, Fin<T5> v5) tuple,
        Func<T1, T2, T3, T4, T5, R> f) =>
        (tuple.v1.ToValidation(), tuple.v2.ToValidation(),
         tuple.v3.ToValidation(), tuple.v4.ToValidation(),
         tuple.v5.ToValidation())
            .Apply(f).ToFin();

    // =========================================================================
    // FinT<IO, R> 반환 — LINQ from 구문에서 직접 사용
    // =========================================================================

    /// <summary>
    /// 2-tuple ApplyT: Fin 합성 결과를 FinT&lt;IO, R&gt;로 리프팅합니다.
    /// LINQ from 첫 구문에서 직접 사용할 수 있습니다.
    /// </summary>
    /// <example>
    /// <code>
    /// FinT&lt;IO, Response&gt; usecase =
    ///     from vos in (Name.Create(req.Name), Price.Create(req.Price))
    ///         .ApplyT((name, price) =&gt; (Name: name, Price: price))
    ///     from product in repo.Create(Product.Create(vos.Name, vos.Price))
    ///     select new Response(...);
    /// </code>
    /// </example>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, R>(
        this (Fin<T1> v1, Fin<T2> v2) tuple,
        Func<T1, T2, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));

    /// <summary>
    /// 3-tuple ApplyT: FinT&lt;IO, R&gt; 반환
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, T3, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3) tuple,
        Func<T1, T2, T3, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));

    /// <summary>
    /// 4-tuple ApplyT: FinT&lt;IO, R&gt; 반환
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, T3, T4, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4) tuple,
        Func<T1, T2, T3, T4, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));

    /// <summary>
    /// 5-tuple ApplyT: FinT&lt;IO, R&gt; 반환
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FinT<IO, R> ApplyT<T1, T2, T3, T4, T5, R>(
        this (Fin<T1> v1, Fin<T2> v2, Fin<T3> v3, Fin<T4> v4, Fin<T5> v5) tuple,
        Func<T1, T2, T3, T4, T5, R> f) =>
        FinT.lift<IO, R>(tuple.Apply(f));
}
