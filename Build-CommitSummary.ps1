#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
  Generates a summary document grouped by commit types.

.DESCRIPTION
  Groups commits by type according to Conventional Commits specification
  and generates a markdown document with statistics and detailed lists.

.PARAMETER Range
  Git range expression (optional)
  Examples: v1.0.0..HEAD, v1.0.0..v1.1.0, HEAD~10..HEAD
  Default: Commits since last tag (all commits if no tags)

.PARAMETER TargetBranch
  Target branch (optional)
  Default: main
  Range resolution priority:
  1. If tag exists: [last-tag]..HEAD
  2. No tag + different branch: [TargetBranch]..HEAD
  3. No tag + same branch: HEAD (all commits)

.PARAMETER OutputDir
  Output directory path (optional)
  Default: .commit-summaries

.EXAMPLE
  .\Build-CommitSummary.ps1
  Summarize commits since last tag

.EXAMPLE
  .\Build-CommitSummary.ps1 -Range "v1.0.0..HEAD"
  Summarize commits in specific range

.EXAMPLE
  .\Build-CommitSummary.ps1 -Range "HEAD~10..HEAD"
  Summarize last 10 commits

.EXAMPLE
  .\Build-CommitSummary.ps1 -TargetBranch develop
  Summarize commits based on develop branch

.EXAMPLE
  .\Build-CommitSummary.ps1 -OutputDir "./releases"
  Specify output directory as ./releases

.NOTES
  Version: 1.0.0
  Requirements: PowerShell 7+, git
  Output directory: .commit-summaries/
  License: MIT
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false, Position = 0, HelpMessage = "Git range expression (e.g., v1.0.0..HEAD)")]
  [Alias("r")]
  [string]$Range,

  [Parameter(Mandatory = $false, HelpMessage = "Target branch (default: main)")]
  [Alias("t")]
  [string]$TargetBranch = "main",

  [Parameter(Mandatory = $false, HelpMessage = "Output directory path (default: .commit-summaries)")]
  [Alias("o")]
  [string]$OutputDir = ".commit-summaries",

  [Parameter(Mandatory = $false, HelpMessage = "Show help")]
  [Alias("h", "?")]
  [switch]$Help
)

# Strict mode settings
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

#region Constants

$script:COMMIT_TYPES = @{
  feat     = @{ Description = "New features" }
  fix      = @{ Description = "Bug fixes" }
  docs     = @{ Description = "Documentation" }
  style    = @{ Description = "Code formatting" }
  refactor = @{ Description = "Refactoring" }
  perf     = @{ Description = "Performance improvements" }
  test     = @{ Description = "Tests" }
  build    = @{ Description = "Build system/dependencies" }
  ci       = @{ Description = "CI configuration" }
  chore    = @{ Description = "Other changes" }
  other    = @{ Description = "Non-conventional commits" }
}

$script:OUTPUT_DIR = $null  # Set at Entry Point
$script:MAX_AUTHOR_LENGTH = 15

#endregion

#region Step 1: Assert-GitRepository

<#
.SYNOPSIS
  Validates that the current directory is a Git repository.
#>
function Assert-GitRepository {
  Write-Host "[1/7] Checking Git repository..." -ForegroundColor Gray

  if (-not (Test-Path .git)) {
    throw "Current directory is not a git repository."
  }

  Write-Host "      Git repository verified" -ForegroundColor DarkGray
}

#endregion

#region Step 2: Resolve-CommitRange

<#
.SYNOPSIS
  Retrieves the last git tag.
#>
function Get-LastTag {
  try {
    $tag = git tag --sort=-v:refname 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($tag)) {
      return $null
    }
    return $tag.Trim()
  }
  catch {
    return $null
  }
}

<#
.SYNOPSIS
  Determines the commit range.
