#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  .NET 솔루션 빌드, 테스트 및 코드 커버리지 리포트 생성 스크립트

.DESCRIPTION
  - Release 모드로 솔루션 빌드
  - MinVer 버전 정보 표시 (MinVer 설정된 경우)
  - 테스트 실행 및 코드 커버리지 수집
  - 핵심 레이어(Domains, Applications) 및 전체 커버리지 출력
  - HTML 리포트 생성

.PARAMETER Solution
  솔루션 파일 경로를 지정합니다. 지정하지 않으면 자동으로 검색합니다.

.PARAMETER ProjectPrefix
  커버리지 필터링용 프로젝트 접두사를 지정합니다.
  기본값: Functorium

.PARAMETER Help
  도움말을 표시합니다.

.EXAMPLE
  ./Build-Local.ps1
  현재 디렉토리에서 솔루션을 자동 검색하여 빌드 및 테스트 실행

.EXAMPLE
  ./Build-Local.ps1 -Solution ./MyApp.sln
  지정된 솔루션 파일로 빌드 및 테스트 실행

.EXAMPLE
  ./Build-Local.ps1 -ProjectPrefix MyApp
  MyApp.* 프로젝트만 커버리지 필터링

.EXAMPLE
  ./Build-Local.ps1 -Help
  도움말 표시

.NOTES
  Version: 1.0.0
  Requirements: PowerShell 7+, .NET SDK, ReportGenerator
  Output directory: .coverage/
  License: MIT
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "솔루션 파일 경로 (.sln 또는 .slnx)")]
  [Alias("s")]
  [string]$Solution,

  [Parameter(Mandatory = $false, HelpMessage = "커버리지 필터링용 프로젝트 접두사")]
  [Alias("p")]
  [string]$ProjectPrefix = "Functorium",

  [Parameter(Mandatory = $false, HelpMessage = "도움말 표시")]
  [Alias("h", "?")]
  [switch]$Help
)

# Strict mode settings
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Set console encoding to UTF-8 for proper Korean character display
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

#region Constants

$script:TOTAL_STEPS = 7
$script:Configuration = "Release"
$script:CoreLayerPatterns = @("*.Domain", "*.Domains", "*.Application", "*.Applications")
$script:ReportGeneratorVersion = "5.5.0"

# These will be set after solution file is found
$script:SolutionDir = $null
$script:CoverageReportDir = $null

#endregion

#region Helper Functions

<#
.SYNOPSIS
  솔루션 파일 경로를 기준으로 출력 경로를 설정합니다.
#>
function Set-OutputPaths {
  param([string]$SolutionPath)

  $script:SolutionDir = Split-Path -Parent $SolutionPath
  $script:CoverageReportDir = Join-Path $script:SolutionDir ".coverage"
}

<#
.SYNOPSIS
  도움말을 표시합니다.
#>
function Show-Help {
  $help = @"

================================================================================
 .NET Solution Build and Test Script
================================================================================

DESCRIPTION
  Build, test, and generate code coverage reports for .NET solutions.

USAGE
  ./Build-Local.ps1 [options]

OPTIONS
  -Solution, -s      Path to solution file (.sln or .slnx)
                     If not specified, auto-detects from current directory
  -ProjectPrefix, -p Project prefix for coverage filtering
                     Default: Functorium
  -Help, -h, -?      Show this help message

FEATURES
  1. Auto-detect solution file (requires exactly 1 .sln or .slnx file)
  2. Build in Release mode
  3. Display MinVer version information (if MinVer is configured)
  4. Run tests with code coverage collection
  5. Generate HTML coverage report (ReportGenerator)
  6. Display coverage summary in console
     - Project: Projects matching prefix (e.g., Functorium.*)
     - Core Layer: Domains + Applications projects
     - Full: All projects (excluding tests)

OUTPUT
  {SolutionDir}/.coverage/
  ├── index.html            <- HTML Report
  └── Cobertura.xml         <- Merged coverage

  {SolutionDir}/Tests/{TestProject}/TestResults/
  ├── {GUID}/
  │   └── coverage.cobertura.xml  <- Raw coverage
  └── *.trx                       <- Test results

PREREQUISITES
  - .NET SDK
  - ReportGenerator v5.5.0 (auto-installed/updated if needed)

EXAMPLES
  # Run build and tests (auto-detect solution)
  ./Build-Local.ps1

  # Specify solution file
  ./Build-Local.ps1 -Solution ./MyApp.sln
  ./Build-Local.ps1 -s ../Other.sln

  # Filter coverage by project prefix
  ./Build-Local.ps1 -ProjectPrefix MyApp
  ./Build-Local.ps1 -p Functorium

  # Show help
  ./Build-Local.ps1 -Help
  ./Build-Local.ps1 -h

================================================================================
"@
  Write-Host $help
}

