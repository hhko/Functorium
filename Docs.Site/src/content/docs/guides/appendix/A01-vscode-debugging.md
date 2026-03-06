---
title: "VSCode 디버깅 및 프로젝트 관리"
---

이 문서는 Visual Studio Code에서 Functorium 프로젝트를 디버깅하고 프로젝트 설정을 관리하는 방법을 설명합니다.

## 요약

### 주요 명령

```bash
# 디버깅 시작
F5

# 디버깅 없이 실행
Ctrl+F5

# 빌드 태스크 실행
Ctrl+Shift+B

# 태스크 목록 열기
Ctrl+Shift+P → "Tasks: Run Task"
```

### 주요 절차

**1. 디버깅 시작:**
```bash
# 1. F5 키 누르기
# 2. 디버그 구성 선택 (예: "LayeredArch")
# 3. 통합 터미널에서 로그 확인
```

### 주요 개념

| 파일 | 역할 |
|------|------|
| `launch.json` | 디버깅 구성 (F5로 실행) |
| `tasks.json` | 빌드/실행 태스크 정의 |
| `settings.json` | 프로젝트별 VSCode 설정 |

| 확장 | 용도 |
|------|------|
| C# Dev Kit | C# 개발 지원 |
| .NET Install Tool | .NET SDK 관리 |




## 설정 파일

### 폴더 구조

```
.vscode/
├── settings.json       # 프로젝트별 VSCode 설정
├── launch.json         # 디버깅 설정 + compounds
├── tasks.json          # 빌드 태스크 설정
└── keybindings.json    # 단축키 설정 (참고용)
```

### launch.json

디버깅 구성을 정의합니다. 구성 이름은 `{ProjectName}` 패턴을 사용합니다.

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "LayeredArch",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-LayeredArch",
            "program": "${workspaceFolder}/Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Infrastructure/bin/Debug/net10.0/LayeredArch.Adapters.Infrastructure.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

**주요 속성:**

| 속성 | 설명 |
|------|------|
| `name` | 디버그 구성 이름 (F5 시 선택 가능), `{ProjectName}` 패턴 사용 |
| `preLaunchTask` | 디버깅 전 실행할 태스크, `build-{ProjectName}` 패턴으로 tasks.json의 label 참조 |
| `program` | 실행할 DLL 경로 |
| `cwd` | 작업 디렉토리 |
| `env` | 환경 변수 |

### tasks.json

빌드 및 실행 태스크를 정의합니다. 태스크 레이블은 `{task}-{ProjectName}` 패턴을 사용합니다.

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build-LayeredArch",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Infrastructure/LayeredArch.Adapters.Infrastructure.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile"
        }
    ]
}
```

| 태스크 패턴 | 설명 | 실행 방법 |
|-------------|------|----------|
| `build-{ProjectName}` | 프로젝트 빌드 | F5 (자동) 또는 Ctrl+Shift+B |
| `publish-{ProjectName}` | 배포용 빌드 | Tasks: Run Task → publish-{ProjectName} |
| `watch-{ProjectName}` | Hot Reload 모드 실행 | Tasks: Run Task → watch-{ProjectName} |

### settings.json

프로젝트별 VSCode 설정:

```json
{
    "dotnet.defaultSolution": "Functorium.slnx",
    "omnisharp.enableRoslynAnalyzers": true,
    "omnisharp.enableEditorConfigSupport": true,
    "csharp.semanticHighlighting.enabled": true,
    "files.exclude": {
        "**/bin": true,
        "**/obj": true,
        "**/TestResults": true
    },
    "dotnet.preferCSharpExtension": true
}
```




## 디버깅

### 기본 디버깅 (F5)

1. `F5` 키를 눌러 디버깅 시작
2. 디버그 구성 선택 (예: `LayeredArch`)
3. `build-{ProjectName}` 태스크가 먼저 실행됨
4. 빌드 성공 시 애플리케이션 시작
5. 통합 터미널에서 로그 확인

### 브레이크포인트 사용

```
1. 코드 왼쪽 여백 클릭 → 빨간 점 표시
2. F5로 디버깅 시작
3. 해당 코드 실행 시 중단
4. F10 (Step Over), F11 (Step Into), F5 (Continue)
```

### 조건부 브레이크포인트

브레이크포인트 우클릭 → `Edit Breakpoint...`에서 조건 설정:

| 유형 | 설명 | 예시 |
|------|------|------|
| Expression | 조건이 true일 때 중단 | `user.Id == 42` |
| Hit Count | N번째 실행 시 중단 | `= 10` |
| Log Message | 중단 없이 로그만 출력 | `User: {user.Name}` |

### 로그포인트 (Logpoint)

코드 수정 없이 디버깅 중 로그를 출력합니다. 코드 왼쪽 여백 우클릭 → `Add Logpoint...`:

```
Processing user: {user.Name}, Role: {user.Role}
Request received: {request.Method} {request.Path}
```

> **팁:** 로그포인트는 빨간 점 대신 다이아몬드 모양으로 표시됩니다.

### Ctrl+C 입력 받기 (Graceful Shutdown)

| 옵션 | Ctrl+C 지원 | 설명 |
|------|-------------|------|
| `integratedTerminal` | O | 통합 터미널에서 실행, Ctrl+C 전달됨 |
| `externalTerminal` | O | 외부 터미널 창에서 실행 |
| `internalConsole` | X | 디버그 콘솔 (Ctrl+C 미지원) |

> **참고:** `internalConsole`을 사용하면 `Ctrl+C`가 전달되지 않아 애플리케이션이 강제 종료됩니다. 리소스 정리가 필요한 경우 반드시 `integratedTerminal`을 사용하세요.




## 디버깅 단축키

### 실행 제어

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F5` | Start/Continue | 디버깅 시작 또는 계속 실행 |
| `Ctrl+F5` | Run Without Debugging | 디버깅 없이 실행 |
| `Shift+F5` | Stop | 디버깅 중지 |
| `Ctrl+Shift+F5` | Restart | 디버깅 재시작 |
| `F6` | Pause | 실행 일시 중지 |