#>
function Resolve-CommitRange {
  param(
    [Parameter(Mandatory = $false)]
    [string]$Range,

    [Parameter(Mandatory = $false)]
    [string]$TargetBranch = "main"
  )

  Write-Host "[2/7] Determining range..." -ForegroundColor Gray

  if ([string]::IsNullOrWhiteSpace($Range)) {
    $lastTag = Get-LastTag

    if ($lastTag) {
      $resolved = "$lastTag..HEAD"
      Write-Host "      Range: $resolved (since last tag)" -ForegroundColor DarkGray
    }
    else {
      # No tag: try TargetBranch..HEAD
      $candidateRange = "$TargetBranch..HEAD"
      $commitCount = @(git log $candidateRange --oneline --no-merges 2>$null).Count

      if ($commitCount -gt 0) {
        $resolved = $candidateRange
        Write-Host "      Range: $resolved (based on target branch)" -ForegroundColor DarkGray
      }
      else {
        # Current branch is same as TargetBranch, use HEAD
        $resolved = "HEAD"
        Write-Host "      Range: $resolved (all commits, no tags)" -ForegroundColor DarkGray
      }
    }
  }
  else {
    $resolved = $Range
    Write-Host "      Range: $resolved (user specified)" -ForegroundColor DarkGray
  }

  return $resolved
}

#endregion

#region Step 3: Get-ValidatedCommits

<#
.SYNOPSIS
  Retrieves commits in the specified range.
#>
function Get-CommitsInRange {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range
  )

  try {
    # Wrap with @() to always return array
    # Format: hash|author|subject
    $commits = @(git log $Range --format="%h|%an|%s" --no-merges 2>$null)
    return $commits
  }
  catch {
    return @()
  }
}

<#
.SYNOPSIS
  Outputs error messages.
#>
function Write-ErrorOutput {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Message,

    [Parameter(Mandatory = $false)]
    [string]$Hint
  )

  Write-Host ""
  Write-Host "[FAILED] Commit summary generation failed" -ForegroundColor Red
  Write-Host ""
  Write-Host "Error:" -ForegroundColor Red
  Write-Host "  $Message"
  Write-Host ""

  if ($Hint) {
    Write-Host "Hint:" -ForegroundColor Yellow
    Write-Host "  $Hint"
    Write-Host ""
  }
}

<#
.SYNOPSIS
  Retrieves commits and validates them.
#>
function Get-ValidatedCommits {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range
  )

  Write-Host "[3/7] Retrieving commits..." -ForegroundColor Gray

  $commits = @(Get-CommitsInRange -Range $Range)
  $count = if ($commits) { $commits.Count } else { 0 }
  Write-Host "      Collected commits: $count" -ForegroundColor DarkGray

  if ($count -eq 0) {
    Write-Host "      Warning: No commits found in specified range." -ForegroundColor Yellow
  }

  return $commits
}

#endregion

#region Step 4: Invoke-CommitAnalysis

<#
.SYNOPSIS
  Parses a Conventional Commit.
#>
function Parse-ConventionalCommit {
  param(
    [Parameter(Mandatory = $true)]
    [string]$CommitLine
  )

  # Commit format: hash|author|subject
  # Subject format: type(scope): description or type: description
  $parts = $CommitLine -split '\|', 3
  if ($parts.Count -lt 3) {
    return $null
  }

  $hash = $parts[0]
  $author = $parts[1]
  $subject = $parts[2]

  # Extract branch name from merge commits
  # Untraceable cases:
  #   - Squash merge: Multiple commits merged into one, original branch info lost
  #   - Rebase: Commits are recreated, no original branch info
  #   - Fast-forward merge: No merge commit, untraceable
  #   - Deleted branch: Branch name unknown after deletion
  $sourceBranch = ""
  if ($subject -match "Merge branch ['\`"](.+?)['\`"]") {
    $sourceBranch = $Matches[1]
  }

  # Parse Conventional Commits format
  if ($subject -match '^(\w+)(\(.*?\))?(!)?:\s*(.+)$') {
    return @{
      Hash         = $hash
      Author       = $author
      Type         = $Matches[1].ToLower()
      Scope        = if ($Matches[2]) { $Matches[2] } else { "" }
      Breaking     = if ($Matches[3]) { $true } else { $false }
      Description  = $Matches[4]
      SourceBranch = $sourceBranch
      FullMessage  = $CommitLine
    }
  }
  else {
    # Commits not following Conventional Commits specification
    return @{
      Hash         = $hash
      Author       = $author
      Type         = "other"
      Scope        = ""
      Breaking     = $false
      Description  = $subject
      SourceBranch = $sourceBranch
      FullMessage  = $CommitLine
    }
  }
}

