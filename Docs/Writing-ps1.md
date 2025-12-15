# PowerShell 스크립트 개발 가이드

이 문서는 프로젝트에서 PowerShell 스크립트를 개발하기 위한 표준 패턴과 공통 모듈 사용법을 설명합니다.

## 목차
- [개요](#개요)
- [프로젝트 구조](#프로젝트-구조)
- [공통 모듈](#공통-모듈)
- [스크립트 템플릿](#스크립트-템플릿)
- [코딩 규칙](#코딩-규칙)
- [출력 패턴](#출력-패턴)
- [에러 처리](#에러-처리)
- [예제 스크립트](#예제-스크립트)
- [FAQ](#faq)

<br/>

## 개요

### 목적

프로젝트의 빌드, 테스트, 배포 등 자동화 작업을 위한 PowerShell 스크립트 개발 표준을 정의합니다.

### 요구사항

- PowerShell 7.0 이상
- Windows, macOS, Linux 지원

### 주요 특징

| 특징 | 설명 |
|------|------|
| **모듈화** | 공통 함수를 `.scripts/` 폴더에 분리 |
| **일관된 출력** | 표준화된 콘솔 출력 패턴 |
| **에러 처리** | try-catch 기반 예외 처리 |
| **도움말** | `-Help` 파라미터 지원 |

<br/>

## 프로젝트 구조

### 디렉토리 구조

```
프로젝트 루트/
├── .config/
│   └── dotnet-tools.json        # .NET 로컬 도구 정의
├── .scripts/                    # 공통 모듈
│   └── Write-Console.ps1        # 콘솔 출력 함수
├── Build-Local.ps1              # 빌드 및 테스트 스크립트
├── Build-VerifyAccept.ps1       # Verify 스냅샷 승인 스크립트
└── Build-CommitSummary.ps1      # 커밋 요약 생성 스크립트
```

### 파일 명명 규칙

| 유형 | 패턴 | 예시 |
|------|------|------|
| 빌드 스크립트 | `Build-*.ps1` | `Build-Local.ps1` |
| 배포 스크립트 | `Deploy-*.ps1` | `Deploy-Production.ps1` |
| 유틸리티 스크립트 | `Invoke-*.ps1` | `Invoke-Migration.ps1` |
| 공통 모듈 | `.scripts/*.ps1` | `.scripts/Write-Console.ps1` |

<br/>

## 공통 모듈

### Write-Console.ps1

콘솔 출력 관련 공통 함수를 제공합니다.

#### 사용 방법

```powershell
# 모듈 로드
$scriptRoot = $PSScriptRoot
. "$scriptRoot/.scripts/Write-Console.ps1"
```

#### 함수 목록

| 함수 | 설명 | 색상 |
|------|------|------|
| `Write-StepProgress` | 단계별 진행 상황 | Gray |
| `Write-Detail` | 상세 정보 | DarkGray |
| `Write-Success` | 성공 메시지 | Green |
| `Write-WarningMessage` | 경고 메시지 | Yellow |
| `Write-StartMessage` | 시작 메시지 | Blue |
| `Write-DoneMessage` | 완료 메시지 | Green |
| `Write-ErrorMessage` | 에러 메시지 | Red |

#### 함수 사용 예시

```powershell
# 단계별 진행 상황
Write-StepProgress -Step 1 -TotalSteps 5 -Message "Building solution..."
# 출력: [1/5] Building solution...

# 상세 정보
Write-Detail "Found 3 test projects"
# 출력:       Found 3 test projects

# 성공 메시지
Write-Success "Build completed successfully"
# 출력:       Build completed successfully

# 경고 메시지
Write-WarningMessage "No tests found"
# 출력:       No tests found

# 시작/완료 메시지
Write-StartMessage -Title "Build Process"
# 출력: [START] Build Process

Write-DoneMessage -Title "Build completed"
# 출력: [DONE] Build completed

# 에러 메시지
Write-ErrorMessage -ErrorRecord $_
# 출력: [ERROR] An unexpected error occurred:
#          {에러 메시지}
#       Stack trace:
#          {스택 트레이스}
```

### .NET 도구 관리

프로젝트는 `.config/dotnet-tools.json`을 통해 .NET 로컬 도구를 관리합니다.

#### dotnet-tools.json 구조

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-reportgenerator-globaltool": {
      "version": "5.5.0",
      "commands": ["reportgenerator"],
      "rollForward": false
    },
    "verify.tool": {
      "version": "0.7.0",
      "commands": ["verify"],
      "rollForward": false
    }
  }
}
```

#### 도구 복원

스크립트에서 도구를 사용하기 전에 복원합니다:

```powershell
function Restore-DotNetTools {
  Write-StepProgress -Step 1 -TotalSteps $script:TOTAL_STEPS -Message "Restoring .NET tools..."

  dotnet tool restore 2>&1 | Out-Null

  if ($LASTEXITCODE -eq 0) {
    Write-Success "Tools restored"
  }
  else {
    Write-WarningMessage "Tool restore failed or no tools to restore"
  }
}
```

#### 로컬 도구 실행

복원된 도구는 `dotnet` 명령어로 실행합니다:

```powershell
# ReportGenerator 실행
dotnet reportgenerator -reports:coverage.xml -targetdir:report/

# Verify 실행
dotnet verify accept -y
```

#### 새 도구 추가

1. `.config/dotnet-tools.json`에 도구 추가
2. `dotnet tool restore` 실행하여 설치
3. 스크립트에서 `dotnet {command}` 형식으로 실행

<br/>

## 스크립트 템플릿

### 기본 템플릿

새 스크립트 작성 시 아래 템플릿을 사용하세요:

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  스크립트에 대한 간단한 설명

.DESCRIPTION
  스크립트에 대한 상세 설명
  - 기능 1
  - 기능 2

.PARAMETER Help
  도움말을 표시합니다.

.EXAMPLE
  ./Build-Example.ps1
  기본 실행

.EXAMPLE
  ./Build-Example.ps1 -Help
  도움말 표시

.NOTES
  Version: 1.0.0
  Requirements: PowerShell 7+, .NET SDK
  License: MIT
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, HelpMessage = "도움말 표시")]
  [Alias("h", "?")]
  [switch]$Help
)

# Strict mode settings
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Set console encoding to UTF-8 for proper Korean character display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Load common modules
$scriptRoot = $PSScriptRoot
. "$scriptRoot/.scripts/Write-Console.ps1"

#region Constants

$script:TOTAL_STEPS = 3

#endregion

#region Helper Functions

<#
.SYNOPSIS
  도움말을 표시합니다.
#>
function Show-Help {
  $help = @"

================================================================================
 Script Title
================================================================================

DESCRIPTION
  스크립트 설명

USAGE
  ./Build-Example.ps1 [options]

OPTIONS
  -Help, -h, -?      Show this help message

EXAMPLES
  # 기본 실행
  ./Build-Example.ps1

================================================================================
"@
  Write-Host $help
}

#endregion

#region Step 1: Step-Name

<#
.SYNOPSIS
  첫 번째 단계
#>
function Invoke-Step1 {
  Write-StepProgress -Step 1 -TotalSteps $script:TOTAL_STEPS -Message "Step 1..."

  # 구현
  Write-Detail "Processing..."
  Write-Success "Step 1 completed"
}

#endregion

#region Step 2: Step-Name

<#
.SYNOPSIS
  두 번째 단계
#>
function Invoke-Step2 {
  Write-StepProgress -Step 2 -TotalSteps $script:TOTAL_STEPS -Message "Step 2..."

  # 구현
  Write-Detail "Processing..."
  Write-Success "Step 2 completed"
}

#endregion

#region Step 3: Step-Name

<#
.SYNOPSIS
  세 번째 단계
#>
function Invoke-Step3 {
  Write-StepProgress -Step 3 -TotalSteps $script:TOTAL_STEPS -Message "Step 3..."

  # 구현
  Write-Detail "Processing..."
  Write-Success "Step 3 completed"
}

#endregion

#region Main

<#
.SYNOPSIS
  메인 실행 함수
#>
function Main {
  Write-StartMessage -Title "Script Title"

  Invoke-Step1
  Invoke-Step2
  Invoke-Step3

  Write-DoneMessage -Title "Script completed"
}

#endregion

#region Entry Point

if ($Help) {
  Show-Help
  exit 0
}

try {
  Main
  exit 0
}
catch {
  Write-ErrorMessage -ErrorRecord $_
  exit 1
}

#endregion
```

### 파라미터가 있는 템플릿

```powershell
[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "솔루션 파일 경로")]
  [Alias("s")]
  [string]$Solution,

  [Parameter(Mandatory = $false, HelpMessage = "프로젝트 접두사")]
  [Alias("p")]
  [string]$ProjectPrefix = "Functorium",

  [Parameter(Mandatory = $false, HelpMessage = "도움말 표시")]
  [Alias("h", "?")]
  [switch]$Help
)
```

<br/>

## 코딩 규칙

### 필수 설정

모든 스크립트는 다음 설정을 포함해야 합니다:

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

# Strict mode settings
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Set console encoding to UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

### 함수 명명 규칙

| 유형 | 접두사 | 예시 |
|------|--------|------|
| 조회/반환 | `Get-` | `Get-SolutionFile` |
| 설정/저장 | `Set-` | `Set-Configuration` |
| 생성 | `New-` | `New-HtmlReport` |
| 삭제 | `Remove-` | `Remove-TempFiles` |
| 실행 | `Invoke-` | `Invoke-Build` |
| 검증 | `Test-` | `Test-Prerequisites` |
| 표시 | `Show-` | `Show-Help` |
| 설치 | `Install-` | `Install-DotNetTool` |
| 출력 | `Write-` | `Write-Detail` |

### 변수 명명 규칙

| 범위 | 접두사 | 예시 |
|------|--------|------|
| 스크립트 전역 | `$script:` | `$script:TOTAL_STEPS` |
| 함수 로컬 | `$` | `$result` |
| 상수 | 대문자 | `$script:TOTAL_STEPS` |

### 리전 구조

코드를 리전으로 구분하여 가독성을 높입니다:

```powershell
#region Constants
# 상수 정의
#endregion

#region Helper Functions
# 헬퍼 함수
#endregion

#region Step 1: StepName
# 단계별 함수
#endregion

#region Main
# 메인 함수
#endregion

#region Entry Point
# 진입점
#endregion
```

### 문서화 주석

모든 함수에 PowerShell 문서화 주석을 추가합니다:

```powershell
<#
.SYNOPSIS
  함수의 간단한 설명

.DESCRIPTION
  함수의 상세 설명

.PARAMETER ParamName
  파라미터 설명

.OUTPUTS
  반환값 설명

.EXAMPLE
  함수 사용 예시
#>
function Get-Example {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ParamName
  )

  # 구현
}
```

<br/>

## 출력 패턴

### 표준 출력 형식

```
[START] Script Title

[1/3] Step 1 description...
      Detail message
      Success message

[2/3] Step 2 description...
      Detail message
      Warning message

[3/3] Step 3 description...
      Detail message
      Success message

[DONE] Script completed
```

### 에러 출력 형식

```
[ERROR] An unexpected error occurred:
   Error message here

Stack trace:
   at Invoke-Step1, script.ps1: line 50
   at Main, script.ps1: line 100
```

### 색상 규칙

| 메시지 유형 | 색상 | 함수 |
|------------|------|------|
| 시작 | Blue | `Write-StartMessage` |
| 단계 진행 | Gray | `Write-StepProgress` |
| 상세 정보 | DarkGray | `Write-Detail` |
| 성공 | Green | `Write-Success` |
| 경고 | Yellow | `Write-WarningMessage` |
| 완료 | Green | `Write-DoneMessage` |
| 에러 | Red | `Write-ErrorMessage` |

<br/>

## 에러 처리

### 기본 패턴

```powershell
try {
  Main
  exit 0
}
catch {
  Write-ErrorMessage -ErrorRecord $_
  exit 1
}
```

### 함수 내 에러 처리

```powershell
function Invoke-Build {
  param([string]$SolutionPath)

  dotnet build $SolutionPath -c Release

  if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
  }

  Write-Success "Build completed"
}
```

### 경고 처리

```powershell
function Get-CoverageFiles {
  $files = Get-ChildItem -Path $path -Filter "*.xml" -Recurse

  if ($files.Count -eq 0) {
    Write-WarningMessage "No coverage files found"
    return $null
  }

  return $files
}
```

### Exit Code 규칙

| Exit Code | 의미 |
|-----------|------|
| 0 | 성공 |
| 1 | 일반 에러 |
| 2 | 파라미터 에러 |

<br/>

## 예제 스크립트

### Build-Local.ps1 분석

빌드 및 테스트 스크립트의 구조:

```powershell
# 7단계 프로세스
$script:TOTAL_STEPS = 7

# 1. 솔루션 파일 찾기
# 2. Release 모드 빌드
# 3. 버전 정보 표시
# 4. 테스트 실행 (커버리지 수집)
# 5. 커버리지 리포트 병합
# 6. HTML 리포트 생성
# 7. 커버리지 결과 출력
```

### Build-VerifyAccept.ps1 분석

스냅샷 승인 스크립트의 구조:

```powershell
# 2단계 프로세스
$script:TOTAL_STEPS = 2

# 1. VerifyTool 설치/업데이트
# 2. 스냅샷 승인 실행
```

### 새 스크립트 작성 예시

데이터베이스 마이그레이션 스크립트:

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false)]
  [string]$ConnectionString,

  [Parameter(Mandatory = $false)]
  [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$scriptRoot = $PSScriptRoot
. "$scriptRoot/.scripts/Write-Console.ps1"

$script:TOTAL_STEPS = 3

function Restore-DotNetTools {
  Write-StepProgress -Step 1 -TotalSteps $script:TOTAL_STEPS -Message "Restoring .NET tools..."

  dotnet tool restore 2>&1 | Out-Null

  if ($LASTEXITCODE -eq 0) {
    Write-Success "Tools restored"
  }
  else {
    Write-WarningMessage "Tool restore failed"
  }
}

function Invoke-Migration {
  Write-StepProgress -Step 2 -TotalSteps $script:TOTAL_STEPS -Message "Running migrations..."

  dotnet ef database update

  if ($LASTEXITCODE -eq 0) {
    Write-Success "Migrations applied"
  }
  else {
    throw "Migration failed"
  }
}

function Show-Status {
  Write-StepProgress -Step 3 -TotalSteps $script:TOTAL_STEPS -Message "Checking status..."

  dotnet ef migrations list
  Write-Success "Status check completed"
}

function Main {
  Write-StartMessage -Title "Database Migration"

  Restore-DotNetTools
  Invoke-Migration
  Show-Status

  Write-DoneMessage -Title "Migration completed"
}

if ($Help) {
  # Show help
  exit 0
}

try {
  Main
  exit 0
}
catch {
  Write-ErrorMessage -ErrorRecord $_
  exit 1
}
```

<br/>

## FAQ

### Q1. 새 공통 모듈을 추가하려면?

**A:** `.scripts/` 폴더에 새 `.ps1` 파일을 생성하세요:

```powershell
# .scripts/New-Module.ps1
#Requires -Version 7.0

<#
.SYNOPSIS
  모듈 설명
#>

function New-Function {
  # 구현
}
```

### Q2. 외부 명령 출력이 버퍼링될 때?

**A:** `| Out-Default`를 사용하세요:

```powershell
# 실시간 출력
dotnet build | Out-Default

# 출력 무시
dotnet build | Out-Null
```

### Q3. 한글이 깨질 때?

**A:** UTF-8 인코딩을 설정하세요:

```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

### Q4. 스크립트를 크로스 플랫폼으로 만들려면?

**A:** 다음 사항을 고려하세요:

- 경로 구분자: `Join-Path` 사용
- 환경변수: `$env:` 접두사 사용
- 줄바꿈: `[Environment]::NewLine` 사용

```powershell
# Good
$path = Join-Path $PSScriptRoot "subfolder" "file.txt"

# Bad
$path = "$PSScriptRoot\subfolder\file.txt"
```

### Q5. 함수 반환값을 무시하려면?

**A:** `| Out-Null`을 사용하세요:

```powershell
Install-DotNetTool -ToolId "tool" -ToolName "Tool" -RequiredVersion "1.0" | Out-Null
```

### Q6. 조건부로 단계를 건너뛰려면?

**A:** 조건문과 함께 `Write-Detail`로 알림:

```powershell
function Invoke-OptionalStep {
  Write-StepProgress -Step 2 -TotalSteps $script:TOTAL_STEPS -Message "Optional step..."

  if (-not $ShouldRun) {
    Write-Detail "Skipped (condition not met)"
    return
  }

  # 구현
}
```

### Q7. 스크립트 실행 시간을 측정하려면?

**A:** `Stopwatch` 또는 `Get-Date`를 사용하세요:

```powershell
$startTime = Get-Date

# ... 작업 수행 ...

$duration = (Get-Date) - $startTime
Write-Detail "Duration: $($duration.ToString('mm\:ss'))"
```

## 참고 문서

- [PowerShell Documentation](https://docs.microsoft.com/powershell/)
- [PowerShell Best Practices](https://docs.microsoft.com/powershell/scripting/community/contributing/powershell-style-guide)
- [.NET CLI Documentation](https://docs.microsoft.com/dotnet/core/tools/)
