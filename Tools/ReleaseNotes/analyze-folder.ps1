# PowerShell version of analyze-folder.sh for Windows
# Comprehensive folder analysis script for release notes generation
# Usage: .\analyze-folder.ps1 -FolderPath "Src/Functorium" [-BaseBranch "origin/release/1.0"] [-TargetBranch "origin/main"]

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$FolderPath,

    [Parameter(Position=1)]
    [string]$BaseBranch,

    [Parameter(Position=2)]
    [string]$TargetBranch
)

$ErrorActionPreference = "Stop"

# Use environment variables if set, otherwise use parameters, otherwise use defaults
if (-not $BaseBranch) {
    $BaseBranch = if ($env:BASE_BRANCH) { $env:BASE_BRANCH } else { "HEAD~10" }
}

if (-not $TargetBranch) {
    $TargetBranch = if ($env:TARGET_BRANCH) { $env:TARGET_BRANCH } else { "HEAD" }
}

# Ensure we're in the git repository root
$RepoRoot = git rev-parse --show-toplevel 2>$null
if (-not $RepoRoot) { $RepoRoot = Get-Location }
Push-Location $RepoRoot

Write-Output "📁 ANALYZING: $FolderPath"
Write-Output "🔄 Comparing: $BaseBranch → $TargetBranch"
Write-Output "📂 Working from: $(Get-Location)"
Write-Output "⏱️  Starting detailed analysis..."
Write-Output "🔍 Note: Only analyzing commits in $TargetBranch that are NOT in $BaseBranch (excluding cherry-picks)"
Write-Output "========================================"
Write-Output ""

# Start timing
$AnalysisStartTime = Get-Date

# Check for changes
Write-Output "📊 Change Summary:"
Write-Output ""
$Stats = git diff --stat "$BaseBranch..$TargetBranch" -- "$FolderPath/" 2>$null
if (-not $Stats) {
    Write-Output "No changes found in this folder"
    Pop-Location
    exit 0
}

# Display full stats (all files + summary)
$Stats | ForEach-Object { Write-Output $_ }
Write-Output ""

Write-Output "📝 All Commits (new in $TargetBranch, excluding cherry-picks):"
Write-Output ""
# Use --cherry-pick to exclude commits that were cherry-picked from base branch
$commits = git log --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$FolderPath/" 2>$null
if ($commits) {
    $commits | ForEach-Object { Write-Output $_ }
} else {
    Write-Output "No commits found"
}
Write-Output ""

Write-Output "👥 Top Contributors:"
Write-Output ""
$Contributors = git log --format="%an" --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$FolderPath/" 2>$null
if ($Contributors) {
    $Contributors |
        Group-Object |
        Sort-Object Count -Descending |
        Select-Object -First 5 |
        ForEach-Object { Write-Output "  $($_.Count) $($_.Name)" }
} else {
    Write-Output "No contributors found"
}
Write-Output ""

Write-Output "📝 Sample Commit Messages (categorized, new commits only):"
Write-Output ""

Write-Output "Feature commits:"
$featureCommits = git log --grep="feat\|feature\|add" --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$FolderPath/" 2>$null | Select-Object -First 5
if ($featureCommits) {
    $featureCommits | ForEach-Object { Write-Output $_ }
} else {
    Write-Output "None found"
}
Write-Output ""

Write-Output "Bug fixes:"
$bugFixes = git log --grep="fix\|bug" --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$FolderPath/" 2>$null | Select-Object -First 5
if ($bugFixes) {
    $bugFixes | ForEach-Object { Write-Output $_ }
} else {
    Write-Output "None found"
}
Write-Output ""

Write-Output "Breaking changes:"
$breakingChanges = git log --grep="breaking\|BREAKING" --oneline --no-merges --cherry-pick --right-only "$BaseBranch...$TargetBranch" -- "$FolderPath/" 2>$null | Select-Object -First 5
if ($breakingChanges) {
    $breakingChanges | ForEach-Object { Write-Output $_ }
} else {
    Write-Output "None found"
}
Write-Output ""

# Calculate and display timing
$AnalysisEndTime = Get-Date
$TotalTime = ($AnalysisEndTime - $AnalysisStartTime).TotalSeconds

Write-Output "========================================"
Write-Output "⏱️  Analysis completed in $([math]::Round($TotalTime, 2))s"
Write-Output "📁 Analysis for: $FolderPath"
Write-Output "🔄 Branch comparison: $BaseBranch → $TargetBranch"
Write-Output "========================================"
Write-Output "✅ Analysis complete for $FolderPath"
Write-Output "📊 Comparison: $BaseBranch → $TargetBranch"
Write-Output "Use the data above to generate release notes for this component"

Pop-Location
