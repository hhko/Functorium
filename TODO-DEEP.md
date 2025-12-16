## Powershell 스크립트 비교
```
● 스크립트 비교 리뷰

  1. 전체 구조

  | 항목        | Build-CommitSummary.ps1 | Build-Local.ps1        |
  |-----------|-------------------------|------------------------|
  | 라인 수      | 757                     | 720                    |
  | Shebang   | 없음                      | #!/usr/bin/env pwsh    |
  | #Requires | 없음                      | #Requires -Version 7.0 |
  | 스코프 변수    | 전역 변수 ($OUTPUT_DIR)     | $script: 접두사 사용        |
  | Main 위치   | 가장 아래 (호출 순서대로)         | 가장 아래                  |
  | region 구성 | Step 단위로 분리             | 기능 단위로 분리              |

  2. 파라미터 정의

  Build-CommitSummary.ps1:
  [CmdletBinding()]
  param(
      [Parameter(Mandatory = $false, Position = 0, HelpMessage = "...")]
      [string]$Range,

      [Alias("h", "?")]
      [switch]$Help
  )

  Build-Local.ps1:
  param(
      [Alias("s")]
      [string]$Solution,

      [switch]$Stable,

      [Alias("suffix")]
      [ValidateSet("dev", "alpha", "beta", "rc")]
      [string]$SuffixPrefix = "dev",

      [Alias("p")]
      [string]$ProjectPrefix = "Functorium",

      [Alias("h", "?")]
      [switch]$Help
  )

  차이점:
  - CommitSummary: [CmdletBinding()] 사용
  - Local: ValidateSet 검증 사용
  - Local: 더 많은 Alias 활용

  3. 상수/설정 관리

  Build-CommitSummary.ps1:
  $COMMIT_TYPES = @{ ... }
  $OUTPUT_DIR = ".commit-summaries"

  Build-Local.ps1:
  $script:ScriptDir = $PSScriptRoot
  $script:WorkingDir = $PWD.Path
  $script:Configuration = "Release"
  $script:CoreLayerPatterns = @(...)
  $script:ReportGeneratorVersion = "5.5.0"
  $script:SectionLine = "═" * 80

  차이점:
  - CommitSummary: 단순 전역 변수
  - Local: $script: 스코프 명시, 더 많은 설정 변수

  4. 도움말 구현

  Build-CommitSummary.ps1: Write-Host로 구조화된 출력
  function Show-Help {
      Write-Host "사용법:" -ForegroundColor Yellow
      Write-Host "  .\$scriptName [[-Range] <string>] [-Help]"
      ...
  }

  Build-Local.ps1: Here-String 사용
  function Show-Help {
      $help = @"
  ================================================================================
   .NET Solution Build and Test Script
  ================================================================================
  ...
  "@
      Write-Host $help
  }

  평가: Local의 Here-String이 유지보수 용이

  5. 출력 함수

  Build-CommitSummary.ps1:
  Write-Host "[1/7] Git 저장소 확인 중..." -ForegroundColor Gray
  Write-Host "   Git 저장소 확인 완료" -ForegroundColor DarkGray

  Build-Local.ps1:
  function Write-StepHeader { ... }
  function Write-SubHeader { ... }
  function Write-Success { ... }
  function Write-Info { ... }

  평가: Local이 출력 함수를 추상화하여 재사용성 높음

  6. 에러 처리

  Build-CommitSummary.ps1:
  try {
      Main -CommitRange $Range
      exit 0
  }
  catch {
      Write-Host "[오류] 예상치 못한 오류가 발생했습니다:" -ForegroundColor Red
      Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
      Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
      exit 1
  }

  Build-Local.ps1:
  try {
      ...
  } catch {
      Write-Host "Error: $_" -ForegroundColor Red
      Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
      exit 1
  }

  평가: CommitSummary가 더 사용자 친화적 에러 메시지

  7. Main 함수 구조

  Build-CommitSummary.ps1: 명확한 단계별 주석
  function Main {
      # 1. Git 저장소 검증
      Assert-GitRepository

      # 2. 커밋 범위 결정
      $resolvedRange = Resolve-CommitRange -Range $CommitRange
      ...
  }

  Build-Local.ps1: 시작/종료 배너, 시간 측정
  function Main {
      $startTime = Get-Date

      Write-Host "═══...═══" -ForegroundColor Green
      Write-Host " .NET Solution Build and Test Script"
      ...

      $duration = $endTime - $startTime
      Write-Host " Duration: $($duration.ToString('mm\\:ss'))"
  }

  8. 권장 개선사항

  | 항목                     | CommitSummary 적용      | Local 적용  |
  |------------------------|-----------------------|-----------|
  | #Requires -Version 7.0 | 추가 권장                 | 이미 있음     |
  | $script: 스코프           | 추가 권장                 | 이미 있음     |
  | 출력 헬퍼 함수               | Write-StepHeader 등 도입 | 이미 있음     |
  | Here-String 도움말        | 변환 권장                 | 이미 있음     |
  | 실행 시간 측정               | 추가 권장                 | 이미 있음     |
  | [CmdletBinding()]      | 이미 있음                 | 추가 권장     |
  | ValidateSet            | 해당 없음                 | 이미 있음     |
  | Step 진행률 표시            | 이미 있음 ([1/7])         | 섹션 헤더로 대체 |

  9. 결론

  | 관점       | 우수 스크립트                      |
  |----------|------------------------------|
  | 코드 구조화   | CommitSummary (Step별 region) |
  | Main 가독성 | CommitSummary (번호 주석)        |
  | 출력 추상화   | Local (헬퍼 함수)                |
  | 설정 관리    | Local ($script: 스코프)         |
  | 파라미터 검증  | Local (ValidateSet)          |
  | 도움말 유지보수 | Local (Here-String)          |
  | 에러 메시지   | CommitSummary (사용자 친화적)      |

  권장: 두 스크립트의 장점을 조합하여 공통 패턴 확립
```

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




