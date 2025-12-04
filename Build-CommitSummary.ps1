#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    커밋 타입별로 그룹화하여 요약 문서를 생성합니다.

.DESCRIPTION
    Conventional Commits 규격에 따라 커밋을 타입별로 그룹화하고,
    통계 및 상세 목록을 포함한 마크다운 문서를 생성합니다.

.PARAMETER Range
    git 범위 표현식 (선택사항)
    예: v1.0.0..HEAD, v1.0.0..v1.1.0, HEAD~10..HEAD
    기본값: 마지막 태그 이후 커밋 (태그 없으면 전체 커밋)

.PARAMETER TargetBranch
    대상 브랜치 (선택사항)
    기본값: main
    범위 결정 우선순위:
    1. 태그가 있으면: [마지막태그]..HEAD
    2. 태그가 없고 다른 브랜치면: [TargetBranch]..HEAD
    3. 태그가 없고 같은 브랜치면: HEAD (전체 커밋)

.PARAMETER OutputDir
    출력 디렉토리 경로 (선택사항)
    기본값: .commit-summaries

.EXAMPLE
    .\Build-CommitSummary.ps1
    마지막 태그 이후 커밋 요약

.EXAMPLE
    .\Build-CommitSummary.ps1 -Range "v1.0.0..HEAD"
    특정 범위 커밋 요약

.EXAMPLE
    .\Build-CommitSummary.ps1 -Range "HEAD~10..HEAD"
    최근 10개 커밋 요약

.EXAMPLE
    .\Build-CommitSummary.ps1 -TargetBranch develop
    develop 브랜치 기준으로 커밋 요약

.EXAMPLE
    .\Build-CommitSummary.ps1 -OutputDir "./releases"
    출력 디렉토리를 ./releases로 지정

.NOTES
    버전: 1.0.0
    요구사항: PowerShell 7+, git
    출력 디렉토리: .commit-summaries/
    라이선스: MIT
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false, Position = 0, HelpMessage = "git 범위 표현식 (예: v1.0.0..HEAD)")]
    [Alias("r")]
    [string]$Range,

    [Parameter(Mandatory = $false, HelpMessage = "대상 브랜치 (기본값: main)")]
    [Alias("t")]
    [string]$TargetBranch = "main",

    [Parameter(Mandatory = $false, HelpMessage = "출력 디렉토리 경로 (기본값: .commit-summaries)")]
    [Alias("o")]
    [string]$OutputDir = ".commit-summaries",

    [Parameter(Mandatory = $false, HelpMessage = "도움말을 표시합니다")]
    [Alias("h", "?")]
    [switch]$Help
)

# 엄격 모드 설정
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

$script:OUTPUT_DIR = $null  # Entry Point에서 설정
$script:MAX_AUTHOR_LENGTH = 15

#endregion

#region Step 1: Assert-GitRepository

<#
.SYNOPSIS
    Git 저장소인지 검증합니다.
#>
function Assert-GitRepository {
    Write-Host "[1/7] Git 저장소 확인 중..." -ForegroundColor Gray

    if (-not (Test-Path .git)) {
        throw "현재 디렉토리는 git 저장소가 아닙니다."
    }

    Write-Host "   Git 저장소 확인 완료" -ForegroundColor DarkGray
}

#endregion

#region Step 2: Resolve-CommitRange

<#
.SYNOPSIS
    마지막 git tag를 조회합니다.
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
    커밋 범위를 결정합니다.
