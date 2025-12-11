# PowerShell version of analyze-all-components.sh
# Automated analysis of all components based on configuration
# Usage: .\analyze-all-components.ps1 -BaseBranch "origin/release/1.0" -TargetBranch "origin/main"

param(
    [string]$BaseBranch = "origin/release/1.0",
    [string]$TargetBranch = "origin/main"
)

$ErrorActionPreference = "Stop"

$ToolsDir = $PSScriptRoot
$ConfigFile = Join-Path $ToolsDir "config\component-priority.json"
$AnalysisDir = Join-Path $ToolsDir "analysis-output"

Write-Host "Starting automated component analysis" -ForegroundColor Cyan
Write-Host "Using config: $ConfigFile"
Write-Host "Output directory: $AnalysisDir"
Write-Host "This may take several minutes for large repositories..."

# Start total timing
$ScriptStartTime = Get-Date

# Ensure analysis directory exists
New-Item -ItemType Directory -Force -Path $AnalysisDir | Out-Null

# Get Git root directory
$GitRoot = git rev-parse --show-toplevel 2>$null
if (-not $GitRoot) { $GitRoot = (Get-Location).Path }
$GitRoot = $GitRoot -replace '\\', '/'

# Read components from config
$Components = @()
if (Test-Path $ConfigFile) {
    Write-Host "Processing components from configuration..." -ForegroundColor Cyan
    $Config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
    $RawComponents = $Config.analysis_priorities

    foreach ($pattern in $RawComponents) {
        # Check if pattern contains wildcard characters (* or ?)
        $isGlobPattern = $pattern -match '\*|\?'

        if ($isGlobPattern) {
            # This is a glob pattern, expand it
            Write-Host "Expanding glob pattern: $pattern" -ForegroundColor Yellow
            Push-Location $GitRoot
            $expandedPaths = Get-ChildItem -Path $pattern -Directory -ErrorAction SilentlyContinue
            foreach ($path in $expandedPaths) {
                # Convert to relative path with forward slashes
                $normalizedFullPath = $path.FullName -replace '\\', '/'
                $relativePath = $normalizedFullPath -replace [regex]::Escape("$GitRoot/"), ''
                $Components += $relativePath
                Write-Host "   Found: $relativePath" -ForegroundColor Green
            }
            Pop-Location
        } else {
            # Regular path, add as-is if it exists
            $fullPath = Join-Path $GitRoot $pattern
            if (Test-Path $fullPath -PathType Container) {
                # Keep the original relative path (normalize slashes)
                $normalizedPattern = $pattern -replace '\\', '/'
                $Components += $normalizedPattern
                Write-Host "   Found: $normalizedPattern" -ForegroundColor Green
            } else {
                Write-Host "   Not found: $pattern" -ForegroundColor Yellow
            }
        }
    }

    if ($Components.Count -eq 0) {
        Write-Host "Could not read config or no valid components found, using fallback" -ForegroundColor Yellow
        $Components = @(
            "Src/Functorium",
            "Src/Functorium.Testing",
            "Docs"
        )
    }
} else {
    Write-Host "Config file not found, using fallback" -ForegroundColor Yellow
    $Components = @(
        "Src/Functorium",
        "Src/Functorium.Testing",
        "Docs"
    )
}

# Function to generate safe filename from component path
# Matches Aspire's generate_filename(): sed 's|/|-|g' | sed 's|^src-||' | sed 's|-$||'
function Get-SafeFilename {
    param([string]$Path)

    $safeName = $Path -replace '[/:\\]', '-'
    # Remove src- or Src- prefix (case-insensitive for Functorium compatibility)
    $safeName = $safeName -replace '^[Ss]rc-', ''
    # Remove trailing dash
    $safeName = $safeName -replace '-$', ''

    return $safeName
}

