#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  .NET 솔루션 빌드, 테스트, 코드 커버리지 및 NuGet 패키지 생성 스크립트

.DESCRIPTION
  - Release 모드로 솔루션 빌드
  - 버전 정보 표시
  - 테스트 실행 및 코드 커버리지 수집 (Microsoft Testing Platform)
  - 핵심 레이어(Domains, Applications) 및 전체 커버리지 출력
  - HTML 리포트 생성
  - NuGet 패키지 생성 (.nupkg, .snupkg)

.PARAMETER Solution
  솔루션 파일 경로를 지정합니다. 지정하지 않으면 자동으로 검색합니다.

.PARAMETER ProjectPrefix
  커버리지 필터링용 프로젝트 접두사를 지정합니다.
  기본값: Functorium

.PARAMETER SkipPack
  NuGet 패키지 생성을 건너뜁니다.

.PARAMETER SlowTestThreshold
  느린 테스트로 판단하는 기준 시간(초)입니다.
  기본값: 30

.PARAMETER Help
  도움말을 표시합니다.

.EXAMPLE
  ./Build-Local.ps1
  현재 디렉토리에서 솔루션을 자동 검색하여 빌드, 테스트, 패키지 생성

.EXAMPLE
  ./Build-Local.ps1 -Solution ./MyApp.sln
  지정된 솔루션 파일로 빌드 및 테스트 실행

.EXAMPLE
  ./Build-Local.ps1 -SkipPack
  NuGet 패키지 생성 없이 빌드 및 테스트만 실행

.EXAMPLE
  ./Build-Local.ps1 -ProjectPrefix MyApp
  MyApp.* 프로젝트만 커버리지 필터링

.EXAMPLE
  ./Build-Local.ps1 -SlowTestThreshold 60
  60초 이상 걸리는 테스트만 느린 테스트로 분류

.EXAMPLE
  ./Build-Local.ps1 -Help
  도움말 표시

.NOTES
  Version: 2.0.0
  Requirements: PowerShell 7+, .NET SDK, ReportGenerator
  Output directories: .coverage/, .nupkg/
  License: MIT
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "솔루션 파일 경로 (.sln 또는 .slnx)")]
  [Alias("s")]
  [string]$Solution = "Functorium.slnx",

  [Parameter(Mandatory = $false, HelpMessage = "커버리지 필터링용 프로젝트 접두사")]
  [Alias("p")]
  [string]$ProjectPrefix = "Functorium",

  [Parameter(Mandatory = $false, HelpMessage = "NuGet 패키지 생성 건너뛰기")]
  [switch]$SkipPack,

  [Parameter(Mandatory = $false, HelpMessage = "느린 테스트 판단 기준 (초)")]
  [Alias("t")]
  [int]$SlowTestThreshold = 30,

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

$script:TOTAL_STEPS = 10
$script:Configuration = "Release"
$script:CoreLayerPatterns = @("*.Domain", "*.Domains", "*.Application", "*.Applications")

# These will be set after solution file is found
$script:SolutionDir = $null
$script:CoverageReportDir = $null
$script:NuGetOutputDir = $null

#endregion

#region Helper Functions

<#
.SYNOPSIS
  솔루션 파일 경로를 기준으로 출력 경로를 설정합니다.
#>
function Set-OutputPaths {
  param([string]$SolutionPath)

  $script:SolutionDir = Split-Path -Parent $SolutionPath
  $script:CoverageReportDir = Join-Path $script:SolutionDir ".coverage/reports"
  $script:NuGetOutputDir = Join-Path $script:SolutionDir ".nupkg"
}

<#
.SYNOPSIS
  도움말을 표시합니다.
