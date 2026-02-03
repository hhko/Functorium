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

    // =========================================================================
    // FinT → IO SelectMany
    // =========================================================================

    /// <summary>
    /// FinT → IO 체이닝: FinT 컨텍스트에서 IO 효과를 체이닝하는 SelectMany
    ///
    /// IO는 항상 성공하는 것으로 가정하여 FinT로 승격:
    ///   FinT&lt;IO, A&gt; → IO&lt;B&gt; → FinT&lt;IO, C&gt;
    ///
    /// LINQ 쿼리:
    ///   from finTVal in finTValue           // FinT&lt;IO, A&gt;
    ///   from ioVal in ioSelector(finTVal)   // IO&lt;B&gt;
    ///   select result                       // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Response&gt; result =
    ///       from product in repository.GetById(id)              // FinT&lt;IO, Product&gt;
    ///       from timestamp in IO.lift(() =&gt; DateTime.UtcNow)    // IO&lt;DateTime&gt;
    ///       select new Response(product.Id, timestamp);
    ///
    /// 주요 사용 시나리오:
    ///   - 타임스탬프, GUID 생성 등 순수 IO 효과를 체인에 포함
    ///   - 로깅, 메트릭 기록 등 항상 성공하는 부수 효과
    ///   - 환경 설정 읽기 등 실패하지 않는 IO 작업
    /// </summary>
    public static FinT<IO, C> SelectMany<A, B, C>(
        this FinT<IO, A> finT,
        Func<A, IO<B>> ioSelector,
        Func<A, B, C> projector)
    {
        return finT.Bind(a =>
            FinT.lift<IO, B>(ioSelector(a).Map(b => Fin.Succ(b)))
                .Map(b => projector(a, b)));
    }
}
