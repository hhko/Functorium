## Feature
- [x] Error
  - ErrorCodeFactory
    - ErrorCodeExpected
    - ErrorCodeExceptional
  - Serilog
    - Serilog.Core.IDestructuringPolicy
    - IErrorDestructurer
- [x] Option
  - OptionsConfigurator
    - GetOptions
    - RegisterConfigureOptions
- [x] Observability 의존성 등록
  - OpenTelemetryOptions
  - OpenTelemetryBuilder
    - LoggerOpenTelemetryBuilder
    - TraceOpenTelemetryBuilder
    - MetricOpenTelemetryBuilder
  - Logging
    - IStartupOptionsLogger
    - StartupLogger : IHostedService
- [ ] Mediator 패턴 Pipeline
- [ ] ValueObject
- [x] Example: Observability 로그 출력
- [ ] VSCode 개발 환경
  - 확장 도구
    - .NET Install Tool
	    - C#
	    - C# Dev Kit
    - Coverage Gutters
    - Test Explorer UI
      - .Net Core Test Explorer
    - Remote Development
	    - Remote - SSH
	    - Remote - Tunnels
	    - Dev Containers
	    - WSL
    - GitHub Actions
    - Markdown??
    - REST Client Api
    - Peek Hidden Files
    - Paste Image
    - Trailing Spaces
    - Code Spell Checker
      ```
    	"cSpell.ignoreWords": [
        "Functorium",
        "Observabilities"
      ]
      ```
  - .vscode
    - launch.json: VSCode 디버깅 환경 설정
    - settings.json
    - tasks.json

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
- OpenTelemetryOptions
  - [x] OpenTelemetryOptions 문서
  - [x] OpenTelemetryOptions 의존성 등록
  - [x] 용어 정리
    - logging 접두사
    - logger 접미사
  - [ ] Observability 의존성 등록과 Builder 관련 테스트
  - [ ] Serilog Destructure 깊이, 배열, ... 제약 조건 테스트
- NuGet 패키지
  - [x] NuGet 배포를 위한 프로젝트 설정
  - [x] 로컬 NuGet 패키지 배포 스크립트
  - [x] publish.yml 개선
  - [x] NuGet 문서
  - [x] NuGet 계정
  - [x] png 아이콘
  - [ ] Release 노트 생성기?
  - [ ] Release 배포
  - [ ] NuGet 배포
- Example 관찰 가능성
  - [x] 예제 프로젝트 구성
  - [x] 소스 정리
  - [x] 로그
  - [x] FtpOptions Startup 로그
  - [x] FtpOptions 통합 테스트
  - [x] OpenTelemetryOptions 통합 테스트
  - [x] 통합 테스트 문서
  - [x] .vscode 구성 문서
- Dashboard
  - [x] Aspire 대시보드 구성
  - [x] OpenSearch 대시보드 구성