<#
.SYNOPSIS
  Groups commits by type.
#>
function Group-CommitsByType {
  param(
    [Parameter(Mandatory = $true)]
    [AllowEmptyCollection()]
    [array]$Commits
  )

  $grouped = @{}

  # Initialize all types
  foreach ($type in $script:COMMIT_TYPES.Keys) {
    $grouped[$type] = @()
  }

  # Classify commits
  foreach ($commitLine in $Commits) {
    $parsed = Parse-ConventionalCommit -CommitLine $commitLine
    if ($null -ne $parsed) {
      $type = $parsed.Type

      # Classify as 'other' if not a known type
      if (-not $script:COMMIT_TYPES.ContainsKey($type)) {
        $type = "other"
      }

      $grouped[$type] += $parsed
    }
  }

  return $grouped
}

<#
.SYNOPSIS
  Analyzes commits and groups them by type.
#>
function Invoke-CommitAnalysis {
  param(
    [Parameter(Mandatory = $true)]
    [AllowEmptyCollection()]
    [array]$Commits
  )

  Write-Host "[4/7] Analyzing commits..." -ForegroundColor Gray

  $grouped = Group-CommitsByType -Commits $Commits
  Write-Host "      Classified by type" -ForegroundColor DarkGray

  return $grouped
}

#endregion

#region Step 5: Invoke-StatisticsCalculation

<#
.SYNOPSIS
  Calculates statistics.
#>
function Calculate-Statistics {
  param(
    [Parameter(Mandatory = $true)]
    [hashtable]$GroupedCommits
  )

  $stats = @{}
  $total = 0

  foreach ($type in $script:COMMIT_TYPES.Keys) {
    $count = $GroupedCommits[$type].Count
    $stats[$type] = $count
    $total += $count
  }

  $stats["total"] = $total

  return $stats
}

<#
.SYNOPSIS
  Aggregates statistics.
#>
function Invoke-StatisticsCalculation {
  param(
    [Parameter(Mandatory = $true)]
    [hashtable]$GroupedCommits
  )

  Write-Host "[5/7] Aggregating statistics..." -ForegroundColor Gray

  $stats = Calculate-Statistics -GroupedCommits $GroupedCommits
  Write-Host "      Total $($stats["total"]) commits analyzed" -ForegroundColor DarkGray

  return $stats
}

#endregion

#region Step 6: Initialize-OutputDirectory

<#
.SYNOPSIS
  Prepares the output directory.
#>
function Initialize-OutputDirectory {
  Write-Host "[6/7] Checking output directory..." -ForegroundColor Gray

  if (-not (Test-Path $script:OUTPUT_DIR)) {
    New-Item -ItemType Directory -Path $script:OUTPUT_DIR -Force | Out-Null
    Write-Host "      Directory created: $script:OUTPUT_DIR" -ForegroundColor DarkGray
  }
  else {
    Write-Host "      Directory exists: $script:OUTPUT_DIR" -ForegroundColor DarkGray
  }
}

#endregion

#region Step 7: Invoke-DocumentGeneration

<#
.SYNOPSIS
  Generates the output filename.
#>
function New-OutputFileName {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range
  )

  # Remove special characters from range and convert to filename
  $sanitized = $Range -replace '[^\w\.\-]', '-'

  # Generate date/time information (yyyyMMdd-HHmmss)
  $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

  return "summary-$sanitized-$timestamp.md"
}

<#
.SYNOPSIS
  Generates a markdown document.
