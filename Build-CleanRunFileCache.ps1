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

.PARAMETER Pattern
  삭제할 캐시 디렉토리 패턴입니다.
  기본값: SummarizeSlowestTests (SummarizeSlowestTests-* 디렉토리 삭제)
  "All"을 지정하면 모든 runfile 캐시를 삭제합니다.

.PARAMETER WhatIf
  실제로 삭제하지 않고 삭제 대상만 표시합니다.

.EXAMPLE
  ./Clear-RunFileCache.ps1
  SummarizeSlowestTests 캐시만 삭제

.EXAMPLE
  ./Clear-RunFileCache.ps1 -Pattern "All"
  모든 runfile 캐시 삭제

.EXAMPLE
  ./Clear-RunFileCache.ps1 -WhatIf
  삭제 대상만 표시 (실제 삭제 안 함)

.NOTES
  Version: 1.0.0
  이 스크립트는 다음 오류 발생 시 사용합니다:
  - System.IO.FileNotFoundException: Could not load file or assembly 'System.CommandLine...'
  - 파일 기반 프로그램 실행 시 패키지 참조 오류
#>

[CmdletBinding(SupportsShouldProcess)]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "삭제할 캐시 패턴 (기본: SummarizeSlowestTests, All: 전체)")]
  [string]$Pattern = "SummarizeSlowestTests"
)

$ErrorActionPreference = "Stop"

# 캐시 디렉토리 경로
$runFileCacheDir = Join-Path $env:TEMP "dotnet\runfile"

Write-Host ""
Write-Host "[RunFile Cache Cleaner]" -ForegroundColor Cyan
Write-Host ""

# 캐시 디렉토리 존재 확인
if (-not (Test-Path $runFileCacheDir)) {
  Write-Host "  Cache directory not found: $runFileCacheDir" -ForegroundColor Yellow
  Write-Host "  Nothing to clean." -ForegroundColor DarkGray
  exit 0
}

Write-Host "  Cache location: $runFileCacheDir" -ForegroundColor DarkGray

# 삭제 대상 결정
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
  exit 0
}

Write-Host "  Found $($targetDirs.Count) cache(s) matching: $patternDescription" -ForegroundColor White
Write-Host ""

# 삭제 실행
$deleted = 0
foreach ($dir in $targetDirs) {
  $size = (Get-ChildItem -Path $dir.FullName -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
  $sizeKB = [math]::Round($size / 1KB, 1)

  if ($PSCmdlet.ShouldProcess($dir.Name, "Delete")) {
    Write-Host "  Deleting: $($dir.Name) ($sizeKB KB)" -ForegroundColor Yellow
    Remove-Item -Path $dir.FullName -Recurse -Force
    $deleted++
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