#>
function Resolve-CommitRange {
    param(
        [Parameter(Mandatory = $false)]
        [string]$Range,

        [Parameter(Mandatory = $false)]
        [string]$TargetBranch = "main"
    )

    Write-Host "[2/7] 범위 결정 중..." -ForegroundColor Gray

    if ([string]::IsNullOrWhiteSpace($Range)) {
        $lastTag = Get-LastTag

        if ($lastTag) {
            $resolved = "$lastTag..HEAD"
            Write-Host "   범위: $resolved (마지막 태그 이후)" -ForegroundColor DarkGray
        }
        else {
            # 태그가 없을 때: TargetBranch..HEAD 시도
            $candidateRange = "$TargetBranch..HEAD"
            $commitCount = @(git log $candidateRange --oneline --no-merges 2>$null).Count

            if ($commitCount -gt 0) {
                $resolved = $candidateRange
                Write-Host "   범위: $resolved (대상 브랜치 기준)" -ForegroundColor DarkGray
            }
            else {
                # 현재 브랜치가 TargetBranch와 동일한 경우 HEAD 사용
                $resolved = "HEAD"
                Write-Host "   범위: $resolved (전체 커밋, 태그 없음)" -ForegroundColor DarkGray
            }
        }
    }
    else {
        $resolved = $Range
        Write-Host "   범위: $resolved (사용자 지정)" -ForegroundColor DarkGray
    }

    return $resolved
}

#endregion

#region Step 3: Get-ValidatedCommits

<#
.SYNOPSIS
    지정된 범위의 커밋을 조회합니다.
#>
function Get-CommitsInRange {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Range
    )

    try {
        # @()로 감싸서 항상 배열로 반환
        # 포맷: hash|author|subject
        $commits = @(git log $Range --format="%h|%an|%s" --no-merges 2>$null)
        return $commits
    }
    catch {
        return @()
    }
}

<#
.SYNOPSIS
    에러 메시지를 출력합니다.
#>
function Write-ErrorOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,

        [Parameter(Mandatory = $false)]
        [string]$Hint
    )

    Write-Host ""
    Write-Host "[실패] 커밋 요약 생성 실패" -ForegroundColor Red
    Write-Host ""
    Write-Host "에러:" -ForegroundColor Red
    Write-Host "  $Message"
    Write-Host ""

    if ($Hint) {
        Write-Host "힌트:" -ForegroundColor Yellow
        Write-Host "  $Hint"
        Write-Host ""
    }
}

<#
.SYNOPSIS
    커밋을 조회하고 유효성을 검증합니다.
#>
function Get-ValidatedCommits {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Range
    )

    Write-Host "[3/7] 커밋 조회 중..." -ForegroundColor Gray

    $commits = @(Get-CommitsInRange -Range $Range)
    $count = if ($commits) { $commits.Count } else { 0 }
    Write-Host "   수집된 커밋: ${count}개" -ForegroundColor DarkGray

    if ($count -eq 0) {
        Write-Host "   경고: 지정된 범위에 커밋이 없습니다." -ForegroundColor Yellow
    }

    return $commits
}

#endregion

#region Step 4: Invoke-CommitAnalysis

<#
.SYNOPSIS
    Conventional Commit을 파싱합니다.
#>
function Parse-ConventionalCommit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommitLine
    )

    # 커밋 형식: hash|author|subject
    # subject 형식: type(scope): description 또는 type: description
    $parts = $CommitLine -split '\|', 3
    if ($parts.Count -lt 3) {
        return $null
    }

    $hash = $parts[0]
    $author = $parts[1]
    $subject = $parts[2]

    # 머지 커밋에서 브랜치 이름 추출
    # 추적 불가능한 경우들:
    #   - Squash merge: 여러 커밋이 하나로 합쳐져 원본 브랜치 정보 손실
    #   - Rebase: 커밋이 새로 생성되어 원본 브랜치 정보 없음
    #   - Fast-forward merge: 머지 커밋이 없어 추적 불가
    #   - 삭제된 브랜치: 브랜치가 삭제되면 이름을 알 수 없음
    $sourceBranch = ""
    if ($subject -match "Merge branch ['\`"](.+?)['\`"]") {
        $sourceBranch = $Matches[1]
    }

    # Conventional Commits 형식 파싱
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
        # Conventional Commits 규격을 따르지 않는 커밋
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
    커밋을 타입별로 그룹화합니다.
