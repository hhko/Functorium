#!/usr/bin/env pwsh

<#
.SYNOPSIS
  Builds, tests, generates coverage reports and NuGet packages for .NET solution.

.DESCRIPTION
  Runs the full local build pipeline: Release build, test execution,
  code coverage report generation, and NuGet package creation.

  Steps:
  1. Restore .NET tools (.config/dotnet-tools.json)
  2. Find solution file (auto-detect or -Solution)
  3. Release mode build
  4. Display version information
  5. Test + code coverage collection (Microsoft Testing Platform)
  6. Generate HTML coverage report (ReportGenerator)
  7. Display coverage summary (per-project + total)
  8. Analyze slow tests
  9. Create NuGet packages (.nupkg, .snupkg)

  Output directories:
    {SolutionDir}/.coverage/reports/  - HTML reports, Cobertura.xml
    {SolutionDir}/.nupkg/             - NuGet packages

.PARAMETER Solution
  Path to the solution file. Auto-detected if not specified.

.PARAMETER ProjectPrefix
  Project prefix for coverage filtering.
  Default: Functorium

.PARAMETER SkipPack
  Skips NuGet package creation.

.PARAMETER SlowTestThreshold
  Threshold in seconds for slow test detection.
  Default: 30

.EXAMPLE
  ./Build-Local.ps1

  Auto-detects solution and runs build, test, and package creation.

.EXAMPLE
  ./Build-Local.ps1 -s ./MyApp.slnx

  Builds and tests with the specified solution file.

.EXAMPLE
  ./Build-Local.ps1 -SkipPack

  Runs build and test without NuGet package creation.

.EXAMPLE
  ./Build-Local.ps1 -p MyApp -t 60

  Filters coverage to MyApp.* projects and classifies tests over 60s as slow.

.NOTES
  Requirements: PowerShell 7+, .NET SDK
  Prerequisites: .config/dotnet-tools.json (ReportGenerator etc.)
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "Solution file path (.sln or .slnx)")]
  [Alias("s")]
  [string]$Solution = "Functorium.slnx",

  [Parameter(Mandatory = $false, HelpMessage = "Project prefix for coverage filtering")]
  [Alias("p")]
  [string]$ProjectPrefix = "Functorium",

  [Parameter(Mandatory = $false, HelpMessage = "Skip NuGet package creation")]
  [switch]$SkipPack,

  [Parameter(Mandatory = $false, HelpMessage = "Slow test threshold in seconds")]
  [Alias("t")]
  [int]$SlowTestThreshold = 30
)

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
    if (-not (Test-Path $searchPath)) { continue }
    $found = Get-ChildItem -Path $searchPath -Directory -Recurse -Filter $DirectoryName -ErrorAction SilentlyContinue
    foreach ($dir in $found) { $results.Add($dir.FullName) }
  }
  return $results.ToArray()
}

function Remove-DirectoriesByName {
  [CmdletBinding(SupportsShouldProcess)]
  param(
    [Parameter(Mandatory = $true)]
    [string[]]$SearchPaths,
    [Parameter(Mandatory = $true)]
    [string]$DirectoryName
  )
  $targetPaths = @(Find-DirectoriesByName -SearchPaths $SearchPaths -DirectoryName $DirectoryName)
  if ($targetPaths.Count -eq 0) { return 0 }
  $deletedCount = 0
  foreach ($path in $targetPaths) {
    if ($PSCmdlet.ShouldProcess($path, "Delete directory")) {
      if (Remove-DirectorySafely -Path $path) { $deletedCount++ }
    }
  }
  return $deletedCount
}

#endregion

#region Constants

$script:TOTAL_STEPS = 10
$script:Configuration = "Release"

# These will be set after solution file is found
$script:SolutionDir = $null
$script:CoverageReportDir = $null
$script:NuGetOutputDir = $null

#endregion

#region Helper Functions

