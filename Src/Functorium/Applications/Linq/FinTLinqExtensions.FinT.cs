using LanguageExt.Traits;

namespace Functorium.Applications.Linq;

/// <summary>
/// FinT&lt;M, A&gt; 타입을 위한 LINQ 확장 메서드
/// </summary>
public static partial class FinTLinqExtensions
{

    // =========================================================================
    // FinT Filter
    // =========================================================================

    /// <summary>
    /// FinT 조건 필터링: 조건을 만족하지 않으면 Fail 반환
    ///
    /// FinT 모나드 트랜스포머에서 조건 필터링 지원
    ///
    /// 사용 예:
    ///   FinT&lt;IO, int&gt; result = FinT&lt;IO, int&gt;.Succ(42).Filter(x =&gt; x &gt; 20);
    ///   // 또는 LINQ:
    ///   from x in FinT&lt;IO, int&gt;.Succ(42).Filter(x =&gt; x &gt; 20) select x
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
    // TraverseSerial (제네릭 모나드)
    // =========================================================================

    /// <summary>
    /// Seq를 순차적으로 순회하며 각 요소를 FinT로 변환합니다.
    /// fold를 사용하여 각 작업이 완전히 끝난 후 다음 작업을 시작하도록 보장합니다.
    ///
    /// 목적:
    ///   - 병렬 실행을 방지하고 진정한 순차 처리 보장
    ///   - DbContext 등 동시성을 지원하지 않는 리소스의 안전한 사용
    ///   - 각 항목의 처리 순서가 중요한 경우
    ///
    /// LanguageExt Traversable 패턴:
    ///   - Traverse (Applicative): Applicative.lift 기반 → 병렬 실행 가능
    ///   - TraverseM (Monad): Monad Bind 체이닝 → 순차 실행 보장
    ///   - TraverseSerial: TraverseM의 FinT 특화 버전 (동일한 fold + Bind 패턴)
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
    ///   FinT&lt;IO, Seq&lt;Result&gt;&gt; results = ftpInfos
    ///       .TraverseSerial(info =&gt; ProcessFtpLine(info, cancellationToken));
    ///
    ///   // LINQ 쿼리 표현식에서 사용
    ///   FinT&lt;IO, Response&gt; response =
    ///       from infos in GetFtpInfos()
    ///       from results in infos.TraverseSerial(info =&gt; Process(info))
    ///       select new Response(results);
    ///
    /// 성능 고려사항:
    ///   - 순차 처리로 인해 병렬 처리보다 느릴 수 있음
    ///   - 하지만 리소스 안전성이 더 중요한 경우 사용
    ///   - 각 작업이 독립적이고 리소스 공유가 없다면 Traverse 사용 권장
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

    // =========================================================================
    // TraverseSerial (IO 모나드 + OpenTelemetry Activity)
    // =========================================================================
    // NOTE: Functorium.Adapters 분리로 인해 주석 처리됨.
    // ElapsedTimeCalculator가 Functorium.Adapters에 위치하므로
    // 코어 프로젝트에서 참조할 수 없음.
    // 향후 Functorium.Adapters에서 확장 메서드로 제공 예정.
    // =========================================================================

    // /// <summary>
    // /// Traverse Activity의 이름 형식
    // /// </summary>
    // private const string TraverseActivityNameFormat = "Application Traverse {0} [{1}]";

