using System.Diagnostics;
using Functorium.Abstractions;
using Functorium.Adapters.Observabilities;
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

    // =========================================================================
    // 상수
    // =========================================================================

    /// <summary>
    /// Traverse Activity의 이름 형식
    /// </summary>
    private const string TraverseActivityNameFormat = "Application Traverse {0} [{1}]";


    // =========================================================================
    // TraverseSerial 메서드 - 순차 처리 보장
    // =========================================================================

    /// <summary>
    /// Seq를 순차적으로 순회하며 각 요소를 FinT로 변환합니다.
    /// fold를 사용하여 각 작업이 완전히 끝난 후 다음 작업을 시작하도록 보장합니다.
    ///
    /// 목적:
    ///   • 병렬 실행을 방지하고 진정한 순차 처리 보장
    ///   • DbContext 등 동시성을 지원하지 않는 리소스의 안전한 사용
    ///   • 각 항목의 처리 순서가 중요한 경우
    ///
    /// LanguageExt Traversable 패턴:
    ///   • Traverse (Applicative): Applicative.lift 기반 → 병렬 실행 가능
    ///   • TraverseM (Monad): Monad Bind 체이닝 → 순차 실행 보장
    ///   • TraverseSerial: TraverseM의 FinT 특화 버전 (동일한 fold + Bind 패턴)
    ///
    /// 동작 방식:
    ///   1. 빈 시퀀스로 시작
    ///   2. fold로 각 항목 처리:
    ///      - 이전 결과 완료 대기 (from results in acc)
    ///      - 현재 항목 처리 (from result in f(item))
    ///      - 결과 누적 (select results.Add(result))
    ///   3. 모든 결과를 포함하는 Seq 반환
    ///
    /// 사용 예:
    ///   // DbContext를 사용하는 여러 작업을 순차 처리
    ///   FinT<IO, Seq<Result>> results = ftpInfos
    ///       .TraverseSerial(info => ProcessFtpLine(info, cancellationToken));
    ///
    ///   // LINQ 쿼리 표현식에서 사용
    ///   FinT<IO, Response> response =
    ///       from infos in GetFtpInfos()
    ///       from results in infos.TraverseSerial(info => Process(info))
    ///       select new Response(results);
    ///
    /// 성능 고려사항:
    ///   • 순차 처리로 인해 병렬 처리보다 느릴 수 있음
    ///   • 하지만 리소스 안전성이 더 중요한 경우 사용
    ///   • 각 작업이 독립적이고 리소스 공유가 없다면 Traverse 사용 권장
    /// </summary>
    /// <typeparam name="M">모나드 타입 (예: IO)</typeparam>
    /// <typeparam name="A">입력 요소 타입</typeparam>
    /// <typeparam name="B">변환된 결과 타입</typeparam>
    /// <param name="seq">처리할 시퀀스</param>
    /// <param name="f">각 요소를 FinT로 변환하는 함수</param>
    /// <returns>모든 결과를 포함하는 FinT Seq</returns>
    public static FinT<M, Seq<B>> TraverseSerial<M, A, B>(
        this Seq<A> seq,
        Func<A, FinT<M, B>> f)
        where M : Monad<M>
    {
        // | Item  | TraverseSerial | TraverseM    |
        // |-------|----------------|--------------|
        // | Style | Seq-first      | Func-first   |

        // 빈 시퀀스로 시작
        FinT<M, Seq<B>> initial = FinT.lift<M, Seq<B>>(M.Pure(Fin.Succ(new Seq<B>())));

        // fold를 사용하여 순차적으로 각 항목 처리
        return seq.Fold(initial, (acc, item) =>
            from results in acc                    // 이전 결과가 완료될 때까지 대기
            from result in f(item)                 // 현재 항목 처리
            select results.Add(result));           // 결과 누적
    }

    /// <summary>
    /// Seq를 순차적으로 순회하며 각 요소를 FinT로 변환합니다. (OpenTelemetry Activity 추적 지원)
    ///
    /// 각 항목 처리 시 개별 Activity를 생성하여 분산 추적 시각화에서 명확히 구분합니다.
    ///
    /// Activity 구조:
    ///   • 이름: "Application Traverse {operationName} [{identifier}]"
    ///   • Tags:
    ///     - request.layer: "Application"
    ///     - request.category: "Traverse"
    ///     - traverse.item.index: 0, 1, 2...
    ///     - traverse.item.count: 총 항목 수
    ///     - traverse.item.identifier: 항목 식별자 (예: LineId)
    ///     - traverse.item.type: 항목 타입명
    ///     - elapsed: 처리 시간 (ms)
    ///
    /// 사용 예:
    ///   from results in ftpInfos.TraverseSerial(
    ///       f: ftpInfo => ProcessFtp(ftpInfo),
    ///       activitySource: _activitySource,
    ///       operationName: "ProcessFtpLine",
    ///       getItemIdentifier: (ftpInfo, index) => ftpInfo.LineId)
    ///   select results;
    ///
    /// 성능 최적화:
    ///   • ActivityTagsCollection: StartActivity에 태그 일괄 전달 (SetTag() 호출 제거)
    ///   • List&lt;B&gt; 누적: O(1) amortized 추가, 마지막에 ToSeq() 한 번만 호출 (O(n²) → O(n))
    ///
    /// 주의:
    ///   • ActivitySource는 DI 컨테이너에서 주입받은 인스턴스 사용
    ///   • Activity 생성 실패 시에도 처리는 정상 진행
    ///   • 순차 처리 보장: 각 항목이 완전히 처리된 후 다음 항목 시작
    ///   • TraverseActivityContext를 통한 AsyncLocal 관리로 Activity 계층 유지
    /// </summary>
    /// <typeparam name="M">모나드 타입 (예: IO)</typeparam>
    /// <typeparam name="A">입력 요소 타입</typeparam>
    /// <typeparam name="B">변환된 결과 타입</typeparam>
    /// <param name="seq">처리할 시퀀스</param>
    /// <param name="f">각 요소를 FinT로 변환하는 함수</param>
    /// <param name="activitySource">Activity를 생성할 ActivitySource (DI 주입)</param>
    /// <param name="operationName">작업 이름 (Activity 이름에 사용)</param>
    /// <param name="getItemIdentifier">각 항목의 식별자를 추출하는 함수</param>
    /// <returns>모든 결과를 포함하는 FinT Seq</returns>
    public static FinT<IO, Seq<B>> TraverseSerial<A, B>(
        this Seq<A> seq,
        Func<A, FinT<IO, B>> f,
        ActivitySource activitySource,
        string operationName,
        Func<A, int, string> getItemIdentifier)
    {
        int totalCount = seq.Count;

        // 전체를 하나의 IO 효과로 처리 (for 루프 기반)
        // 성능 최적화: List<B> 사용으로 O(n²) → O(n) 개선
        IO<Fin<Seq<B>>> io = IO.liftAsync<Fin<Seq<B>>>(async () =>
        {
            List<B> results = new List<B>(totalCount);  // 용량 미리 예약

            for (int i = 0; i < totalCount; i++)
            {
                A item = seq[i];
                string itemIdentifier = getItemIdentifier(item, i);

                // 1. Activity 태그 컬렉션 준비 (성능 최적화: SetTag() 호출 제거)
                ActivityTagsCollection tags = new ActivityTagsCollection
                {
                    { "request.layer", "Application" },
                    { "request.category", "Traverse" },
                    { "traverse.item.index", i },
                    { "traverse.item.count", totalCount },
                    { "traverse.item.identifier", itemIdentifier },
                    { "traverse.item.type", typeof(A).Name }
                };

                // 2. Activity 생성 (태그를 StartActivity에 전달)
                // 부모 ActivityContext 결정:
                // 1순위: UsecaseActivityContext (Adapter에서 설정된 Usecase의 Context)
                // 2순위: Activity.Current (일반적인 경우)
                ActivityContext? parentContext = null;

                Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
                if (traverseActivity != null)
                {
                    parentContext = traverseActivity.Context;
                }
                else if (Activity.Current != null)
                {
                    parentContext = Activity.Current.Context;
                }

                string activityName = string.Format(TraverseActivityNameFormat, operationName, itemIdentifier);

                Activity? activity = parentContext.HasValue
                    ? activitySource.StartActivity(
                        activityName,
                        ActivityKind.Internal,
                        parentContext.Value,
                        tags)
                    : activitySource.StartActivity(
                        activityName,
                        ActivityKind.Internal,
                        default(ActivityContext),
                        tags);

                // 참고: StartActivity는 sampling 정책이나 리소스 제약으로 인해 null을 반환할 수 있습니다.
                // null인 경우에도 비즈니스 로직은 계속 실행되며, Activity는 관찰성(observability) 목적으로만 사용됩니다.
                // TraverseActivityContext.Enter()는 null Activity를 안전하게 처리합니다.

                // TraverseActivityContext를 사용하여 현재 Traverse Activity 관리
                using (ActivityContextHolder.EnterActivity(activity))
                {

                    long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

                    try
                    {
                        // 3. 실제 작업 실행 (Activity.Current가 설정된 상태)
                        Fin<B> finResult = await f(item).Run().RunAsync();

                        // 4. 실패 시 즉시 반환
                        if (finResult.IsFail)
                        {
                            return finResult.Match(
                                Succ: _ => throw new InvalidOperationException("Unreachable"),
                                Fail: error => Fin.Fail<Seq<B>>(error)
                            );
                        }

                        // 5. 성공 시 결과 누적 (List.Add는 O(1) amortized)
                        results.Add(finResult.ThrowIfFail());

                        // 6. 성능 메트릭 설정
                        if (activity != null)
                        {
                            double elapsed = ElapsedTimeCalculator.CalculateElapsedMilliseconds(startTimestamp);
                            activity.SetTag("elapsed", elapsed);
                            activity.SetStatus(ActivityStatusCode.Ok);
                        }
                    }
                    finally
                    {
                        // 7. Activity 정리 (성공/실패 관계없이)
                        activity?.Stop();
                        activity?.Dispose();
                    }
                } // using이 끝나면 자동으로 이전 TraverseActivity로 복원
            }

            // List<B>를 Seq<B>로 변환 (마지막에 한 번만)
            return Fin.Succ(new Seq<B>(results));
        });

        return FinT.lift<IO, Seq<B>>(io);
    }
}
