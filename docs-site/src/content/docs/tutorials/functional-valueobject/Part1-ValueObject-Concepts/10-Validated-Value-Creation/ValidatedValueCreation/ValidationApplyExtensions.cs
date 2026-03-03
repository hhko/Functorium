using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;

namespace ValidatedValueCreation;

/// <summary>
/// Validation&lt;Error, T&gt; Tuple Apply 확장 메서드
/// .As() 없이 concrete Validation 반환
/// </summary>
public static class ValidationApplyExtensions
{
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
}