- Release Notes 자동화
  - [x] Aspire Release Notes 자동화 이해
  - [x] analyze-all-components.sh/ps1 포팅
  - [x] analyze-folder.sh/ps1 포팅
  - [x] extract-api-changes.sh/ps1 포팅
  - [x] GenApi -> PublicApiGenerator 패키지 교체
  - [x] Docs 폴더 .md 문서 한글화
  - [x] Docs 폴더 포팅 출력 기준 업데이트
  - [ ] PublicApiGenerator 단일 파일 코드 .NET 10
  - [ ] analyze-all-components.sh/ps1 이전 결과 삭제
  - [x] 릴리스 노트 자동 생성을 위한 AI 프럼프트
    ```
    1단계: 데이터 수집 (사람이 먼저 실행)

      # 컴포넌트 분석
      ./analyze-all-components.sh origin/release/9.4 origin/main

      $FIRST_COMMIT = git rev-list --max-parents=0 HEAD
      .\analyze-all-components.ps1 -BaseBranch $FIRST_COMMIT -TargetBranch origin/main

      # API 변경사항 추출
      ./extract-api-changes.sh

    2단계: AI에게 요청

      @Tools\ReleaseNotes\Docs\  폴더의 모든 문서를 참고하여
      analysis-output/ 폴더에 있는 분석 결과를 기반으로
      Functorium {version} 릴리스 노트를 작성해줘.

      핵심 원칙:
      1. 모든 API는 all-api-changes.txt (Uber 파일)에서 검증할 것
      2. API를 임의로 만들어내지 말 것
      3. 모든 기능은 커밋/PR로 추적 가능해야 함
      4. writing-guidelines.md의 템플릿 구조를 따를 것
      5. validation-checklist.md로 최종 검증할 것

      문서 구조의 의도

      | 문서                      | AI에게 주는 역할           |
      |-------------------------|----------------------|
      | data-collection.md      | 입력 데이터 위치와 구조 설명     |
      | commit-analysis.md      | 커밋 → 기능 변환 방법        |
      | api-documentation.md    | API 검증 및 코드 샘플 작성 규칙 |
      | writing-guidelines.md   | 출력 문서 템플릿과 스타일       |
      | validation-checklist.md | 품질 검증 체크리스트          |
    ```
  - [ ] 세부 버전까지 표시
    - 파일명: RELEASE-v1.0.0-alpha.0.132.md
    - 문서: v1.0.0-alpha.0 <--- x
  - [ ] markdownlint-cli@0.45.0 사전 설치
  - [x] ExtractApiChanges 이모지 제거
  - [x] ExtractApiChanges CommandLine 패키지 버전 업그레이드
  - [x] ExtractApiChanges.cs 정렬
  - [x] ExtractApiChanges.cs .api 비교
  - [x] ExtractApiChanges 콘솔 출력 색상
  - [x] analyze-all-components.ps1, analyze-folder 포팅
  - [x] ~~Docs -> Reference 폴더 이름 변경~~
  - [x] .NET 10 File-based 실행 오류 개선
  - [x] ReleaseNotes 문서 내용 업데이트
  - [x] Tools/ReleaseNotes -> .release-notes/script로 이동
  - [x] aspire 릴리스 노트 한글화
  - [x] AnalyzeAllComponents.cs base branch 유효성 검사 추가
  - [x] config -> Config
  - [x] 첫 배포일 때 cli 명령어를 한줄로(스크립트 변수 제거)
  - [x] analysis_output -> .analysis_output
  - [x] C# 10 File-based 트러블슈팅 추가
  - [x] 스크립트에서 사용한 Git 명령어 문서 추가 반영(Git.md)
  - [x] PublicApiGenerator 패키지 버전 최신화
  - [x] commit 타입 통일 시킴
    - commit.md
    - data-collection.md
    - AnalyzeAllComponents.cs
  - [x] dotnet tool 설치 방법 변경
    - 변경 전: ps1 파일을 이용해서 명시적 도구 설치
    - 변경 후: .config/dotnet-tools.json
  - [x] .config/dotnet-tools.json 에서 사용하지 않는 도구 제거 publicapigenerator.tool
  - [x] .gitignore 자동 생성 폴더 추가 .release-notes/scripts/.analysis-output/
  - [ ] 릴리즈 노트 생성
  - [ ] 버전?
  - [ ] 브랜치 전략?
  - [ ] GitHub Release 배포
  - [ ] NuGet 배포
  - [ ] git 명령어 시나리오
- [ ] .config 폴더를 이용해서 ReportGenerator 설치(ps1 파일 개선)

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

```shell
rm -rf ~/AppData/Local/Temp/dotnet/runfile/ExtractApiChanges*
%LOCALAPPDATA%\Temp\dotnet\runfile\<스크립트명>*
dotnet ExtractApiChanges.cs

# 실행 전 캐시 정리 스크립트
  Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Temp\dotnet\runfile\*" -ErrorAction SilentlyContinue


● 파일 잠금 해결 방법

  1. 프로세스 확인 및 종료

  # 잠금 프로세스 확인
  tasklist | findstr "ExtractApiChanges"
  tasklist | findstr "ApiGenerator"
  tasklist | findstr "AnalyzeAllComponents"

  # 프로세스 강제 종료
  taskkill /F /IM ExtractApiChanges.exe
  taskkill /F /IM ApiGenerator.exe

  2. 캐시 폴더 삭제

  # 특정 스크립트 캐시만 삭제
  Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Temp\dotnet\runfile\ExtractApiChanges*"

  # 모든 file-based program 캐시 삭제
  Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Temp\dotnet\runfile\*"

  3. 그래도 안 되면

  # 파일 잠금 프로세스 찾기 (Handle 도구 필요)
  handle.exe "ExtractApiChanges"

  # 또는 Resource Monitor 사용
  resmon.exe
  # CPU → Associated Handles → 검색

  4. 최후의 수단

  # 재부팅 없이 Explorer 재시작
  taskkill /F /IM explorer.exe
  start explorer.exe

  # 또는 PC 재시작

  권장: 실행 스크립트에 정리 로직 추가

  # run-extract-api.ps1
  $scriptName = "ExtractApiChanges"
  $cachePattern = "$env:LOCALAPPDATA\Temp\dotnet\runfile\$scriptName*"

  # 기존 프로세스 종료
  Get-Process -Name $scriptName -ErrorAction SilentlyContinue | Stop-Process -Force

  # 캐시 정리
  Remove-Item -Recurse -Force $cachePattern -ErrorAction SilentlyContinue

  # 실행
  dotnet ExtractApiChanges.cs

```