## Traverse
- Language-Ext의 Traverse 동작:
  - Traverse는 기본적으로 순차적으로 동작
  - 하지만 FinT<IO, T>의 비동기 특성 때문에:
  - 첫 번째 작업: IO.liftAsync(() => await _dbContext.Set<JobIntegrate>()...)
  - 첫 번째 await가 시작되면 제어권이 반환
  - Traverse가 다음 항목을 처리하기 시작
  - 결과: 두 작업이 "동시에" 동일한 DbContext에 접근
- Traverse: 내부 구현에 따라 병렬로 시작될 수 있음
- fold: 각 단계가 완전히 끝난 후 다음 단계 시작 보장
  - 왜 이것이 동작하는가?:
    - fold는 축적(accumulation) 방식으로 동작
    - 각 단계가 완전히 끝난 후 다음 단계 시작 보장
    - LINQ의 from ... from ... 체인이 순차 실행 강제
    - DbContext는 한 번에 하나의 작업만 처리하므로 충돌 없음

```cs
private FinT<IO, Seq<Fin<string>>> ProcessLinesSequentially(
    IReadOnlyList<FtpConnectionInfo> ftpInfos,
    CancellationToken cancellationToken)
{
    // 빈 시퀀스로 시작
    FinT<IO, Seq<Fin<string>>> initial = FinT<IO, Seq<Fin<string>>>.Succ(new Seq<Fin<string>>());

    // fold를 사용하여 순차적으로 각 라인 처리
    return new Seq<FtpConnectionInfo>(ftpInfos).Fold(initial, (acc, ftpInfo) =>
        from results in acc
        from result in WatchLimitSampleForLine(ftpInfo, cancellationToken)
        select results.Add(result));
}
```

 구분        | Traverse         | TraverseM
-----------|------------------|-----------
 기반        | Applicative.lift | Bind 체이닝
 실행        | 병렬 가능            | 순차 보장
 DbContext | ❌ 동시성 예외         | ✅ 안전
 성능        | 빠름               | 느림 (순차)


- TraverseSerial vs TraverseM

   항목      | TraverseSerial | TraverseM
  ---------|----------------|----------------
   스타일     | Seq-first      | Func-first
   LINQ 통합 | ✅ 자연스러움        | ⚠️ 별도 변수 필요
   가독성     | ✅ 직관적          | ⚠️ 복잡
   기능      | fold + Bind    | fold + Bind
   성능      | 동일             | 동일
   출처      | 커스텀 확장         | LanguageExt 공식

  ```cs
  // TraverseM
  Func<FtpConnectionInfo, FinT<IO, Fin<string>>> processFunc =
      ftpInfo => WatchLimitSampleForLine(ftpInfo, cancellationToken);

  FinT<IO, Seq<Fin<string>>> usecase =
      from ftpInfos in _repository.GetFtpConnectionInfosAsync(cancellationToken)
      from results in processFunc
          .TraverseM(new Seq<FtpConnectionInfo>(ftpInfos))
          .Map(seq => seq.As())
          .As()
      select results;

  static K<F, K<Seq, B>> Traverse<F, A, B>(...)
  {
      Func<K<F, Seq<B>>, K<F, Seq<B>>> add(A value) =>
          state => Applicative.lift((bs, b) => bs.Add(b), state, f(value));
  }

  // Line 126-136: TraverseM (Monad 기반 - 순차 보장)
  static K<F, K<Seq, B>> TraverseM<F, A, B>(...)
  {
      Func<K<F, Seq<B>>, K<F, Seq<B>>> add(A value) =>
          state =>
              state.Bind(
                  bs => f(value).Bind(
                      b => F.Pure(bs.Add(b))));
  }
  ```
  ```
  ```cs
  // TraverseSerial 인라인

  FinT<IO, Seq<Fin<string>>> usecase =
      from ftpInfos in _repository.GetFtpConnectionInfosAsync(cancellationToken)

      from results in new Seq<FtpConnectionInfo>(ftpInfos)
          .TraverseSerial(ftpInfo => WatchLimitSampleForLine(ftpInfo, cancellationToken))
          .As()

      select results;
  ```




