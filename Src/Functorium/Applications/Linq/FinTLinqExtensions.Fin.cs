using LanguageExt.Traits;

namespace Functorium.Applications.Linq;

/// <summary>
/// Fin&lt;A&gt; 타입을 위한 LINQ 확장 메서드
/// </summary>
public static partial class FinTLinqExtensions
{
    // =========================================================================
    // Fin → FinT SelectMany
    // =========================================================================

    /// <summary>
    /// Fin → FinT 변환: Fin을 FinT로 승격하고 체이닝하는 SelectMany
    ///
    /// LanguageExt FinT.lift() 패턴 적용:
    ///   Fin&lt;A&gt; → FinT&lt;M, A&gt; → FinT&lt;M, B&gt; → FinT&lt;M, C&gt;
    ///
    /// LINQ 쿼리:
    ///   from finVal in finValue              // Fin&lt;A&gt;
    ///   from finTVal in finTSelector(finVal) // FinT&lt;M, B&gt;
    ///   select result                        // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, int&gt; result =
    ///       from finVal in CreateDomainObjects()  // Fin&lt;Domain&gt;
    ///       from ioVal in PublishMessages()       // FinT&lt;IO, Unit&gt;
    ///       select finVal;
    /// </summary>
    public static FinT<M, C> SelectMany<M, A, B, C>(
        this Fin<A> fin,
        Func<A, FinT<M, B>> finTSelector,
        Func<A, B, C> projector)
        where M : Monad<M>
    {
        return FinT.lift<M, A>(fin).SelectMany(finTSelector, projector);
    }

    // =========================================================================
    // Fin Filter
    // =========================================================================

    /// <summary>
    /// Fin 조건 필터링: 조건을 만족하지 않으면 Fail 반환
    ///
    /// LanguageExt의 Fin&lt;A&gt;.Where()와 충돌을 피하기 위해 Filter 사용
    ///
    /// 사용 예:
    ///   Fin&lt;int&gt; result = FinTest(25).Filter(x =&gt; x &gt; 20);
    ///   // 또는 LINQ:
    ///   from x in FinTest(25).Filter(x =&gt; x &gt; 20) select x
    /// </summary>
    public static Fin<A> Filter<A>(
        this Fin<A> fin,
        Func<A, bool> predicate)
    {
        return fin.Match(
            Succ: value => predicate(value) ? fin : Fin.Fail<A>("Condition not met"),
            Fail: error => Fin.Fail<A>(error)
        );
    }
}
