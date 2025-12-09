#Requires -Version 7.0

<#
.SYNOPSIS
  dotnet global tool 설치/업데이트 공통 함수 모듈

.DESCRIPTION
  dotnet global tool을 설치하거나 업데이트하는 공통 함수를 제공합니다.
  버전 확인, 설치, 업데이트를 자동으로 처리합니다.

.EXAMPLE
  . ./.scripts/Install-DotNetTool.ps1
  Install-DotNetTool -ToolId "dotnet-reportgenerator-globaltool" -ToolName "ReportGenerator" -RequiredVersion "5.5.0"

.NOTES
  Version: 1.0.0
  License: MIT
#>

<#
.SYNOPSIS
  dotnet global tool을 설치하거나 업데이트합니다.
.PARAMETER ToolId
  dotnet tool ID (예: dotnet-reportgenerator-globaltool, verify.tool)
.PARAMETER ToolName
  표시용 도구 이름 (예: ReportGenerator, VerifyTool)
.PARAMETER RequiredVersion
  필요한 최소 버전 (예: 5.5.0)
.PARAMETER RefreshPath
  설치 후 PATH 환경변수 갱신 여부 (기본값: $false)
.OUTPUTS
  설치/업데이트 성공 시 $true, 실패 시 예외 발생
#>
function Install-DotNetTool {
  param(
    [Parameter(Mandatory = $true)]
    [string]$ToolId,

    [Parameter(Mandatory = $true)]
    [string]$ToolName,

    [Parameter(Mandatory = $true)]
    [string]$RequiredVersion,

    [Parameter(Mandatory = $false)]
    [switch]$RefreshPath
  )

  $requiredVer = [version]$RequiredVersion

  # Check if tool is installed
  $toolList = dotnet tool list -g 2>$null | Where-Object { $_ -match $ToolId }

  if ($toolList) {
    # Extract installed version
    $installedVersionStr = ($toolList -split '\s+')[1]
    $installedVersion = [version]$installedVersionStr

    Write-Detail "$ToolName installed: v$installedVersionStr"

    if ($installedVersion -lt $requiredVer) {
      Write-Detail "Updating to v$RequiredVersion..."
      dotnet tool update -g $ToolId --version $RequiredVersion 2>$null

      if ($LASTEXITCODE -eq 0) {
        Write-Success "Updated to v$RequiredVersion"
      }
      else {
        Write-WarningMessage "Failed to update $ToolName"
      }
    }
    else {
      Write-Detail "Version is up to date"
    }
  }
  else {
    # Install tool
    Write-Detail "Installing $ToolName v$RequiredVersion..."
    dotnet tool install -g $ToolId --version $RequiredVersion 2>$null

    if ($LASTEXITCODE -eq 0) {
      Write-Success "Installed v$RequiredVersion"

      # Refresh PATH if requested
      if ($RefreshPath) {
        $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "User") + ";" + $env:PATH
      }
    }
    else {
      throw "Failed to install $ToolName"
    }
  }

  return $true
}

<#
.SYNOPSIS
  ReportGenerator를 설치하거나 업데이트합니다.
.PARAMETER RequiredVersion
  필요한 버전 (기본값: 5.5.0)
#>
function Install-ReportGenerator {
  param(
    [Parameter(Mandatory = $false)]
    [string]$RequiredVersion = "5.5.0"
  )

  Install-DotNetTool `
    -ToolId "dotnet-reportgenerator-globaltool" `
    -ToolName "ReportGenerator" `
    -RequiredVersion $RequiredVersion
}

<#
.SYNOPSIS
  VerifyTool을 설치하거나 업데이트합니다.
.PARAMETER RequiredVersion
  필요한 버전 (기본값: 0.7.0)
#>
function Install-VerifyTool {
  param(
    [Parameter(Mandatory = $false)]
    [string]$RequiredVersion = "0.7.0"
  )

  Install-DotNetTool `
    -ToolId "verify.tool" `
    -ToolName "VerifyTool" `
    -RequiredVersion $RequiredVersion `
    -RefreshPath
}