# Function to analyze a single component
function Analyze-Component {
    param(
        [string]$ComponentPath,
        [string]$OutputFile
    )

    $componentStart = Get-Date
    Write-Host "  Analyzing: $ComponentPath" -ForegroundColor Cyan

    # Ensure ComponentPath is relative
    $RelativeComponentPath = $ComponentPath -replace '\\', '/'
    $RelativeComponentPath = $RelativeComponentPath -replace [regex]::Escape("$GitRoot/"), ''
    $RelativeComponentPath = $RelativeComponentPath -replace '^/', ''

    Push-Location $GitRoot

    # Check for changes (use double dot for commit SHA compatibility)
    $ChangeCount = (git diff --name-status "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null | Measure-Object -Line).Lines
    $CommitCount = (git log --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null | Measure-Object -Line).Lines

    Pop-Location

    if ($ChangeCount -eq 0 -and $CommitCount -eq 0) {
        $componentEnd = Get-Date
        $elapsed = ($componentEnd - $componentStart).TotalSeconds
        Write-Host "    No changes found, skipping file creation" -ForegroundColor Gray
        Write-Host "    Completed in $([math]::Round($elapsed, 2))s"
        return $false
    }

    Write-Host "    Found $ChangeCount file changes and $CommitCount commits, creating analysis file" -ForegroundColor Green

    # Use analyze-folder.ps1 if available
    $AnalyzeFolderScript = Join-Path $ToolsDir "analyze-folder.ps1"
    if (Test-Path $AnalyzeFolderScript) {
        # Set environment variables for the script
        $env:BASE_BRANCH = $BaseBranch
        $env:TARGET_BRANCH = $TargetBranch
        & $AnalyzeFolderScript -FolderPath $RelativeComponentPath -BaseBranch $BaseBranch -TargetBranch $TargetBranch | Out-File -FilePath $OutputFile -Encoding UTF8
    } else {
        # Fallback manual analysis
        Push-Location $GitRoot

        $Content = @"
# Analysis for $RelativeComponentPath

Generated: $(Get-Date)
Comparing: $BaseBranch -> $TargetBranch

## Change Summary

"@
        $Content | Out-File -FilePath $OutputFile -Encoding UTF8

        git diff --stat "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

## All Commits

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

## Top Contributors

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --format="%an" "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null |
            Group-Object |
            Sort-Object Count -Descending |
            Select-Object -First 5 |
            ForEach-Object { "  $($_.Count) $($_.Name)" } |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

## Categorized Commits

### Feature Commits

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --grep="feat\|feature\|add" --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null |
            Select-Object -First 10 |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

### Bug Fixes

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --grep="fix\|bug" --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null |
            Select-Object -First 10 |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

### Breaking Changes

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        $BreakingChanges = git log --grep="breaking\|BREAKING" --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeComponentPath/" 2>$null |
            Select-Object -First 10
        if ($BreakingChanges) {
            $BreakingChanges | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        } else {
            "None found" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        }

        Pop-Location
    }

    $componentEnd = Get-Date
    $elapsed = ($componentEnd - $componentStart).TotalSeconds
    Write-Host "    Completed in $([math]::Round($elapsed, 2))s" -ForegroundColor Gray
    return $true
}

# Analyze all components
Write-Host "Analyzing components..." -ForegroundColor Cyan
$AnalysisStart = Get-Date

$ComponentCount = 0
$FilesCreated = 0
$TotalComponents = $Components.Count
Write-Host "Processing $TotalComponents components..."

foreach ($Component in $Components) {
    $ComponentCount++
    Write-Host "[$ComponentCount/$TotalComponents] Processing: $Component" -ForegroundColor Green

    $ComponentName = Get-SafeFilename $Component
    $OutputFile = Join-Path $AnalysisDir "$ComponentName.md"

    if (Analyze-Component -ComponentPath $Component -OutputFile $OutputFile) {
        $FilesCreated++
    }
}

$AnalysisEnd = Get-Date
$AnalysisElapsed = ($AnalysisEnd - $AnalysisStart).TotalSeconds
Write-Host "Component analysis completed in $([math]::Round($AnalysisElapsed, 2))s" -ForegroundColor Green
Write-Host "Created $FilesCreated analysis files out of $TotalComponents components"

# Generate summary report
Write-Host "Generating summary report..." -ForegroundColor Cyan
$SummaryStart = Get-Date
$SummaryFile = Join-Path $AnalysisDir "analysis-summary.md"

$SummaryContent = @"
# Component Analysis Summary

Generated on: $(Get-Date)
Branch comparison: $BaseBranch -> $TargetBranch

## Components Analyzed

"@

Push-Location $GitRoot

foreach ($Component in $Components) {
    $ComponentName = Get-SafeFilename $Component
    $ComponentFile = Join-Path $AnalysisDir "$ComponentName.md"
    if (Test-Path $ComponentFile) {
        # Use relative path for git command
        $RelativePath = $Component -replace '\\', '/'
        $RelativePath = $RelativePath -replace [regex]::Escape("$GitRoot/"), ''
        $RelativePath = $RelativePath -replace '^/', ''

        $FileCount = (git diff --name-status "$BaseBranch..$TargetBranch" -- "$RelativePath" 2>$null | Measure-Object -Line).Lines
        $SummaryContent += "- **$RelativePath** ($FileCount files) - [Analysis]($ComponentName.md)`n"
    }
}

Pop-Location

$SummaryContent += @"

## Analysis Files Generated

"@

$AnalysisFiles = Get-ChildItem "$AnalysisDir\*.md" -ErrorAction SilentlyContinue
foreach ($file in $AnalysisFiles) {
    $SummaryContent += "- $($file.Name) ($($file.Length) bytes)`n"
}

$SummaryContent | Out-File -FilePath $SummaryFile -Encoding UTF8

$SummaryEnd = Get-Date
$SummaryElapsed = ($SummaryEnd - $SummaryStart).TotalSeconds
Write-Host "Summary generation completed in $([math]::Round($SummaryElapsed, 2))s" -ForegroundColor Green

# Calculate total time
$ScriptEndTime = Get-Date
$TotalTime = ($ScriptEndTime - $ScriptStartTime).TotalSeconds

Write-Host ""
Write-Host "Component analysis complete!" -ForegroundColor Green
Write-Host "Total execution time: $([math]::Round($TotalTime, 2))s"
Write-Host ""
Write-Host "Timing Summary:"
Write-Host "   Component Analysis: $([math]::Round($AnalysisElapsed, 2))s"
Write-Host "   Summary Generation: $([math]::Round($SummaryElapsed, 2))s"
Write-Host ""
Write-Host "Summary: $SummaryFile"
Write-Host "Detailed analysis files in: $AnalysisDir/"
Write-Host ""
Write-Host "Analysis files generated:"
Get-ChildItem "$AnalysisDir\*.md" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "   $($_.Name)" }