#>
function Group-CommitsByType {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [array]$Commits
    )

    $grouped = @{}

    # 모든 타입 초기화
    foreach ($type in $script:COMMIT_TYPES.Keys) {
        $grouped[$type] = @()
    }

    # 커밋 분류
    foreach ($commitLine in $Commits) {
        $parsed = Parse-ConventionalCommit -CommitLine $commitLine
        if ($null -ne $parsed) {
            $type = $parsed.Type

            # 알려진 타입이 아니면 other로 분류
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
    커밋을 분석하고 타입별로 그룹화합니다.
#>
function Invoke-CommitAnalysis {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [array]$Commits
    )

    Write-Host "[4/7] 커밋 분석 중..." -ForegroundColor Gray

    $grouped = Group-CommitsByType -Commits $Commits
    Write-Host "   타입별 분류 완료" -ForegroundColor DarkGray

    return $grouped
}

#endregion

#region Step 5: Invoke-StatisticsCalculation

<#
.SYNOPSIS
    통계를 계산합니다.
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
    통계를 집계합니다.
#>
function Invoke-StatisticsCalculation {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$GroupedCommits
    )

    Write-Host "[5/7] 통계 집계 중..." -ForegroundColor Gray

    $stats = Calculate-Statistics -GroupedCommits $GroupedCommits
    Write-Host "   총 $($stats["total"])개 커밋 분석 완료" -ForegroundColor DarkGray

    return $stats
}

#endregion

#region Step 6: Initialize-OutputDirectory

<#
.SYNOPSIS
    출력 디렉토리를 준비합니다.
#>
function Initialize-OutputDirectory {
    Write-Host "[6/7] 출력 디렉토리 확인 중..." -ForegroundColor Gray

    if (-not (Test-Path $script:OUTPUT_DIR)) {
        New-Item -ItemType Directory -Path $script:OUTPUT_DIR -Force | Out-Null
        Write-Host "   디렉토리 생성: $script:OUTPUT_DIR" -ForegroundColor DarkGray
    }
    else {
        Write-Host "   디렉토리 존재: $script:OUTPUT_DIR" -ForegroundColor DarkGray
    }
}

#endregion

#region Step 7: Invoke-DocumentGeneration

<#
.SYNOPSIS
    파일명을 생성합니다.
#>
function New-OutputFileName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Range
    )

    # 범위에서 특수문자 제거하고 파일명으로 변환
    $sanitized = $Range -replace '[^\w\.\-]', '-'

    # 날짜/시간 정보 생성 (yyyyMMdd-HHmmss)
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

    return "summary-$sanitized-$timestamp.md"
}

