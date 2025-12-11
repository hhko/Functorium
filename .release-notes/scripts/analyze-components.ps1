# PowerShell version of analyze-components.sh for Windows
# Automated analysis of all components based on configuration
# Usage: .\analyze-components.ps1 -BaseBranch "origin/release/1.0" -TargetBranch "origin/main"

param(
    [string]$BaseBranch = "origin/release/1.0",
    [string]$TargetBranch = "origin/main"
)

$ErrorActionPreference = "Stop"

$ScriptsDir = $PSScriptRoot
$ReleaseNotesDir = Split-Path $ScriptsDir -Parent
$ConfigFile = Join-Path $ScriptsDir "config\component-priority.json"
$AnalysisDir = Join-Path $ReleaseNotesDir "analysis-output"

Write-Host "🔍 Starting automated component analysis" -ForegroundColor Cyan
Write-Host "📋 Using config: $ConfigFile"
Write-Host "📊 Output directory: $AnalysisDir"
Write-Host "⏱️  This may take several minutes for large repositories..."

$ScriptStartTime = Get-Date

# Ensure analysis directory exists
New-Item -ItemType Directory -Force -Path $AnalysisDir | Out-Null

# Read components from config
$Components = @()
if (Test-Path $ConfigFile) {
    Write-Host "📊 Processing components from configuration..." -ForegroundColor Cyan
    $Config = Get-Content $ConfigFile | ConvertFrom-Json
    $RawComponents = $Config.analysis_priorities

    # Expand glob patterns to actual directories
    $GitRoot = git rev-parse --show-toplevel 2>$null
    if (-not $GitRoot) { $GitRoot = "." }

    foreach ($pattern in $RawComponents) {
        if ($pattern -like "*`**") {
            # This is a glob pattern, expand it
            Write-Host "🔍 Expanding glob pattern: $pattern" -ForegroundColor Yellow
            Push-Location $GitRoot
            $expandedPaths = Get-ChildItem -Path $pattern -Directory -ErrorAction SilentlyContinue
            foreach ($path in $expandedPaths) {
                $relativePath = $path.FullName.Replace("$GitRoot\", "").Replace("\", "/")
                $Components += $relativePath
                Write-Host "   ✅ Found: $relativePath" -ForegroundColor Green
            }
            Pop-Location
        } else {
            # Regular path, add as-is if it exists
            $fullPath = Join-Path $GitRoot $pattern
            if (Test-Path $fullPath -PathType Container) {
                $Components += $pattern
                Write-Host "   ✅ Found: $pattern" -ForegroundColor Green
            }
        }
    }

    if ($Components.Count -eq 0) {
        Write-Host "⚠️  Could not read config or no valid components found, using fallback" -ForegroundColor Yellow
        $Components = @(
            "src/MyProject.Core",
            "src/MyProject.Extensions",
            "src/MyProject.Integrations"
        )
    }
} else {
    Write-Host "⚠️  Config file not found, using fallback" -ForegroundColor Yellow
    $Components = @(
        "src/MyProject.Core",
        "src/MyProject.Extensions",
        "src/MyProject.Integrations"
    )
}

# Function to analyze a single component
function Analyze-Component {
    param(
        [string]$ComponentPath,
        [string]$OutputFile
    )

    $componentStart = Get-Date
    Write-Host "  📁 Analyzing: $ComponentPath" -ForegroundColor Cyan

    $GitRoot = git rev-parse --show-toplevel 2>$null
    if (-not $GitRoot) { $GitRoot = "." }

    Push-Location $GitRoot

    # Check for changes
    $ChangeCount = (git diff --name-status "$BaseBranch..$TargetBranch" -- "$ComponentPath/" 2>$null | Measure-Object -Line).Lines
    $CommitCount = (git log --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$ComponentPath/" 2>$null | Measure-Object -Line).Lines

    Pop-Location

    if ($ChangeCount -eq 0 -and $CommitCount -eq 0) {
        $componentEnd = Get-Date
        $elapsed = ($componentEnd - $componentStart).TotalSeconds
        Write-Host "    ⏭️  No changes found, skipping file creation" -ForegroundColor Gray
        Write-Host "    ⏱️  Completed in $([math]::Round($elapsed, 2))s"
        return $false
    }

    Write-Host "    ✅ Found $ChangeCount file changes and $CommitCount commits, creating analysis file" -ForegroundColor Green

    # Check if analyze-folder.ps1 exists
    $AnalyzeFolderScript = Join-Path $ScriptsDir "analyze-folder.ps1"
    if (Test-Path $AnalyzeFolderScript) {
        # Use analyze-folder.ps1
        $env:BASE_BRANCH = $BaseBranch
        $env:TARGET_BRANCH = $TargetBranch
        & $AnalyzeFolderScript -FolderPath $ComponentPath -BaseBranch $BaseBranch -TargetBranch $TargetBranch | Out-File -FilePath $OutputFile -Encoding UTF8
    } else {
        # Fallback manual analysis
        Push-Location $GitRoot

        $Content = @"
# Analysis for $ComponentPath

Generated: $(Get-Date)
Comparing: $BaseBranch → $TargetBranch

## Change Summary

"@
        $Content | Out-File -FilePath $OutputFile -Encoding UTF8

        git diff --stat "$BaseBranch..$TargetBranch" -- "$ComponentPath/" 2>$null | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

## All Commits (Chronological)

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$ComponentPath/" 2>$null | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

## Top Contributors

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --format="%an" --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$ComponentPath/" 2>$null |
            Group-Object |
            Sort-Object Count -Descending |
            Select-Object -First 5 |
            ForEach-Object { "  $($_.Count) $($_.Name)" } |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

## Categorized Commits

### Feature Commits

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --grep="feat\|feature\|add" --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$ComponentPath/" 2>$null |
            Select-Object -First 10 |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

### Bug Fixes

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --grep="fix\|bug" --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$ComponentPath/" 2>$null |
            Select-Object -First 10 |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        @"

### Breaking Changes

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

        git log --grep="breaking\|BREAKING" --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$ComponentPath/" 2>$null |
            Select-Object -First 10 |
            Out-File -FilePath $OutputFile -Append -Encoding UTF8

        # Add playground/test examples if available
        if ($ComponentPath -like "playground/*" -or $ComponentPath -like "tests/*") {
            @"

## Notable Changes

"@ | Out-File -FilePath $OutputFile -Append -Encoding UTF8

            git diff --name-status "$BaseBranch..$TargetBranch" -- "$ComponentPath/" 2>$null |
                Where-Object { $_ -match "^A" } |
                Select-Object -First 10 |
                Out-File -FilePath $OutputFile -Append -Encoding UTF8
        }

        Pop-Location
    }

    $componentEnd = Get-Date
    $elapsed = ($componentEnd - $componentStart).TotalSeconds
    Write-Host "    ⏱️  Completed in $([math]::Round($elapsed, 2))s" -ForegroundColor Gray
    return $true
}

# Function to generate safe filename
function Get-SafeFilename {
    param([string]$Path)

    $GitRoot = git rev-parse --show-toplevel 2>$null
    $component = $Path

    # If component is an absolute path, try to make it relative to git root
    if ($component -match '^[A-Za-z]:' -or $component -match '^/') {
        if ($GitRoot) {
            # Normalize paths for comparison
            $normalizedGitRoot = $GitRoot -replace '\\', '/'
            $normalizedComponent = $component -replace '\\', '/'

            # Remove git root prefix if present
            if ($normalizedComponent.StartsWith($normalizedGitRoot + '/')) {
                $component = $normalizedComponent.Substring($normalizedGitRoot.Length + 1)
            } elseif ($normalizedComponent.StartsWith($normalizedGitRoot)) {
                $component = $normalizedComponent.Substring($normalizedGitRoot.Length)
            }
        }
    }

    # Generate safe filename: replace slashes, backslashes, and colons
    $safeName = $component -replace '[/:\\]', '-'
    # Remove src- prefix if present
    $safeName = $safeName -replace '^src-', ''
    # Remove trailing dash
    $safeName = $safeName -replace '-$', ''
    # Remove drive letter prefix (e.g., "C-" or "E-")
    $safeName = $safeName -replace '^[A-Za-z]-', ''

    return $safeName
}

# Analyze all components
Write-Host "🎯 Analyzing components..." -ForegroundColor Cyan
$AnalysisStart = Get-Date

$ComponentCount = 0
$FilesCreated = 0
$TotalComponents = $Components.Count
Write-Host "📊 Processing $TotalComponents components..."

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
Write-Host "✅ Component analysis completed in $([math]::Round($AnalysisElapsed, 2))s" -ForegroundColor Green
Write-Host "📊 Created $FilesCreated analysis files out of $TotalComponents components"

# Generate summary report
Write-Host "📊 Generating summary report..." -ForegroundColor Cyan
$SummaryStart = Get-Date
$SummaryFile = Join-Path $AnalysisDir "summary.md"

$SummaryContent = @"
# Release Notes Analysis Summary

Generated: $(Get-Date)
Comparison: $BaseBranch → $TargetBranch

## Components Analyzed

"@

foreach ($Component in $Components) {
    $ComponentName = Get-SafeFilename $Component
    $ComponentFile = Join-Path $AnalysisDir "$ComponentName.md"
    if (Test-Path $ComponentFile) {
        # Extract actual component path from analysis file
        $actualPath = (Select-String -Path $ComponentFile -Pattern "📁 ANALYZING:" -ErrorAction SilentlyContinue |
            Select-Object -First 1 |
            ForEach-Object { $_.Line -replace '📁 ANALYZING: ', '' -replace '\s', '' })

        if (-not $actualPath) { $actualPath = $Component }

        $GitRoot = git rev-parse --show-toplevel 2>$null
        if (-not $GitRoot) { $GitRoot = "." }
        Push-Location $GitRoot
        $FileCount = (git diff --name-status "$BaseBranch..$TargetBranch" -- "$actualPath" 2>$null | Measure-Object -Line).Lines
        Pop-Location

        $SummaryContent += "- [$Component]($ComponentName.md) - $FileCount files changed`n"
    }
}

$SummaryContent += @"

## Statistics

- Total components checked: $TotalComponents
- Components with changes: $FilesCreated
- Analysis files generated: $FilesCreated

## Next Steps

1. Review each component analysis file
2. Extract key features and changes
3. Write user-facing release notes using tools/ReleaseNotes/docs/template.md
4. Validate code examples and API references

"@

$SummaryContent | Out-File -FilePath $SummaryFile -Encoding UTF8

$SummaryEnd = Get-Date
$SummaryElapsed = ($SummaryEnd - $SummaryStart).TotalSeconds
Write-Host "✅ Summary generation completed in $([math]::Round($SummaryElapsed, 2))s" -ForegroundColor Green

# Calculate total time
$ScriptEndTime = Get-Date
$TotalTime = ($ScriptEndTime - $ScriptStartTime).TotalSeconds

Write-Host ""
Write-Host "✅ Component analysis complete!" -ForegroundColor Green
Write-Host "⏱️  Total execution time: $([math]::Round($TotalTime, 2))s"
Write-Host ""
Write-Host "📊 Timing Summary:"
Write-Host "   Component Analysis: $([math]::Round($AnalysisElapsed, 2))s"
Write-Host "   Summary Generation: $([math]::Round($SummaryElapsed, 2))s"
Write-Host ""
Write-Host "📄 Summary: $SummaryFile"
Write-Host "📁 Detailed analysis files in: $AnalysisDir/"
Write-Host ""
Write-Host "📋 Analysis files generated:"
Get-ChildItem "$AnalysisDir\*.md" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "   $($_.Name)" }
