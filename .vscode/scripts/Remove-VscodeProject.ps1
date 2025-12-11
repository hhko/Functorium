#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    VSCode launch.json, tasks.json, keybindings.json에서 프로젝트 설정을 제거합니다.

.DESCRIPTION
    프로젝트 이름을 입력받아 Add-VscodeProject.ps1로 추가된 설정을 제거합니다.
    해당 프로젝트가 포함된 compound도 함께 제거됩니다.

.PARAMETER ProjectName
    제거할 프로젝트의 이름
    여러 프로젝트를 쉼표로 구분하여 지정할 수 있습니다.

.EXAMPLE
    ./Remove-VscodeProject.ps1 -ProjectName Observability
    ./Remove-VscodeProject.ps1 Observability
    ./Remove-VscodeProject.ps1 Project1, Project2, Project3
    ./Remove-VscodeProject.ps1 -ProjectName Project1, Project2

.NOTES
    - 해당 프로젝트의 설정이 없으면 메시지를 출력하고 건너뜁니다.
    - 프로젝트가 포함된 compound는 자동으로 제거됩니다.
    - keybindings.json은 마지막으로 제거된 프로젝트의 키바인딩만 기본값으로 복원됩니다.
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string[]]$ProjectName
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
Write-Host "프로젝트: $($ProjectName -join ', ')"
Write-Host "워크스페이스: $workspaceRoot"
Write-Host ""

# JSON 파일 읽기
$launchJson = $null
$tasksJson = $null

if (Test-Path $launchJsonPath) {
    $launchJsonContent = Get-Content $launchJsonPath -Raw
    $launchJsonClean = $launchJsonContent -replace '//.*$', '' -replace '/\*[\s\S]*?\*/', ''
    $launchJson = $launchJsonClean | ConvertFrom-Json
}

if (Test-Path $tasksJsonPath) {
    $tasksJsonContent = Get-Content $tasksJsonPath -Raw
    $tasksJson = $tasksJsonContent | ConvertFrom-Json
}

# 결과 추적
$removedProjects = @()
$notFoundProjects = @()
$removedCompounds = @()
$keybindingsRestored = $false

# 각 프로젝트 처리
foreach ($project in $ProjectName) {
    Write-Host ""
    Write-Info "--- $project 처리 중 ---"

    $projectRemoved = $false
    $removedItems = @{
        launch = $false
        tasks = @()
    }

    # 1. launch.json에서 configuration 제거
    if ($launchJson) {
        $originalCount = $launchJson.configurations.Count
        $launchJson.configurations = @($launchJson.configurations | Where-Object { $_.name -ne $project })

        if ($launchJson.configurations.Count -lt $originalCount) {
            $removedItems.launch = $true
            $projectRemoved = $true
            Write-Success "  제거됨: launch.json configuration '$project'"
        } else {
            Write-Warning "  없음: launch.json configuration '$project'"
        }
    }

    # 2. tasks.json에서 tasks 제거
    if ($tasksJson) {
        $taskLabelsToRemove = @(
            "build-$project",
            "publish-$project",
            "watch-$project"
        )

        foreach ($label in $taskLabelsToRemove) {
            $exists = $tasksJson.tasks | Where-Object { $_.label -eq $label }
            if ($exists) {
                $removedItems.tasks += $label
                $projectRemoved = $true
            }
        }

        $tasksJson.tasks = @($tasksJson.tasks | Where-Object { $_.label -notin $taskLabelsToRemove })

        if ($removedItems.tasks.Count -gt 0) {
            foreach ($label in $removedItems.tasks) {
                Write-Success "  제거됨: tasks.json task '$label'"
            }
        } else {
            Write-Warning "  없음: tasks.json '$project' 관련 task"
        }
    }

    # 3. keybindings.json 복원
    if (Test-Path $keybindingsJsonPath) {
        $keybindingsContent = Get-Content $keybindingsJsonPath -Raw

        if ($keybindingsContent -match "build-$project") {
            $keybindingsContent = $keybindingsContent -replace "build-$([regex]::Escape($project))", "build"
            $keybindingsContent = $keybindingsContent -replace "publish-$([regex]::Escape($project))", "publish"
            $keybindingsContent = $keybindingsContent -replace "watch-$([regex]::Escape($project))", "watch"

            $keybindingsContent | Set-Content $keybindingsJsonPath -Encoding UTF8 -NoNewline
            $keybindingsRestored = $true
            $projectRemoved = $true
            Write-Success "  복원됨: keybindings.json args를 기본값으로"
        } else {
            Write-Warning "  없음: keybindings.json '$project' 관련 키바인딩"
        }
    }

    if ($projectRemoved) {
        $removedProjects += $project
    } else {
        $notFoundProjects += $project
    }
}

