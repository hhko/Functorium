#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  안전한 디렉토리 삭제 함수 모듈

.DESCRIPTION
  PowerShell의 Remove-Item -Recurse 비동기 문제(GitHub Issue #8211)를 회피하기 위해
  .NET의 Directory.Delete()를 사용하는 함수들을 제공합니다.

  주요 기능:
  - 파이프라인 객체 참조 문제 회피
  - 크로스 플랫폼 지원 (Windows, Linux, macOS)
  - 재시도 로직 포함
  - 읽기 전용 속성 자동 해제

.NOTES
  Version: 1.0.0
  License: MIT
#>

<#
.SYNOPSIS
  디렉토리를 안전하게 삭제합니다 (힙 손상 방지).

.DESCRIPTION
  PowerShell의 Remove-Item -Recurse 비동기 문제를 회피하기 위해
  .NET의 Directory.Delete()를 사용합니다.

.PARAMETER Path
  삭제할 디렉토리 경로

.PARAMETER MaxRetries
  최대 재시도 횟수 (기본값: 3)

.PARAMETER RetryDelayMs
  재시도 간 대기 시간(ms) (기본값: 100)

.OUTPUTS
  [bool] 삭제 성공 여부

.EXAMPLE
  Remove-DirectorySafely -Path "C:\Temp\TestResults"

.EXAMPLE
  Remove-DirectorySafely -Path "/tmp/TestResults" -MaxRetries 5
#>
function Remove-DirectorySafely {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
    [string]$Path,

    [Parameter(Mandatory = $false)]
    [int]$MaxRetries = 3,

    [Parameter(Mandatory = $false)]
    [int]$RetryDelayMs = 100
  )

  process {
    if (-not (Test-Path $Path -PathType Container)) {
      return $true
    }

    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
      try {
        [System.IO.Directory]::Delete($Path, $true)
        return $true
      }
      catch [System.IO.IOException] {
        # File in use - retry
        if ($attempt -lt $MaxRetries) {
          Start-Sleep -Milliseconds $RetryDelayMs
        }
      }
      catch [System.UnauthorizedAccessException] {
        # Permission issue - try removing read-only attributes
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
          if ($attempt -lt $MaxRetries) {
            Start-Sleep -Milliseconds $RetryDelayMs
          }
        }
      }
      catch {
        if ($attempt -lt $MaxRetries) {
          Start-Sleep -Milliseconds $RetryDelayMs
        }
      }
    }

    # All retries failed
    Write-Warning "Failed to delete directory after $MaxRetries attempts: $Path"
    return $false
  }
}

<#
.SYNOPSIS
  지정된 이름의 디렉토리를 검색하여 경로 목록을 반환합니다.

.DESCRIPTION
  Get-ChildItem으로 검색 후 경로 문자열만 반환하여
  파이프라인 객체 참조 문제를 방지합니다.

.PARAMETER SearchPaths
  검색할 상위 디렉토리 경로 배열

.PARAMETER DirectoryName
  찾을 디렉토리 이름 (Filter에 사용)

.OUTPUTS
  [string[]] 발견된 디렉토리의 전체 경로 배열

.EXAMPLE
  $paths = Find-DirectoriesByName -SearchPaths @("C:\Src", "C:\Tests") -DirectoryName "TestResults"
#>
function Find-DirectoriesByName {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory = $true)]
    [string[]]$SearchPaths,

    [Parameter(Mandatory = $true)]
    [string]$DirectoryName
  )

  $results = [System.Collections.Generic.List[string]]::new()

  foreach ($searchPath in $SearchPaths) {
    if (-not (Test-Path $searchPath)) {
      continue
    }

    # Convert to path strings immediately to avoid object reference issues
    $found = Get-ChildItem -Path $searchPath -Directory -Recurse -Filter $DirectoryName -ErrorAction SilentlyContinue
    foreach ($dir in $found) {
      $results.Add($dir.FullName)
    }
  }

  return $results.ToArray()
}

<#
.SYNOPSIS
  지정된 검색 경로에서 특정 이름의 디렉토리를 모두 삭제합니다.

.DESCRIPTION
  검색과 삭제를 분리하여 힙 손상을 방지합니다:
  1. 먼저 모든 대상 디렉토리 경로를 문자열로 수집
  2. 수집 완료 후 .NET Directory.Delete()로 삭제

.PARAMETER SearchPaths
  검색할 상위 디렉토리 경로 배열

.PARAMETER DirectoryName
  삭제할 디렉토리 이름

.OUTPUTS
  [int] 삭제된 디렉토리 수

.EXAMPLE
  Remove-DirectoriesByName -SearchPaths @($srcDir, $testsDir) -DirectoryName "TestResults"
#>
function Remove-DirectoriesByName {
  [CmdletBinding(SupportsShouldProcess)]
  param(
    [Parameter(Mandatory = $true)]
    [string[]]$SearchPaths,

    [Parameter(Mandatory = $true)]
    [string]$DirectoryName
  )

  # Phase 1: Collect paths (separated from pipeline)
  $targetPaths = @(Find-DirectoriesByName -SearchPaths $SearchPaths -DirectoryName $DirectoryName)

  if ($targetPaths.Count -eq 0) {
    return 0
  }

  # Phase 2: Delete (after collection is complete)
  $deletedCount = 0
  foreach ($path in $targetPaths) {
    if ($PSCmdlet.ShouldProcess($path, "Delete directory")) {
      if (Remove-DirectorySafely -Path $path) {
        $deletedCount++
      }
    }
  }

  return $deletedCount
}
