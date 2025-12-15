#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  Verify.Xunit 스냅샷 테스트 결과를 승인합니다.

.DESCRIPTION
  - VerifyTool을 설치 또는 업데이트합니다.
  - 모든 pending 스냅샷을 자동으로 승인합니다.

.PARAMETER Help
  도움말을 표시합니다.

.EXAMPLE
  ./Build-VerifyAccept.ps1
  모든 pending 스냅샷을 승인합니다.

.EXAMPLE
  ./Build-VerifyAccept.ps1 -Help
  도움말을 표시합니다.

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

$script:TOTAL_STEPS = 2

#endregion

#region Helper Functions

<#
.SYNOPSIS
  도움말을 표시합니다.
#>
function Show-Help {
  $help = @"

================================================================================
 Verify.Xunit Snapshot Accept Script
================================================================================

DESCRIPTION
  Accept all pending Verify.Xunit snapshots.

USAGE
  ./Build-VerifyAccept.ps1 [options]

OPTIONS
  -Help, -h, -?      Show this help message

FEATURES
  1. Restore .NET tools from .config/dotnet-tools.json
  2. Accept all pending snapshots with 'dotnet verify accept -y'

PREREQUISITES
  - .NET SDK
  - Tools defined in .config/dotnet-tools.json (auto-restored)

EXAMPLES
  # Accept all pending snapshots
  ./Build-VerifyAccept.ps1

  # Show help
  ./Build-VerifyAccept.ps1 -Help

================================================================================
"@
  Write-Host $help
}

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
