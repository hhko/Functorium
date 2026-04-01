#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  .NET 파일 기반 프로그램(run-file) 캐시를 정리합니다.

.DESCRIPTION
  .NET 10의 파일 기반 프로그램은 컴파일된 아티팩트를 캐시합니다.
  패키지 참조 문제나 캐시 손상으로 인해 실행이 실패할 경우,
  이 스크립트로 캐시를 정리하여 문제를 해결할 수 있습니다.

  캐시 위치: %TEMP%\dotnet\runfile\

  다음 오류 발생 시 사용합니다:
  - System.IO.FileNotFoundException: Could not load file or assembly 'System.CommandLine...'
  - 파일 기반 프로그램 실행 시 패키지 참조 오류

.PARAMETER Pattern
  삭제할 캐시 디렉토리 패턴입니다.
  기본값: SummarizeSlowestTests (SummarizeSlowestTests-* 디렉토리 삭제)
  "All"을 지정하면 모든 runfile 캐시를 삭제합니다.

.PARAMETER WhatIf
  실제로 삭제하지 않고 삭제 대상만 표시합니다.

.EXAMPLE
  ./Build-CleanRunFileCache.ps1

  SummarizeSlowestTests 캐시만 삭제합니다.

.EXAMPLE
  ./Build-CleanRunFileCache.ps1 -Pattern "All"

  모든 runfile 캐시를 삭제합니다.

.EXAMPLE
  ./Build-CleanRunFileCache.ps1 -WhatIf

  삭제 대상만 표시합니다 (실제 삭제 안 함).

.NOTES
  Requirements: PowerShell 7+
#>

[CmdletBinding(SupportsShouldProcess)]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "삭제할 캐시 패턴 (기본: SummarizeSlowestTests, All: 전체)")]
  [string]$Pattern = "SummarizeSlowestTests",

  [Parameter(Mandatory = $false, HelpMessage = "도움말 표시")]
  [Alias("h", "?")]
  [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

#region Helpers

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

if ($Help) {
  Get-Help $PSCommandPath -Detailed
  exit 0
}

try {
  Main
  exit 0
}
catch {
  Write-Host ""
  Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
  Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
  Write-Host ""
  exit 1
}

#endregion