<#
.SYNOPSIS
    마크다운 문서를 생성합니다.
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

    # 헤더
    [void]$sb.AppendLine("# 커밋 요약")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("**기간**: $Range")
    [void]$sb.AppendLine("**생성일**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine("")

    # 통계 테이블
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

    # 총계
    [void]$sb.AppendLine("| **Total**   | -                         | **$total** | **100%** |")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")

    # 타입별 상세 목록
    foreach ($type in @("feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "other")) {
        $commits = $GroupedCommits[$type]

        if ($commits.Count -gt 0) {
            $desc = $script:COMMIT_TYPES[$type].Description
            $typeDisplay = if ($type -eq "other") { "기타" } else { $type }

            [void]$sb.AppendLine("## $typeDisplay ($desc) - $($commits.Count)건")
            [void]$sb.AppendLine("")

            foreach ($commit in $commits) {
                $hash = $commit.Hash
                $author = $commit.Author
                $scope = $commit.Scope
                $description = $commit.Description
                $breaking = if ($commit.Breaking) { "!" } else { "" }
                $sourceBranch = $commit.SourceBranch

                # 작성자 이름 길이 제한 및 패딩
                if ($author.Length -gt $script:MAX_AUTHOR_LENGTH) {
                    $author = $author.Substring(0, $script:MAX_AUTHOR_LENGTH - 3) + "..."
                }
                $authorPadded = $author.PadRight($script:MAX_AUTHOR_LENGTH)

                # 브랜치 정보 추가
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

    # 푸터
    [void]$sb.AppendLine("---")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("**생성 경로**: ``$script:OUTPUT_DIR/$OutputFileName``")

    return $sb.ToString()
}

<#
.SYNOPSIS
    마크다운 문서를 생성합니다.
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

    Write-Host "[7/7] 마크다운 문서 생성 중..." -ForegroundColor Gray

    $markdown = Generate-MarkdownDocument -Range $Range -GroupedCommits $GroupedCommits -Statistics $Statistics -OutputFileName $OutputFileName
    Write-Host "   문서 생성 완료" -ForegroundColor DarkGray

    return $markdown
}

#endregion

#region Step 8: Save-Document, Show-Result

<#
.SYNOPSIS
    문서를 파일로 저장합니다.
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
    커밋 기간을 가져옵니다.
#>
function Get-CommitPeriod {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Range
    )

    try {
        # 범위의 첫 번째와 마지막 커밋 날짜 가져오기
        $dates = git log $Range --format="%ai" --no-merges 2>$null

        if ($dates) {
            $dateList = $dates | ForEach-Object { [DateTime]::Parse($_) }
            $oldest = ($dateList | Measure-Object -Maximum).Maximum
            $newest = ($dateList | Measure-Object -Minimum).Minimum

            return "$($newest.ToString('yyyy-MM-dd')) ~ $($oldest.ToString('yyyy-MM-dd'))"
        }
    }
    catch {
        return "알 수 없음"
    }

    return "알 수 없음"
}

<#
.SYNOPSIS
    성공 메시지를 출력합니다.
#>
function Write-SuccessOutput {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Params
    )

    $hasOutputPath = -not [string]::IsNullOrEmpty($Params.OutputPath)
    $completionMessage = if ($hasOutputPath) { "[완료] 커밋 요약 문서 생성 완료" } else { "[완료] 커밋 요약 완료" }

    Write-Host ""
    Write-Host $completionMessage -ForegroundColor Green
    Write-Host ""
    Write-Host "범위: $($Params.Range)" -ForegroundColor Cyan
    Write-Host "기간: $($Params.Period)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "커밋 통계:" -ForegroundColor Cyan

    foreach ($type in @("feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "other")) {
        $count = $Params.Statistics[$type]
        $total = $Params.Statistics["total"]
        $ratio = if ($total -gt 0) { [math]::Round(($count / $total) * 100, 1) } else { 0.0 }

        $typeDisplay = if ($type -eq "other") { "other" } else { $type }
        $line = "  {0,-10} {1,4}건 ({2,5:0.0}%)" -f "${typeDisplay}:", $count, $ratio
        Write-Host $line
    }

    $total = $Params.Statistics["total"]
    Write-Host "  ─────────────────────────"
    $line = "  {0,-10} {1,4}건 ({2,5}%)" -f "total:", $total, "100.0"
    Write-Host $line -ForegroundColor White
    Write-Host ""

    if ($hasOutputPath) {
        Write-Host "생성된 파일:" -ForegroundColor Cyan
        Write-Host "  $($Params.OutputPath)" -ForegroundColor White
        Write-Host ""
    }
}

<#
.SYNOPSIS
    결과를 출력합니다.
#>
function Show-Result {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Range,

        [Parameter(Mandatory = $true)]
        [hashtable]$Statistics,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [AllowEmptyString()]
        [string]$OutputPath
    )

    $period = Get-CommitPeriod -Range $Range

    Write-SuccessOutput -Params @{
        Range      = $Range
        Period     = $period
        Statistics = $Statistics
        OutputPath = $OutputPath
    }
}

#endregion

#region Show-Help

<#
.SYNOPSIS
    도움말을 출력합니다.
#>
function Show-Help {
    $help = @"

================================================================================
 Commit Summary Generator
================================================================================

DESCRIPTION
    Conventional Commits 규격에 따라 커밋을 타입별로 그룹화하고,
    통계 및 상세 목록을 포함한 마크다운 문서를 생성합니다.

USAGE
    ./Build-CommitSummary.ps1 [options]

OPTIONS
    -Range, -r <string>     git 범위 표현식 (선택사항)
                            기본값: 자동 결정 (아래 RANGE RESOLUTION 참조)
    -TargetBranch, -t       대상 브랜치 (선택사항)
                            기본값: main
    -OutputDir, -o          출력 디렉토리 경로 (선택사항)
                            기본값: .commit-summaries
    -Help, -h, -?           도움말을 표시합니다

RANGE RESOLUTION (범위가 지정되지 않았을 때)
    1. 태그가 있으면         -> [마지막태그]..HEAD
    2. 태그 없음 + 다른 브랜치 -> [TargetBranch]..HEAD
    3. 태그 없음 + 같은 브랜치 -> HEAD (전체 커밋)

COMMIT TYPES
    feat       New features (새로운 기능)
    fix        Bug fixes (버그 수정)
    docs       Documentation (문서 변경)
    style      Code formatting (코드 포맷팅)
    refactor   Refactoring (리팩터링)
    perf       Performance improvements (성능 개선)
    test       Tests (테스트)
    build      Build system/dependencies (빌드/의존성)
    ci         CI configuration (CI 설정)
    chore      Other changes (기타 변경)

OUTPUT
    <OutputDir>/
    └── summary-<range>-<timestamp>.md

EXAMPLES
    # 마지막 태그 이후 커밋 요약 (마크다운 파일 생성)
    ./Build-CommitSummary.ps1

    # 특정 범위 커밋 요약
    ./Build-CommitSummary.ps1 -Range "v1.0.0..HEAD"
    ./Build-CommitSummary.ps1 -r "v1.0.0..HEAD"

    # 최근 10개 커밋 요약
    ./Build-CommitSummary.ps1 -Range "HEAD~10..HEAD"
    ./Build-CommitSummary.ps1 -r "HEAD~10..HEAD"

    # develop 브랜치 기준으로 커밋 요약
    ./Build-CommitSummary.ps1 -TargetBranch develop
    ./Build-CommitSummary.ps1 -t develop

    # 출력 디렉토리 지정
    ./Build-CommitSummary.ps1 -OutputDir "./releases"
    ./Build-CommitSummary.ps1 -o "./releases"

    # 전체 커밋 요약 (태그 없는 경우와 동일)
    ./Build-CommitSummary.ps1 -Range "HEAD"

    # 도움말 표시
    ./Build-CommitSummary.ps1 -Help

================================================================================
"@
    Write-Host $help
}

#endregion

#region Main

<#
.SYNOPSIS
    메인 실행 함수입니다.

.DESCRIPTION
    커밋 요약 문서 생성의 주요 흐름:
    1. Git 저장소 검증
    2. 커밋 범위 결정
    3. 커밋 조회
    4. 커밋 분석 및 그룹화
    5. 통계 집계
    6. 출력 디렉토리 준비
    7. 마크다운 문서 생성
    8. 파일 저장 및 결과 출력
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

    # 출력 디렉토리 설정
    $script:OUTPUT_DIR = $OutputDirectory

    Write-Host ""
    Write-Host "[시작] 커밋 요약 시작..." -ForegroundColor Blue
    Write-Host ""

    # 1. Git 저장소 검증
    Assert-GitRepository

    # 2. 커밋 범위 결정
    $resolvedRange = Resolve-CommitRange -Range $CommitRange -TargetBranch $TargetBranch

    # 3. 커밋 조회
    $commits = @(Get-ValidatedCommits -Range $resolvedRange)

    # 4. 커밋 분석 및 그룹화
    $groupedCommits = Invoke-CommitAnalysis -Commits $commits

    # 5. 통계 집계
    $statistics = Invoke-StatisticsCalculation -GroupedCommits $groupedCommits

    # 6. 출력 디렉토리 준비
    Initialize-OutputDirectory

    # 7. 마크다운 문서 생성
    $outputFileName = New-OutputFileName -Range $resolvedRange
    $markdown = Invoke-DocumentGeneration -Range $resolvedRange -GroupedCommits $groupedCommits -Statistics $statistics -OutputFileName $outputFileName

    # 8. 파일 저장 및 결과 출력
    $outputPath = Save-Document -Markdown $markdown -FileName $outputFileName
    Show-Result -Range $resolvedRange -Statistics $statistics -OutputPath $outputPath
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
    Write-Host "[오류] 예상치 못한 오류가 발생했습니다:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "스택 트레이스:" -ForegroundColor DarkGray
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    Write-Host ""
    exit 1
}

#endregion
