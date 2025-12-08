using LanguageExt.Traits;

namespace Functorium.Applications.Linq;

// - [ ] (List<string> Paths, DateTime LatestTime)> GetFilePathsWithLatestTimeAsync( ... )
// - [ ] N개 인터페이스 -> 1개 구현 소스 생성기
// - [ ] Traverse: NULL을 반한하는 배열 요소가 있을 때 예외 발생! 해결 방법은?

//    - Fin<A> → FinT<M, C> SelectMany(체이닝)
//    - IO<A> → FinT<IO, B> SelectMany(단순)
//    - IO<A> → FinT<IO, C> SelectMany(체이닝)
//    - Fin<A> Filter
//    - FinT<M, A> Filter

/// <summary>
/// FinT 모나드 트랜스포머를 위한 LINQ 확장 메서드
///
/// 목적: 다양한 타입(Fin, IO)을 FinT로 통합하여 LINQ 쿼리 표현식 지원
///
/// 핵심 패턴:
///   • Fin<A> → FinT<M, B>: 순수 결과 값을 모나드 트랜스포머로 승격
///   • IO<A> → FinT<IO, B>: IO 효과를 성공으로 가정하고 FinT로 변환
///   • FinT 모나드 트랜스포머 패턴 적용
/// </summary>
public static class FinTUtilites
{
  // =========================================================================
  // SelectMany 확장 메서드 - LINQ 쿼리 표현식 지원
  // =========================================================================

  /// <summary>
  /// Fin → FinT 변환: Fin을 FinT로 승격하고 체이닝하는 SelectMany
  ///
  /// LanguageExt FinT.lift() 패턴 적용:
  ///   Fin<A> → FinT<M, A> → FinT<M, B> → FinT<M, C>
  ///
  /// LINQ 쿼리:
  ///   from finVal in finValue              // Fin<A>
  ///   from finTVal in finTSelector(finVal) // FinT<M, B>
  ///   select result                        // C
  ///
  /// 사용 예:
  ///   FinT<IO, int> result =
  ///       from finVal in CreateDomainObjects()  // Fin<Domain>
  ///       from ioVal in PublishMessages()       // FinT<IO, Unit>
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

  /// <summary>
  /// IO → FinT 변환 (단순): IO를 성공으로 가정하고 FinT로 변환
  ///
  /// Lift 패턴 적용:
  ///   IO<A> → FinT<IO, A> (성공으로 자동 승격)
  ///
  /// LINQ 쿼리:
  ///   from val in ioValue  // IO<A>
  ///   select result        // B
  ///
  /// 사용 예:
  ///   FinT<IO, Unit> result =
  ///       from _ in IO.lift(() => Console.WriteLine("Hello"))
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
  ///   from val in ioValue                 // IO<A>
  ///   from nextVal in finTSelector(val)   // FinT<IO, B>
  ///   select result                       // C
  ///
  /// 사용 예:
  ///   FinT<IO, int> result =
  ///       from ftpInfos in GetFtpInfos()        // IO<List<FtpInfo>>
  ///       from processed in Process(ftpInfos)   // FinT<IO, int>
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
  // Filter 메서드 - 조건부 필터링 지원
  // =========================================================================

  /// <summary>
  /// Fin 조건 필터링: 조건을 만족하지 않으면 Fail 반환
  ///
  /// LanguageExt의 Fin<A>.Where()와 충돌을 피하기 위해 Filter 사용
  ///
  /// 사용 예:
  ///   Fin<int> result = FinTest(25).Filter(x => x > 20);
  ///   // 또는 LINQ:
  ///   from x in FinTest(25).Filter(x => x > 20) select x
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

  /// <summary>
  /// FinT 조건 필터링: 조건을 만족하지 않으면 Fail 반환
  ///
  /// FinT 모나드 트랜스포머에서 조건 필터링 지원
  ///
  /// 사용 예:
  ///   FinT<IO, int> result = FinT<IO, int>.Succ(42).Filter(x => x > 20);
  ///   // 또는 LINQ:
  ///   from x in FinT<IO, int>.Succ(42).Filter(x => x > 20) select x
  /// </summary>
  public static FinT<M, A> Filter<M, A>(
    this FinT<M, A> finT,
    Func<A, bool> predicate)
    where M : Monad<M>
  {
    return finT.Bind(value => predicate(value)
      ? finT
      : FinT.Fail<M, A>(Error.New("Condition not met")));
  }
}
