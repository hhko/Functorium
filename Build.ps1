#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    .NET 솔루션 빌드, 테스트 및 코드 커버리지 리포트 생성 스크립트

.DESCRIPTION
    - Release 모드로 솔루션 빌드
    - 테스트 실행 및 코드 커버리지 수집
    - 핵심 레이어(Domains, Applications) 및 전체 커버리지 출력
    - HTML 리포트 생성

.PARAMETER Solution
    솔루션 파일 경로를 지정합니다. 지정하지 않으면 자동으로 검색합니다.

.PARAMETER Help
    도움말을 표시합니다.

.EXAMPLE
    ./Build.ps1

.EXAMPLE
    ./Build.ps1 -Solution ./MyApp.sln

.EXAMPLE
    ./Build.ps1 -Help
#>

param(
    [Alias("s")]
    [string]$Solution,

    [switch]$Stable,

    [Alias("suffix")]
    [ValidateSet("dev", "alpha", "beta", "rc")]
    [string]$SuffixPrefix = "dev",

    [Alias("h", "?")]
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

#region Configuration
$script:ScriptDir = $PSScriptRoot
$script:WorkingDir = $PWD.Path
$script:Configuration = "Release"
$script:CoreLayerPatterns = @("*.Domain", "*.Domains", "*.Application", "*.Applications")
$script:ReportGeneratorVersion = "5.5.0"
$script:SectionLine = "═" * 80

# These will be set after solution file is found
$script:SolutionDir = $null
$script:TestResultsDir = $null
$script:CoverageDir = $null
$script:ReportDir = $null
#endregion

#region Helper Functions

function Set-OutputPaths {
    <#
    .SYNOPSIS
        솔루션 파일 경로를 기준으로 출력 경로를 설정합니다.
    #>
    param([string]$SolutionPath)

    $script:SolutionDir = Split-Path -Parent $SolutionPath
    $script:TestResultsDir = Join-Path $script:SolutionDir ".TestResults"
    $script:CoverageDir = Join-Path $script:TestResultsDir "coverage"
    $script:ReportDir = Join-Path $script:CoverageDir "report"
}

function Show-Help {
    <#
    .SYNOPSIS
        도움말을 표시합니다.
    #>

    $help = @"

================================================================================
 .NET Solution Build and Test Script
================================================================================

DESCRIPTION
    Build, test, and generate code coverage reports for .NET solutions.

USAGE
    ./Build.ps1 [options]

OPTIONS
    -Solution, -s    Path to solution file (.sln)
                     If not specified, auto-detects from parent directory
    -Stable          Build as stable release (no version suffix)
                     Default: false (adds version suffix)
    -SuffixPrefix    Version suffix prefix (dev, alpha, beta, rc)
    -suffix          Default: dev
    -Help, -h, -?    Show this help message

FEATURES
    1. Auto-detect solution file (requires exactly 1 .sln file)
    2. Build in Release mode
    3. Run tests with code coverage collection
    4. Generate HTML coverage report (ReportGenerator)
    5. Display coverage summary in console
       - Core Layer: Domains + Applications projects
       - Full: All projects (excluding tests)

OUTPUT
    {SolutionDir}/.TestResults/
    ├── coverage/
    │   ├── report/
    │   │   └── index.html    <- HTML Report
    │   └── Cobertura.xml     <- Merged coverage
    └── {GUID}/               <- Raw test results

PREREQUISITES
    - .NET SDK
    - ReportGenerator v5.5.0 (auto-installed/updated if needed)

EXAMPLES
    # Run build and tests (auto-detect solution, dev suffix)
    ./Build.ps1
    # Result: 1.0.1-dev-20251125-143052

    # Build with alpha pre-release suffix
    ./Build.ps1 -SuffixPrefix alpha
    # Result: 1.0.1-alpha-20251125-143052

    # Build as stable release (no version suffix)
    ./Build.ps1 -Stable
    # Result: 1.0.1

    # Specify solution file
    ./Build.ps1 -Solution ./MyApp.sln
    ./Build.ps1 -s ../Other.sln

    # Show help
    ./Build.ps1 -Help
    ./Build.ps1 -h

================================================================================
"@
    Write-Host $help
}

function Write-StepHeader {
    param([string]$Title)

    Write-Host ""
    Write-Host $script:SectionLine -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host $script:SectionLine -ForegroundColor Cyan
}

function Write-SubHeader {
    param([string]$Title)

    Write-Host ""
    Write-Host $Title -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host $Message -ForegroundColor White
}

function Get-VersionSuffix {
    <#
    .SYNOPSIS
        Stable이 false일 때 VersionSuffix를 생성합니다.
    .DESCRIPTION
        형식: {prefix}-yyyyMMdd-HHmmss (예: dev-20251125-143052)
    #>
    param(
        [bool]$IsStable,
        [string]$Prefix = "dev"
    )

    if ($IsStable) {
        return $null
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    return "$Prefix-$timestamp"
}

#endregion

#region Core Functions

function Find-SolutionFile {
    <#
    .SYNOPSIS
        솔루션 파일을 찾습니다. 지정된 경로가 없으면 스크립트 상위 디렉토리에서 검색합니다.
    .PARAMETER SolutionPath
        사용자가 지정한 솔루션 파일 경로 (옵션)
    .OUTPUTS
        솔루션 파일이 1개면 해당 FileInfo 반환, 아니면 $null
    #>
    param([string]$SolutionPath)

    # If solution path is specified, validate and return
    if ($SolutionPath) {
        if (-not (Test-Path $SolutionPath)) {
            Write-Host "Solution file not found: $SolutionPath" -ForegroundColor Red
            return $null
        }

        $file = Get-Item $SolutionPath
        if ($file.Extension -ne ".sln") {
            Write-Host "Invalid solution file: $SolutionPath" -ForegroundColor Red
            return $null
        }

        return $file
    }

    # Auto-detect: search in current working directory
    $searchPath = $script:WorkingDir
    Write-Info "Searching for solution in: $searchPath"

    $slnFiles = @(Get-ChildItem -Path $searchPath -Filter "*.sln" -File)

    if ($slnFiles.Count -eq 0) {
        Write-Host "No solution file found in: $searchPath" -ForegroundColor Red
        Write-Host "Use -Solution parameter to specify the path." -ForegroundColor Yellow
        return $null
    }

    if ($slnFiles.Count -gt 1) {
        Write-Host "Found $($slnFiles.Count) solution files:" -ForegroundColor Red
        $slnFiles | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }
        Write-Host "Use -Solution parameter to specify which one to use." -ForegroundColor Yellow
        return $null
    }

    return $slnFiles[0]
}

function Invoke-Build {
    <#
    .SYNOPSIS
        솔루션을 Release 모드로 빌드합니다.
    #>
    param(
        [string]$SolutionPath,
        [string]$VersionSuffix
    )

    Write-StepHeader "Build Solution ($script:Configuration)"
    Write-Info "Solution: $SolutionPath"
    if ($VersionSuffix) {
        Write-Info "VersionSuffix: $VersionSuffix"
    }

    dotnet restore $SolutionPath

    $buildArgs = @(
        $SolutionPath
        "-c", $script:Configuration
        "--nologo"
        "-v:q"
    )

    if ($VersionSuffix) {
        $buildArgs += "-p:VersionSuffix=$VersionSuffix"
    }

    dotnet build @buildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }

    Write-Success "Build succeeded"
}

