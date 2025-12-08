# Build-Local.ps1 스크립트 매뉴얼

이 문서는 `Build-Local.ps1` 스크립트의 사용법과 기능을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [설치 및 요구사항](#설치-및-요구사항)
- [사용법](#사용법)
- [실행 단계](#실행-단계)
- [출력 구조](#출력-구조)
- [커버리지 리포트](#커버리지-리포트)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

.NET 솔루션의 빌드, 테스트, 코드 커버리지 수집 및 리포트 생성을 자동화합니다.

### 주요 기능

| 기능 | 설명 |
|------|------|
| 솔루션 빌드 | Release 모드로 전체 솔루션 빌드 |
| 버전 정보 표시 | MinVer 기반 버전 정보 출력 |
| 테스트 실행 | 모든 테스트 프로젝트 실행 |
| 커버리지 수집 | XPlat Code Coverage로 커버리지 수집 |
| HTML 리포트 | ReportGenerator로 HTML 리포트 생성 |
| 콘솔 요약 | 프로젝트별, 레이어별 커버리지 요약 출력 |

### 파일 구조

```
프로젝트루트/
├── Build-Local.ps1           # 스크립트
├── *.sln 또는 *.slnx         # 솔루션 파일
└── .coverage/                # 출력 디렉토리 (자동 생성)
    ├── index.html            # HTML 리포트
    └── Cobertura.xml         # 병합된 커버리지 데이터
```

<br/>

## 요약

### 주요 명령

**기본 실행:**
```powershell
./Build-Local.ps1
```

**솔루션 지정:**
```powershell
./Build-Local.ps1 -Solution ./MyApp.sln
./Build-Local.ps1 -s ../Other.slnx
```

**프로젝트 접두사 지정:**
```powershell
./Build-Local.ps1 -ProjectPrefix MyApp
./Build-Local.ps1 -p Functorium
```

**도움말:**
```powershell
./Build-Local.ps1 -Help
./Build-Local.ps1 -h
```

### 매개변수

| 매개변수 | 별칭 | 설명 | 기본값 |
|----------|------|------|--------|
| `-Solution` | `-s` | 솔루션 파일 경로 | 자동 검색 |
| `-ProjectPrefix` | `-p` | 커버리지 필터링용 프로젝트 접두사 | `Functorium` |
| `-Help` | `-h`, `-?` | 도움말 표시 | - |

<br/>

## 설치 및 요구사항

### 필수 요구사항

| 항목 | 버전 | 비고 |
|------|------|------|
| PowerShell | 7.0 이상 | `#Requires -Version 7.0` |
| .NET SDK | 설치 필요 | `dotnet` 명령 사용 |
| ReportGenerator | 5.5.0 | 자동 설치/업데이트 |

### ReportGenerator 자동 설치

스크립트 실행 시 ReportGenerator가 자동으로 설치 또는 업데이트됩니다:

```powershell
# 설치 확인
dotnet tool list -g | Select-String "reportgenerator"

# 수동 설치 (필요 시)
dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.0
```

<br/>

## 사용법

### 기본 사용

현재 디렉토리에서 솔루션 파일을 자동 검색하여 실행:

```powershell
cd C:\Workspace\Github\Functorium
./Build-Local.ps1
```

### 솔루션 파일 지정

여러 솔루션 파일이 있거나 다른 경로의 솔루션을 빌드할 때:

```powershell
# 절대 경로
./Build-Local.ps1 -Solution C:\Projects\MyApp\MyApp.sln

# 상대 경로
./Build-Local.ps1 -s ./src/MyApp.slnx
```

### 프로젝트 접두사 필터링

커버리지 리포트에서 특정 프로젝트만 필터링:

```powershell
# Functorium.* 프로젝트만 필터링 (기본값)
./Build-Local.ps1

# MyApp.* 프로젝트만 필터링
./Build-Local.ps1 -ProjectPrefix MyApp
```

<br/>

## 실행 단계

스크립트는 다음 순서로 실행됩니다:

```
1. Find Solution      솔루션 파일 검색
        ↓
2. Build Solution     Release 모드 빌드
        ↓
3. Show Version       버전 정보 표시
        ↓
4. Run Tests          테스트 실행 + 커버리지 수집
        ↓
5. Merge Coverage     커버리지 파일 병합
        ↓
6. Generate Report    HTML 리포트 생성
        ↓
7. Show Summary       콘솔에 결과 요약 출력
```

### 1. 솔루션 파일 검색

- `-Solution` 매개변수가 있으면 해당 파일 사용
- 없으면 현재 디렉토리에서 `.sln` 또는 `.slnx` 파일 검색
- 솔루션 파일이 0개 또는 2개 이상이면 오류

### 2. 솔루션 빌드

```powershell
dotnet build $SolutionPath -c Release --nologo -p:MinVerVerbosity=normal
```

- Release 구성으로 빌드
- `dotnet build`가 자동으로 패키지 복원 수행
- MinVer 버전 정보 출력 (MinVer 패키지 설정된 경우)

### 3. 버전 정보 표시

빌드된 어셈블리에서 버전 정보를 읽어 표시:

```
Project                                  ProductVer                          FileVer         Assembly
-----------------------------------------------------------------------------------------------------------
Functorium                               1.0.0-alpha.0.15+abc123             1.0.0.0         1.0.0.0
Functorium.Testing                       1.0.0-alpha.0.15+abc123             1.0.0.0         1.0.0.0
```

| 버전 속성 | 설명 |
|-----------|------|
| ProductVer | InformationalVersion (MinVer 전체 버전) |
| FileVer | FileVersion (파일 속성) |
| Assembly | AssemblyVersion (바이너리 호환성) |

### 4. 테스트 실행

```powershell
dotnet test $SolutionPath `
    --configuration Release `
    --no-build `
    --nologo `
    --collect:"XPlat Code Coverage" `
    --logger "trx" `
    --logger "console;verbosity=minimal" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

- 기존 `.coverage/` 및 `TestResults/` 디렉토리 삭제 후 실행
- Cobertura 형식으로 커버리지 수집

### 5. 커버리지 병합

여러 테스트 프로젝트의 커버리지 파일을 검색하여 병합 준비

### 6. HTML 리포트 생성

```powershell
reportgenerator `
    -reports:$CoverageFiles `
    -targetdir:.coverage `
    -reporttypes:"Html;Cobertura" `
    -assemblyfilters:"-*.Tests*"
```

- 테스트 프로젝트는 리포트에서 제외

### 7. 콘솔 요약

세 가지 관점의 커버리지 요약:

1. **Project Coverage**: 지정된 접두사 프로젝트
2. **Core Layer Coverage**: Domain + Application 레이어
3. **Full Coverage**: 전체 프로젝트 (테스트 제외)

<br/>

## 출력 구조

### 디렉토리 구조

```
{SolutionDir}/
├── .coverage/                        # 커버리지 리포트
│   ├── index.html                    # HTML 리포트 진입점
│   ├── Cobertura.xml                 # 병합된 커버리지 데이터
│   └── ...                           # 기타 HTML 파일들
│
└── Tests/
    └── {TestProject}/
        └── TestResults/
            ├── {GUID}/
            │   └── coverage.cobertura.xml  # 원본 커버리지
            └── *.trx                       # 테스트 결과
```

### HTML 리포트 열기

```powershell
# Windows
start .coverage/index.html

# macOS
open .coverage/index.html

# Linux
xdg-open .coverage/index.html
```

<br/>

## 커버리지 리포트

### 콘솔 출력 예시

```
[START] .NET Solution Build and Test
       Started: 2025-01-15 14:30:00

[1/7] Finding solution file...
      Found: Functorium.slnx
      Coverage output: C:\Workspace\Github\Functorium\.coverage
[2/7] Building solution (Release)...

  복원할 프로젝트를 확인하는 중...
  Functorium -> C:\...\Functorium.dll
  빌드했습니다.
    경고 0개
    오류 0개

[3/7] Reading version information...

Project                                  ProductVer                          FileVer         Assembly
-----------------------------------------------------------------------------------------------------------
Functorium                               1.0.0-alpha.0.54+abc123...          1.0.0.0         1.0.0.0
Functorium.Testing                       1.0.0-alpha.0.54+abc123...          1.0.0.0         1.0.0.0

[4/7] Running tests with coverage...
      Tests passed
[5/7] Merging coverage reports...
      Found 2 coverage file(s)
[6/7] Generating HTML report...
      ReportGenerator installed: v5.5.0
      Report generated: C:\...\index.html
[7/7] Displaying coverage results...

[Project Coverage] (Functorium.*)
Assembly                                   Line Coverage Branch Coverage
------------------------------------------------------------------------
Functorium                                          85.3%           72.1%
Functorium.Testing                                  91.2%           80.5%
------------------------------------------------------------------------
Total                                               88.2%           76.3%

[Core Layer Coverage] (Domains + Applications)
Assembly                                   Line Coverage Branch Coverage
------------------------------------------------------------------------
(해당 프로젝트가 있는 경우 표시)

[Full Coverage]
Assembly                                   Line Coverage Branch Coverage
------------------------------------------------------------------------
Functorium                                          85.3%           72.1%
Functorium.Testing                                  91.2%           80.5%
------------------------------------------------------------------------
Total                                               89.3%           79.3%

[DONE] Build and test completed
       Duration: 01:23
       Report: .coverage/index.html
```

### 커버리지 분류

| 분류 | 포함 패턴 | 설명 |
|------|-----------|------|
| Project | `{Prefix}.*` | 지정된 접두사로 시작하는 프로젝트 |
| Core Layer | `*.Domain`, `*.Domains`, `*.Application`, `*.Applications` | 핵심 비즈니스 레이어 |
| Full | 전체 (테스트 제외) | 모든 프로덕션 코드 |

<br/>

## 트러블슈팅

### 솔루션 파일을 찾을 수 없음

```
No solution file (.sln or .slnx) found in: C:\Projects
```

**해결:**
```powershell
# 솔루션 파일 직접 지정
./Build-Local.ps1 -Solution ./src/MyApp.sln
```

### 여러 솔루션 파일 발견

```
Found 2 solution files:
  - App1.sln
  - App2.sln
Use -Solution parameter to specify which one to use.
```

**해결:**
```powershell
./Build-Local.ps1 -Solution ./App1.sln
```

### 빌드 실패

빌드 실패 시 `dotnet build` 출력이 콘솔에 직접 표시됩니다.

**해결:**
```powershell
# 에러 메시지 확인 후 코드 수정

# 상세 빌드 로그 확인
dotnet build -v detailed

# 클린 후 재빌드
dotnet clean
./Build-Local.ps1
```

### 테스트 실패

```
Error: Tests failed
```

**해결:**
```powershell
# 개별 테스트 실행으로 실패 원인 확인
dotnet test --filter "FullyQualifiedName~TestClassName"
```

### ReportGenerator 설치 실패

```
Failed to install ReportGenerator
```

**해결:**
```powershell
# 수동 설치
dotnet tool install -g dotnet-reportgenerator-globaltool

# PATH 새로고침 후 재실행
$env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "User") + ";" + $env:PATH
```

### 커버리지 파일 없음

```
No coverage files found. Cannot generate report.
```

**해결:**
- 테스트 프로젝트에 `coverlet.collector` 패키지가 설치되어 있는지 확인
- 테스트가 실제로 실행되었는지 확인

<br/>

## FAQ

### Q1. PowerShell 7이 필요한 이유는?

**A:** 스크립트가 PowerShell 7.0 이상의 기능을 사용합니다:
- `#Requires -Version 7.0` 지시문
- 향상된 오류 처리
- 크로스 플랫폼 호환성

```powershell
# 버전 확인
$PSVersionTable.PSVersion
```

### Q2. 특정 테스트만 실행하려면?

**A:** 이 스크립트는 전체 테스트 실행용입니다. 특정 테스트만 실행하려면:

```powershell
# 필터로 특정 테스트 실행
dotnet test --filter "Category=Unit"
dotnet test --filter "FullyQualifiedName~MyTest"
```

### Q3. 커버리지 임계값을 설정하려면?

**A:** 이 스크립트는 리포트 생성만 합니다. CI/CD에서 임계값 검사는 별도로 설정하세요:

```yaml
# GitHub Actions 예시
- name: Check coverage
  run: |
    $coverage = [xml](Get-Content .coverage/Cobertura.xml)
    $lineRate = [double]$coverage.coverage.'line-rate' * 100
    if ($lineRate -lt 80) { exit 1 }
```

### Q4. Debug 모드로 빌드하려면?

**A:** 스크립트를 수정하거나 직접 명령 실행:

```powershell
dotnet build -c Debug
dotnet test -c Debug --collect:"XPlat Code Coverage"
```

### Q5. 리포트 출력 경로를 변경하려면?

**A:** 현재 스크립트는 `.coverage` 디렉토리를 사용합니다. 변경하려면 스크립트의 `$script:CoverageReportDir` 값을 수정하세요.


## 참고 자료

- [ReportGenerator](https://github.com/danielpalme/ReportGenerator)
- [Coverlet](https://github.com/coverlet-coverage/coverlet)
- [MinVer](https://github.com/adamralph/minver)
- [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
