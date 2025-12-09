#Requires -Version 7.0

<#
.SYNOPSIS
  콘솔 출력 관련 공통 함수 모듈

.DESCRIPTION
  빌드 스크립트에서 공통으로 사용하는 콘솔 출력 함수들을 제공합니다.
  - Write-StepProgress: 단계별 진행 상황 출력
  - Write-Detail: 상세 정보 출력
  - Write-Success: 성공 메시지 출력
  - Write-WarningMessage: 경고 메시지 출력
  - Write-StartMessage: 시작 메시지 출력
  - Write-DoneMessage: 완료 메시지 출력
  - Write-ErrorMessage: 에러 메시지 출력

.EXAMPLE
  . ./.scripts/Write-Console.ps1
  Write-StepProgress -Step 1 -TotalSteps 5 -Message "Building solution..."

.NOTES
  Version: 1.0.0
  License: MIT
#>

<#
.SYNOPSIS
  단계별 진행 상황을 출력합니다.
.PARAMETER Step
  현재 단계 번호
.PARAMETER TotalSteps
  전체 단계 수
.PARAMETER Message
  출력할 메시지
#>
function Write-StepProgress {
  param(
    [Parameter(Mandatory = $true)]
    [int]$Step,

    [Parameter(Mandatory = $true)]
    [int]$TotalSteps,

    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "[$Step/$TotalSteps] $Message" -ForegroundColor Gray
}

<#
.SYNOPSIS
  상세 정보를 출력합니다.
.PARAMETER Message
  출력할 메시지
#>
function Write-Detail {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "      $Message" -ForegroundColor DarkGray
}

<#
.SYNOPSIS
  성공 메시지를 출력합니다.
.PARAMETER Message
  출력할 메시지
#>
function Write-Success {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "      $Message" -ForegroundColor Green
}

<#
.SYNOPSIS
  경고 메시지를 출력합니다.
.PARAMETER Message
  출력할 메시지
.NOTES
  기본 Write-Warning cmdlet과 충돌을 피하기 위해 Write-WarningMessage로 명명
#>
function Write-WarningMessage {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "      $Message" -ForegroundColor Yellow
}

<#
.SYNOPSIS
  시작 메시지를 출력합니다.
.PARAMETER Title
  작업 제목
#>
function Write-StartMessage {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Title
  )

  Write-Host ""
  Write-Host "[START] $Title" -ForegroundColor Blue
  Write-Host ""
}

<#
.SYNOPSIS
  완료 메시지를 출력합니다.
.PARAMETER Title
  작업 제목
#>
function Write-DoneMessage {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Title
  )

  Write-Host ""
  Write-Host "[DONE] $Title" -ForegroundColor Green
  Write-Host ""
}

<#
.SYNOPSIS
  에러 메시지를 출력합니다.
.PARAMETER Exception
  예외 객체
#>
function Write-ErrorMessage {
  param(
    [Parameter(Mandatory = $true)]
    [System.Management.Automation.ErrorRecord]$ErrorRecord
  )

  Write-Host ""
  Write-Host "[ERROR] An unexpected error occurred:" -ForegroundColor Red
  Write-Host "   $($ErrorRecord.Exception.Message)" -ForegroundColor Red
  Write-Host ""
  Write-Host "Stack trace:" -ForegroundColor DarkGray
  Write-Host $ErrorRecord.ScriptStackTrace -ForegroundColor DarkGray
  Write-Host ""
}

