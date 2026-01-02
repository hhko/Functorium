#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    VSCode launch.json, tasks.json에 프로젝트 설정을 자동으로 추가합니다.

.DESCRIPTION
    프로젝트 이름을 입력받아 해당 .csproj 파일을 찾고,
    VSCode의 launch.json, tasks.json에 실행/빌드 설정을 추가합니다.
    여러 프로젝트를 지정하면 compounds를 생성하여 동시 실행이 가능합니다.

.PARAMETER ProjectName
    추가할 프로젝트의 이름 (.csproj 파일명에서 확장자 제외)
    여러 프로젝트를 쉼표로 구분하여 지정할 수 있습니다.

.PARAMETER Force
    이미 존재하는 설정을 제거하고 다시 추가합니다.

.EXAMPLE
    ./Build-SetAsSetupProject.ps1 -ProjectName Observability
    ./Build-SetAsSetupProject.ps1 Observability
    ./Build-SetAsSetupProject.ps1 Project1, Project2, Project3
    ./Build-SetAsSetupProject.ps1 -ProjectName Project1, Project2 -Force

.NOTES
    - -Force 옵션 없이 실행 시 동일한 이름의 설정이 있으면 건너뜁니다.
    - -Force 옵션 사용 시 기존 설정을 제거하고 다시 추가합니다.
    - 여러 개의 .csproj 파일이 발견되면 선택할 수 있습니다.
    - 여러 프로젝트 지정 시 compounds가 생성되어 동시 실행 가능합니다.
    - keybindings.json은 참고용으로만 제공됩니다 (수동 복사 필요).
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string[]]$ProjectName,

    [Parameter(Mandatory = $false)]
    [switch]$Force
)

# 색상 출력 함수
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }

# 기존 configuration 제거 함수
function Remove-ExistingConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$LaunchJson,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $launchJson.configurations = @($launchJson.configurations | Where-Object { $_.name -ne $Name })
}

# 기존 tasks 제거 함수
function Remove-ExistingTasks {
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$TasksJson,
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )

    $labelsToRemove = @(
        "build-$ProjectName",
        "publish-$ProjectName",
        "watch-$ProjectName"
    )

    $tasksJson.tasks = @($tasksJson.tasks | Where-Object { $_.label -notin $labelsToRemove })
}

# 기존 compound 제거 함수 (프로젝트 포함된 compound 제거)
function Remove-ExistingCompound {
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$LaunchJson,
        [Parameter(Mandatory = $true)]
        [string]$ProjectName
    )

    if ($launchJson.PSObject.Properties['compounds'] -and $launchJson.compounds) {
        $launchJson.compounds = @($launchJson.compounds | Where-Object {
            $_.configurations -notcontains $ProjectName
        })
    }
}

# 스크립트가 루트에 위치하므로 $PSScriptRoot가 워크스페이스 루트
$workspaceRoot = $PSScriptRoot

# VSCode 설정 파일 경로
$launchJsonPath = Join-Path $workspaceRoot ".vscode/launch.json"
$tasksJsonPath = Join-Path $workspaceRoot ".vscode/tasks.json"
$keybindingsJsonPath = Join-Path $workspaceRoot ".vscode/keybindings.json"

Write-Host ""
Write-Info "=== VSCode 프로젝트 설정 추가 ==="
Write-Host ""
Write-Host "프로젝트: $($ProjectName -join ', ')"
Write-Host "워크스페이스: $workspaceRoot"
if ($Force) {
    Write-Warning "Force 모드: 기존 설정을 제거하고 다시 추가합니다."
}
Write-Host ""

# JSON 파일 존재 확인
if (-not (Test-Path $launchJsonPath)) {
    Write-Error "오류: launch.json 파일을 찾을 수 없습니다: $launchJsonPath"
    exit 1
}

if (-not (Test-Path $tasksJsonPath)) {
    Write-Error "오류: tasks.json 파일을 찾을 수 없습니다: $tasksJsonPath"
    exit 1
}

# JSON 파일 읽기
$launchJsonContent = Get-Content $launchJsonPath -Raw
$launchJsonContent = $launchJsonContent -replace '//.*$', '' -replace '/\*[\s\S]*?\*/', ''
$launchJson = $launchJsonContent | ConvertFrom-Json

$tasksJsonContent = Get-Content $tasksJsonPath -Raw
$tasksJson = $tasksJsonContent | ConvertFrom-Json

# compounds 배열 초기화 (없으면 생성)
if (-not $launchJson.PSObject.Properties['compounds']) {
    $launchJson | Add-Member -NotePropertyName 'compounds' -NotePropertyValue @()
}

# 결과 추적
$addedProjects = @()
$skippedProjects = @()
$failedProjects = @()
$removedProjects = @()
$lastAddedProject = $null

