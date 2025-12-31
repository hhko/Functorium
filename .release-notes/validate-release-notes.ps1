<#
.SYNOPSIS
    릴리스 노트 파일의 크기를 검증합니다.

.DESCRIPTION
    GitHub Release body는 최대 125,000자로 제한됩니다.
    이 스크립트는 지정한 MD 파일의 크기를 확인하고 제한을 초과하는지 검증합니다.

.PARAMETER FilePath
    검증할 릴리스 노트 파일의 경로

.EXAMPLE
    .\validate-release-notes.ps1 -FilePath ".release-notes\RELEASE_NOTES_v1.0.0.md"
    
.EXAMPLE
    .\validate-release-notes.ps1 -FilePath "RELEASE_NOTES.md"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath
)

# GitHub Release body 최대 문자 수 제한
$MAX_CHARS = 125000

# 파일 존재 확인
if (-not (Test-Path $FilePath)) {
    Write-Host "❌ Error: File not found: $FilePath" -ForegroundColor Red
    exit 1
}

# 파일 정보 가져오기
$fileInfo = Get-Item $FilePath
$fileSize = $fileInfo.Length

# 파일 내용 읽기
$fileContent = Get-Content $FilePath -Raw -Encoding UTF8
$charCount = $fileContent.Length

# 비율 계산
$percentage = [math]::Round(($charCount / $MAX_CHARS) * 100, 2)

# 결과 출력
Write-Host ""
Write-Host "📄 File: $FilePath" -ForegroundColor Cyan
Write-Host "📊 File size: $fileSize bytes" -ForegroundColor Yellow
Write-Host "📊 Character count: $charCount characters ($percentage% of $MAX_CHARS max)" -ForegroundColor Yellow
Write-Host ""

# 제한 초과 확인
if ($charCount -gt $MAX_CHARS) {
    $excess = $charCount - $MAX_CHARS
    Write-Host "❌ Error: Release notes file exceeds GitHub Release body limit!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Current size: $charCount characters" -ForegroundColor Red
    Write-Host "Maximum allowed: $MAX_CHARS characters" -ForegroundColor Red
    Write-Host "Excess: $excess characters" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please reduce the size of the release notes file." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "✅ Release notes file is within the limit." -ForegroundColor Green

    # 경고: 90% 이상 사용 시
    if ($percentage -ge 90) {
        Write-Host "⚠️  Warning: File is using $percentage% of the maximum limit." -ForegroundColor Yellow
        Write-Host "   Consider reducing the file size to leave room for future updates." -ForegroundColor Yellow
    }

    exit 0
}