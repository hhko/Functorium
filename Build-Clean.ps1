#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  .NET 프로젝트의 bin 및 obj 폴더를 삭제합니다.

.DESCRIPTION
  - 현재 경로의 모든 하위 폴더에서 .csproj 파일을 검색합니다.
  - 각 .csproj 파일이 있는 경로의 bin 및 obj 폴더를 삭제합니다.
  - 삭제한 폴더 건수를 출력합니다.

.PARAMETER Help
  도움말을 표시합니다.

.EXAMPLE
  ./Build-Clean.ps1
  모든 프로젝트의 bin 및 obj 폴더를 삭제합니다.

.EXAMPLE
  ./Build-Clean.ps1 -Help
  도움말을 표시합니다.

.NOTES
  Version: 1.0.0
  Requirements: PowerShell 7+
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
. "$scriptRoot/.scripts/Remove-DirectorySafely.ps1"

#region Constants

$script:TOTAL_STEPS = 3
$script:CsprojFiles = @()
$script:DeletedBinCount = 0
$script:DeletedObjCount = 0

#endregion

#region Helper Functions

<#
.SYNOPSIS
  도움말을 표시합니다.
#>
function Show-Help {
  $help = @"

================================================================================
 .NET Project Clean Script
================================================================================

DESCRIPTION
  Clean bin and obj folders from all .NET projects in the current directory
  and its subdirectories.

USAGE
  ./Build-Clean.ps1 [options]

OPTIONS
  -Help, -h, -?      Show this help message

FEATURES
  1. Search for all .csproj files recursively
  2. Delete bin and obj folders from each project directory
  3. Display summary of deleted folders

EXAMPLES
  # Clean all projects
  ./Build-Clean.ps1

  # Show help
  ./Build-Clean.ps1 -Help

================================================================================
"@
  Write-Host $help
}

#endregion

#region Step 1: Find-CsprojFiles

<#
.SYNOPSIS
  .csproj 파일을 검색합니다.
#>
function Find-CsprojFiles {
  Write-StepProgress -Step 1 -TotalSteps $script:TOTAL_STEPS -Message "Searching for .csproj files..."

  $script:CsprojFiles = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "*.csproj"
  $count = $script:CsprojFiles.Count

  Write-Detail "Found $count .csproj file(s)"
}

#endregion

#region Step 2: Remove-BuildArtifacts

<#
.SYNOPSIS
  bin 및 obj 폴더를 삭제합니다.
#>
function Remove-BuildArtifacts {
  Write-StepProgress -Step 2 -TotalSteps $script:TOTAL_STEPS -Message "Cleaning bin and obj folders..."

  # Phase 1: Collect targets (for progress display)
  $targets = [System.Collections.Generic.List[PSObject]]::new()
  foreach ($csproj in $script:CsprojFiles) {
    $projectDir = $csproj.Directory.FullName
    $projectName = $csproj.Directory.Name

    $binPath = Join-Path $projectDir "bin"
    if (Test-Path $binPath) {
      $targets.Add([PSCustomObject]@{ Path = $binPath; Name = "$projectName\bin"; Type = "bin" })
    }

    $objPath = Join-Path $projectDir "obj"
    if (Test-Path $objPath) {
      $targets.Add([PSCustomObject]@{ Path = $objPath; Name = "$projectName\obj"; Type = "obj" })
    }
  }

  $total = $targets.Count
  if ($total -eq 0) {
    Write-Detail "No bin/obj folders to delete"
    return
  }

  # Phase 2: Delete with progress (using .NET Directory.Delete to avoid heap corruption)
  $current = 0
  foreach ($target in $targets) {
    $current++
    if (Remove-DirectorySafely -Path $target.Path) {
      if ($target.Type -eq "bin") { $script:DeletedBinCount++ }
      else { $script:DeletedObjCount++ }
      Write-Detail "$current/$total Deleted: $($target.Name)"
    }
    else {
      Write-WarningMessage "$current/$total Failed: $($target.Path)"
    }
  }
}

#endregion

#region Step 3: Show-CleanSummary

<#
.SYNOPSIS
  정리 통계를 표시합니다.
#>
function Show-CleanSummary {
  Write-StepProgress -Step 3 -TotalSteps $script:TOTAL_STEPS -Message "Clean summary"

  $totalDeleted = $script:DeletedBinCount + $script:DeletedObjCount

  Write-Host ""
  Write-Host "Clean Summary:" -ForegroundColor Cyan
  Write-Detail "Total .csproj files found: $($script:CsprojFiles.Count)"
  Write-Detail "bin folders deleted: $script:DeletedBinCount"
  Write-Detail "obj folders deleted: $script:DeletedObjCount"
  Write-Success "Total folders deleted: $totalDeleted"
}

#endregion

#region Main

<#
.SYNOPSIS
  메인 실행 함수입니다.
#>
function Main {
  Write-StartMessage -Title "Build Clean..."

  # Step 1: Search for .csproj files
  Find-CsprojFiles

  # Step 2: Remove bin and obj folders
  Remove-BuildArtifacts

  # Step 3: Show clean summary
  Show-CleanSummary

  Write-DoneMessage -Title "Build clean completed"
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
