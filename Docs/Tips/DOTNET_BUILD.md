# .NET 빌드 명령어 가이드

## 패키지 복원

```bash
dotnet restore <solution-file>
```

NuGet 패키지 의존성을 복원합니다.

## 보안 취약점 검사

```bash
dotnet list <solution-file> package --vulnerable --include-transitive
```

| 옵션 | 설명 |
|------|------|
| `--vulnerable` | 알려진 보안 취약점이 있는 패키지 표시 |
| `--include-transitive` | 간접 의존성(transitive) 패키지도 검사 |

## 빌드

```bash
dotnet build <solution-file> --configuration Release --no-restore
```

| 옵션 | 설명 |
|------|------|
| `--configuration Release` | Release 구성으로 빌드 |
| `--no-restore` | 빌드 전 복원 단계 건너뛰기 (이미 복원된 경우) |
| `--nologo` | 시작 배너 숨기기 |
| `-v:q` | 출력 상세 수준: quiet (최소 출력) |
| `-p:VersionSuffix=<suffix>` | 버전 접미사 지정 (예: `dev-20251130-120000`) |

## 테스트 실행

### 프로젝트별 결과 생성

```bash
dotnet test <solution-file> \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --logger "trx" \
    --logger "console;verbosity=minimal" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

| 옵션 | 설명 |
|------|------|
| `--configuration Release` | Release 구성으로 테스트 |
| `--no-build` | 테스트 전 빌드 단계 건너뛰기 |
| `--collect:"XPlat Code Coverage"` | 코드 커버리지 수집 활성화 |
| `--logger "trx"` | TRX 형식으로 테스트 결과 저장 |
| `--logger "console;verbosity=minimal"` | 콘솔에 최소 출력 |
| `-- DataCollectionRunSettings...` | 커버리지 출력 형식을 Cobertura로 지정 |

> **참고:** `--results-directory` 옵션을 생략하면 각 테스트 프로젝트의 `TestResults/` 디렉토리에 결과가 생성되므로, `LogFilePrefix` 없이도 파일명 충돌이 발생하지 않습니다.

### TRX 로거 옵션

| 옵션 | 설명 | 파일명 예시 |
|------|------|-------------|
| `--logger "trx"` | 기본 (사용자명_컴퓨터명_타임스탬프) | `User_Machine_2025-11-30_14_13_45.trx` |
| `--logger "trx;LogFileName=results.trx"` | 고정 파일명 | `results.trx` |
| `--logger "trx;LogFilePrefix=testresults"` | 접두사 + 프레임워크 + 타임스탬프 | `testresults_net9.0_20251130141716.trx` |

> **참고:** 프로젝트별 결과 생성 시(`--results-directory` 생략)에는 기본 `--logger "trx"`로 충분합니다. 중앙 집중식 결과 생성 시에는 `LogFilePrefix`를 사용하여 파일명 충돌을 방지하세요.

## 커버리지 리포트 생성

### CLI 도구 사용

```bash
# 도구 설치
dotnet tool install -g dotnet-reportgenerator-globaltool

# 리포트 생성
reportgenerator \
    -reports:**/TestResults/**/coverage.cobertura.xml \
    -targetdir:.CoverageReport \
    -reporttypes:"Html;Cobertura;TextSummary;MarkdownSummaryGithub" \
    -assemblyfilters:"-*.Tests.*"
```

### GitHub Action 사용

```yaml
- name: Generate coverage report
  uses: danielpalme/ReportGenerator-GitHub-Action@v5.4.4
  with:
    reports: '**/TestResults/**/coverage.cobertura.xml'
    targetdir: '.CoverageReport'
    reporttypes: 'Html;Cobertura;TextSummary;MarkdownSummaryGithub'
    assemblyfilters: '-*.Tests.*'