#>
function Generate-MarkdownDocument {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range,

    [Parameter(Mandatory = $true)]
    [hashtable]$GroupedCommits,

    [Parameter(Mandatory = $true)]
    [hashtable]$Statistics,

    [Parameter(Mandatory = $true)]
    [string]$OutputFileName
  )

  $sb = [System.Text.StringBuilder]::new()

  # Header
  [void]$sb.AppendLine("# Commit Summary")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("**Range**: $Range")
  [void]$sb.AppendLine("**Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
  [void]$sb.AppendLine("")

  # Statistics table
  [void]$sb.AppendLine("## Commit Statistics by Type")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("| Type        | Description               | Count  | Ratio   |")
  [void]$sb.AppendLine("|-------------|---------------------------|--------|---------|")

  $total = $Statistics["total"]

  foreach ($type in @("feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "other")) {
    $count = $Statistics[$type]
    $ratio = if ($total -gt 0) { [math]::Round(($count / $total) * 100, 1) } else { 0.0 }
    $desc = $script:COMMIT_TYPES[$type].Description

    $typeDisplay = if ($type -eq "other") { "Others" } else { "``$type``" }

    [void]$sb.AppendLine("| $($typeDisplay.PadRight(11)) | $($desc.PadRight(25)) | $($count.ToString().PadRight(6)) | $(("{0:0.0}%" -f $ratio).PadRight(7)) |")
  }

  # Total
  [void]$sb.AppendLine("| **Total**   | -                         | **$total** | **100%** |")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("---")
  [void]$sb.AppendLine("")

  # Detailed list by type
  foreach ($type in @("feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "other")) {
    $commits = $GroupedCommits[$type]

    if ($commits.Count -gt 0) {
      $desc = $script:COMMIT_TYPES[$type].Description
      $typeDisplay = if ($type -eq "other") { "Other" } else { $type }

      [void]$sb.AppendLine("## $typeDisplay ($desc) - $($commits.Count) commits")
      [void]$sb.AppendLine("")

      foreach ($commit in $commits) {
        $hash = $commit.Hash
        $author = $commit.Author
        $scope = $commit.Scope
        $description = $commit.Description
        $breaking = if ($commit.Breaking) { "!" } else { "" }
        $sourceBranch = $commit.SourceBranch

        # Limit author name length and pad
        if ($author.Length -gt $script:MAX_AUTHOR_LENGTH) {
          $author = $author.Substring(0, $script:MAX_AUTHOR_LENGTH - 3) + "..."
        }
        $authorPadded = $author.PadRight($script:MAX_AUTHOR_LENGTH)

        # Add branch info
        $branchInfo = if ($sourceBranch) { " ``[from: $sourceBranch]``" } else { "" }

        if ($scope) {
          [void]$sb.AppendLine("- ``$hash`` **$authorPadded** $type$scope$breaking`: $description$branchInfo")
        }
        else {
          [void]$sb.AppendLine("- ``$hash`` **$authorPadded** $type$breaking`: $description$branchInfo")
        }
      }

      [void]$sb.AppendLine("")
    }
  }

  # Footer
  [void]$sb.AppendLine("---")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("**Output path**: ``$script:OUTPUT_DIR/$OutputFileName``")

  return $sb.ToString()
}

<#
.SYNOPSIS
  Generates a markdown document.
#>
function Invoke-DocumentGeneration {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range,

    [Parameter(Mandatory = $true)]
    [hashtable]$GroupedCommits,

    [Parameter(Mandatory = $true)]
    [hashtable]$Statistics,

    [Parameter(Mandatory = $true)]
    [string]$OutputFileName
  )

  Write-Host "[7/7] Generating markdown document..." -ForegroundColor Gray

  $markdown = Generate-MarkdownDocument -Range $Range -GroupedCommits $GroupedCommits -Statistics $Statistics -OutputFileName $OutputFileName
  Write-Host "      Document generated" -ForegroundColor DarkGray

  return $markdown
}

#endregion

#region Step 8: Save-Document, Show-Result

<#
.SYNOPSIS
  Saves the document to a file.
#>
function Save-Document {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Markdown,

    [Parameter(Mandatory = $true)]
    [string]$FileName
  )

  $outputPath = Join-Path $script:OUTPUT_DIR $FileName
  $Markdown | Out-File -FilePath $outputPath -Encoding utf8 -Force

  return $outputPath
}

