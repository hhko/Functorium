namespace Functorium.Applications.Linq;

/// <summary>
/// IO&lt;A&gt; 타입을 위한 LINQ 확장 메서드
/// </summary>
public static partial class FinTLinqExtensions
{
    // =========================================================================
    // IO → FinT SelectMany
    // =========================================================================

    /// <summary>
    /// IO → FinT 변환 (단순): IO를 성공으로 가정하고 FinT로 변환
    ///
    /// Lift 패턴 적용:
    ///   IO&lt;A&gt; → FinT&lt;IO, A&gt; (성공으로 자동 승격)
    ///
    /// LINQ 쿼리:
    ///   from val in ioValue  // IO&lt;A&gt;
    ///   select result        // B
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Unit&gt; result =
    ///       from _ in IO.lift(() =&gt; Console.WriteLine("Hello"))
    ///       select Unit.Default;
    /// </summary>
    public static FinT<IO, B> SelectMany<A, B>(
        this IO<A> io,
        Func<A, B> selector)
    {
        return FinT.lift<IO, A>(io.Map(a => Fin.Succ(a))).Map(selector);
    }

    /// <summary>
    /// IO → FinT 변환 (체이닝): IO를 FinT로 변환하고 다른 FinT와 체이닝
    ///
    /// LINQ 쿼리:
    ///   from val in ioValue                 // IO&lt;A&gt;
    ///   from nextVal in finTSelector(val)   // FinT&lt;IO, B&gt;
    ///   select result                       // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, int&gt; result =
    ///       from ftpInfos in GetFtpInfos()        // IO&lt;List&lt;FtpInfo&gt;&gt;
    ///       from processed in Process(ftpInfos)   // FinT&lt;IO, int&gt;
    ///       select processed;
    /// </summary>
    public static FinT<IO, C> SelectMany<A, B, C>(
        this IO<A> io,
        Func<A, FinT<IO, B>> finTSelector,
        Func<A, B, C> projector)
    {
        return FinT.lift<IO, A>(io.Map(a => Fin.Succ(a))).SelectMany(finTSelector, projector);
    }
}