```

### ReportGenerator 옵션

| 옵션 | 설명 |
|------|------|
| `-reports` | 커버리지 파일 경로 (glob 패턴 지원) |
| `-targetdir` | 리포트 출력 디렉토리 |
| `-reporttypes` | 출력 형식 (Html, Cobertura, TextSummary, MarkdownSummaryGithub 등) |
| `-assemblyfilters` | 어셈블리 필터 (`-`는 제외, `+`는 포함) |

### 주요 리포트 타입

| 타입 | 출력 파일 | 설명 |
|------|-----------|------|
| `Html` | `index.html` | 브라우저에서 볼 수 있는 상세 리포트 |
| `Cobertura` | `Cobertura.xml` | 병합된 커버리지 XML (CI 도구 연동) |
| `TextSummary` | `Summary.txt` | 텍스트 요약 (콘솔 출력용) |
| `MarkdownSummaryGithub` | `SummaryGithub.md` | GitHub Actions Job Summary용 마크다운 |

## 출력 구조

### 프로젝트별 결과 생성

`--results-directory` 옵션을 생략하면 각 테스트 프로젝트 디렉토리에 `TestResults/` 폴더가 생성됩니다.

```
<solution-root>/
├── Functorium.sln
├── Src/
│   └── Functorium/
│       ├── Functorium.csproj
│       └── bin/
│           └── Release/
│               └── net9.0/
│                   └── Functorium.dll
├── Tests/
│   ├── Functorium.Tests.Unit/
│   │   ├── Functorium.Tests.Unit.csproj
│   │   └── TestResults/                              # 테스트 프로젝트 1 결과
│   │       ├── {GUID}/
│   │       │   └── coverage.cobertura.xml            # 커버리지 데이터
│   │       └── User_Machine_2025-11-30_14_13_45.trx  # 테스트 결과
│   └── Functorium.Tests.Integration/
│       ├── Functorium.Tests.Integration.csproj
│       └── TestResults/                              # 테스트 프로젝트 2 결과
│           ├── {GUID}/
│           │   └── coverage.cobertura.xml            # 커버리지 데이터
│           └── User_Machine_2025-11-30_14_13_46.trx  # 테스트 결과
└── .CoverageReport/                                  # ReportGenerator 출력 (병합)
    ├── index.html                                    # HTML 리포트
    ├── Cobertura.xml                                 # 병합된 커버리지
    ├── Summary.txt                                   # 텍스트 요약
    └── SummaryGithub.md                              # GitHub Actions 요약
```

**장점:**
- 각 테스트 프로젝트의 결과가 해당 프로젝트 디렉토리에 위치
- 프로젝트별로 독립적인 테스트 결과 관리 가능
- `.gitignore`에서 `**/TestResults/` 패턴으로 일괄 제외 가능


### 폴더 구성 요소

| 경로 | 설명 |
|------|------|
| `TestResults/` | dotnet test가 생성하는 기본 결과 디렉토리 |
| `{GUID}/` | 각 테스트 실행마다 고유하게 생성되는 디렉토리 |
| `coverage.cobertura.xml` | XPlat Code Coverage가 생성하는 커버리지 데이터 |
| `*.trx` | Visual Studio Test Results 형식의 테스트 결과 |
| `.CoverageReport/` | ReportGenerator가 생성하는 병합된 리포트 |

> **참고:** 각 테스트 프로젝트마다 고유한 GUID 디렉토리가 생성되어 커버리지 파일이 충돌하지 않습니다. ReportGenerator가 `**/coverage.cobertura.xml` 패턴으로 모든 파일을 찾아 병합합니다.

## 참고

- [dotnet restore](https://learn.microsoft.com/dotnet/core/tools/dotnet-restore)
- [dotnet build](https://learn.microsoft.com/dotnet/core/tools/dotnet-build)
- [dotnet test](https://learn.microsoft.com/dotnet/core/tools/dotnet-test)
- [dotnet list package](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package)
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator)
- [ReportGenerator GitHub Action](https://github.com/danielpalme/ReportGenerator-GitHub-Action)