```
● ReportDiagnostic은 소스 생성기가 컴파일러 경고/오류 메시지를 생성하는 방법입니다.

  작동 방식

  // 1. DiagnosticDescriptor 정의 (보통 static readonly로)
  private static readonly DiagnosticDescriptor InvalidCollectionTypeDescriptor = new(
      id: "FUNC001",                           // 고유 ID (예: CS0001처럼)
      title: "Invalid Collection Type",         // 제목
      messageFormat: "Type '{0}' is not a supported collection type",  // 메시지
      category: "Functorium.SourceGenerator",   // 카테고리
      defaultSeverity: DiagnosticSeverity.Warning,  // Warning, Error, Info, Hidden
      isEnabledByDefault: true);

  // 2. 문제 발생 시 보고
  context.ReportDiagnostic(Diagnostic.Create(
      descriptor: InvalidCollectionTypeDescriptor,
      location: syntax.GetLocation(),  // 문제가 있는 코드 위치
      messageArgs: typeFullName));     // 메시지 포맷 인수

  결과

  사용자가 빌드하면 IDE와 빌드 출력에 표시됩니다:

  MyService.cs(15,8): warning FUNC001: Type 'MyCustomType' is not a supported collection type

  장점

  | 예외 던지기            | ReportDiagnostic    |
  |-------------------|---------------------|
  | 빌드 실패 (원인 파악 어려움) | 정확한 위치와 메시지 표시      |
  | IDE 불안정           | IDE 정상 작동           |
  | 스택 트레이스만 제공       | 문제 코드 라인으로 바로 이동 가능 |

  현재 코드에 적용한다면

  현재 CollectionTypeHelper는 단순히 null을 반환하고 호출부에서 무시합니다. 만약 "컬렉션으로 표시했는데 지원 안 되는 타입"을 사용자에게 알려주고
  싶다면:

  if (param.IsCollection)
  {
      if (CollectionTypeHelper.GetCountExpression(param.Name, param.Type) is { } countExpression)
      {
          sb.Append($", {countExpression}");
      }
      else
      {
          // 사용자에게 경고 표시
          context.ReportDiagnostic(Diagnostic.Create(
              UnsupportedCollectionTypeDescriptor,
              parameterSyntax.GetLocation(),
              param.Type));
      }
  }

  현재 구현에서는 이런 상황이 발생할 가능성이 낮아서(IsCollection이 true면 GetCountExpression도 성공) 적용하지 않았지만, 더 복잡한 검증이 필요한 경우
  이 패턴을 사용하면 좋습니다.
```
```
● Functorium 라이브러리에 없어서 Mock으로 추가한 타입들입니다:

  Mock으로 추가한 타입 리스트

  | Mock 파일                   | 네임스페이스                                                     | 타입                     |
  |---------------------------|------------------------------------------------------------|------------------------|
  | IAdapterTrace.cs          | Functorium.Adapters.Observabilities                        | IAdapterTrace          |
  | IAdapterMetric.cs         | Functorium.Adapters.Observabilities                        | IAdapterMetric         |
  | MockAdapterTrace.cs       | Functorium.Adapters.Observabilities                        | MockAdapterTrace       |
  | MockAdapterMetric.cs      | Functorium.Adapters.Observabilities                        | MockAdapterMetric      |
  | ObservabilityFields.cs    | Functorium.Adapters.Observabilities.Abstractions.Fields    | ObservabilityFields    |
  | ElapsedTimeCalculator.cs  | Functorium.Adapters.Observabilities.Abstractions.Utilities | ElapsedTimeCalculator  |
  | UsecaseActivityContext.cs | Functorium.Applications.Observabilities                    | UsecaseActivityContext |

  참고:
  - ElapsedTimeCalculator는 Functorium.Applications에 이미 존재하지만, Source Generator가 Functorium.Adapters.Observabilities.Abstractions.Utilities
  네임스페이스를 사용
  - UsecaseActivityContext도 Functorium.Applications에 이미 존재하지만, Source Generator가 Functorium.Applications.Observabilities 네임스페이스를 사용

  Source Generator 코드가 참조하는 네임스페이스가 실제 라이브러리와 다르기 때문에 Mock이 필요합니다.

  또한 빌드 에러를 확인해보니 Source Generator 자체에도 문제가 있습니다. GetAllUsers 메서드의 반환 타입 IReadOnlyList<User>를 처리할 때 문법 에러가
  발생합니다. 이는 Source Generator의 버그로 보입니다.

  튜토리얼이 빌드되지 않는 이유는:
  1. LanguageExt 5.x API 변경: FinT.Succ, IO.Fail, FinT.LiftIO 메서드가 다름
  2. Source Generator 버그: 제네릭 타입 파라미터 처리 문제 (line 271, 279 등)

● Functorium에 없어서 Mock으로 추가한 타입들

  | Mock 파일                   | 네임스페이스                                                     | 타입                      |
  |---------------------------|------------------------------------------------------------|-------------------------|
  | IAdapterTrace.cs          | Functorium.Adapters.Observabilities                        | IAdapterTrace           |
  | IAdapterMetric.cs         | Functorium.Adapters.Observabilities                        | IAdapterMetric          |
  | MockAdapterTrace.cs       | Functorium.Adapters.Observabilities                        | MockAdapterTrace        |
  | MockAdapterMetric.cs      | Functorium.Adapters.Observabilities                        | MockAdapterMetric       |
  | ObservabilityFields.cs    | Functorium.Adapters.Observabilities.Abstractions.Fields    | ObservabilityFields     |
  | ElapsedTimeCalculator.cs  | Functorium.Adapters.Observabilities.Abstractions.Utilities | ElapsedTimeCalculator*  |
  | UsecaseActivityContext.cs | Functorium.Applications.Observabilities                    | UsecaseActivityContext* |

  참고 (*표시 항목):
  - ElapsedTimeCalculator - Functorium.Applications에 존재하지만, Source Generator는 Functorium.Adapters.Observabilities.Abstractions.Utilities
  네임스페이스를 사용
  - UsecaseActivityContext - Functorium.Applications에 존재하지만, Source Generator는 Functorium.Applications.Observabilities 네임스페이스를 사용

  결론: Source Generator가 생성하는 코드가 참조하는 네임스페이스가 실제 Functorium 라이브러리의 네임스페이스와 불일치합니다. Source Generator 코드
  수정이 필요합니다.
```