<#
.SYNOPSIS
  Gets the commit period.
#>
function Get-CommitPeriod {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range
  )

  try {
    # Get the first and last commit dates in the range
    $dates = git log $Range --format="%ai" --no-merges 2>$null

    if ($dates) {
      $dateList = $dates | ForEach-Object { [DateTime]::Parse($_) }
      $oldest = ($dateList | Measure-Object -Maximum).Maximum
      $newest = ($dateList | Measure-Object -Minimum).Minimum

      return "$($newest.ToString('yyyy-MM-dd')) ~ $($oldest.ToString('yyyy-MM-dd'))"
    }
  }
  catch {
    return "Unknown"
  }

  return "Unknown"
}

<#
.SYNOPSIS
  Outputs success message.
#>
function Write-SuccessOutput {
  param(
    [Parameter(Mandatory = $true)]
    [hashtable]$Params
  )

  $hasOutputPath = -not [string]::IsNullOrEmpty($Params.OutputPath)
  $completionMessage = if ($hasOutputPath) { "[DONE] Commit summary document generated" } else { "[DONE] Commit summary completed" }

  Write-Host ""
  Write-Host $completionMessage -ForegroundColor Green
  Write-Host ""
  Write-Host "Target branch: $($Params.TargetBranch)" -ForegroundColor Cyan
  Write-Host "Range: $($Params.Range)" -ForegroundColor Cyan
  Write-Host "Period: $($Params.Period)" -ForegroundColor Cyan
  Write-Host ""
  Write-Host "Commit statistics:" -ForegroundColor Cyan

  foreach ($type in @("feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "other")) {
    $count = $Params.Statistics[$type]
    $total = $Params.Statistics["total"]
    $ratio = if ($total -gt 0) { [math]::Round(($count / $total) * 100, 1) } else { 0.0 }

    $typeDisplay = if ($type -eq "other") { "other" } else { $type }
    $line = "  {0,-10} {1,4} ({2,5:0.0}%)" -f "${typeDisplay}:", $count, $ratio
    Write-Host $line
  }

  $total = $Params.Statistics["total"]
  Write-Host "  ─────────────────────────"
  $line = "  {0,-10} {1,4} ({2,5}%)" -f "total:", $total, "100.0"
  Write-Host $line -ForegroundColor White
  Write-Host ""

  if ($hasOutputPath) {
    Write-Host "Generated file:" -ForegroundColor Cyan
    Write-Host "  $($Params.OutputPath)" -ForegroundColor White
    Write-Host ""
  }
}

<#
.SYNOPSIS
  Outputs the results.
#>
function Show-Result {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Range,

    [Parameter(Mandatory = $true)]
    [hashtable]$Statistics,

    [Parameter(Mandatory = $true)]
    [string]$TargetBranch,

    [Parameter(Mandatory = $false)]
    [AllowNull()]
    [AllowEmptyString()]
    [string]$OutputPath
  )

  $period = Get-CommitPeriod -Range $Range

  Write-SuccessOutput -Params @{
    TargetBranch = $TargetBranch
    Range        = $Range
    Period       = $period
    Statistics   = $Statistics
    OutputPath   = $OutputPath
  }
}

#endregion

#region Show-Help

<#
.SYNOPSIS
  Outputs help information.
