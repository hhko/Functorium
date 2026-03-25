using LanguageExt.Traits;

namespace Functorium.Applications.Linq;

/// <summary>
/// Fin&lt;A&gt; 타입을 위한 LINQ 확장 메서드
/// </summary>
public static partial class FinTLinqExtensions
{
    // =========================================================================
    // Fin Unwrap — 파이프라인 검증 후 안전하게 값 꺼내기
    // =========================================================================

    /// <summary>
    /// 파이프라인 Validator가 검증을 완료한 후 안전하게 값을 꺼냅니다.
    /// <c>Create()</c>가 반드시 성공하는 컨텍스트(핸들러 내부)에서 사용합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>UsecaseValidationPipeline</c>이 <c>MustSatisfyValidation(VO.Validate)</c>으로
    /// 검증을 완료한 후 핸들러가 실행됩니다. 이 시점에서 <c>Create()</c>는 정규화(Trim 등)
    /// 목적으로만 호출되며, 검증은 반드시 성공합니다.
    /// </para>
    /// <code>
    /// // 파이프라인이 검증 완료. Create()는 정규화 목적.
    /// var name = ProductName.Create(request.Name).Unwrap();
    /// var price = Money.Create(request.Price).Unwrap();
    /// var product = Product.Create(name, price);
    /// </code>
    /// </remarks>
    public static A Unwrap<A>(this Fin<A> fin) => fin.ThrowIfFail();

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
    // FinT → Fin SelectMany
    // =========================================================================

    /// <summary>
    /// FinT → Fin 체이닝: FinT 컨텍스트에서 Fin 결과를 체이닝하는 SelectMany
    ///
    /// LanguageExt FinT.lift() 패턴 적용:
    ///   FinT&lt;M, A&gt; → Fin&lt;B&gt; → FinT&lt;M, C&gt;
    ///
    /// LINQ 쿼리:
    ///   from finTVal in finTValue              // FinT&lt;M, A&gt;
    ///   from finVal in finSelector(finTVal)    // Fin&lt;B&gt;
    ///   select result                          // C
    ///
    /// 사용 예:
    ///   FinT&lt;IO, Response&gt; result =
    ///       from product in repository.GetById(id)     // FinT&lt;IO, Product&gt;
    ///       from _ in product.DeductStock(quantity)    // Fin&lt;Unit&gt;
    ///       from updated in repository.Update(product) // FinT&lt;IO, Product&gt;
    ///       select new Response(updated.Id);
    ///
    /// 주요 사용 시나리오:
    ///   - 도메인 로직 (Fin 반환)을 FinT 체인에 포함
    ///   - Value Object 검증 결과를 FinT 체인에 포함
    ///   - 동기적 검증/변환 로직을 비동기 체인에 포함
    /// </summary>
    public static FinT<M, C> SelectMany<M, A, B, C>(
        this FinT<M, A> finT,
        Func<A, Fin<B>> finSelector,
        Func<A, B, C> projector)
        where M : Monad<M>
    {
        return finT.Bind(a => FinT.lift<M, B>(finSelector(a)).Map(b => projector(a, b)));
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
