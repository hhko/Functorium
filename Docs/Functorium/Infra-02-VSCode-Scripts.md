# VSCode 프로젝트 설정 스크립트 가이드

이 문서는 VSCode 프로젝트 설정을 자동으로 관리하는 PowerShell 스크립트 사용법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [Add-VscodeProject.ps1](#add-vscodeprojectps1)
- [Remove-VscodeProject.ps1](#remove-vscodeprojectps1)
- [Compounds (동시 실행)](#compounds-동시-실행)
- [설정 파일 구조](#설정-파일-구조)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

.NET 프로젝트를 VSCode에서 실행/디버깅하려면 `launch.json`, `tasks.json`, `keybindings.json` 파일을 수동으로 편집해야 합니다. 이 스크립트들은 프로젝트 이름만으로 필요한 설정을 자동으로 추가/제거합니다.

### 스크립트 목록

| 스크립트 | 위치 | 설명 |
|----------|------|------|
| `Add-VscodeProject.ps1` | `.vscode/scripts/` | 프로젝트 설정 추가 |
| `Remove-VscodeProject.ps1` | `.vscode/scripts/` | 프로젝트 설정 제거 |

### 수정되는 파일

| 파일 | 추가 시 | 제거 시 |
|------|---------|---------|
| `launch.json` | configuration 추가, compounds 생성 | configuration 제거, 관련 compounds 제거 |
| `tasks.json` | build/publish/watch task 추가 | task 제거 |
| `keybindings.json` | 단축키 args 업데이트 | args를 기본값으로 복원 |

### 주요 기능

- **단일/복수 프로젝트 지원**: 쉼표로 구분하여 여러 프로젝트를 동시에 추가/제거
- **Compounds 자동 생성**: 2개 이상 프로젝트 추가 시 동시 실행 설정 자동 생성
- **중복 방지**: 이미 존재하는 설정은 건너뜀
- **자동 정리**: 프로젝트 제거 시 관련 compounds도 함께 제거

<br/>

## 요약

### 주요 명령

```powershell
# 단일 프로젝트 설정 추가
./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName MyProject
./.vscode/scripts/Add-VscodeProject.ps1 MyProject

# 여러 프로젝트 동시 추가 (compounds 자동 생성)
./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName Api, Worker, Gateway

# 단일 프로젝트 설정 제거
./.vscode/scripts/Remove-VscodeProject.ps1 -ProjectName MyProject
./.vscode/scripts/Remove-VscodeProject.ps1 MyProject

# 여러 프로젝트 동시 제거
./.vscode/scripts/Remove-VscodeProject.ps1 -ProjectName Api, Worker
```

### 주요 절차

**1. 새 프로젝트 추가:**
```powershell
# 1. 프로젝트 폴더에 .csproj 파일이 있는지 확인
ls -Recurse -Filter "*.csproj"

# 2. 스크립트 실행
./.vscode/scripts/Add-VscodeProject.ps1 Observability

# 3. VSCode 재시작 또는 F5로 실행 확인
```

**2. 여러 프로젝트 동시 실행 설정:**
```powershell
# 1. 여러 프로젝트를 쉼표로 구분하여 추가
./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName Api, Worker

# 2. VSCode에서 "Run: Api + Worker" 선택 후 F5로 동시 실행
```

**3. 프로젝트 제거:**
```powershell
# 1. 스크립트 실행 (관련 compounds도 자동 제거)
./.vscode/scripts/Remove-VscodeProject.ps1 Observability

# 2. VSCode 재시작
```

### 주요 개념

**1. 프로젝트 검색**
- 스크립트는 워크스페이스 전체에서 `{ProjectName}.csproj` 파일을 검색합니다.
- 여러 개 발견 시 목록을 표시하고 선택할 수 있습니다.

**2. 중복 방지**
- 이미 동일한 이름의 설정이 있으면 해당 프로젝트는 건너뜁니다.
- 메시지: "이미 존재하는 설정: '{ProjectName}' - 건너뜀"

**3. 단축키 관리**
- 단축키는 새로 추가하지 않고 기존 단축키의 `args`만 업데이트합니다.
- 충돌 없이 프로젝트 전환이 가능합니다.

**4. Compounds (동시 실행)**
- 2개 이상의 프로젝트를 추가하면 `compounds` 설정이 자동 생성됩니다.
- VSCode에서 compound를 선택하면 여러 프로젝트를 동시에 실행할 수 있습니다.

<br/>

## Add-VscodeProject.ps1

### 사용법

```powershell
# 단일 프로젝트
./Add-VscodeProject.ps1 -ProjectName <프로젝트명>
./Add-VscodeProject.ps1 <프로젝트명>

# 여러 프로젝트 (쉼표로 구분)
./Add-VscodeProject.ps1 -ProjectName <프로젝트1>, <프로젝트2>, <프로젝트3>
```

### 파라미터

| 파라미터 | 필수 | 타입 | 설명 |
|----------|------|------|------|
| `-ProjectName` | 예 | `string[]` | .csproj 파일명 (확장자 제외), 쉼표로 구분하여 여러 개 지정 가능 |

### 동작 순서

```
1. 각 프로젝트에 대해:
   a. {ProjectName}.csproj 파일 검색
   b. 여러 개 발견 시 → 목록 표시 후 선택
   c. 중복 확인 (launch.json, tasks.json)
   d. launch.json에 configuration 추가
   e. tasks.json에 build/publish/watch task 추가
2. 2개 이상 프로젝트 추가 시 → compounds 생성
3. keybindings.json의 단축키 args 업데이트 (마지막 프로젝트로)
```

### 추가되는 설정

**launch.json - configuration:**
```json
{
    "name": "Observability",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build-Observability",
    "program": "${workspaceFolder}/path/to/bin/Debug/net10.0/Observability.dll",
    "cwd": "${workspaceFolder}/path/to/project",
    "console": "integratedTerminal",
    "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
    }
}
```

**launch.json - compounds (여러 프로젝트 추가 시):**
```json
{
    "compounds": [
        {
            "name": "Run: Api + Worker",
            "configurations": ["Api", "Worker"],
            "stopAll": true
        }
    ]
}
```

**tasks.json:**
```json
// build-{ProjectName}
{
    "label": "build-Observability",
    "command": "dotnet",
    "type": "process",
    "args": ["build", "${workspaceFolder}/path/to/Observability.csproj", ...]
}

// publish-{ProjectName}
// watch-{ProjectName}
```

**keybindings.json:**
```json
// args만 업데이트됨 (마지막 프로젝트로)
{ "key": "ctrl+shift+b", "args": "build-Observability" }
{ "key": "ctrl+alt+p", "args": "publish-Observability" }
{ "key": "ctrl+alt+w", "args": "watch-Observability" }
```

### 출력 예시 (여러 프로젝트)

```
=== VSCode 프로젝트 설정 추가 ===

프로젝트: Api, Worker
워크스페이스: C:\Workspace\Github\Functorium

--- Api 처리 중 ---
프로젝트 파일 검색 중...
  발견: Src/Api/Api.csproj
  설정 추가됨: Api

--- Worker 처리 중 ---
프로젝트 파일 검색 중...
  발견: Src/Worker/Worker.csproj
  설정 추가됨: Worker

Compound 설정 생성 중...
  Compound 생성됨: 'Run: Api + Worker'

keybindings.json 업데이트 중...

============================================
=== 처리 완료 ===
============================================

추가된 프로젝트 (2개):
  - Api
      launch.json: configuration 'Api'
      tasks.json: build-Api, publish-Api, watch-Api
  - Worker
      launch.json: configuration 'Worker'
      tasks.json: build-Worker, publish-Worker, watch-Worker

Compound (동시 실행 설정):
  - Run: Api + Worker
      VSCode에서 'Run: Api + Worker' 선택 후 F5로 동시 실행

keybindings.json (args 업데이트 → Worker):
  - Ctrl+Shift+B → build-Worker
  - Ctrl+Alt+P   → publish-Worker
  - Ctrl+Alt+W   → watch-Worker

VSCode를 다시 로드하거나 F5를 눌러 실행하세요.
```

<br/>

## Remove-VscodeProject.ps1

### 사용법

```powershell
# 단일 프로젝트
./Remove-VscodeProject.ps1 -ProjectName <프로젝트명>
./Remove-VscodeProject.ps1 <프로젝트명>

# 여러 프로젝트 (쉼표로 구분)
./Remove-VscodeProject.ps1 -ProjectName <프로젝트1>, <프로젝트2>
```

### 파라미터

| 파라미터 | 필수 | 타입 | 설명 |
|----------|------|------|------|
| `-ProjectName` | 예 | `string[]` | 제거할 프로젝트명, 쉼표로 구분하여 여러 개 지정 가능 |

### 동작 순서

```
1. 각 프로젝트에 대해:
   a. launch.json에서 configuration 제거
   b. tasks.json에서 build/publish/watch task 제거
   c. keybindings.json의 단축키 args를 기본값으로 복원
2. 제거된 프로젝트가 포함된 compounds 자동 제거
```

### 출력 예시

```
=== VSCode 프로젝트 설정 제거 ===

프로젝트: Api
워크스페이스: C:\Workspace\Github\Functorium

--- Api 처리 중 ---
  제거됨: launch.json configuration 'Api'
  제거됨: tasks.json task 'build-Api'
  제거됨: tasks.json task 'publish-Api'
  제거됨: tasks.json task 'watch-Api'
  없음: keybindings.json 'Api' 관련 키바인딩

Compound 확인 중...
  제거됨: compound 'Run: Api + Worker'

============================================
=== 처리 완료 ===
============================================

제거된 프로젝트 (1개):
  - Api

제거된 Compound (1개):
  - Run: Api + Worker

VSCode를 다시 로드하여 변경사항을 적용하세요.
```

<br/>

## Compounds (동시 실행)

### 개요

VSCode의 `compounds` 기능을 사용하면 여러 프로젝트를 동시에 실행할 수 있습니다. 마이크로서비스 아키텍처에서 API 서버와 Worker를 동시에 실행하거나, Frontend와 Backend를 함께 실행할 때 유용합니다.

### 자동 생성 조건

- `Add-VscodeProject.ps1`으로 **2개 이상의 프로젝트**를 동시에 추가할 때 자동 생성
- compound 이름: `"Run: {Project1} + {Project2} + ..."`
- `stopAll: true` - 하나를 중지하면 모두 중지

### 사용 방법

1. 여러 프로젝트를 쉼표로 구분하여 추가:
   ```powershell
   ./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName Api, Worker, Gateway
   ```

2. VSCode 실행 패널에서 compound 선택:
   - `Ctrl+Shift+D`로 실행 패널 열기
   - 드롭다운에서 `"Run: Api + Worker + Gateway"` 선택
   - `F5`로 동시 실행

### 수동 생성

스크립트 없이 직접 compound를 생성하려면 `launch.json`에 추가:

```json
{
    "version": "0.2.0",
    "configurations": [
        // 개별 configuration들...
    ],
    "compounds": [
        {
            "name": "Run: Api + Worker",
            "configurations": ["Api", "Worker"],
            "stopAll": true
        }
    ]
}
```

### Compound 제거

- 프로젝트 제거 시 해당 프로젝트가 포함된 compound는 **자동으로 제거**됨
- 수동 제거: `launch.json`에서 `compounds` 배열에서 해당 항목 삭제

<br/>

## 설정 파일 구조

### 파일 위치

```
프로젝트/
├── .vscode/
│   ├── launch.json       # 디버그 설정 + compounds
│   ├── tasks.json        # 빌드 Task 설정
│   ├── keybindings.json  # 단축키 설정 (참고용)
│   └── scripts/
│       ├── Add-VscodeProject.ps1
│       └── Remove-VscodeProject.ps1
└── ...
```

### 단축키 매핑

| 단축키 | Task | 설명 |
|--------|------|------|
| `Ctrl+Shift+B` | `build-{ProjectName}` | 프로젝트 빌드 |
| `Ctrl+Alt+P` | `publish-{ProjectName}` | 프로젝트 배포 |
| `Ctrl+Alt+W` | `watch-{ProjectName}` | Hot Reload 실행 |
| `Ctrl+Shift+T` | - | Task 목록 표시 |
| `Ctrl+Shift+K` | - | 실행 중인 Task 종료 |
| `Ctrl+Shift+R` | - | 마지막 Task 재실행 |

<br/>

## 트러블슈팅

### 프로젝트를 찾을 수 없을 때

**원인:** `.csproj` 파일명이 입력한 프로젝트명과 다릅니다.

**해결:**
```powershell
# 프로젝트 파일 목록 확인
Get-ChildItem -Recurse -Filter "*.csproj" | Select-Object Name

# 정확한 이름으로 다시 실행
./.vscode/scripts/Add-VscodeProject.ps1 CorrectProjectName
```

### 이미 존재하는 설정 메시지

**원인:** 동일한 프로젝트 설정이 이미 추가되어 있습니다.

**해결:**
```powershell
# 기존 설정 제거 후 다시 추가
./.vscode/scripts/Remove-VscodeProject.ps1 MyProject
./.vscode/scripts/Add-VscodeProject.ps1 MyProject
```

### VSCode에서 설정이 반영되지 않을 때

**원인:** VSCode가 설정 파일을 다시 로드하지 않았습니다.

**해결:**
- `Ctrl+Shift+P` → "Developer: Reload Window"
- 또는 VSCode 재시작

### PowerShell 실행 정책 오류

**원인:** 스크립트 실행이 차단되어 있습니다.

**해결:**
```powershell
# 현재 세션에서만 허용
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 또는 사용자 수준에서 허용
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 여러 프로젝트가 발견되었을 때

**원인:** 동일한 이름의 `.csproj` 파일이 여러 위치에 있습니다.

**해결:** 스크립트가 목록을 표시하면 번호를 입력하여 선택합니다.

```
동일한 이름의 프로젝트 파일이 여러 개 발견되었습니다:

  [1] Src/MyProject/MyProject.csproj
  [2] Tests/MyProject.Tests/MyProject.csproj

선택할 번호를 입력하세요 (1-2): 1
```

### 배열 파라미터 전달 오류

**원인:** bash에서 PowerShell 스크립트를 호출할 때 배열 파라미터가 제대로 전달되지 않음

**해결:**
```powershell
# pwsh -File 대신 pwsh -Command 사용
pwsh -Command "./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName Api, Worker"
```

<br/>

## FAQ

### Q1. 새 .NET 프로젝트를 추가한 후 어떻게 VSCode 설정을 추가하나요?

**A:** `Add-VscodeProject.ps1` 스크립트를 실행합니다:

```powershell
./.vscode/scripts/Add-VscodeProject.ps1 MyNewProject
```

### Q2. 여러 프로젝트를 동시에 추가할 수 있나요?

**A:** 네, 쉼표로 구분하여 여러 프로젝트를 한 번에 추가할 수 있습니다:

```powershell
./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName Api, Worker, Gateway
```

2개 이상 프로젝트를 추가하면 `compounds`가 자동 생성되어 VSCode에서 동시 실행이 가능합니다.

### Q3. 여러 프로젝트를 동시에 실행하려면?

**A:** 두 가지 방법이 있습니다:

1. **스크립트로 compound 자동 생성:**
   ```powershell
   ./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName Api, Worker
   # VSCode에서 "Run: Api + Worker" 선택 후 F5
   ```

2. **수동으로 compound 생성:**
   `launch.json`의 `compounds` 배열에 직접 추가

### Q4. 단축키로 다른 프로젝트를 빌드하려면?

**A:** 두 가지 방법이 있습니다:

1. **스크립트로 전환:**
   ```powershell
   ./.vscode/scripts/Add-VscodeProject.ps1 OtherProject
   ```

2. **Task 목록에서 선택:**
   - `Ctrl+Shift+T` → Task 목록에서 원하는 프로젝트의 task 선택

### Q5. 프로젝트 설정을 수정하려면?

**A:** 제거 후 다시 추가하거나, 직접 JSON 파일을 편집합니다:

```powershell
# 방법 1: 스크립트 사용
./.vscode/scripts/Remove-VscodeProject.ps1 MyProject
./.vscode/scripts/Add-VscodeProject.ps1 MyProject

# 방법 2: 직접 편집
code .vscode/launch.json
code .vscode/tasks.json
```

### Q6. Target Framework가 다르면 어떻게 되나요?

**A:** 스크립트가 `.csproj` 파일에서 `<TargetFramework>`를 자동으로 읽어 설정합니다. 수동 수정이 필요 없습니다.

### Q7. keybindings.json은 왜 "참고용"인가요?

**A:** VSCode는 프로젝트별 `keybindings.json`을 지원하지 않습니다. 이 파일의 내용을 사용자 키바인딩에 복사해야 합니다:

- `Ctrl+Shift+P` → "Preferences: Open Keyboard Shortcuts (JSON)"
- 내용 복사하여 붙여넣기

### Q8. 스크립트를 수정하려면?

**A:** `.vscode/scripts/` 폴더의 PowerShell 파일을 직접 편집합니다. 주요 수정 포인트:

- Target Framework 기본값: `$targetFramework = "net10.0"`
- 환경 변수: `ASPNETCORE_ENVIRONMENT`
- 단축키 매핑

### Q9. Git에서 설정 파일을 추적하나요?

**A:** 네, `.vscode/launch.json`, `tasks.json`, `keybindings.json`, `scripts/` 폴더가 모두 Git으로 추적됩니다. 팀원들과 공유할 수 있습니다.

### Q10. Compound를 제거하면 개별 프로젝트도 제거되나요?

**A:** 아니요, compound만 제거됩니다. 개별 프로젝트 설정은 유지됩니다. 반대로, 프로젝트를 제거하면 해당 프로젝트가 포함된 compound는 자동으로 제거됩니다.

<br/>

## 참고 문서

- [VSCode 가이드](./Infra-01-VSCode.md) - VSCode 설정 전체 가이드
- [Git Hooks 가이드](./Infra-03-Git-Hooks.md) - Git Hook 설정 가이드