#>
function Show-Help {
  $help = @"

================================================================================
 .NET Solution Build, Test, and Pack Script
================================================================================

DESCRIPTION
  Build, test, generate code coverage reports, and create NuGet packages
  for .NET solutions.

USAGE
  ./Build-Local.ps1 [options]

OPTIONS
  -Solution, -s           Path to solution file (.sln or .slnx)
                          If not specified, auto-detects from current directory
  -ProjectPrefix, -p      Project prefix for coverage filtering
                          Default: Functorium
  -SkipPack               Skip NuGet package generation
  -SlowTestThreshold, -t  Threshold in seconds to classify slow tests
                          Default: 30
  -Help, -h, -?           Show this help message

FEATURES
  1. Auto-detect solution file (requires exactly 1 .sln or .slnx file)
  2. Build in Release mode
  3. Display version information from built assemblies
  4. Run tests with code coverage collection (Microsoft Testing Platform)
  5. Generate HTML coverage report (ReportGenerator)
  6. Display coverage summary in console
     - Project: Projects matching prefix (e.g., Functorium.*)
     - Core Layer: Domains + Applications projects
     - Full: All projects (excluding tests)
  7. Generate NuGet packages (.nupkg and .snupkg)

OUTPUT
  {SolutionDir}/.coverage/
  ├── index.html            <- HTML Report
  └── Cobertura.xml         <- Merged coverage

  {SolutionDir}/.nupkg/
  ├── *.nupkg               <- NuGet packages
  └── *.snupkg              <- Symbol packages

  {SolutionDir}/Tests/{TestProject}/bin/{Configuration}/{TFM}/TestResults/
  └── coverage.cobertura.xml  <- Raw coverage (MTP format)

PREREQUISITES
  - .NET SDK
  - Tools defined in .config/dotnet-tools.json (auto-restored)

EXAMPLES
  # Run build, tests, and pack (auto-detect solution)
  ./Build-Local.ps1

  # Specify solution file
  ./Build-Local.ps1 -Solution ./MyApp.sln
  ./Build-Local.ps1 -s ../Other.sln

  # Skip NuGet package generation
  ./Build-Local.ps1 -SkipPack

  # Filter coverage by project prefix
  ./Build-Local.ps1 -ProjectPrefix MyApp
  ./Build-Local.ps1 -p Functorium

  # Set slow test threshold (default: 30s)
  ./Build-Local.ps1 -SlowTestThreshold 60
  ./Build-Local.ps1 -t 10

  # Show help
  ./Build-Local.ps1 -Help
  ./Build-Local.ps1 -h

================================================================================
"@
  Write-Host $help
}

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

  # If solution path is specified, validate and return
  if ($SolutionPath) {
    if (-not (Test-Path $SolutionPath)) {
      Write-Host "  Solution file not found: $SolutionPath" -ForegroundColor Red
      return $null
    }

    $file = Get-Item $SolutionPath
    if ($file.Extension -ne ".sln" -and $file.Extension -ne ".slnx") {
      Write-Host "  Invalid solution file: $SolutionPath (expected .sln or .slnx)" -ForegroundColor Red
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
    Write-Host "  No solution file (.sln or .slnx) found" -ForegroundColor Red
    Write-WarningMessage "Use -Solution parameter to specify the path"
    return $null
  }

  if ($slnFiles.Count -gt 1) {
    Write-Host "  Found $($slnFiles.Count) solution files:" -ForegroundColor Red
    $slnFiles | ForEach-Object { Write-WarningMessage "- $($_.Name)" }
    Write-WarningMessage "Use -Solution parameter to specify which one to use"
    return $null
  }

  Write-Detail "Found: $($slnFiles[0].Name)"
  return $slnFiles[0]
}

<#
.SYNOPSIS
  빌드된 어셈블리에서 버전 정보를 읽어 출력합니다.