<#
.SYNOPSIS
  단계별 진행 상황을 출력합니다.
#>
function Write-StepProgress {
  param(
    [Parameter(Mandatory = $true)]
    [int]$Step,

    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "[$Step/$script:TOTAL_STEPS] $Message" -ForegroundColor Gray
}

<#
.SYNOPSIS
  상세 정보를 출력합니다.
#>
function Write-Detail {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "      $Message" -ForegroundColor DarkGray
}

<#
.SYNOPSIS
  성공 메시지를 출력합니다.
#>
function Write-Success {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "      $Message" -ForegroundColor Green
}

<#
.SYNOPSIS
  경고 메시지를 출력합니다.
#>
function Write-Warning {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message
  )

  Write-Host "      $Message" -ForegroundColor Yellow
}

#endregion

#region Step 1: Find-SolutionFile

<#
.SYNOPSIS
  솔루션 파일을 찾습니다.
.DESCRIPTION
  지정된 경로가 없으면 현재 작업 디렉토리에서 검색합니다.
.PARAMETER SolutionPath
  사용자가 지정한 솔루션 파일 경로 (옵션)
.OUTPUTS
  솔루션 파일이 1개면 해당 FileInfo 반환, 아니면 $null
#>
function Find-SolutionFile {
  param(
    [Parameter(Mandatory = $false)]
    [string]$SolutionPath
  )

  Write-StepProgress -Step 1 -Message "Finding solution file..."

  # If solution path is specified, validate and return
  if ($SolutionPath) {
    if (-not (Test-Path $SolutionPath)) {
      Write-Host "      Solution file not found: $SolutionPath" -ForegroundColor Red
      return $null
    }

    $file = Get-Item $SolutionPath
    if ($file.Extension -ne ".sln" -and $file.Extension -ne ".slnx") {
      Write-Host "      Invalid solution file: $SolutionPath (expected .sln or .slnx)" -ForegroundColor Red
      return $null
    }

    Write-Detail "Found: $($file.Name)"
    return $file
  }

  # Auto-detect: search in current working directory
  $searchPath = $PWD.Path
  Write-Detail "Searching in: $searchPath"

  $slnFiles = @(Get-ChildItem -Path $searchPath -File | Where-Object { $_.Extension -eq ".sln" -or $_.Extension -eq ".slnx" })

  if ($slnFiles.Count -eq 0) {
    Write-Host "      No solution file (.sln or .slnx) found" -ForegroundColor Red
    Write-Warning "Use -Solution parameter to specify the path"
    return $null
  }

  if ($slnFiles.Count -gt 1) {
    Write-Host "      Found $($slnFiles.Count) solution files:" -ForegroundColor Red
    $slnFiles | ForEach-Object { Write-Warning "- $($_.Name)" }
    Write-Warning "Use -Solution parameter to specify which one to use"
    return $null
  }

  Write-Detail "Found: $($slnFiles[0].Name)"
  return $slnFiles[0]
}

#endregion

#region Step 2: Invoke-Build

<#
.SYNOPSIS
  솔루션을 Release 모드로 빌드합니다.