function Invoke-TestWithCoverage {
    <#
    .SYNOPSIS
        테스트를 실행하고 코드 커버리지를 수집합니다.
    #>
    param([string]$SolutionPath)

    Write-StepHeader "Run Tests with Coverage"

    # Remove existing test results
    if (Test-Path $script:TestResultsDir) {
        Remove-Item -Path $script:TestResultsDir -Recurse -Force
    }

    # Run tests with coverage collection
    dotnet test $SolutionPath `
        -c $script:Configuration `
        --no-build `
        --nologo `
        --results-directory $script:TestResultsDir `
        --collect:"XPlat Code Coverage" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed"
    }

    Write-Success "Tests passed"
}

function Merge-CoverageReports {
    <#
    .SYNOPSIS
        여러 커버리지 파일을 병합합니다.
    #>

    Write-StepHeader "Merge Coverage Reports"

    # Find coverage files
    $coverageFiles = @(Get-ChildItem -Path $script:TestResultsDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue)

    if ($coverageFiles.Count -eq 0) {
        Write-Host "No coverage files found." -ForegroundColor Red
        return $null
    }

    Write-Info "Found coverage files: $($coverageFiles.Count)"

    # Create directory
    if (-not (Test-Path $script:CoverageDir)) {
        New-Item -ItemType Directory -Path $script:CoverageDir -Force | Out-Null
    }

    # Build file path list
    $coverageFilePaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

    return $coverageFilePaths
}

function Install-ReportGenerator {
    <#
    .SYNOPSIS
        ReportGenerator 도구를 설치하거나 업데이트합니다.
    #>

    $requiredVersion = [version]$script:ReportGeneratorVersion

    # Check if ReportGenerator is installed
    $toolList = dotnet tool list -g 2>$null | Where-Object { $_ -match "dotnet-reportgenerator-globaltool" }

    if ($toolList) {
        # Extract installed version
        $installedVersionStr = ($toolList -split '\s+')[1]
        $installedVersion = [version]$installedVersionStr

        Write-Info "ReportGenerator installed: v$installedVersionStr"

        if ($installedVersion -lt $requiredVersion) {
            Write-Info "Updating ReportGenerator to v$script:ReportGeneratorVersion..."
            dotnet tool update -g dotnet-reportgenerator-globaltool --version $script:ReportGeneratorVersion 2>$null

            if ($LASTEXITCODE -eq 0) {
                Write-Success "ReportGenerator updated to v$script:ReportGeneratorVersion"
            } else {
                Write-Host "Failed to update ReportGenerator" -ForegroundColor Yellow
            }
        } elseif ($installedVersion -gt $requiredVersion) {
            Write-Info "Installed version (v$installedVersionStr) is newer than required (v$script:ReportGeneratorVersion)"
        } else {
            Write-Info "ReportGenerator is up to date (v$script:ReportGeneratorVersion)"
        }
    } else {
        # Install ReportGenerator
        Write-Info "Installing ReportGenerator v$script:ReportGeneratorVersion..."
        dotnet tool install -g dotnet-reportgenerator-globaltool --version $script:ReportGeneratorVersion 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Success "ReportGenerator v$script:ReportGeneratorVersion installed"

            # Refresh PATH
            $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "User") + ";" + $env:PATH
        } else {
            throw "Failed to install ReportGenerator"
        }
    }
}

function New-HtmlReport {
    <#
    .SYNOPSIS
        ReportGenerator를 사용하여 HTML 리포트를 생성합니다.
    #>
    param([string]$CoverageFiles)

    Write-StepHeader "Generate HTML Report"

    # Install or update ReportGenerator
    Install-ReportGenerator

    # Generate HTML report
    reportgenerator `
        -reports:$CoverageFiles `
        -targetdir:$script:ReportDir `
        -reporttypes:"Html;Cobertura" `
        -assemblyfilters:"-*.Tests*"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to generate HTML report" -ForegroundColor Red
        return
    }

    $reportPath = Join-Path $script:ReportDir "index.html"
    Write-Success "HTML report generated"
    Write-Info "Report path: $reportPath"
}

function Show-CoverageReport {
    <#
    .SYNOPSIS
        콘솔에 커버리지 결과를 출력합니다.
    #>
    param([string]$CoverageFiles)

    Write-StepHeader "Code Coverage Results"

    # Merged cobertura file path
    $mergedCoverageFile = Join-Path $script:ReportDir "Cobertura.xml"

    if (-not (Test-Path $mergedCoverageFile)) {
        # Use first coverage file
        $firstFile = $CoverageFiles.Split(";")[0]
        if (Test-Path $firstFile) {
            $mergedCoverageFile = $firstFile
        } else {
            Write-Host "Coverage file not found." -ForegroundColor Red
            return
        }
    }

    # Parse XML
    [xml]$coverage = Get-Content $mergedCoverageFile

    # Extract coverage by assembly
    $packages = @($coverage.SelectNodes("//packages/package"))

    if ($packages.Count -eq 0) {
        Write-Host "No coverage data available." -ForegroundColor Yellow
        return
    }

    # Core layer coverage
    Write-SubHeader "[Core Layer Coverage] (Domains + Applications)"
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
                $coverage = $condition.GetAttribute("coverage")
                if ($coverage -and [double]$coverage -gt 0) { $coreCoveredBranches++ }
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
    Write-SubHeader "[Full Coverage]"
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

#region Main

function Main {
    <#
    .SYNOPSIS
        메인 진입점 - 전체 빌드/테스트/커버리지 흐름을 제어합니다.
    #>

    $startTime = Get-Date

    Write-Host ""
    Write-Host "═════════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host " .NET Solution Build and Test Script" -ForegroundColor Green
    Write-Host " Started: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Green
    Write-Host "═════════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green

    try {
        # 1. Find solution file
        $solution = Find-SolutionFile -SolutionPath $Solution
        if (-not $solution) {
            exit 1
        }

        $solutionPath = $solution.FullName
        Write-Info "Selected solution: $($solution.Name)"

        # Set output paths based on solution location
        Set-OutputPaths -SolutionPath $solutionPath
        Write-Info "Output directory: $script:TestResultsDir"

        # VersionSuffix 생성
        $versionSuffix = Get-VersionSuffix -IsStable $Stable.IsPresent -Prefix $SuffixPrefix
        if ($versionSuffix) {
            Write-Info "Version mode: Pre-release ($versionSuffix)"
        } else {
            Write-Info "Version mode: Stable (production)"
        }

        # 2. Build
        Invoke-Build -SolutionPath $solutionPath -VersionSuffix $versionSuffix

        # 3. Run tests with coverage
        Invoke-TestWithCoverage -SolutionPath $solutionPath

        # 4. Merge coverage reports
        $coverageFiles = Merge-CoverageReports
        if (-not $coverageFiles) {
            Write-Host "No coverage files found. Cannot generate report." -ForegroundColor Yellow
            exit 0
        }

        # 5. Generate HTML report
        New-HtmlReport -CoverageFiles $coverageFiles

        # 6. Display coverage results in console
        Show-CoverageReport -CoverageFiles $coverageFiles

        # Complete
        $endTime = Get-Date
        $duration = $endTime - $startTime

        Write-Host ""
        Write-Host "═════════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
        Write-Host " Completed!" -ForegroundColor Green
        Write-Host " Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Green
        Write-Host " HTML Report: $script:ReportDir/index.html" -ForegroundColor Green
        Write-Host "═════════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green

    } catch {
        Write-Host ""
        Write-Host "Error: $_" -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
        exit 1
    }
}

# Run script
if ($Help) {
    Show-Help
    exit 0
}

Main

#endregion
