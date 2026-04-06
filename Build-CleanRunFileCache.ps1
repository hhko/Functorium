#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Cleans .NET run-file program cache.

.DESCRIPTION
  .NET 10 run-file programs cache compiled artifacts.
  Use this script to clean the cache when execution fails
  due to package reference issues or cache corruption.

  Cache location: %TEMP%\dotnet\runfile\

  Use when encountering:
  - System.IO.FileNotFoundException: Could not load file or assembly 'System.CommandLine...'
  - Package reference errors in run-file programs

.PARAMETER Pattern
  Cache directory pattern to delete.
  Default: SummarizeSlowestTests (deletes SummarizeSlowestTests-* directories)
  Use "All" to delete all runfile caches.

.EXAMPLE
  ./Build-CleanRunFileCache.ps1

  Deletes only SummarizeSlowestTests cache.

.EXAMPLE
  ./Build-CleanRunFileCache.ps1 -Pattern "All"

  Deletes all runfile caches.

.EXAMPLE
  ./Build-CleanRunFileCache.ps1 -WhatIf

  Shows deletion targets without deleting.

.NOTES
  Requirements: PowerShell 7+
#>

[CmdletBinding(SupportsShouldProcess)]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "Cache pattern to delete")]
  [string]$Pattern = "SummarizeSlowestTests"
)

#Requires -Version 7.0

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

#region Helpers

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

#region Main

function Main {
  $runFileCacheDir = Join-Path $env:TEMP "dotnet\runfile"

  Write-Host ""
  Write-Host "[RunFile Cache Cleaner]" -ForegroundColor Cyan
  Write-Host ""

  if (-not (Test-Path $runFileCacheDir)) {
    Write-Host "  Cache directory not found: $runFileCacheDir" -ForegroundColor Yellow
    Write-Host "  Nothing to clean." -ForegroundColor DarkGray
    return
  }

  Write-Host "  Cache location: $runFileCacheDir" -ForegroundColor DarkGray

  if ($Pattern -eq "All") {
    $targetDirs = Get-ChildItem -Path $runFileCacheDir -Directory -ErrorAction SilentlyContinue
    $patternDescription = "All runfile caches"
  }
  else {
    $targetDirs = Get-ChildItem -Path $runFileCacheDir -Directory -Filter "$Pattern-*" -ErrorAction SilentlyContinue
    $patternDescription = "$Pattern-* caches"
  }

  if ($targetDirs.Count -eq 0) {
    Write-Host "  No matching cache directories found for pattern: $Pattern" -ForegroundColor Yellow
    return
  }

  Write-Host "  Found $($targetDirs.Count) cache(s) matching: $patternDescription" -ForegroundColor White
  Write-Host ""

  $deleted = 0
  foreach ($dir in $targetDirs) {
    $size = (Get-ChildItem -Path $dir.FullName -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
    $sizeKB = [math]::Round($size / 1KB, 1)

    if ($PSCmdlet.ShouldProcess($dir.Name, "Delete")) {
      Write-Host "  Deleting: $($dir.Name) ($sizeKB KB)" -ForegroundColor Yellow
      if (Remove-DirectorySafely -Path $dir.FullName) {
        $deleted++
      }
    }
  }

  Write-Host ""

  if ($deleted -gt 0) {
    Write-Host "  Cache cleared successfully. ($deleted item(s) deleted)" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Next steps:" -ForegroundColor White
    Write-Host "    1. Re-run the script (e.g., dotnet SummarizeSlowestTests.cs)" -ForegroundColor DarkGray
    Write-Host "    2. It will be recompiled with fresh package references" -ForegroundColor DarkGray
  }
  elseif ($WhatIfPreference) {
    Write-Host "  Run without -WhatIf to actually delete the caches." -ForegroundColor DarkGray
  }

  Write-Host ""
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
