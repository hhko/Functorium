#!/usr/bin/env pwsh
# .claude\guides 디렉토리의 모든 가이드 파일을 시스템 프롬프트에 추가하여 Claude를 실행합니다.

param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$ClaudeArgs
)

$guidesDir = Join-Path $PSScriptRoot '.claude' 'guides'

if (-not (Test-Path $guidesDir)) {
    Write-Error "가이드 디렉토리를 찾을 수 없습니다: $guidesDir"
    exit 1
}

$guideFiles = Get-ChildItem -Path $guidesDir -File -Filter '*.md' |
    Where-Object { $_.Name -ne 'README.md' } |
    Sort-Object Name

if ($guideFiles.Count -eq 0) {
    Write-Host '가이드 파일이 없습니다. Claude를 기본 설정으로 실행합니다.'
    claude @ClaudeArgs
    exit $LASTEXITCODE
}

$parts = foreach ($file in $guideFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    "# [$($file.Name)]`n`n$content"
}
$prompt = $parts -join "`n`n---`n`n"

Write-Host "로드된 가이드: $($guideFiles.Count)개"
$guideFiles | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ''

# Windows 명령줄 길이 제한(32,767자) 우회: 임시 파일을 통해 전달
$tempFile = Join-Path ([System.IO.Path]::GetTempPath()) "claude-guides-prompt-$PID.md"
try {
    $prompt | Set-Content -Path $tempFile -Encoding UTF8NoBOM -NoNewline
    claude --append-system-prompt-file $tempFile @ClaudeArgs
    exit $LASTEXITCODE
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}