    // /// <summary>
    // /// Seq를 순차적으로 순회하며 각 요소를 FinT로 변환합니다. (OpenTelemetry Activity 추적 지원)
    // ///
    // /// 각 항목 처리 시 개별 Activity를 생성하여 분산 추적 시각화에서 명확히 구분합니다.
    // ///
    // /// Activity 구조:
    // ///   - 이름: "Application Traverse {operationName} [{identifier}]"
    // ///   - Tags:
    // ///     - request.layer: "Application"
    // ///     - request.category: "Traverse"
    // ///     - traverse.item.index: 0, 1, 2...
    // ///     - traverse.item.count: 총 항목 수
    // ///     - traverse.item.identifier: 항목 식별자 (예: LineId)
    // ///     - traverse.item.type: 항목 타입명
    // ///     - elapsed: 처리 시간 (ms)
    // ///
    // /// 사용 예:
    // ///   from results in ftpInfos.TraverseSerial(
    // ///       f: ftpInfo =&gt; ProcessFtp(ftpInfo),
    // ///       activitySource: _activitySource,
    // ///       operationName: "ProcessFtpLine",
    // ///       getItemIdentifier: (ftpInfo, index) =&gt; ftpInfo.LineId)
    // ///   select results;
    // ///
    // /// 성능 최적화:
    // ///   - ActivityTagsCollection: StartActivity에 태그 일괄 전달 (SetTag() 호출 제거)
    // ///   - List&lt;B&gt; 누적: O(1) amortized 추가, 마지막에 ToSeq() 한 번만 호출 (O(n^2) → O(n))
    // ///
    // /// 주의:
    // ///   - ActivitySource는 DI 컨테이너에서 주입받은 인스턴스 사용
    // ///   - Activity 생성 실패 시에도 처리는 정상 진행
    // ///   - 순차 처리 보장: 각 항목이 완전히 처리된 후 다음 항목 시작
    // ///   - .NET의 ExecutionContext가 Activity.Current를 자동으로 전파하여 Activity 계층 유지
    // /// </summary>
    // /// <typeparam name="A">입력 요소 타입</typeparam>
    // /// <typeparam name="B">변환된 결과 타입</typeparam>
    // /// <param name="seq">처리할 시퀀스</param>
    // /// <param name="f">각 요소를 FinT로 변환하는 함수</param>
    // /// <param name="activitySource">Activity를 생성할 ActivitySource (DI 주입)</param>
    // /// <param name="operationName">작업 이름 (Activity 이름에 사용)</param>
    // /// <param name="getItemIdentifier">각 항목의 식별자를 추출하는 함수</param>
    // /// <returns>모든 결과를 포함하는 FinT Seq</returns>
    // public static FinT<IO, Seq<B>> TraverseSerial<A, B>(
    //     this Seq<A> seq,
    //     Func<A, FinT<IO, B>> f,
    //     ActivitySource activitySource,
    //     string operationName,
    //     Func<A, int, string> getItemIdentifier)
    // {
    //     int totalCount = seq.Count;
    //
    //     // 전체를 하나의 IO 효과로 처리 (for 루프 기반)
    //     // 성능 최적화: List<B> 사용으로 O(n²) → O(n) 개선
    //     IO<Fin<Seq<B>>> io = IO.liftAsync<Fin<Seq<B>>>(async () =>
    //     {
    //         List<B> results = new List<B>(totalCount);  // 용량 미리 예약
    //
    //         for (int i = 0; i < totalCount; i++)
    //         {
    //             A item = seq[i];
    //             string itemIdentifier = getItemIdentifier(item, i);
    //
    //             // 1. Activity 태그 컬렉션 준비 (성능 최적화: SetTag() 호출 제거)
    //             ActivityTagsCollection tags = new ActivityTagsCollection
    //             {
    //                 { "request.layer", "Application" },
    //                 { "request.category", "Traverse" },
    //                 { "traverse.item.index", i },
    //                 { "traverse.item.count", totalCount },
    //                 { "traverse.item.identifier", itemIdentifier },
    //                 { "traverse.item.type", typeof(A).Name }
    //             };
    //
    //             // 2. Activity 생성 (태그를 StartActivity에 전달)
    //             // 부모 ActivityContext 결정:
    //             // .NET의 ExecutionContext가 Activity.Current를 자동으로 전파하므로
    //             // Activity.Current를 직접 사용합니다.
    //             string activityName = string.Format(TraverseActivityNameFormat, operationName, itemIdentifier);
    //
    //             Activity? activity = Activity.Current != null
    //                 ? activitySource.StartActivity(
    //                     activityName,
    //                     ActivityKind.Internal,
    //                     Activity.Current.Context,
    //                     tags)
    //                 : activitySource.StartActivity(
    //                     activityName,
    //                     ActivityKind.Internal,
    //                     default(ActivityContext),
    //                     tags);
    //
    //             // 참고: StartActivity는 sampling 정책이나 리소스 제약으로 인해 null을 반환할 수 있습니다.
    //             // null인 경우에도 비즈니스 로직은 계속 실행되며, Activity는 관찰성(observability) 목적으로만 사용됩니다.
    //
    //             long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();
    //
    //             try
    //             {
    //                 // 3. 실제 작업 실행
    //                 // .NET의 ExecutionContext가 Activity.Current를 자동으로 전파하므로
    //                 // 추가적인 AsyncLocal 관리가 필요하지 않습니다.
    //                 Fin<B> finResult = await f(item).Run().RunAsync();
    //
    //                 // 4. 실패 시 즉시 반환
    //                 if (finResult.IsFail)
    //                 {
    //                     return finResult.Match(
    //                         Succ: _ => throw new InvalidOperationException("Unreachable"),
    //                         Fail: error => Fin.Fail<Seq<B>>(error)
    //                     );
    //                 }
    //
    //                 // 5. 성공 시 결과 누적 (List.Add는 O(1) amortized)
    //                 results.Add(finResult.ThrowIfFail());
    //
    //                 // 6. 성능 메트릭 설정
    //                 if (activity != null)
    //                 {
    //                     double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
    //                     activity.SetTag("elapsed", elapsed);
    //                     activity.SetStatus(ActivityStatusCode.Ok);
    //                 }
    //             }
    //             finally
    //             {
    //                 // 7. Activity 정리 (성공/실패 관계없이)
    //                 activity?.Stop();
    //                 activity?.Dispose();
    //             }
    //         }
    //
    //         // List<B>를 Seq<B>로 변환 (마지막에 한 번만)
    //         return Fin.Succ(new Seq<B>(results));
    //     });
    //
    //     return FinT.lift<IO, Seq<B>>(io);
    // }
}