#>
function Show-VersionInfo {
  param(
    [Parameter(Mandatory = $true)]
    [string]$SolutionPath
  )

  # Get solution directory
  $solutionDir = Split-Path -Parent $SolutionPath

  # Search directories: Src and Examples (exclude GitHub, Docs, etc.)
  $searchDirs = @()

  $srcDir = Join-Path $solutionDir "Src"
  if (Test-Path $srcDir) {
    $searchDirs += $srcDir
  }

  # Build exclusion patterns relative to solution directory
  $excludePatterns = @(
    (Join-Path $solutionDir "GitHub"),
    (Join-Path $solutionDir "node_modules")
  )

  # Find all .csproj files from search directories (exclude external folders)
  $projectFiles = @($searchDirs | ForEach-Object {
    Get-ChildItem -Path $_ -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue
  } | Where-Object {
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
    Write-WarningMessage "No project files found in search directories"
    return
  }

  # Filter main projects (exclude Tests)
  $mainProjects = @($projectFiles | Where-Object { $_.Name -notlike "*Tests*" })

  if ($mainProjects.Count -eq 0) {
    Write-WarningMessage "No main projects found"
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
  Write-Detail "ProductVer: InformationalVersion"
  Write-Detail "FileVer: FileVersion (file properties)"
  Write-Detail "Assembly: AssemblyVersion (binary compatibility)"
}

<#
.SYNOPSIS
  여러 커버리지 파일을 병합합니다.
#>
function Get-CoverageFiles {
  # Find coverage files from each test project's TestResults directory
  $coverageFiles = @(Get-ChildItem -Path $script:SolutionDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue)

  if ($coverageFiles.Count -eq 0) {
    Write-Host "  No coverage files found" -ForegroundColor Red
    return $null
  }

  Write-Detail "Found $($coverageFiles.Count) coverage file(s)"

  # Create directory
  if (-not (Test-Path $script:CoverageReportDir)) {
    New-Item -ItemType Directory -Path $script:CoverageReportDir -Force | Out-Null
  }

  # Use wildcard pattern instead of joining all file paths to avoid command line length limitations
  # This pattern works across different environments (local, CI/CD)
  $coveragePattern = Join-Path $script:SolutionDir "**/TestResults/coverage.cobertura.xml"

  return $coveragePattern
}

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

  # Merged cobertura file path
  $mergedCoverageFile = Join-Path $script:CoverageReportDir "Cobertura.xml"

  if (-not (Test-Path $mergedCoverageFile)) {
    # Use first coverage file
    $firstFile = $CoverageFiles.Split(";")[0]
    if (Test-Path $firstFile) {
      $mergedCoverageFile = $firstFile
    }
    else {
      Write-Host "  Coverage file not found" -ForegroundColor Red
      return
    }
  }

  # Parse XML
  [xml]$coverage = Get-Content $mergedCoverageFile

  # Extract coverage by assembly
  $packages = @($coverage.SelectNodes("//packages/package"))

  if ($packages.Count -eq 0) {
    Write-WarningMessage "No coverage data available"
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
        $lineRateAttr = $pkg.GetAttribute("line-rate")
        $branchRateAttr = $pkg.GetAttribute("branch-rate")
        $lineRate = if ($lineRateAttr) { [double]$lineRateAttr * 100 } else { 0 }
        $branchRate = if ($branchRateAttr) { [double]$branchRateAttr * 100 } else { 0 }

        Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $name, $lineRate, $branchRate)

        $prefixPackages += $pkg

        # Accumulate for total calculation
        $lines = $pkg.SelectNodes(".//line")
        foreach ($line in $lines) {
          $prefixTotalLines++
          if ([int]$line.GetAttribute("hits") -gt 0) { $prefixCoveredLines++ }
        }

        # Accumulate branch coverage from lines with branch="true"
        $branchLines = $pkg.SelectNodes(".//line[@branch='true']")
        foreach ($branchLine in $branchLines) {
          $condCoverage = $branchLine.GetAttribute("condition-coverage")
          if ($condCoverage -match '\((\d+)/(\d+)\)') {
            $prefixCoveredBranches += [int]$Matches[1]
            $prefixTotalBranches += [int]$Matches[2]
          }
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
      Write-WarningMessage "No matching projects found"
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
      $lineRateAttr = $pkg.GetAttribute("line-rate")
      $branchRateAttr = $pkg.GetAttribute("branch-rate")
      $lineRate = if ($lineRateAttr) { [double]$lineRateAttr * 100 } else { 0 }
      $branchRate = if ($branchRateAttr) { [double]$branchRateAttr * 100 } else { 0 }

      Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $name, $lineRate, $branchRate)

      $corePackages += $pkg

      # Accumulate for total calculation
      $lines = $pkg.SelectNodes(".//line")
      foreach ($line in $lines) {
        $coreTotalLines++
        if ([int]$line.GetAttribute("hits") -gt 0) { $coreCoveredLines++ }
      }

      # Accumulate branch coverage from lines with branch="true"
      $branchLines = $pkg.SelectNodes(".//line[@branch='true']")
      foreach ($branchLine in $branchLines) {
        $condCoverage = $branchLine.GetAttribute("condition-coverage")
        if ($condCoverage -match '\((\d+)/(\d+)\)') {
          $coreCoveredBranches += [int]$Matches[1]
          $coreTotalBranches += [int]$Matches[2]
        }
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

    $lineRateAttr = $pkg.GetAttribute("line-rate")
    $branchRateAttr = $pkg.GetAttribute("branch-rate")
    $lineRate = if ($lineRateAttr) { [double]$lineRateAttr * 100 } else { 0 }
    $branchRate = if ($branchRateAttr) { [double]$branchRateAttr * 100 } else { 0 }

    Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $name, $lineRate, $branchRate)
  }

  # Overall total
  $coverageNode = $coverage.SelectSingleNode("//coverage")
  $totalLineRateAttr = $coverageNode.GetAttribute("line-rate")
  $totalBranchRateAttr = $coverageNode.GetAttribute("branch-rate")
  $totalLineRate = if ($totalLineRateAttr) { [double]$totalLineRateAttr * 100 } else { 0 }
  $totalBranchRate = if ($totalBranchRateAttr) { [double]$totalBranchRateAttr * 100 } else { 0 }

  Write-Host ("-" * 72) -ForegroundColor DarkGray
  Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f "Total", $totalLineRate, $totalBranchRate) -ForegroundColor Green
}