# 솔루션 파일 경로를 기준으로 출력 경로를 설정합니다.
function Set-OutputPaths {
  param([string]$SolutionPath)

  $script:SolutionDir = Split-Path -Parent $SolutionPath
  $script:CoverageReportDir = Join-Path $script:SolutionDir ".coverage/reports"
  $script:NuGetOutputDir = Join-Path $script:SolutionDir ".nupkg"
}

# 솔루션 파일을 찾습니다.
# 지정된 경로가 없으면 현재 작업 디렉토리에서 검색합니다.
# SolutionPath: 사용자가 지정한 솔루션 파일 경로 (옵션)
# 솔루션 파일이 1개면 해당 FileInfo 반환, 아니면 $null
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

# 빌드된 어셈블리에서 버전 정보를 읽어 출력합니다.
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

# Src 및 Tests 폴더에서 커버리지 파일을 수집합니다.
# 검색 범위를 Src와 Tests 폴더로 제한하여 메모리 사용량을 줄입니다.
function Get-CoverageFiles {
  # Search only in Src and Tests folders to avoid heap corruption with large solutions
  $searchPaths = @(
    (Join-Path $script:SolutionDir "Src"),
    (Join-Path $script:SolutionDir "Tests")
  ) | Where-Object { Test-Path $_ }

  if ($searchPaths.Count -eq 0) {
    Write-Host "  No Src or Tests folder found" -ForegroundColor Red
    return $null
  }

  # Find coverage files from Src and Tests folders only
  $coverageFiles = @($searchPaths | ForEach-Object {
    Get-ChildItem -Path $_ -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
  })

  if ($coverageFiles.Count -eq 0) {
    Write-Host "  No coverage files found in Src/Tests folders" -ForegroundColor Red
    return $null
  }

  Write-Detail "Found $($coverageFiles.Count) coverage file(s) in Src/Tests"

  # Create directory
  if (-not (Test-Path $script:CoverageReportDir)) {
    New-Item -ItemType Directory -Path $script:CoverageReportDir -Force | Out-Null
  }

  # Use glob pattern for Tests folder only (Src folder has no TestResults)
  # ReportGenerator accepts glob patterns with semicolon separator
  $testsDir = Join-Path $script:SolutionDir "Tests"
  return Join-Path $testsDir "**\TestResults\coverage.cobertura.xml"
}

