#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    VSCode launch.json, tasks.json, keybindings.json에서 프로젝트 설정을 제거합니다.

.DESCRIPTION
    프로젝트 이름을 입력받아 Add-VscodeProject.ps1로 추가된 설정을 제거합니다.

.PARAMETER ProjectName
    제거할 프로젝트의 이름

.EXAMPLE
    ./Remove-VscodeProject.ps1 -ProjectName Observability
    ./Remove-VscodeProject.ps1 Observability

.NOTES
    - 해당 프로젝트의 설정이 없으면 메시지를 출력하고 종료합니다.
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$ProjectName
)

# 색상 출력 함수
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }

# 스크립트 루트에서 워크스페이스 루트 찾기
$scriptPath = $PSScriptRoot
$workspaceRoot = (Get-Item $scriptPath).Parent.Parent.FullName

# VSCode 설정 파일 경로
$launchJsonPath = Join-Path $workspaceRoot ".vscode/launch.json"
$tasksJsonPath = Join-Path $workspaceRoot ".vscode/tasks.json"
$keybindingsJsonPath = Join-Path $workspaceRoot ".vscode/keybindings.json"

Write-Host ""
Write-Info "=== VSCode 프로젝트 설정 제거 ==="
Write-Host ""
Write-Host "프로젝트: $ProjectName"
Write-Host "워크스페이스: $workspaceRoot"
Write-Host ""

$removedItems = @{
    launch = $false
    tasks = @()
    keybindings = $false
}

# 1. launch.json에서 configuration 제거
Write-Info "launch.json 확인 중..."

if (Test-Path $launchJsonPath) {
    $launchJsonContent = Get-Content $launchJsonPath -Raw
    $launchJsonClean = $launchJsonContent -replace '//.*$', '' -replace '/\*[\s\S]*?\*/', ''
    $launchJson = $launchJsonClean | ConvertFrom-Json

    $originalCount = $launchJson.configurations.Count
    $launchJson.configurations = @($launchJson.configurations | Where-Object { $_.name -ne $ProjectName })

    if ($launchJson.configurations.Count -lt $originalCount) {
        $removedItems.launch = $true
        Write-Success "  제거됨: configuration '$ProjectName'"
    } else {
        Write-Warning "  없음: configuration '$ProjectName'"
    }

    # 저장
    $jsonOptions = @{ Depth = 10 }
    $launchJsonOutput = $launchJson | ConvertTo-Json @jsonOptions
    $launchJsonOutput = $launchJsonOutput -replace '(\[\s*\])', '[]'
    $launchJsonOutput | Set-Content $launchJsonPath -Encoding UTF8
} else {
    Write-Warning "  launch.json 파일이 없습니다."
}

# 2. tasks.json에서 tasks 제거
Write-Info "tasks.json 확인 중..."

if (Test-Path $tasksJsonPath) {
    $tasksJsonContent = Get-Content $tasksJsonPath -Raw
    $tasksJson = $tasksJsonContent | ConvertFrom-Json

    $taskLabelsToRemove = @(
        "build-$ProjectName",
        "publish-$ProjectName",
        "watch-$ProjectName"
    )

    $originalCount = $tasksJson.tasks.Count

    foreach ($label in $taskLabelsToRemove) {
        $exists = $tasksJson.tasks | Where-Object { $_.label -eq $label }
        if ($exists) {
            $removedItems.tasks += $label
        }
    }

    $tasksJson.tasks = @($tasksJson.tasks | Where-Object { $_.label -notin $taskLabelsToRemove })

    if ($removedItems.tasks.Count -gt 0) {
        foreach ($label in $removedItems.tasks) {
            Write-Success "  제거됨: task '$label'"
        }
    } else {
        Write-Warning "  없음: '$ProjectName' 관련 task"
    }

    # 저장
    $jsonOptions = @{ Depth = 10 }
    $tasksJsonOutput = $tasksJson | ConvertTo-Json @jsonOptions
    $tasksJsonOutput = $tasksJsonOutput -replace '(\[\s*\])', '[]'
    $tasksJsonOutput | Set-Content $tasksJsonPath -Encoding UTF8
} else {
    Write-Warning "  tasks.json 파일이 없습니다."
}

# 3. keybindings.json에서 키바인딩 복원 (args를 기본값으로)
Write-Info "keybindings.json 확인 중..."

if (Test-Path $keybindingsJsonPath) {
    $keybindingsContent = Get-Content $keybindingsJsonPath -Raw

    # 해당 프로젝트의 키바인딩이 있는지 확인
    if ($keybindingsContent -match "build-$ProjectName") {
        # args를 기본값으로 복원
        # "args": "build-ProjectName" → "args": "build"
        # "args": "publish-ProjectName" → "args": "publish"
        # "args": "watch-ProjectName" → "args": "watch"

        $keybindingsContent = $keybindingsContent -replace "build-$([regex]::Escape($ProjectName))", "build"
        $keybindingsContent = $keybindingsContent -replace "publish-$([regex]::Escape($ProjectName))", "publish"
        $keybindingsContent = $keybindingsContent -replace "watch-$([regex]::Escape($ProjectName))", "watch"

        $keybindingsContent | Set-Content $keybindingsJsonPath -Encoding UTF8 -NoNewline
        $removedItems.keybindings = $true
        Write-Success "  복원됨: 단축키 args를 기본값으로 복원"
    } else {
        Write-Warning "  없음: '$ProjectName' 관련 키바인딩"
    }
} else {
    Write-Warning "  keybindings.json 파일이 없습니다."
}

# 결과 요약
Write-Host ""
if ($removedItems.launch -or $removedItems.tasks.Count -gt 0 -or $removedItems.keybindings) {
    Write-Success "=== 설정 제거 완료 ==="
    Write-Host ""
    Write-Host "제거된 설정:"

    if ($removedItems.launch) {
        Write-Host "  launch.json:"
        Write-Host "    - configuration: $ProjectName"
    }

    if ($removedItems.tasks.Count -gt 0) {
        Write-Host "  tasks.json:"
        foreach ($task in $removedItems.tasks) {
            Write-Host "    - task: $task"
        }
    }

    if ($removedItems.keybindings) {
        Write-Host "  keybindings.json (args 복원):"
        Write-Host "    - Ctrl+Shift+B → build"
        Write-Host "    - Ctrl+Alt+P   → publish"
        Write-Host "    - Ctrl+Alt+W   → watch"
    }

    Write-Host ""
    Write-Info "VSCode를 다시 로드하여 변경사항을 적용하세요."
} else {
    Write-Warning "=== 제거할 설정이 없습니다 ==="
    Write-Host ""
    Write-Host "'$ProjectName' 프로젝트의 설정을 찾을 수 없습니다."
}
