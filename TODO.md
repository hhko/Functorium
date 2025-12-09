## Feature
- [x] Error
  - ErrorCodeFactory
    - ErrorCodeExpected
    - ErrorCodeExceptional
  - Serilog
    - Serilog.Core.IDestructuringPolicy
    - IErrorDestructurer
- [x] Option
  - GetOptions
  - RegisterConfigureOptions
  - IStartupOptionsLoggable
    - StartupLogger : IHostedService
- [x] Observability 의존성 등록
  - OpenTelemetryOptions
  - OpenTelemetryBuilder
    - LoggerOpenTelemetryBuilder
    - TraceOpenTelemetryBuilder
    - MetricOpenTelemetryBuilder
- [ ] Mediator 패턴 Pipeline
- [ ] ValueObject

## TODO
- [x] claude: claude commit 명령어
- [x] doc: 아키텍처 문서
- [x] doc: git command guide 문서: `Git-Commands.md`
- [x] doc: git commit guide 문서: `Git-Commit.md`
- [x] doc: guide 문서 작성을 위한 guide 문서: `Guide-Writing.md`
- [x] dev: 솔루션 구성
- [x] dev: 코드 품질((코드 스타일, 코드 분석 규칙)) 빌드 통합: .editorconfig을 이용한 "코드 품질" 컴파일 과정 통합
- [x] doc: 코드 품질((코드 스타일, 코드 분석 규칙)) 빌드 통합 문서: `Code-Quality.md`
- [x] doc: DOTNET SDK 빌드 명시 문서: `Build-SdkVersion-GlobalJson.md
- [x] dev: 솔루션 구성: global.json SDK 버전 허용 범위 지정
- [X] dev: 솔루션 구성: nuget.config 파일 생성
- [x] dev: 커밋 이력 ps1
- [x] dev: Build-CommitSummary 대상 브랜치 지정
- [x] dev: Build-CommitSummary 커밋 작성자 추가
- [x] dev: Build-CommitSummary 커밋 소스 브랜치 추가
- [x] dev: Build-CommitSummary 태그 없을 때 버그 수정
- [x] dev: Build-CommitSummary 출력 경로 매개변수화
- [x] dev: Build-CommitSummary 타겟 브랜치 이름 출력
- [ ] dev: Build-CommitSummary --no-merges
- [x] claude: commit 주제 전달일 때는 주체만 commit하기
- [x] dev: ci.yml -> build.yml
- [x] dev: build.yml 실패 처리
- [x] std: MinVer 이해(형상관리 tag 연동)
- [x] dev: 로컬 빌드
- [x] dev: GitHub actions build
- [x] dev: GitHub actions publish
- [x] doc: GitHub actions 문서
- [x] dev: Functorium.Testing 프로젝트 소스 추가
- [x] dev: Functorium.Testing xunit.v3 기반으로 패키지 참조 및 소스 개선
- [x] dev: 패키지 .NET 10 기준으로 최신 버전 업그레이드
- [x] doc: 단위 테스트 가이드 문서
- [x] dev: Build-Local.ps1 dotnet cli 외부 명령 출력이 버퍼링되어 함수 반환 시까지 표시되지 않는 븍구 수정(| Out-Host)
- [x] dev: ErrorCode 개발 이해
- [x] dev: ErrorCode 테스트 자동화 이해
- [x] dev: Build-VerifyAccept.ps1 파일
- [x] dev: ps1 공통 모듈 분리
- [x] doc: ps1 파일 작성 가이드
- [ ] dev: ps1 출력을 실시간 처리하기 위해 명령을 함수 밖으로 이동 배치
- [x] dev: 관찰 가능성 의존성 등록 코드: Logger, Trrace, Metric
- [ ] dev: 관찰 가능성 의존성 등록 리뷰
- [ ] doc: 옵션, 로그 출력 중심으로 문서화
- [ ] std: Functorium.Testing 애해: 아키텍처 단위 테스트
- [ ] std: Functorium.Testing 애해:  구조적 로그 단위 테스팅
- [ ] std: Functorium.Testing 애해:  WebApi 통합 테스트
- [ ] std: Functorium.Testing 애해:  ScheduledJob 통합 테스트
- [ ] DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true, DOTNET_CLI_TELEMETRY_OPTOUT: true 이해
- [ ] powershell 학습 문서
- [ ] powershell 가이드 문서
- [ ] powershell 가이드 문서 기준 개선
- [ ] 로컬 빌드 문서(dotnet 명령어)
- [ ] 솔루션 구성: .editorconfig 폴더 단위 개별 지정
- [ ] 솔루션 구성: Directory.Packages.props 하위 폴더 새로 시작, 버전 재정의
- [ ] **dev: nuget 배포을 위한 프로젝트 설정**
- [ ] **dev: publish.yml 파일로 NuGet 배포**
- [ ] **dev: publish.yml 파일로 Release 배포**

Item                                      | Type    | File                          | todo
---                                       | ---     | ---                           | ---
Build-CommitSummary.ps1                   | Manual  | Build-CommitSummary.md        | done
Build-Local.ps1                           | Manual  | Build-Local.md                | done
.claude/commands/commit.md                | Manual  | Command-Commit.md             | done
.claude/commands/suggest-next-version.md  | Manual  | Command-SuggestNextVersion.md | x
.editorconfig                             |         | Code-Quality.md               |
CLAUDE.md                                 |         |                               |
Directory.Build.props                     |         |                               |
Directory.Packages.props                  |         |                               |
global.json                               |         | SdkVersion.md                 |
nuget.config                              |         |                               |
.github/workflows/build.yml               |         | GitHub Actions.md             |
.github/workflows/publish.md              |         | GitHub Actions.md             |
                                          |         | Git.md                        |
                                          |         | MinVer.md                     |
                                          |         | UnitTesting.md                |
                                          |         | xUnitV3.md                    |

- [x] Language-Ext 업그레이드
  - `FinT<M, A>.Lift(fin)` → `FinT.lift<M, A>(fin)`
  - `Fin<A>.Succ` (메서드 참조) → `Fin.Succ` (람다 사용)
  - `Fin<A>.Fail(error)` → `Fin.Fail<A>(error)`
  - `FinT<M, A>.Fail(error)` → `FinT.Fail<M, A>(error)`