# 콘솔에 커버리지 결과를 출력합니다.
# 병합된 Cobertura XML에서 패키지별 커버리지를 읽어 출력합니다.
# 메모리 효율을 위해 package 노드의 속성만 사용하고,
# line 노드를 직접 순회하지 않습니다.
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

  # Parse XML using XmlReader for memory efficiency
  $xmlSettings = [System.Xml.XmlReaderSettings]::new()
  $xmlSettings.DtdProcessing = [System.Xml.DtdProcessing]::Ignore

  # Read package data into lightweight objects
  $packageData = [System.Collections.Generic.List[PSObject]]::new()

  try {
    $reader = [System.Xml.XmlReader]::Create($mergedCoverageFile, $xmlSettings)

    while ($reader.Read()) {
      if ($reader.NodeType -eq [System.Xml.XmlNodeType]::Element -and $reader.Name -eq "package") {
        $name = $reader.GetAttribute("name")
        $lineRate = [double]($reader.GetAttribute("line-rate") ?? "0")
        $branchRate = [double]($reader.GetAttribute("branch-rate") ?? "0")

        $packageData.Add([PSCustomObject]@{
          Name = $name
          LineRate = $lineRate
          BranchRate = $branchRate
        })
      }
    }
  }
  finally {
    if ($reader) { $reader.Dispose() }
  }

  if ($packageData.Count -eq 0) {
    Write-WarningMessage "No coverage data available"
    return
  }

  # Helper function to calculate average coverage
  function Get-AverageCoverage {
    param([System.Collections.Generic.List[PSObject]]$Packages)

    if ($Packages.Count -eq 0) { return @{ LineRate = 0; BranchRate = 0 } }

    $totalLineRate = 0.0
    $totalBranchRate = 0.0

    foreach ($pkg in $Packages) {
      $totalLineRate += $pkg.LineRate
      $totalBranchRate += $pkg.BranchRate
    }

    return @{
      LineRate = ($totalLineRate / $Packages.Count) * 100
      BranchRate = ($totalBranchRate / $Packages.Count) * 100
    }
  }

  # Project prefix coverage (e.g., Functorium.*)
  if ($Prefix) {
    Write-Host ""
    Write-Host "[Project Coverage] ($Prefix.*)" -ForegroundColor Yellow
    Write-Host ("{0,-40} {1,15} {2,15}" -f "Assembly", "Line Coverage", "Branch Coverage") -ForegroundColor White
    Write-Host ("-" * 72) -ForegroundColor DarkGray

    $prefixPackages = [System.Collections.Generic.List[PSObject]]::new()

    foreach ($pkg in $packageData) {
      # Match prefix pattern (e.g., Functorium.* but exclude tests)
      if ($pkg.Name -like "$Prefix*" -and $pkg.Name -notlike "*.Tests*") {
        $lineRate = $pkg.LineRate * 100
        $branchRate = $pkg.BranchRate * 100

        Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $pkg.Name, $lineRate, $branchRate)
        $prefixPackages.Add($pkg)
      }
    }

    if ($prefixPackages.Count -gt 0) {
      $avg = Get-AverageCoverage -Packages $prefixPackages
      Write-Host ("-" * 72) -ForegroundColor DarkGray
      Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f "Total (avg)", $avg.LineRate, $avg.BranchRate) -ForegroundColor Green
    }
    else {
      Write-WarningMessage "No matching projects found"
    }
  }

  # Full coverage
  Write-Host ""
  Write-Host "[Full Coverage]" -ForegroundColor Yellow
  Write-Host ("{0,-40} {1,15} {2,15}" -f "Assembly", "Line Coverage", "Branch Coverage") -ForegroundColor White
  Write-Host ("-" * 72) -ForegroundColor DarkGray

  $fullPackages = [System.Collections.Generic.List[PSObject]]::new()

  foreach ($pkg in $packageData) {
    # Exclude test projects
    if ($pkg.Name -like "*.Tests*") { continue }

    $lineRate = $pkg.LineRate * 100
    $branchRate = $pkg.BranchRate * 100

    Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f $pkg.Name, $lineRate, $branchRate)
    $fullPackages.Add($pkg)
  }

  # Overall total (use average of non-test packages)
  if ($fullPackages.Count -gt 0) {
    $avg = Get-AverageCoverage -Packages $fullPackages
    Write-Host ("-" * 72) -ForegroundColor DarkGray
    Write-Host ("{0,-40} {1,14:N1}% {2,14:N1}%" -f "Total (avg)", $avg.LineRate, $avg.BranchRate) -ForegroundColor Green
  }
}

#endregion

#region Main

function Main {

$startTime = Get-Date

Write-Host ""
Write-Host "[START] .NET Solution Build and Test" -ForegroundColor Blue
Write-Host "   Started: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor DarkGray
Write-Host ""
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
    throw "Solution file not found"
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

  # Remove existing coverage report (using .NET Directory.Delete to avoid heap corruption)
  if (Test-Path $script:CoverageReportDir) {
    Remove-DirectorySafely -Path $script:CoverageReportDir | Out-Null
  }

  # Remove existing TestResults folders (search and delete in separate phases to avoid heap corruption)
  $searchPaths = @(
    (Join-Path $script:SolutionDir "Src"),
    (Join-Path $script:SolutionDir "Tests")
  ) | Where-Object { Test-Path $_ }

  $deletedCount = Remove-DirectoriesByName -SearchPaths $searchPaths -DirectoryName "TestResults"
  if ($deletedCount -gt 0) {
    Write-Detail "Removed $deletedCount TestResults folder(s)"
  }

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
    return
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
      # Remove existing packages (using .NET Directory.Delete to avoid heap corruption)
      if (Test-Path $script:NuGetOutputDir) {
        Remove-DirectorySafely -Path $script:NuGetOutputDir | Out-Null
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
    Write-Detail "Skipped (-SkipPack)"
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
