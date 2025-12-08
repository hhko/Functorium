$script:VerifyToolVersion = "0.7.0"

<#
.SYNOPSIS
  VerifyTool 도구를 설치하거나 업데이트합니다.
#>
function Install-VerifyTool {
  $requiredVersion = [version]$script:VerifyToolVersion

  # Check if VerifyTool is installed
  $toolList = dotnet tool list -g 2>$null | Where-Object { $_ -match "verify.tool" }

  if ($toolList) {
    # Extract installed version
    $installedVersionStr = ($toolList -split '\s+')[1]
    $installedVersion = [version]$installedVersionStr

    Write-Detail "VerifyTool installed: v$installedVersionStr"

    if ($installedVersion -lt $requiredVersion) {
      Write-Detail "Updating to v$script:VerifyToolVersion..."
      dotnet tool update -g verify.tool --version $script:VerifyToolVersion 2>$null

      if ($LASTEXITCODE -eq 0) {
        Write-Success "Updated to v$script:VerifyToolVersion"
      }
      else {
        Write-Warning "Failed to update VerifyTool"
      }
    }
  }
  else {
    # Install VerifyTool
    Write-Detail "Installing VerifyTool v$script:VerifyToolVersion..."
    dotnet tool install -g verify.tool --version $script:VerifyToolVersion 2>$null

    if ($LASTEXITCODE -eq 0) {
      Write-Success "Installed v$script:VerifyToolVersion"

      # Refresh PATH
      $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "User") + ";" + $env:PATH
    }
    else {
      throw "Failed to install VerifyTool"
    }
  }
}

Install-VerifyTool

dotnet verify accept -y