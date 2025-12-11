#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    VSCode launch.json, tasks.json, keybindings.json에 프로젝트 설정을 자동으로 추가합니다.

.DESCRIPTION
    프로젝트 이름을 입력받아 해당 .csproj 파일을 찾고,
    VSCode의 launch.json, tasks.json, keybindings.json에 실행/빌드 설정을 추가합니다.

.PARAMETER ProjectName
    추가할 프로젝트의 이름 (.csproj 파일명에서 확장자 제외)

.EXAMPLE
    ./Add-VscodeProject.ps1 -ProjectName Observability
    ./Add-VscodeProject.ps1 Observability

.NOTES
    - 이미 동일한 이름의 설정이 있으면 종료합니다.
    - 여러 개의 .csproj 파일이 발견되면 선택할 수 있습니다.
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
Write-Info "=== VSCode 프로젝트 설정 추가 ==="
Write-Host ""
Write-Host "프로젝트: $ProjectName"
Write-Host "워크스페이스: $workspaceRoot"
Write-Host ""

# 1. .csproj 파일 검색
Write-Info "프로젝트 파일 검색 중..."
$csprojFiles = Get-ChildItem -Path $workspaceRoot -Recurse -Filter "$ProjectName.csproj" -ErrorAction SilentlyContinue

if ($csprojFiles.Count -eq 0) {
    Write-Error "오류: '$ProjectName.csproj' 파일을 찾을 수 없습니다."
    exit 1
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
        Write-Error "잘못된 선택입니다."
        exit 1
    }
} else {
    $selectedCsproj = $csprojFiles[0]
}