# 각 프로젝트 처리
foreach ($project in $ProjectName) {
    Write-Host ""
    Write-Info "--- $project 처리 중 ---"

    # 1. .csproj 파일 검색
    Write-Host "프로젝트 파일 검색 중..."
    $csprojFiles = Get-ChildItem -Path $workspaceRoot -Recurse -Filter "$project.csproj" -ErrorAction SilentlyContinue

    if ($csprojFiles.Count -eq 0) {
        Write-Error "  오류: '$project.csproj' 파일을 찾을 수 없습니다."
        $failedProjects += $project
        continue
    }

    # 여러 개 발견 시 선택
    $selectedCsproj = $null
    if ($csprojFiles.Count -gt 1) {
        Write-Warning "동일한 이름의 프로젝트 파일이 여러 개 발견되었습니다:"
        Write-Host ""

        for ($i = 0; $i -lt $csprojFiles.Count; $i++) {
            $relativePath = $csprojFiles[$i].FullName.Replace($workspaceRoot, "").TrimStart("\", "/")
            Write-Host "  [$($i + 1)] $relativePath"
        }

        Write-Host ""
        $selection = Read-Host "선택할 번호를 입력하세요 (1-$($csprojFiles.Count))"

        if ($selection -match '^\d+$' -and [int]$selection -ge 1 -and [int]$selection -le $csprojFiles.Count) {
            $selectedCsproj = $csprojFiles[[int]$selection - 1]
        } else {
            Write-Error "  잘못된 선택입니다. '$project' 건너뜀."
            $failedProjects += $project
            continue
        }
    } else {
        $selectedCsproj = $csprojFiles[0]
    }

    $csprojPath = $selectedCsproj.FullName
    $projectDir = $selectedCsproj.DirectoryName
    $relativeCsprojPath = $csprojPath.Replace($workspaceRoot, "").TrimStart("\", "/").Replace("\", "/")
    $relativeProjectDir = $projectDir.Replace($workspaceRoot, "").TrimStart("\", "/").Replace("\", "/")

    Write-Success "  발견: $relativeCsprojPath"

    # 2. 중복 확인 및 처리
    $existingConfig = $launchJson.configurations | Where-Object { $_.name -eq $project }
    $buildTaskLabel = "build-$project"
    $existingTask = $tasksJson.tasks | Where-Object { $_.label -eq $buildTaskLabel }

    if ($existingConfig -or $existingTask) {
        if ($Force) {
            # 기존 설정 제거
            Write-Info "  기존 설정 제거 중..."
            Remove-ExistingConfiguration -LaunchJson $launchJson -Name $project
            Remove-ExistingTasks -TasksJson $tasksJson -ProjectName $project
            Remove-ExistingCompound -LaunchJson $launchJson -ProjectName $project
            $removedProjects += $project
            Write-Success "  기존 설정 제거됨: '$project'"
        } else {
            Write-Warning "  이미 존재하는 설정: '$project' - 건너뜀 (-Force로 덮어쓰기 가능)"
            $skippedProjects += $project
            continue
        }
    }

    # 3. Target Framework 감지
    $csprojContent = Get-Content $csprojPath -Raw
    $targetFramework = "net10.0"
    if ($csprojContent -match '<TargetFramework>([^<]+)</TargetFramework>') {
        $targetFramework = $matches[1]
    }

    # 4. 새 configuration 생성
    $newConfig = [PSCustomObject]@{
        name = $project
        type = "coreclr"
        request = "launch"
        preLaunchTask = $buildTaskLabel
        program = "`${workspaceFolder}/$relativeProjectDir/bin/Debug/$targetFramework/$project.dll"
        args = @()
        cwd = "`${workspaceFolder}/$relativeProjectDir"
        stopAtEntry = $false
        console = "integratedTerminal"
        env = [PSCustomObject]@{
            ASPNETCORE_ENVIRONMENT = "Development"
        }
    }

    # tasks.json tasks
    $buildTask = [PSCustomObject]@{
        label = $buildTaskLabel
        command = "dotnet"
        type = "process"
        args = @(
            "build",
            "`${workspaceFolder}/$relativeCsprojPath",
            "/property:GenerateFullPaths=true",
            "/consoleloggerparameters:NoSummary;ForceNoAlign"
        )
        problemMatcher = "`$msCompile"
    }

    $publishTask = [PSCustomObject]@{
        label = "publish-$project"
        command = "dotnet"
        type = "process"
        args = @(
            "publish",
            "`${workspaceFolder}/$relativeCsprojPath",
            "/property:GenerateFullPaths=true",
            "/consoleloggerparameters:NoSummary;ForceNoAlign"
        )
        problemMatcher = "`$msCompile"
    }

    $watchTask = [PSCustomObject]@{
        label = "watch-$project"
        command = "dotnet"
        type = "process"
        args = @(
            "watch",
            "run",
            "--project",
            "`${workspaceFolder}/$relativeCsprojPath"
        )
        problemMatcher = "`$msCompile"
    }

    # 5. JSON에 추가
    $launchJson.configurations += $newConfig
    $tasksJson.tasks += $buildTask
    $tasksJson.tasks += $publishTask
    $tasksJson.tasks += $watchTask

    $addedProjects += $project
    $lastAddedProject = $project
    Write-Success "  설정 추가됨: $project"
}

# 6. Compound 생성 (2개 이상의 프로젝트가 성공적으로 추가된 경우)
$compoundCreated = $false
$compoundName = $null

if ($addedProjects.Count -ge 2) {
    Write-Host ""
    Write-Info "Compound 설정 생성 중..."

    # Compound 이름 생성: "Run: Project1 + Project2"
    $compoundName = "Run: " + ($addedProjects -join " + ")

    # 동일한 이름의 compound가 있는지 확인
    $existingCompound = $launchJson.compounds | Where-Object { $_.name -eq $compoundName }

    if ($existingCompound) {
        Write-Warning "  이미 존재하는 compound: '$compoundName'"
    } else {
        # 새 compound 생성
        $newCompound = [PSCustomObject]@{
            name = $compoundName
            configurations = $addedProjects
            stopAll = $true
        }

        $launchJson.compounds += $newCompound
        $compoundCreated = $true
        Write-Success "  Compound 생성됨: '$compoundName'"
    }
}

# 7. JSON 파일 저장 (추가된 프로젝트가 있을 때만)
if ($addedProjects.Count -gt 0) {
    $jsonOptions = @{ Depth = 10 }

    # launch.json 저장
    $launchJsonOutput = $launchJson | ConvertTo-Json @jsonOptions
    $launchJsonOutput = $launchJsonOutput -replace '(\[\s*\])', '[]'
    $launchJsonOutput | Set-Content $launchJsonPath -Encoding UTF8

    # tasks.json 저장
    $tasksJsonOutput = $tasksJson | ConvertTo-Json @jsonOptions
    $tasksJsonOutput = $tasksJsonOutput -replace '(\[\s*\])', '[]'
    $tasksJsonOutput | Set-Content $tasksJsonPath -Encoding UTF8
}

# 결과 요약
Write-Host ""
Write-Host "============================================"
Write-Success "=== 처리 완료 ==="
Write-Host "============================================"
Write-Host ""

if ($removedProjects.Count -gt 0) {
    Write-Host "제거 후 다시 추가된 프로젝트 ($($removedProjects.Count)개):"
    foreach ($p in $removedProjects) {
        Write-Warning "  - $p (기존 설정 제거됨)"
    }
    Write-Host ""
}

if ($addedProjects.Count -gt 0) {
    Write-Host "추가된 프로젝트 ($($addedProjects.Count)개):"
    foreach ($p in $addedProjects) {
        Write-Success "  - $p"
        Write-Host "      launch.json: configuration '$p'"
        Write-Host "      tasks.json: build-$p, publish-$p, watch-$p"
    }
    Write-Host ""
}

if ($compoundCreated) {
    Write-Host "Compound (동시 실행 설정):"
    Write-Success "  - $compoundName"
    Write-Host "      VSCode에서 '$compoundName' 선택 후 F5로 동시 실행"
    Write-Host ""
}

if ($skippedProjects.Count -gt 0) {
    Write-Host "건너뛴 프로젝트 (이미 존재, $($skippedProjects.Count)개):"
    foreach ($p in $skippedProjects) {
        Write-Warning "  - $p (-Force 옵션으로 덮어쓰기 가능)"
    }
    Write-Host ""
}

if ($failedProjects.Count -gt 0) {
    Write-Host "실패한 프로젝트 ($($failedProjects.Count)개):"
    foreach ($p in $failedProjects) {
        Write-Error "  - $p"
    }
    Write-Host ""
}

# keybindings.json 안내
if ($addedProjects.Count -gt 0 -and (Test-Path $keybindingsJsonPath)) {
    Write-Host ""
    Write-Info "=== keybindings.json 안내 ==="
    Write-Host ""
    Write-Warning "  .vscode/keybindings.json은 VSCode에서 자동 적용되지 않습니다."
    Write-Host "  단축키를 사용하려면 아래 방법 중 하나를 선택하세요:"
    Write-Host ""
    Write-Host "  1. VSCode에서 Ctrl+Shift+P → 'Preferences: Open Keyboard Shortcuts (JSON)'"
    Write-Host "     열린 파일에 .vscode/keybindings.json 내용을 복사"
    Write-Host ""
    Write-Host "  2. 또는 tasks.json의 'build' 태스크가 기본 빌드로 설정되어 있으므로"
    Write-Host "     Ctrl+Shift+B로 솔루션 전체 빌드 가능"
    Write-Host ""
}

if ($addedProjects.Count -gt 0) {
    Write-Info "VSCode를 다시 로드하거나 F5를 눌러 실행하세요."
}
