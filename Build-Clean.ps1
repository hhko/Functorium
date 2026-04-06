#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Deletes bin and obj folders from all .NET projects.

.DESCRIPTION
  Recursively searches for .csproj files in all subdirectories
  and deletes the bin and obj folders for each project.

  Steps:
  1. Recursively search for .csproj files
  2. Delete bin/obj folders for each project
  3. Display deletion summary

.EXAMPLE
  ./Build-Clean.ps1

  Deletes all bin and obj folders.

.NOTES
  Requirements: PowerShell 7+
#>

[CmdletBinding()]
param()

#Requires -Version 7.0

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

function Remove-DirectorySafely {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
    [string]$Path,
    [int]$MaxRetries = 3,
    [int]$RetryDelayMs = 100
  )
  process {
    if (-not (Test-Path $Path -PathType Container)) { return $true }
    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
      try {
        [System.IO.Directory]::Delete($Path, $true)
        return $true
      }
      catch [System.IO.IOException] {
        if ($attempt -lt $MaxRetries) { Start-Sleep -Milliseconds $RetryDelayMs }
      }
      catch [System.UnauthorizedAccessException] {
        try {
          Get-ChildItem -Path $Path -Recurse -Force -ErrorAction SilentlyContinue |
            ForEach-Object {
              if ($_.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
                $_.Attributes = $_.Attributes -bxor [System.IO.FileAttributes]::ReadOnly
              }
            }
          [System.IO.Directory]::Delete($Path, $true)
          return $true
        }
        catch {
          if ($attempt -lt $MaxRetries) { Start-Sleep -Milliseconds $RetryDelayMs }
        }
      }
      catch {
        if ($attempt -lt $MaxRetries) { Start-Sleep -Milliseconds $RetryDelayMs }
      }
    }
    Write-Warning "Failed to delete directory after $MaxRetries attempts: $Path"
    return $false
  }
}

#endregion

#region Constants

$script:TOTAL_STEPS = 3
$script:CsprojFiles = @()
$script:DeletedBinCount = 0
$script:DeletedObjCount = 0

#endregion

#region Step 1: Find-CsprojFiles

# .csproj 파일을 검색합니다.
function Find-CsprojFiles {
  Write-StepProgress -Step 1 -TotalSteps $script:TOTAL_STEPS -Message "Searching for .csproj files..."

  $script:CsprojFiles = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "*.csproj"
  $count = $script:CsprojFiles.Count

  Write-Detail "Found $count .csproj file(s)"
}

#endregion

#region Step 2: Remove-BuildArtifacts

# bin 및 obj 폴더를 삭제합니다.
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

# 정리 통계를 표시합니다.
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

try {
  Main
  exit 0
}
catch {
  Write-ErrorMessage -ErrorRecord $_
  exit 1
}

#endregion