$csprojPath = $selectedCsproj.FullName
$projectDir = $selectedCsproj.DirectoryName
$relativeCsprojPath = $csprojPath.Replace($workspaceRoot, "").TrimStart("\", "/").Replace("\", "/")
$relativeProjectDir = $projectDir.Replace($workspaceRoot, "").TrimStart("\", "/").Replace("\", "/")

Write-Success "발견: $relativeCsprojPath"
Write-Host ""

# 2. launch.json 읽기 및 중복 확인
Write-Info "launch.json 확인 중..."

if (-not (Test-Path $launchJsonPath)) {
    Write-Error "오류: launch.json 파일을 찾을 수 없습니다: $launchJsonPath"
    exit 1
}

# JSON 읽기 (주석 제거)
$launchJsonContent = Get-Content $launchJsonPath -Raw
$launchJsonContent = $launchJsonContent -replace '//.*$', '' -replace '/\*[\s\S]*?\*/', ''
$launchJson = $launchJsonContent | ConvertFrom-Json

# 중복 확인
$existingConfig = $launchJson.configurations | Where-Object { $_.name -eq $ProjectName }
if ($existingConfig) {
    Write-Warning "이미 존재하는 설정: '$ProjectName'"
    Write-Host ""
    Write-Host "launch.json에 '$ProjectName' 이름의 configuration이 이미 있습니다."
    exit 0
}

# 3. tasks.json 읽기 및 중복 확인
Write-Info "tasks.json 확인 중..."

if (-not (Test-Path $tasksJsonPath)) {
    Write-Error "오류: tasks.json 파일을 찾을 수 없습니다: $tasksJsonPath"
    exit 1
}

$tasksJsonContent = Get-Content $tasksJsonPath -Raw
$tasksJson = $tasksJsonContent | ConvertFrom-Json

$buildTaskLabel = "build-$ProjectName"
$existingTask = $tasksJson.tasks | Where-Object { $_.label -eq $buildTaskLabel }
if ($existingTask) {
    Write-Warning "이미 존재하는 설정: '$buildTaskLabel'"
    Write-Host ""
    Write-Host "tasks.json에 '$buildTaskLabel' 라벨의 task가 이미 있습니다."
    exit 0
}

# 4. 새 configuration 생성
Write-Info "설정 추가 중..."

# Target Framework 감지 (csproj에서 읽기)
$csprojContent = Get-Content $csprojPath -Raw
$targetFramework = "net10.0"  # 기본값
if ($csprojContent -match '<TargetFramework>([^<]+)</TargetFramework>') {
    $targetFramework = $matches[1]
}

# launch.json configuration
$newConfig = [PSCustomObject]@{
    name = $ProjectName
    type = "coreclr"
    request = "launch"
    preLaunchTask = $buildTaskLabel
    program = "`${workspaceFolder}/$relativeProjectDir/bin/Debug/$targetFramework/$ProjectName.dll"
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
    label = "publish-$ProjectName"
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
    label = "watch-$ProjectName"
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

# 5. JSON 파일 업데이트
# launch.json에 configuration 추가
$launchJson.configurations += $newConfig

# tasks.json에 tasks 추가
$tasksJson.tasks += $buildTask
$tasksJson.tasks += $publishTask
$tasksJson.tasks += $watchTask

# 6. 파일 저장
$jsonOptions = @{
    Depth = 10
}

# launch.json 저장
$launchJsonOutput = $launchJson | ConvertTo-Json @jsonOptions
# JSON 형식 정리 (배열 한 줄로)
$launchJsonOutput = $launchJsonOutput -replace '(\[\s*\])', '[]'
$launchJsonOutput | Set-Content $launchJsonPath -Encoding UTF8

# tasks.json 저장
$tasksJsonOutput = $tasksJson | ConvertTo-Json @jsonOptions
$tasksJsonOutput = $tasksJsonOutput -replace '(\[\s*\])', '[]'
$tasksJsonOutput | Set-Content $tasksJsonPath -Encoding UTF8

# 7. keybindings.json 업데이트 (기존 단축키의 args만 변경)
$keybindingsUpdated = $false
if (Test-Path $keybindingsJsonPath) {
    Write-Info "keybindings.json 업데이트 중..."

    $keybindingsContent = Get-Content $keybindingsJsonPath -Raw

    # 이미 해당 프로젝트의 키바인딩이 있는지 확인
    if ($keybindingsContent -match "build-$ProjectName") {
        Write-Warning "keybindings.json에 '$ProjectName' 관련 키바인딩이 이미 있습니다."
    } else {
        # 기존 단축키의 args를 새 프로젝트로 업데이트
        # "args": "build" → "args": "build-ProjectName"
        # "args": "publish" → "args": "publish-ProjectName"
        # "args": "watch" → "args": "watch-ProjectName"

        $updated = $false

        # build 단축키 업데이트
        if ($keybindingsContent -match '"args":\s*"build[^-]') {
            $keybindingsContent = $keybindingsContent -replace '("args":\s*")build(")', "`$1build-$ProjectName`$2"
            $updated = $true
        } elseif ($keybindingsContent -match '"args":\s*"build-[^"]+') {
            # 이미 다른 프로젝트로 설정된 경우 업데이트
            $keybindingsContent = $keybindingsContent -replace '("args":\s*")build-[^"]+(")', "`$1build-$ProjectName`$2"
            $updated = $true
        }

        # publish 단축키 업데이트
        if ($keybindingsContent -match '"args":\s*"publish[^-]') {
            $keybindingsContent = $keybindingsContent -replace '("args":\s*")publish(")', "`$1publish-$ProjectName`$2"
            $updated = $true
        } elseif ($keybindingsContent -match '"args":\s*"publish-[^"]+') {
            $keybindingsContent = $keybindingsContent -replace '("args":\s*")publish-[^"]+(")', "`$1publish-$ProjectName`$2"
            $updated = $true
        }

        # watch 단축키 업데이트
        if ($keybindingsContent -match '"args":\s*"watch[^-]') {
            $keybindingsContent = $keybindingsContent -replace '("args":\s*")watch(")', "`$1watch-$ProjectName`$2"
            $updated = $true
        } elseif ($keybindingsContent -match '"args":\s*"watch-[^"]+') {
            $keybindingsContent = $keybindingsContent -replace '("args":\s*")watch-[^"]+(")', "`$1watch-$ProjectName`$2"
            $updated = $true
        }

        if ($updated) {
            $keybindingsContent | Set-Content $keybindingsJsonPath -Encoding UTF8 -NoNewline
            $keybindingsUpdated = $true
        }
    }
}

Write-Host ""
Write-Success "=== 설정 추가 완료 ==="
Write-Host ""
Write-Host "추가된 설정:"
Write-Host "  launch.json:"
Write-Host "    - configuration: $ProjectName"
Write-Host ""
Write-Host "  tasks.json:"
Write-Host "    - task: $buildTaskLabel"
Write-Host "    - task: publish-$ProjectName"
Write-Host "    - task: watch-$ProjectName"
Write-Host ""
if ($keybindingsUpdated) {
    Write-Host "  keybindings.json (args 업데이트):"
    Write-Host "    - Ctrl+Shift+B → build-$ProjectName"
    Write-Host "    - Ctrl+Alt+P   → publish-$ProjectName"
    Write-Host "    - Ctrl+Alt+W   → watch-$ProjectName"
    Write-Host ""
}
Write-Info "VSCode를 다시 로드하거나 F5를 눌러 실행하세요."
