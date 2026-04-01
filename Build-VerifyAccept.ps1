#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  Verify.Xunit 스냅샷 테스트 결과를 승인합니다.

.DESCRIPTION
  .NET 도구를 복원하고 모든 pending Verify.Xunit 스냅샷을 자동으로 승인합니다.

  처리 과정:
  1. .NET 도구 복원 (.config/dotnet-tools.json)
  2. 'dotnet verify accept -y'로 모든 pending 스냅샷 승인

.EXAMPLE
  ./Build-VerifyAccept.ps1

  모든 pending 스냅샷을 승인합니다.

.NOTES
  Requirements: PowerShell 7+, .NET SDK
  Prerequisites: .config/dotnet-tools.json (VerifyTool)
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

#region Helpers

function Write-StepProgress {
  param([int]$Step, [int]$TotalSteps, [string]$Message)
  Write-Host "[$Step/$TotalSteps] $Message" -ForegroundColor Gray
}

function Write-Detail {
  param([string]$Message)
  Write-Host "  $Message" -ForegroundColor DarkGray
}

function Write-Success {
  param([string]$Message)
  Write-Host "  $Message" -ForegroundColor Green
}

function Write-WarningMessage {
  param([string]$Message)
  Write-Host "  $Message" -ForegroundColor Yellow
}

function Write-StartMessage {
  param([string]$Title)
  Write-Host ""
  Write-Host "[START] $Title" -ForegroundColor Blue
  Write-Host ""
}

function Write-DoneMessage {
  param([string]$Title)
  Write-Host ""
  Write-Host "[DONE] $Title" -ForegroundColor Green
  Write-Host ""
}

function Write-ErrorMessage {
  param([System.Management.Automation.ErrorRecord]$ErrorRecord)
  Write-Host ""
  Write-Host "[ERROR] An unexpected error occurred:" -ForegroundColor Red
  Write-Host "   $($ErrorRecord.Exception.Message)" -ForegroundColor Red
  Write-Host ""
  Write-Host "Stack trace:" -ForegroundColor DarkGray
  Write-Host $ErrorRecord.ScriptStackTrace -ForegroundColor DarkGray
  Write-Host ""
}

#endregion

#region Constants

$script:TOTAL_STEPS = 2

#endregion

#region Step 1: Restore-DotNetTools

<#
.SYNOPSIS
  .NET 로컬 도구를 복원합니다.
#>
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

#endregion

#region Step 2: Invoke-VerifyAccept

<#
.SYNOPSIS
  모든 pending 스냅샷을 승인합니다.
#>
function Invoke-VerifyAccept {
  Write-StepProgress -Step 2 -TotalSteps $script:TOTAL_STEPS -Message "Accepting pending snapshots..."

  dotnet verify accept -y

  if ($LASTEXITCODE -eq 0) {
    Write-Success "Snapshots accepted"
  }
  else {
    Write-Detail "No pending snapshots or accept failed"
  }
}

#endregion

#region Main

<#
.SYNOPSIS
  메인 실행 함수입니다.
#>
function Main {
  Write-StartMessage -Title "Verify Accept..."

  # Step 1: Restore .NET tools
  Restore-DotNetTools

  # Step 2: Accept pending snapshots
  Invoke-VerifyAccept

  Write-DoneMessage -Title "Verify accept completed"
}

#endregion

#region Entry Point

if ($Help) {
  Get-Help $PSCommandPath -Detailed
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