#>
function Show-Help {
  $help = @"

================================================================================
 Commit Summary Generator
================================================================================

DESCRIPTION
  Groups commits by type according to Conventional Commits specification
  and generates a markdown document with statistics and detailed lists.

USAGE
  ./Build-CommitSummary.ps1 [options]

OPTIONS
  -Range, -r <string>     Git range expression (optional)
                          Default: Auto-determined (see RANGE RESOLUTION below)
  -TargetBranch, -t       Target branch (optional)
                          Default: main
  -OutputDir, -o          Output directory path (optional)
                          Default: .commit-summaries
  -Help, -h, -?           Show help

RANGE RESOLUTION (when range is not specified)
  1. If tag exists         -> [last-tag]..HEAD
  2. No tag + other branch -> [TargetBranch]..HEAD
  3. No tag + same branch  -> HEAD (all commits)

COMMIT TYPES
  feat       New features
  fix        Bug fixes
  docs       Documentation
  style      Code formatting
  refactor   Refactoring
  perf       Performance improvements
  test       Tests
  build      Build system/dependencies
  ci         CI configuration
  chore      Other changes

OUTPUT
  <OutputDir>/
  └── summary-<range>-<timestamp>.md

EXAMPLES
  # Summarize commits since last tag (generates markdown file)
  ./Build-CommitSummary.ps1

  # Summarize commits in specific range
  ./Build-CommitSummary.ps1 -Range "v1.0.0..HEAD"
  ./Build-CommitSummary.ps1 -r "v1.0.0..HEAD"

  # Summarize last 10 commits
  ./Build-CommitSummary.ps1 -Range "HEAD~10..HEAD"
  ./Build-CommitSummary.ps1 -r "HEAD~10..HEAD"

  # Summarize commits based on develop branch
  ./Build-CommitSummary.ps1 -TargetBranch develop
  ./Build-CommitSummary.ps1 -t develop

  # Specify output directory
  ./Build-CommitSummary.ps1 -OutputDir "./releases"
  ./Build-CommitSummary.ps1 -o "./releases"

  # Summarize all commits (same as when no tags exist)
  ./Build-CommitSummary.ps1 -Range "HEAD"

  # Show help
  ./Build-CommitSummary.ps1 -Help

================================================================================
"@
  Write-Host $help
}

#endregion

#region Main

<#
.SYNOPSIS
  Main execution function.

.DESCRIPTION
  Main flow for commit summary document generation:
  1. Validate Git repository
  2. Determine commit range
  3. Retrieve commits
  4. Analyze and group commits
  5. Aggregate statistics
  6. Prepare output directory
  7. Generate markdown document
  8. Save file and output results
#>
function Main {
  param(
    [Parameter(Mandatory = $false)]
    [string]$CommitRange,

    [Parameter(Mandatory = $false)]
    [string]$TargetBranch = "main",

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = ".commit-summaries"
  )

  # Set output directory
  $script:OUTPUT_DIR = $OutputDirectory

  Write-Host ""
  Write-Host "[START] Starting commit summary..." -ForegroundColor Blue
  Write-Host ""

  # 1. Validate Git repository
  Assert-GitRepository

  # 2. Determine commit range
  $resolvedRange = Resolve-CommitRange -Range $CommitRange -TargetBranch $TargetBranch

  # 3. Retrieve commits
  $commits = @(Get-ValidatedCommits -Range $resolvedRange)

  # 4. Analyze and group commits
  $groupedCommits = Invoke-CommitAnalysis -Commits $commits

  # 5. Aggregate statistics
  $statistics = Invoke-StatisticsCalculation -GroupedCommits $groupedCommits

  # 6. Prepare output directory
  Initialize-OutputDirectory

  # 7. Generate markdown document
  $outputFileName = New-OutputFileName -Range $resolvedRange
  $markdown = Invoke-DocumentGeneration -Range $resolvedRange -GroupedCommits $groupedCommits -Statistics $statistics -OutputFileName $outputFileName

  # 8. Save file and output results
  $outputPath = Save-Document -Markdown $markdown -FileName $outputFileName
  Show-Result -Range $resolvedRange -Statistics $statistics -TargetBranch $TargetBranch -OutputPath $outputPath
}

#endregion

#region Entry Point

if ($Help) {
  Show-Help
  exit 0
}

try {
  Main -CommitRange $Range -TargetBranch $TargetBranch -OutputDirectory $OutputDir
  exit 0
}
catch {
  Write-Host ""
  Write-Host "[ERROR] An unexpected error occurred:" -ForegroundColor Red
  Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
  Write-Host ""
  Write-Host "Stack trace:" -ForegroundColor DarkGray
  Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
  Write-Host ""
  exit 1
}

#endregion