### 스텝 실행

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F10` | Step Over | 현재 줄 실행 (함수 내부로 들어가지 않음) |
| `F11` | Step Into | 현재 줄 실행 (함수 내부로 들어감) |
| `Shift+F11` | Step Out | 현재 함수에서 빠져나옴 |
| `Ctrl+F10` | Run to Cursor | 커서 위치까지 실행 |

### 브레이크포인트

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F9` | Toggle Breakpoint | 브레이크포인트 설정/해제 |
| `Ctrl+F9` | Enable/Disable | 브레이크포인트 활성화/비활성화 |
| `Ctrl+Shift+F9` | Remove All | 모든 브레이크포인트 제거 |

### 패널 및 뷰

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `Ctrl+Shift+D` | Debug View | 디버그 패널 열기 |
| `Ctrl+Shift+Y` | Debug Console | 디버그 콘솔 열기 |
| `Ctrl+K Ctrl+I` | Show Hover | 변수 정보 팝업 표시 |

### 일반 단축키

**파일 탐색:**

| 단축키 | 동작 |
|--------|------|
| `Ctrl+P` | 파일 빠른 열기 |
| `Ctrl+Shift+P` | 명령 팔레트 |
| `Ctrl+Shift+E` | 탐색기 패널 |
| `Ctrl+Shift+F` | 전체 검색 |

**코드 편집:**

| 단축키 | 동작 |
|--------|------|
| `F12` | 정의로 이동 |
| `Alt+F12` | 정의 미리보기 |
| `Shift+F12` | 모든 참조 찾기 |
| `F2` | 심볼 이름 변경 |
| `Ctrl+Space` | IntelliSense 표시 |




## 고급 디버깅

### Watch와 변수 검사

디버깅 중 `Ctrl+Shift+D` → WATCH 섹션에서 표현식 추가:

```csharp
users.Count
users.Where(u => u.IsActive).ToList()
response?.Content?.Headers?.ContentType
user?.Name ?? "Anonymous"
```

### 디버그 콘솔

디버깅 중 `Ctrl+Shift+Y`로 표현식을 실행:

```csharp
// 변수 값 확인
user.Name

// 객체 JSON 직렬화
System.Text.Json.JsonSerializer.Serialize(user, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })

// 컬렉션 내용 확인
string.Join(", ", items.Select(i => i.Name))
```

### Edit and Continue (Hot Reload)

디버깅 중 코드를 수정하고 계속 실행합니다.

**지원되는 변경:** 메서드 본문 수정, 지역 변수 추가/수정, 람다 표현식 수정, 속성 값 변경

**지원되지 않는 변경:** 메서드 시그니처 변경, 새 클래스/인터페이스 추가, 제네릭 타입 변경, async/await 추가

### 호출 스택 탐색

브레이크포인트에서 중단 후 `CALL STACK` 패널에서 함수 호출 경로를 추적합니다.

```json
{
    "justMyCode": true,
    "enableStepFiltering": true,
    "symbolOptions": {
        "searchMicrosoftSymbolServer": true
    }
}
```

### 동시에 여러 프로젝트 실행

`launch.json`의 `compounds` 구성 사용:

```json
{
    "compounds": [
        {
            "name": "API + Worker",
            "configurations": ["API Server", "Worker Service"],
            "stopAll": true
        }
    ]
}
```

| 속성 | 설명 |
|------|------|
| `name` | 복합 구성 이름 (F5 시 선택 가능) |
| `configurations` | 동시에 실행할 구성 이름 배열 |
| `stopAll` | 하나가 종료되면 모두 종료 (`true` 권장) |

### 원격/컨테이너 디버깅

Docker 컨테이너에서 실행 중인 애플리케이션을 디버깅합니다.

