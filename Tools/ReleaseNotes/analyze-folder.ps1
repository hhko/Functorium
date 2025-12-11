# PowerShell version of analyze-folder.sh
# Comprehensive folder analysis script for release notes generation
# Usage: .\analyze-folder.ps1 -FolderPath "Src/Functorium" [-BaseBranch "origin/main"] [-TargetBranch "HEAD"]

param(
    [Parameter(Mandatory = $true)]
    [string]$FolderPath,
    [string]$BaseBranch = "origin/release/1.0",
    [string]$TargetBranch = "origin/main"
)

# Use environment variables if set
if ($env:BASE_BRANCH) { $BaseBranch = $env:BASE_BRANCH }
if ($env:TARGET_BRANCH) { $TargetBranch = $env:TARGET_BRANCH }

# Get script directory and repository root
$ScriptDir = $PSScriptRoot
$RepoRoot = git rev-parse --show-toplevel 2>$null
if (-not $RepoRoot) { $RepoRoot = (Get-Location).Path }
$RepoRoot = $RepoRoot -replace '\\', '/'

# Ensure FolderPath is relative
$RelativeFolderPath = $FolderPath -replace '\\', '/'
$RelativeFolderPath = $RelativeFolderPath -replace [regex]::Escape("$RepoRoot/"), ''
$RelativeFolderPath = $RelativeFolderPath -replace '^/', ''

Push-Location $RepoRoot

Write-Output ""
Write-Output "========================================"
Write-Output "ANALYZING: $RelativeFolderPath"
Write-Output "Comparing: $BaseBranch -> $TargetBranch"
Write-Output "Working from: $(Get-Location)"
Write-Output "Starting detailed analysis..."
Write-Output "Note: Only analyzing commits in $TargetBranch that are NOT in $BaseBranch"
Write-Output "========================================"

# Start timing
$AnalysisStartTime = Get-Date

Write-Output ""
Write-Output "Change Summary:"
Write-Output ""
$Stats = git diff --stat "$BaseBranch..$TargetBranch" -- "$RelativeFolderPath/" 2>$null
if ($Stats) {
    $Stats
} else {
    Write-Output "No changes found in this folder"
    Pop-Location
    exit 0
}

Write-Output ""
Write-Output "All Commits (new in $TargetBranch):"
Write-Output ""
# Use double dot for commit SHA compatibility
git log --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeFolderPath/" 2>$null

Write-Output ""
Write-Output "Top Contributors:"
Write-Output ""
$Contributors = git log --format="%an" "$BaseBranch..$TargetBranch" -- "$RelativeFolderPath/" 2>$null |
    Group-Object |
    Sort-Object Count -Descending |
    Select-Object -First 5
foreach ($c in $Contributors) {
    Write-Output "  $($c.Count) $($c.Name)"
}

Write-Output ""
Write-Output "Sample Commit Messages (categorized, new commits only):"
Write-Output ""
Write-Output "Feature commits:"
git log --grep="feat\|feature\|add" --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeFolderPath/" 2>$null | Select-Object -First 5
if (-not $?) { Write-Output "None found" }

Write-Output ""
Write-Output "Bug fixes:"
git log --grep="fix\|bug" --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeFolderPath/" 2>$null | Select-Object -First 5
if (-not $?) { Write-Output "None found" }

Write-Output ""
Write-Output "Breaking changes:"
$BreakingChanges = git log --grep="breaking\|BREAKING" --oneline --no-merges "$BaseBranch..$TargetBranch" -- "$RelativeFolderPath/" 2>$null | Select-Object -First 5
if ($BreakingChanges) {
    $BreakingChanges
} else {
    Write-Output "None found"
}

# Calculate and display timing
$AnalysisEndTime = Get-Date
$TotalTime = ($AnalysisEndTime - $AnalysisStartTime).TotalSeconds

Write-Output ""
Write-Output "========================================"
Write-Output "Analysis completed in $([math]::Round($TotalTime, 2))s"
Write-Output "Analysis for: $RelativeFolderPath"
Write-Output "Branch comparison: $BaseBranch -> $TargetBranch"
Write-Output "========================================"
Write-Output "Analysis complete for $RelativeFolderPath"
Write-Output "Comparison: $BaseBranch -> $TargetBranch"
Write-Output "Use the data above to generate release notes for this component"

Pop-Location
