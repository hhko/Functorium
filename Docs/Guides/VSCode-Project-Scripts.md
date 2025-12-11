# VSCode 프로젝트 설정 스크립트 가이드

이 문서는 VSCode 프로젝트 설정을 자동으로 관리하는 PowerShell 스크립트 사용법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [Add-VscodeProject.ps1](#add-vscodeprojectps1)
- [Remove-VscodeProject.ps1](#remove-vscodeprojectps1)
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
| `launch.json` | configuration 추가 | configuration 제거 |
| `tasks.json` | build/publish/watch task 추가 | task 제거 |
| `keybindings.json` | 단축키 args 업데이트 | args를 기본값으로 복원 |

<br/>

## 요약

### 주요 명령

```powershell
# 프로젝트 설정 추가
./.vscode/scripts/Add-VscodeProject.ps1 -ProjectName MyProject
./.vscode/scripts/Add-VscodeProject.ps1 MyProject

# 프로젝트 설정 제거
./.vscode/scripts/Remove-VscodeProject.ps1 -ProjectName MyProject
./.vscode/scripts/Remove-VscodeProject.ps1 MyProject
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

**2. 프로젝트 제거:**
```powershell
# 1. 스크립트 실행
./.vscode/scripts/Remove-VscodeProject.ps1 Observability

# 2. VSCode 재시작
```

### 주요 개념

**1. 프로젝트 검색**
- 스크립트는 워크스페이스 전체에서 `{ProjectName}.csproj` 파일을 검색합니다.
- 여러 개 발견 시 목록을 표시하고 선택할 수 있습니다.

**2. 중복 방지**
- 이미 동일한 이름의 설정이 있으면 추가하지 않고 종료합니다.
- 메시지: "이미 존재하는 설정: '{ProjectName}'"

**3. 단축키 관리**
- 단축키는 새로 추가하지 않고 기존 단축키의 `args`만 업데이트합니다.
- 충돌 없이 프로젝트 전환이 가능합니다.

<br/>

## Add-VscodeProject.ps1

### 사용법

```powershell
./Add-VscodeProject.ps1 -ProjectName <프로젝트명>
./Add-VscodeProject.ps1 <프로젝트명>
```

### 파라미터

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| `-ProjectName` | 예 | .csproj 파일명 (확장자 제외) |

### 동작 순서

```
1. {ProjectName}.csproj 파일 검색
2. 여러 개 발견 시 → 목록 표시 후 선택
3. 중복 확인 (launch.json, tasks.json)
4. launch.json에 configuration 추가
5. tasks.json에 build/publish/watch task 추가
6. keybindings.json의 단축키 args 업데이트
```

### 추가되는 설정

**launch.json:**
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
// args만 업데이트됨
{ "key": "ctrl+shift+b", "args": "build-Observability" }
{ "key": "ctrl+alt+p", "args": "publish-Observability" }
{ "key": "ctrl+alt+w", "args": "watch-Observability" }
```

### 출력 예시

```
=== VSCode 프로젝트 설정 추가 ===

프로젝트: Observability
워크스페이스: C:\Workspace\Github\Functorium

프로젝트 파일 검색 중...
발견: Tutorials/Observability/Src/Observability/Observability.csproj

launch.json 확인 중...
tasks.json 확인 중...
keybindings.json 업데이트 중...

=== 설정 추가 완료 ===

추가된 설정:
  launch.json:
    - configuration: Observability

  tasks.json:
    - task: build-Observability
    - task: publish-Observability
    - task: watch-Observability

  keybindings.json (args 업데이트):
    - Ctrl+Shift+B → build-Observability
    - Ctrl+Alt+P   → publish-Observability
    - Ctrl+Alt+W   → watch-Observability

VSCode를 다시 로드하거나 F5를 눌러 실행하세요.
```

<br/>

## Remove-VscodeProject.ps1

### 사용법

```powershell
./Remove-VscodeProject.ps1 -ProjectName <프로젝트명>
./Remove-VscodeProject.ps1 <프로젝트명>
```

### 파라미터

| 파라미터 | 필수 | 설명 |
|----------|------|------|
| `-ProjectName` | 예 | 제거할 프로젝트명 |

### 동작 순서

```
1. launch.json에서 configuration 제거
2. tasks.json에서 build/publish/watch task 제거
3. keybindings.json의 단축키 args를 기본값으로 복원
```

### 출력 예시

```
=== VSCode 프로젝트 설정 제거 ===

프로젝트: Observability

launch.json 확인 중...
  제거됨: configuration 'Observability'
tasks.json 확인 중...
  제거됨: task 'build-Observability'
  제거됨: task 'publish-Observability'
  제거됨: task 'watch-Observability'
keybindings.json 확인 중...
  복원됨: 단축키 args를 기본값으로 복원

=== 설정 제거 완료 ===

제거된 설정:
  launch.json:
    - configuration: Observability
  tasks.json:
    - task: build-Observability
    - task: publish-Observability
    - task: watch-Observability
  keybindings.json (args 복원):
    - Ctrl+Shift+B → build
    - Ctrl+Alt+P   → publish
    - Ctrl+Alt+W   → watch

VSCode를 다시 로드하여 변경사항을 적용하세요.
```

<br/>

## 설정 파일 구조

### 파일 위치

```
프로젝트/
├── .vscode/
│   ├── launch.json       # 디버그 설정
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

<br/>

## FAQ

### Q1. 새 .NET 프로젝트를 추가한 후 어떻게 VSCode 설정을 추가하나요?

**A:** `Add-VscodeProject.ps1` 스크립트를 실행합니다:

```powershell
./.vscode/scripts/Add-VscodeProject.ps1 MyNewProject
```

### Q2. 여러 프로젝트를 동시에 추가할 수 있나요?

**A:** 스크립트를 여러 번 실행하면 됩니다. 각 프로젝트는 별도의 configuration과 task로 추가됩니다:

```powershell
./.vscode/scripts/Add-VscodeProject.ps1 ProjectA
./.vscode/scripts/Add-VscodeProject.ps1 ProjectB
```

단, 단축키는 마지막으로 추가한 프로젝트로 설정됩니다.

### Q3. 단축키로 다른 프로젝트를 빌드하려면?

**A:** 두 가지 방법이 있습니다:

1. **스크립트로 전환:**
   ```powershell
   ./.vscode/scripts/Add-VscodeProject.ps1 OtherProject
   ```

2. **Task 목록에서 선택:**
   - `Ctrl+Shift+T` → Task 목록에서 원하는 프로젝트의 task 선택

### Q4. 프로젝트 설정을 수정하려면?

**A:** 제거 후 다시 추가하거나, 직접 JSON 파일을 편집합니다:

```powershell
# 방법 1: 스크립트 사용
./.vscode/scripts/Remove-VscodeProject.ps1 MyProject
./.vscode/scripts/Add-VscodeProject.ps1 MyProject

# 방법 2: 직접 편집
code .vscode/launch.json
code .vscode/tasks.json
```

### Q5. Target Framework가 다르면 어떻게 되나요?

**A:** 스크립트가 `.csproj` 파일에서 `<TargetFramework>`를 자동으로 읽어 설정합니다. 수동 수정이 필요 없습니다.

### Q6. keybindings.json은 왜 "참고용"인가요?

**A:** VSCode는 프로젝트별 `keybindings.json`을 지원하지 않습니다. 이 파일의 내용을 사용자 키바인딩에 복사해야 합니다:

- `Ctrl+Shift+P` → "Preferences: Open Keyboard Shortcuts (JSON)"
- 내용 복사하여 붙여넣기

### Q7. 스크립트를 수정하려면?

**A:** `.vscode/scripts/` 폴더의 PowerShell 파일을 직접 편집합니다. 주요 수정 포인트:

- Target Framework 기본값: `$targetFramework = "net10.0"`
- 환경 변수: `ASPNETCORE_ENVIRONMENT`
- 단축키 매핑

### Q8. Git에서 설정 파일을 추적하나요?

**A:** 네, `.vscode/launch.json`, `tasks.json`, `keybindings.json`, `scripts/` 폴더가 모두 Git으로 추적됩니다. 팀원들과 공유할 수 있습니다.

<br/>

## 참고 문서

- [VSCode 가이드](../Functorium/001-VSCode.md) - VSCode 설정 전체 가이드
- [Git Hooks 가이드](./Git-Hooks.md) - Git Hook 설정 가이드