**launch.json 설정:**
```json
{
    "name": "Docker Attach",
    "type": "coreclr",
    "request": "attach",
    "processId": "${command:pickRemoteProcess}",
    "pipeTransport": {
        "pipeProgram": "docker",
        "pipeArgs": ["exec", "-i", "container-name"],
        "debuggerPath": "/vsdbg/vsdbg",
        "pipeCwd": "${workspaceFolder}"
    },
    "sourceFileMap": {
        "/app": "${workspaceFolder}/Src/MyApp"
    }
}
```





## 태스크

태스크 레이블은 `{task}-{ProjectName}` 네이밍 규칙을 따릅니다.

### build-{ProjectName}

프로젝트를 빌드합니다.

```bash
# 수동 실행
Ctrl+Shift+B

# 또는
Ctrl+Shift+P → "Tasks: Run Task" → "build-LayeredArch"
```

### publish-{ProjectName}

배포용으로 빌드합니다.

```bash
Ctrl+Shift+P → "Tasks: Run Task" → "publish-LayeredArch"
```

### watch-{ProjectName}

Hot Reload 모드로 실행합니다. 코드 변경 시 자동으로 재빌드하고 재시작합니다.

```bash
Ctrl+Shift+P → "Tasks: Run Task" → "watch-LayeredArch"
```




## 트러블슈팅

### 디버깅이 시작되지 않을 때

**원인:** C# 확장이 설치되지 않았거나 활성화되지 않음

**해결:**
```bash
# 1. 확장 설치 확인
Ctrl+Shift+X → "C# Dev Kit" 검색 → 설치

# 2. VSCode 재시작
```

### preLaunchTask 오류

**증상:** `preLaunchTask 'build-{ProjectName}' could not be found`

**원인:** `tasks.json` 파일이 없거나 해당 build 태스크가 정의되지 않음

**해결:** `.vscode/tasks.json` 파일에 `build-{ProjectName}` 태스크가 정의되어 있는지 확인

### 빌드 실패

**해결:**
```bash
# 1. .NET SDK 설치 확인
dotnet --version

# 2. 터미널에서 직접 빌드 시도
dotnet build <프로젝트경로>/<프로젝트>.csproj
```

### Hot Reload가 작동하지 않을 때

메서드 시그니처 변경, 새 클래스 추가 등은 Hot Reload를 지원하지 않습니다. watch 태스크를 중지(Ctrl+C) 후 재시작하세요.

### VSCode에서 설정이 반영되지 않을 때

**해결:** `Ctrl+Shift+P` → "Developer: Reload Window" 또는 VSCode 재시작

### PowerShell 실행 정책 오류

**해결:**
```powershell
# 현재 세션에서만 허용
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 또는 사용자 수준에서 허용
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```




## FAQ

### Q1. launch.json과 tasks.json의 차이점은 무엇인가요?

| 파일 | 용도 | 실행 방법 |
|------|------|----------|
| `launch.json` | 디버깅 구성 정의 | F5 |
| `tasks.json` | 빌드/실행 태스크 정의 | Ctrl+Shift+B 또는 Run Task |

`launch.json`의 `preLaunchTask`가 `tasks.json`의 태스크를 참조합니다.

### Q2. 다른 프로젝트를 디버깅하려면 어떻게 하나요?

`launch.json`에 `{ProjectName}` 패턴으로 새 구성을 추가하고, `tasks.json`에 대응하는 `build-{ProjectName}` 태스크를 추가합니다.

### Q3. 여러 프로젝트를 동시에 실행하려면 어떻게 하나요?

`launch.json`의 `compounds`에 동시에 실행할 구성 이름을 배열로 지정합니다. F5에서 compound 구성을 선택하면 여러 프로젝트가 동시에 시작됩니다.

### Q4. 환경 변수를 추가하려면 어떻게 하나요?

`launch.json`의 `env` 섹션에 추가:

```json
{
    "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "MY_CUSTOM_VAR": "value"
    }
}
```

### Q5. Ctrl+C로 애플리케이션이 종료되지 않습니다.

`console` 설정을 `integratedTerminal`로 변경하세요:

```json
{
    "console": "integratedTerminal"
}
```

`internalConsole`은 `Ctrl+C` 신호가 전달되지 않습니다.

### Q6. 동시 실행 시 각 프로젝트의 포트가 충돌합니다.

각 구성의 `env`에서 다른 포트를 지정하세요:

```json
{
    "env": {
        "ASPNETCORE_URLS": "http://localhost:5000"
    }
}
```

### Q7. 단축키로 다른 프로젝트를 빌드하려면?

`Ctrl+Shift+P` → "Tasks: Run Task"에서 원하는 `build-{ProjectName}` 태스크를 선택합니다.

## 참고 문서

- [VSCode Debugging](https://code.visualstudio.com/docs/editor/debugging)
- [VSCode Tasks](https://code.visualstudio.com/docs/editor/tasks)
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