#>
function Invoke-Build {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SolutionPath
  )

  Write-StepProgress -Step 2 -Message "Building solution ($script:Configuration)..."

  Write-Host ""
  dotnet build $SolutionPath `
    -c $script:Configuration `
    --nologo `
    -p:MinVerVerbosity=normal | Out-Default

  if ($LASTEXITCODE -ne 0) {
    throw "Build failed"
  }
}

#endregion

#region Step 3: Show-VersionInfo

<#
.SYNOPSIS
  빌드된 어셈블리에서 버전 정보를 읽어 출력합니다.
#>
function Show-VersionInfo {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SolutionPath
  )

  Write-StepProgress -Step 3 -Message "Reading version information..."

  # Get solution directory
  $solutionDir = Split-Path -Parent $SolutionPath

  # Search in Src folder only (exclude GitHub, Docs, etc.)
  $srcDir = Join-Path $solutionDir "Src"

  if (-not (Test-Path $srcDir)) {
    Write-Detail "Src folder not found, using solution directory"
    $srcDir = $solutionDir
  }

  # Build exclusion patterns relative to solution directory
  $excludePatterns = @(
    (Join-Path $solutionDir "GitHub"),
    (Join-Path $solutionDir "node_modules")
  )

  # Find all .csproj files (exclude external folders within solution)
  $projectFiles = @(Get-ChildItem -Path $srcDir -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue |
    Where-Object {
      $path = $_.FullName
      $exclude = $false
      foreach ($pattern in $excludePatterns) {
        if ($path.StartsWith($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
          $exclude = $true
          break
        }
      }
      -not $exclude
    })

  if ($projectFiles.Count -eq 0) {
    Write-Warning "No project files found in $srcDir"
    return
  }

  # Filter main projects (exclude Tests)
  $mainProjects = @($projectFiles | Where-Object { $_.Name -notlike "*Tests*" })

  if ($mainProjects.Count -eq 0) {
    Write-Warning "No main projects found"
    return
  }

  Write-Host ""
  Write-Host ("{0,-40} {1,-35} {2,-15} {3,-15}" -f "Project", "ProductVer", "FileVer", "Assembly") -ForegroundColor White
  Write-Host ("-" * 107) -ForegroundColor DarkGray

  foreach ($proj in $mainProjects) {
    try {
      $projectName = $proj.BaseName
      if ($projectName.Length -gt 38) {
        $projectName = $projectName.Substring(0, 35) + "..."
      }

      # Find built DLL in Release configuration
      $projDir = Split-Path -Parent $proj.FullName
      $dllPath = Get-ChildItem -Path $projDir -Filter "$($proj.BaseName).dll" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\bin\\$script:Configuration\\" } |
        Select-Object -First 1

      if (-not $dllPath) {
        Write-Host ("{0,-40} {1,-35} {2,-15} {3,-15}" -f $projectName, "-", "-", "Not built") -ForegroundColor DarkGray
        continue
      }

      # Read version info from DLL
      try {
        $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath.FullName)
        $fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath.FullName)

        $assemblyVersion = $assemblyName.Version.ToString()
        $fileVersion = $fileVersionInfo.FileVersion
        $productVersion = $fileVersionInfo.ProductVersion

        # Truncate long product version for display
        if ($productVersion.Length -gt 33) {
          $productVersion = $productVersion.Substring(0, 30) + "..."
        }

        Write-Host ("{0,-40} {1,-35} {2,-15} {3,-15}" -f $projectName, $productVersion, $fileVersion, $assemblyVersion)
      }
      catch {
        Write-Host ("{0,-40} {1,-35} {2,-15} {3,-15}" -f $projectName, "-", "-", "Read error") -ForegroundColor Yellow
      }
    }
    catch {
      Write-Host ("{0,-40} {1,-35} {2,-15} {3,-15}" -f $proj.BaseName, "-", "-", "Error") -ForegroundColor Yellow
    }
  }

  Write-Host ""
  Write-Detail "ProductVer: InformationalVersion (MinVer)"
  Write-Detail "FileVer: FileVersion (file properties)"
  Write-Detail "Assembly: AssemblyVersion (binary compatibility)"
}

#endregion

#region Step 4: Invoke-TestWithCoverage

<#
.SYNOPSIS
  테스트를 실행하고 코드 커버리지를 수집합니다.
#>
function Invoke-TestWithCoverage {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SolutionPath
  )

  Write-StepProgress -Step 4 -Message "Running tests with coverage..."

  # Remove existing coverage report
  if (Test-Path $script:CoverageReportDir) {
    Remove-Item -Path $script:CoverageReportDir -Recurse -Force
  }

  # Remove existing TestResults from each test project
  Get-ChildItem -Path $script:SolutionDir -Directory -Recurse -Filter "TestResults" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item -Path $_.FullName -Recurse -Force }

  # Run tests with coverage collection
  dotnet test $SolutionPath `
    --configuration $script:Configuration `
    --no-build `
    --nologo `
    --collect:"XPlat Code Coverage" `
    --logger "trx" `
    --logger "console;verbosity=minimal" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura | Out-Default

  if ($LASTEXITCODE -ne 0) {
    throw "Tests failed"
  }

  Write-Success "Tests passed"
}

#endregion

#region Step 5: Merge-CoverageReports

<#
.SYNOPSIS
  여러 커버리지 파일을 병합합니다.
#>
function Merge-CoverageReports {
  Write-StepProgress -Step 5 -Message "Merging coverage reports..."

  # Find coverage files from each test project's TestResults directory
  $coverageFiles = @(Get-ChildItem -Path $script:SolutionDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue)

  if ($coverageFiles.Count -eq 0) {
    Write-Host "      No coverage files found" -ForegroundColor Red
    return $null
  }

  Write-Detail "Found $($coverageFiles.Count) coverage file(s)"

  # Create directory
  if (-not (Test-Path $script:CoverageReportDir)) {
    New-Item -ItemType Directory -Path $script:CoverageReportDir -Force | Out-Null
  }

  # Build file path list
  $coverageFilePaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

  return $coverageFilePaths
}

#endregion

#region Step 6: New-HtmlReport

<#
.SYNOPSIS
  ReportGenerator 도구를 설치하거나 업데이트합니다.
#>
function Install-ReportGenerator {
  $requiredVersion = [version]$script:ReportGeneratorVersion

  # Check if ReportGenerator is installed
  $toolList = dotnet tool list -g 2>$null | Where-Object { $_ -match "dotnet-reportgenerator-globaltool" }

  if ($toolList) {
    # Extract installed version
    $installedVersionStr = ($toolList -split '\s+')[1]
    $installedVersion = [version]$installedVersionStr

    Write-Detail "ReportGenerator installed: v$installedVersionStr"

    if ($installedVersion -lt $requiredVersion) {
      Write-Detail "Updating to v$script:ReportGeneratorVersion..."
      dotnet tool update -g dotnet-reportgenerator-globaltool --version $script:ReportGeneratorVersion 2>$null

      if ($LASTEXITCODE -eq 0) {
        Write-Success "Updated to v$script:ReportGeneratorVersion"
      }
      else {
        Write-Warning "Failed to update ReportGenerator"
      }
    }
  }
  else {
    # Install ReportGenerator
    Write-Detail "Installing ReportGenerator v$script:ReportGeneratorVersion..."
    dotnet tool install -g dotnet-reportgenerator-globaltool --version $script:ReportGeneratorVersion 2>$null

    if ($LASTEXITCODE -eq 0) {
      Write-Success "Installed v$script:ReportGeneratorVersion"

      # Refresh PATH
      $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "User") + ";" + $env:PATH
    }
    else {
      throw "Failed to install ReportGenerator"
    }
  }
}

<#
.SYNOPSIS
  ReportGenerator를 사용하여 HTML 리포트를 생성합니다.
#>
function New-HtmlReport {
  param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageFiles
  )

  Write-StepProgress -Step 6 -Message "Generating HTML report..."

  # Install or update ReportGenerator
  Install-ReportGenerator

  # Generate HTML report
  reportgenerator `
    -reports:$CoverageFiles `
    -targetdir:$script:CoverageReportDir `
    -reporttypes:"Html;Cobertura" `
    -assemblyfilters:"-*.Tests*"

  if ($LASTEXITCODE -ne 0) {
    Write-Host "      Failed to generate HTML report" -ForegroundColor Red
    return
  }

  $reportPath = Join-Path $script:CoverageReportDir "index.html"
  Write-Success "Report generated: $reportPath"
}

#endregion

#region Step 7: Show-CoverageReport

<#
.SYNOPSIS
  콘솔에 커버리지 결과를 출력합니다.
#>
function Show-CoverageReport {
  param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageFiles,

    [Parameter(Mandatory = $false)]
    [string]$Prefix
  )

  Write-StepProgress -Step 7 -Message "Displaying coverage results..."

  # Merged cobertura file path
  $mergedCoverageFile = Join-Path $script:CoverageReportDir "Cobertura.xml"

  if (-not (Test-Path $mergedCoverageFile)) {
    # Use first coverage file
    $firstFile = $CoverageFiles.Split(";")[0]
    if (Test-Path $firstFile) {
      $mergedCoverageFile = $firstFile
    }
    else {
      Write-Host "      Coverage file not found" -ForegroundColor Red
      return
    }
  }

  # Parse XML
  [xml]$coverage = Get-Content $mergedCoverageFile

  # Extract coverage by assembly
  $packages = @($coverage.SelectNodes("//packages/package"))

  if ($packages.Count -eq 0) {
    Write-Warning "No coverage data available"
    return
  }

  # Project prefix coverage (e.g., Functorium.*)
  if ($Prefix) {
    Write-Host ""
    Write-Host "[Project Coverage] ($Prefix.*)" -ForegroundColor Yellow
    Write-Host ("{0,-40} {1,15} {2,15}" -f "Assembly", "Line Coverage", "Branch Coverage") -ForegroundColor White
    Write-Host ("-" * 72) -ForegroundColor DarkGray

    $prefixPackages = @()
    $prefixTotalLines = 0
    $prefixCoveredLines = 0
    $prefixTotalBranches = 0
    $prefixCoveredBranches = 0

    foreach ($pkg in $packages) {
      $name = $pkg.GetAttribute("name")

      # Match prefix pattern (e.g., Functorium.* but exclude tests)
      if ($name -like "$Prefix*" -and $name -notlike "*.Tests*") {
        $lineRate = [double]$pkg.GetAttribute("line-rate") * 100
        $branchRateAttr = $pkg.GetAttribute("branch-rate")
        $branchRate = if ($branchRateAttr) { [double]$branchRateAttr * 100 } else { 0 }

        Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $name, $lineRate, $branchRate)

        $prefixPackages += $pkg

        # Accumulate for total calculation
        $lines = $pkg.SelectNodes(".//line")
        foreach ($line in $lines) {
          $prefixTotalLines++
          if ([int]$line.GetAttribute("hits") -gt 0) { $prefixCoveredLines++ }
        }

        # Accumulate branch coverage
        $conditions = $pkg.SelectNodes(".//condition")
        foreach ($condition in $conditions) {
          $prefixTotalBranches++
          $cov = $condition.GetAttribute("coverage")
          if ($cov -and [double]$cov -gt 0) { $prefixCoveredBranches++ }
        }
      }
    }

    if ($prefixPackages.Count -gt 0 -and $prefixTotalLines -gt 0) {
      $prefixLineRate = ($prefixCoveredLines / $prefixTotalLines) * 100
      $prefixBranchRate = if ($prefixTotalBranches -gt 0) { ($prefixCoveredBranches / $prefixTotalBranches) * 100 } else { 0 }
      Write-Host ("-" * 72) -ForegroundColor DarkGray
      Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f "Total", $prefixLineRate, $prefixBranchRate) -ForegroundColor Green
    }
    elseif ($prefixPackages.Count -eq 0) {
      Write-Warning "No matching projects found"
    }
  }

  # Core layer coverage
  Write-Host ""
  Write-Host "[Core Layer Coverage] (Domains + Applications)" -ForegroundColor Yellow
  Write-Host ("{0,-40} {1,15} {2,15}" -f "Assembly", "Line Coverage", "Branch Coverage") -ForegroundColor White
  Write-Host ("-" * 72) -ForegroundColor DarkGray

  $corePackages = @()
  $coreTotalLines = 0
  $coreCoveredLines = 0
  $coreTotalBranches = 0
  $coreCoveredBranches = 0

  foreach ($pkg in $packages) {
    $name = $pkg.GetAttribute("name")
    $isCoreLayer = $false

    foreach ($pattern in $script:CoreLayerPatterns) {
      if ($name -like $pattern) {
        $isCoreLayer = $true
        break
      }
    }

    if ($isCoreLayer) {
      $lineRate = [double]$pkg.GetAttribute("line-rate") * 100
      $branchRateAttr = $pkg.GetAttribute("branch-rate")
      $branchRate = if ($branchRateAttr) { [double]$branchRateAttr * 100 } else { 0 }

      Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $name, $lineRate, $branchRate)

      $corePackages += $pkg

      # Accumulate for total calculation
      $lines = $pkg.SelectNodes(".//line")
      foreach ($line in $lines) {
        $coreTotalLines++
        if ([int]$line.GetAttribute("hits") -gt 0) { $coreCoveredLines++ }
      }

      # Accumulate branch coverage
      $conditions = $pkg.SelectNodes(".//condition")
      foreach ($condition in $conditions) {
        $coreTotalBranches++
        $coverageAttr = $condition.GetAttribute("coverage")
        if ($coverageAttr -and [double]$coverageAttr -gt 0) { $coreCoveredBranches++ }
      }
    }
  }

  if ($corePackages.Count -gt 0 -and $coreTotalLines -gt 0) {
    $coreLineRate = ($coreCoveredLines / $coreTotalLines) * 100
    $coreBranchRate = if ($coreTotalBranches -gt 0) { ($coreCoveredBranches / $coreTotalBranches) * 100 } else { 0 }
    Write-Host ("-" * 72) -ForegroundColor DarkGray
    Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f "Total", $coreLineRate, $coreBranchRate) -ForegroundColor Green
  }

  # Full coverage
  Write-Host ""
  Write-Host "[Full Coverage]" -ForegroundColor Yellow
  Write-Host ("{0,-40} {1,15} {2,15}" -f "Assembly", "Line Coverage", "Branch Coverage") -ForegroundColor White
  Write-Host ("-" * 72) -ForegroundColor DarkGray

  foreach ($pkg in $packages) {
    $name = $pkg.GetAttribute("name")

    # Exclude test projects
    if ($name -like "*.Tests*") { continue }

    $lineRate = [double]$pkg.GetAttribute("line-rate") * 100
    $branchRateAttr = $pkg.GetAttribute("branch-rate")
    $branchRate = if ($branchRateAttr) { [double]$branchRateAttr * 100 } else { 0 }

    Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $name, $lineRate, $branchRate)
  }

  # Overall total
  $coverageNode = $coverage.SelectSingleNode("//coverage")
  $totalLineRate = [double]$coverageNode.GetAttribute("line-rate") * 100
  $totalBranchRate = if ($coverageNode.GetAttribute("branch-rate")) { [double]$coverageNode.GetAttribute("branch-rate") * 100 } else { 0 }

  Write-Host ("-" * 72) -ForegroundColor DarkGray
  Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f "Total", $totalLineRate, $totalBranchRate) -ForegroundColor Green
}

#endregion

#region Show-Help

# Show-Help function is defined in Helper Functions region

#endregion

#region Main

<#
.SYNOPSIS
  메인 실행 함수

.DESCRIPTION
  전체 빌드/테스트/커버리지 흐름을 제어합니다:
  1. 솔루션 파일 검색
  2. 솔루션 빌드
  3. 버전 정보 표시
  4. 테스트 실행 및 커버리지 수집
  5. 커버리지 리포트 병합
  6. HTML 리포트 생성
  7. 콘솔에 커버리지 결과 표시
#>
function Main {
  param(
    [Parameter(Mandatory = $false)]
    [string]$SolutionPath,

    [Parameter(Mandatory = $false)]
    [string]$Prefix = "Functorium"
  )

  $startTime = Get-Date

  Write-Host ""
  Write-Host "[START] .NET Solution Build and Test" -ForegroundColor Blue
  Write-Host "       Started: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor DarkGray
  Write-Host ""

  # 1. Find solution file
  $solution = Find-SolutionFile -SolutionPath $SolutionPath
  if (-not $solution) {
    return $false
  }

  $solutionFullPath = $solution.FullName

  # Set output paths based on solution location
  Set-OutputPaths -SolutionPath $solutionFullPath
  Write-Detail "Coverage output: $script:CoverageReportDir"

  # 2. Build
  Invoke-Build -SolutionPath $solutionFullPath

  # 3. Show version information
  Show-VersionInfo -SolutionPath $solutionFullPath

  # 4. Run tests with coverage
  Invoke-TestWithCoverage -SolutionPath $solutionFullPath

  # 5. Merge coverage reports
  $coverageFiles = Merge-CoverageReports
  if (-not $coverageFiles) {
    Write-Warning "No coverage files found. Cannot generate report."
    return $true
  }

  # 6. Generate HTML report
  New-HtmlReport -CoverageFiles $coverageFiles

  # 7. Display coverage results in console
  Show-CoverageReport -CoverageFiles $coverageFiles -Prefix $Prefix

  # Complete
  $endTime = Get-Date
  $duration = $endTime - $startTime

  Write-Host ""
  Write-Host "[DONE] Build and test completed" -ForegroundColor Green
  Write-Host "       Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor DarkGray
  Write-Host "       Report: $script:CoverageReportDir/index.html" -ForegroundColor DarkGray
  Write-Host ""

  return $true
}

#endregion

#region Entry Point

if ($Help) {
  Show-Help
  exit 0
}

try {
  $result = Main -SolutionPath $Solution -Prefix $ProjectPrefix
  if ($result) {
    exit 0
  }
  else {
    exit 1
  }
}
catch {
  Write-Host ""
  Write-Host "[ERROR] An unexpected error occurred:" -ForegroundColor Red
  Write-Host "        $($_.Exception.Message)" -ForegroundColor Red
  Write-Host ""
  Write-Host "Stack trace:" -ForegroundColor DarkGray
  Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
  Write-Host ""
  exit 1
}

#endregion