#endregion

#region Show-Help

if ($Help) {
  Show-Help
  exit 0
}

#endregion

#region Main Execution

$startTime = Get-Date

Write-Host ""
Write-Host "[START] .NET Solution Build and Test" -ForegroundColor Blue
Write-Host "   Started: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor DarkGray
Write-Host ""

try {
  # ============================================================================
  # Step 1: Restore .NET tools
  # ============================================================================
  Write-StepProgress -Step 1 -TotalSteps $script:TOTAL_STEPS -Message "Restoring .NET tools..."

  dotnet tool restore 2>&1 | Out-Null

  if ($LASTEXITCODE -eq 0) {
    Write-Success "Tools restored"
  }
  else {
    Write-WarningMessage "Tool restore failed or no tools to restore"
  }

  # ============================================================================
  # Step 2: Find solution file
  # ============================================================================
  Write-StepProgress -Step 2 -TotalSteps $script:TOTAL_STEPS -Message "Finding solution file..."

  $solutionFile = Find-SolutionFile -SolutionPath $Solution
  if (-not $solutionFile) {
    exit 1
  }

  $solutionFullPath = $solutionFile.FullName

  # Set output paths based on solution location
  Set-OutputPaths -SolutionPath $solutionFullPath
  Write-Detail "Coverage output: $script:CoverageReportDir"

  # ============================================================================
  # Step 3: Build solution
  # ============================================================================
  Write-StepProgress -Step 3 -TotalSteps $script:TOTAL_STEPS -Message "Building solution ($script:Configuration)..."

  Write-Host ""
  dotnet build $solutionFullPath -c $script:Configuration --nologo
  Write-Host ""

  if ($LASTEXITCODE -ne 0) {
    throw "Build failed"
  }

  # ============================================================================
  # Step 4: Show version information
  # ============================================================================
  Write-StepProgress -Step 4 -TotalSteps $script:TOTAL_STEPS -Message "Reading version information..."

  Show-VersionInfo -SolutionPath $solutionFullPath

  # ============================================================================
  # Step 5: Run tests with coverage (Microsoft Testing Platform)
  # ============================================================================
  Write-StepProgress -Step 5 -TotalSteps $script:TOTAL_STEPS -Message "Running tests with coverage (MTP)..."

  # Remove existing coverage report
  if (Test-Path $script:CoverageReportDir) {
    Remove-Item -Path $script:CoverageReportDir -Recurse -Force
  }

  # Remove existing TestResults from each test project (including bin folders)
  Get-ChildItem -Path $script:SolutionDir -Directory -Recurse -Filter "TestResults" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notlike "*\node_modules\*" } |
    ForEach-Object { Remove-Item -Path $_.FullName -Recurse -Force }

  # Run tests with MTP coverage collection and TRX report
  Write-Host ""
  dotnet test `
    --solution $solutionFullPath `
    --configuration $script:Configuration `
    --no-build `
    -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx
  Write-Host ""

  if ($LASTEXITCODE -ne 0) {
    throw "Tests failed"
  }

  Write-Success "Tests passed"

  # ============================================================================
  # Step 6: Merge coverage reports
  # ============================================================================
  Write-StepProgress -Step 6 -TotalSteps $script:TOTAL_STEPS -Message "Collecting coverage reports..."

  $coverageFiles = Get-CoverageFiles
  if (-not $coverageFiles) {
    Write-WarningMessage "No coverage files found. Cannot generate report."
    exit 0
  }

  # ============================================================================
  # Step 7: Generate HTML report
  # ============================================================================
  Write-StepProgress -Step 7 -TotalSteps $script:TOTAL_STEPS -Message "Generating HTML report..."

  # Generate HTML report using local tool
  # Note: Source Generator files (*.g.cs) are excluded to avoid "does not exist" warnings
  # Verbosity set to Warning to minimize output (available: Verbose, Info, Warning, Error, Off)
  Write-Host ""
  dotnet reportgenerator `
    -reports:$coverageFiles `
    -targetdir:$script:CoverageReportDir `
    -reporttypes:"Html;Cobertura;MarkdownSummaryGithub" `
    -assemblyfilters:"-*.Tests*" `
    -filefilters:"-*.g.cs"
    #-verbosity:Warning
  Write-Host ""

  if ($LASTEXITCODE -ne 0) {
    Write-Host "  Failed to generate HTML report" -ForegroundColor Red
  }
  else {
    $reportPath = Join-Path $script:CoverageReportDir "index.html"
    Write-Success "Report generated: $reportPath"
  }

  # ============================================================================
  # Step 8: Display coverage results
  # ============================================================================
  Write-StepProgress -Step 8 -TotalSteps $script:TOTAL_STEPS -Message "Displaying coverage results..."

  Show-CoverageReport -CoverageFiles $coverageFiles -Prefix $ProjectPrefix

  # ============================================================================
  # Step 9: Analyze slow tests
  # ============================================================================
  Write-StepProgress -Step 9 -TotalSteps $script:TOTAL_STEPS -Message "Analyzing slow tests (threshold: ${SlowTestThreshold}s)..."

  $slowTestScript = Join-Path $script:SolutionDir ".coverage/scripts/SummarizeSlowestTests.cs"
  $slowTestOutputDir = Join-Path $script:SolutionDir ".coverage/reports"
  if (Test-Path $slowTestScript) {
    Push-Location (Split-Path -Parent $slowTestScript)
    try {
      dotnet $slowTestScript --output-dir $slowTestOutputDir --threshold $SlowTestThreshold 2>&1 | Out-Null
      if ($LASTEXITCODE -eq 0) {
        $slowTestReport = Join-Path $slowTestOutputDir "SummarySlowestTests.md"
        if (Test-Path $slowTestReport) {
          Write-Success "Slow test report: $slowTestReport (threshold: ${SlowTestThreshold}s)"
        }
      }
      else {
        Write-WarningMessage "Failed to generate slow test report"
      }
    }
    finally {
      Pop-Location
    }
  }
  else {
    Write-Detail "Slow test script not found, skipping"
  }

  # ============================================================================
  # Step 10: Create NuGet packages
  # ============================================================================
  if (-not $SkipPack) {
    Write-StepProgress -Step 10 -TotalSteps $script:TOTAL_STEPS -Message "Creating NuGet packages..."

    # Get solution directory
    $solutionDir = Split-Path -Parent $solutionFullPath

    # Search in Src folder only
    $srcDir = Join-Path $solutionDir "Src"

    if (-not (Test-Path $srcDir)) {
      Write-Detail "Src folder not found, using solution directory"
      $srcDir = $solutionDir
    }

    # Find packable projects (exclude Tests and Testing projects that are not meant to be published)
    $projectFiles = @(Get-ChildItem -Path $srcDir -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -notlike "*Tests*" })

    if ($projectFiles.Count -eq 0) {
      Write-WarningMessage "No packable projects found in $srcDir"
    }
    else {
      # Remove existing packages
      if (Test-Path $script:NuGetOutputDir) {
        Remove-Item -Path $script:NuGetOutputDir -Recurse -Force
      }
      New-Item -ItemType Directory -Path $script:NuGetOutputDir -Force | Out-Null

      Write-Host ""
      Write-Host ("{0,-40} {1,-15} {2}" -f "Project", "Status", "Package") -ForegroundColor White
      Write-Host ("-" * 90) -ForegroundColor DarkGray

      $packCount = 0
      $skipCount = 0

      foreach ($proj in $projectFiles) {
        $projectName = $proj.BaseName
        if ($projectName.Length -gt 38) {
          $projectName = $projectName.Substring(0, 35) + "..."
        }

        # Check if project is packable (not explicitly set to false)
        $csprojContent = Get-Content $proj.FullName -Raw
        if ($csprojContent -match '<IsPackable>false</IsPackable>') {
          Write-Host ("{0,-40} {1,-15} {2}" -f $projectName, "Skipped", "(IsPackable=false)") -ForegroundColor DarkGray
          $skipCount++
          continue
        }

        # Pack the project
        $packOutput = dotnet pack $proj.FullName `
          --configuration $script:Configuration `
          --no-build `
          --nologo `
          --output $script:NuGetOutputDir 2>&1

        if ($LASTEXITCODE -eq 0) {
          # Find generated package name from output
          $packageFile = $packOutput | Where-Object { $_ -match "Successfully created package" } |
            ForEach-Object { if ($_ -match "'([^']+\.nupkg)'") { $Matches[1] } }

          if ($packageFile) {
            $packageFileName = Split-Path -Leaf $packageFile
          }
          else {
            # Fallback: find the package in output directory
            $latestPackage = Get-ChildItem -Path $script:NuGetOutputDir -Filter "$($proj.BaseName)*.nupkg" -ErrorAction SilentlyContinue |
              Sort-Object LastWriteTime -Descending |
              Select-Object -First 1
            $packageFileName = if ($latestPackage) { $latestPackage.Name } else { "*.nupkg" }
          }

          Write-Host ("{0,-40} {1,-15} {2}" -f $projectName, "Packed", $packageFileName) -ForegroundColor Green
          $packCount++
        }
        else {
          Write-Host ("{0,-40} {1,-15} {2}" -f $projectName, "Failed", "(pack error)") -ForegroundColor Red
        }
      }

      Write-Host ""

      # List generated packages
      $packages = @(Get-ChildItem -Path $script:NuGetOutputDir -Filter "*.nupkg" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.snupkg" })
      $symbolPackages = @(Get-ChildItem -Path $script:NuGetOutputDir -Filter "*.snupkg" -ErrorAction SilentlyContinue)

      if ($packages.Count -gt 0) {
        Write-Success "Created $($packages.Count) package(s), $($symbolPackages.Count) symbol package(s)"
        Write-Detail "Output: $script:NuGetOutputDir"
      }
      else {
        Write-WarningMessage "No packages were created"
      }
    }
  }
  else {
    Write-StepProgress -Step 10 -TotalSteps $script:TOTAL_STEPS -Message "Skipping NuGet package creation..."
    Write-Detail "Use -SkipPack to skip packaging"
  }

  # ============================================================================
  # Complete
  # ============================================================================
  $endTime = Get-Date
  $duration = $endTime - $startTime

  Write-Host ""
  Write-Host "[DONE] Build, test, and pack completed" -ForegroundColor Green
  Write-Host ""
  Write-Host "  Duration    : " -NoNewline -ForegroundColor White
  Write-Host "$($duration.ToString('mm\:ss'))" -ForegroundColor Cyan
  Write-Host "  Coverage    : " -NoNewline -ForegroundColor White
  Write-Host "$(Join-Path $script:CoverageReportDir 'index.html')" -ForegroundColor Cyan
  Write-Host "  Slow Tests  : " -NoNewline -ForegroundColor White
  Write-Host "$(Join-Path $script:CoverageReportDir 'SummarySlowestTests.md')" -ForegroundColor Cyan
  if (-not $SkipPack) {
    Write-Host "  Packages    : " -NoNewline -ForegroundColor White
    Write-Host "$script:NuGetOutputDir" -ForegroundColor Cyan
  }
  Write-Host ""

  exit 0
}
catch {
  Write-Host ""
  Write-Host "[ERROR] An unexpected error occurred:" -ForegroundColor Red
  Write-Host "    $($_.Exception.Message)" -ForegroundColor Red
  Write-Host ""
  Write-Host "Stack trace:" -ForegroundColor DarkGray
  Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
  Write-Host ""
  exit 1
}

#endregion