# 4. Compound 제거 (제거된 프로젝트가 포함된 compound 제거)
if ($launchJson -and $launchJson.PSObject.Properties['compounds'] -and $launchJson.compounds.Count -gt 0) {
    Write-Host ""
    Write-Info "Compound 확인 중..."

    $compoundsToKeep = @()
    foreach ($compound in $launchJson.compounds) {
        # compound의 configurations에 제거된 프로젝트가 포함되어 있는지 확인
        $hasRemovedProject = $false
        foreach ($removedProject in $removedProjects) {
            if ($compound.configurations -contains $removedProject) {
                $hasRemovedProject = $true
                break
            }
        }

        if ($hasRemovedProject) {
            $removedCompounds += $compound.name
            Write-Success "  제거됨: compound '$($compound.name)'"
        } else {
            $compoundsToKeep += $compound
        }
    }

    $launchJson.compounds = $compoundsToKeep

    if ($removedCompounds.Count -eq 0) {
        Write-Warning "  없음: 제거할 compound 없음"
    }
}

# 5. JSON 파일 저장 (제거된 프로젝트가 있을 때만)
if ($removedProjects.Count -gt 0 -or $removedCompounds.Count -gt 0) {
    $jsonOptions = @{ Depth = 10 }

    if ($launchJson) {
        $launchJsonOutput = $launchJson | ConvertTo-Json @jsonOptions
        $launchJsonOutput = $launchJsonOutput -replace '(\[\s*\])', '[]'
        $launchJsonOutput | Set-Content $launchJsonPath -Encoding UTF8
    }

    if ($tasksJson) {
        $tasksJsonOutput = $tasksJson | ConvertTo-Json @jsonOptions
        $tasksJsonOutput = $tasksJsonOutput -replace '(\[\s*\])', '[]'
        $tasksJsonOutput | Set-Content $tasksJsonPath -Encoding UTF8
    }
}

# 결과 요약
Write-Host ""
Write-Host "============================================"
Write-Success "=== 처리 완료 ==="
Write-Host "============================================"
Write-Host ""

if ($removedProjects.Count -gt 0) {
    Write-Host "제거된 프로젝트 ($($removedProjects.Count)개):"
    foreach ($p in $removedProjects) {
        Write-Success "  - $p"
    }
    Write-Host ""
}

if ($removedCompounds.Count -gt 0) {
    Write-Host "제거된 Compound ($($removedCompounds.Count)개):"
    foreach ($c in $removedCompounds) {
        Write-Success "  - $c"
    }
    Write-Host ""
}

if ($notFoundProjects.Count -gt 0) {
    Write-Host "설정을 찾을 수 없는 프로젝트 ($($notFoundProjects.Count)개):"
    foreach ($p in $notFoundProjects) {
        Write-Warning "  - $p"
    }
    Write-Host ""
}

if ($keybindingsRestored) {
    Write-Host "keybindings.json (args 복원):"
    Write-Host "  - Ctrl+Shift+B → build"
    Write-Host "  - Ctrl+Alt+P   → publish"
    Write-Host "  - Ctrl+Alt+W   → watch"
    Write-Host ""
}

if ($removedProjects.Count -gt 0 -or $removedCompounds.Count -gt 0) {
    Write-Info "VSCode를 다시 로드하여 변경사항을 적용하세요."
}
