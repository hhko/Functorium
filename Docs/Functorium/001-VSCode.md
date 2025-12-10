# VSCode 설정 가이드

이 문서는 Visual Studio Code에서 Functorium 프로젝트를 개발하기 위한 설정을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [설정 파일](#설정-파일)
- [디버깅](#디버깅)
  - [디버깅 단축키](#디버깅-단축키)
  - [일반 단축키](#일반-단축키)
  - [조건부 브레이크포인트](#조건부-브레이크포인트)
  - [로그포인트 (Logpoint)](#로그포인트-logpoint)
  - [예외 브레이크포인트](#예외-브레이크포인트)
  - [Watch와 변수 검사](#watch와-변수-검사)
  - [디버그 콘솔](#디버그-콘솔)
  - [Edit and Continue (Hot Reload)](#edit-and-continue-hot-reload)
  - [호출 스택 탐색](#호출-스택-탐색)
  - [원격/컨테이너 디버깅](#원격컨테이너-디버깅)
  - [동시에 여러 프로젝트 실행](#동시에-여러-프로젝트-실행)
  - [Ctrl+C 입력 받기 (Graceful Shutdown)](#ctrlc-입력-받기-graceful-shutdown)
- [태스크](#태스크)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

`.vscode/` 폴더에는 VSCode에서 프로젝트를 빌드, 디버깅, 실행하기 위한 설정 파일들이 포함되어 있습니다.

### 필수 확장

| 확장 | 용도 |
|------|------|
| C# Dev Kit | C# 개발 지원 |
| .NET Install Tool | .NET SDK 관리 |

### 폴더 구조

```
.vscode/
├── settings.json   # 프로젝트별 VSCode 설정
├── launch.json     # 디버깅 설정
└── tasks.json      # 빌드 태스크 설정
```

<br/>

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
# 2. ".NET Core Launch (web)" 선택
# 3. 브라우저에서 http://localhost:5000 접속
```

**2. Hot Reload 개발:**
```bash
# 1. Ctrl+Shift+P → "Tasks: Run Task"
# 2. "watch" 선택
# 3. 코드 수정 시 자동 재빌드/재시작
```

### 주요 개념

| 파일 | 역할 |
|------|------|
| `launch.json` | 디버깅 구성 (F5로 실행) |
| `tasks.json` | 빌드/실행 태스크 정의 |
| `settings.json` | 프로젝트별 VSCode 설정 |

<br/>

## 설정 파일

### launch.json

디버깅 구성을 정의합니다.

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/Tutorials/Observability/Src/Observability/bin/Debug/net10.0/Observability.dll",
            "cwd": "${workspaceFolder}/Tutorials/Observability/Src/Observability",
            "console": "integratedTerminal",
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
| `name` | 디버그 구성 이름 (F5 시 선택 가능) |
| `preLaunchTask` | 디버깅 전 실행할 태스크 (tasks.json의 label) |
| `program` | 실행할 DLL 경로 |
| `cwd` | 작업 디렉토리 |
| `console` | 콘솔 유형 (`integratedTerminal` 권장) |
| `env` | 환경 변수 |

### tasks.json

빌드 및 실행 태스크를 정의합니다.

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/Tutorials/Observability/Src/Observability/Observability.csproj"
            ],
            "problemMatcher": "$msCompile"
        }
    ]
}
```

**정의된 태스크:**

| 태스크 | 설명 | 실행 방법 |
|--------|------|----------|
| `build` | 프로젝트 빌드 | F5 (자동) 또는 Ctrl+Shift+B |
| `publish` | 배포용 빌드 | Tasks: Run Task → publish |
| `watch` | Hot Reload 모드 실행 | Tasks: Run Task → watch |

### settings.json

프로젝트별 VSCode 설정입니다. 현재는 비어있으며, 필요 시 설정을 추가합니다.

```json
{
    // 프로젝트별 설정 추가
}
```

**일반적인 설정 예시:**

```json
{
    "editor.formatOnSave": true,
    "dotnet.defaultSolution": "Functorium.slnx",
    "omnisharp.enableRoslynAnalyzers": true
}
```

<br/>

## 디버깅

### 디버깅 단축키

**실행 제어:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F5` | Start/Continue | 디버깅 시작 또는 계속 실행 |
| `Ctrl+F5` | Run Without Debugging | 디버깅 없이 실행 |
| `Shift+F5` | Stop | 디버깅 중지 |
| `Ctrl+Shift+F5` | Restart | 디버깅 재시작 |
| `F6` | Pause | 실행 일시 중지 |

**스텝 실행:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F10` | Step Over | 현재 줄 실행 (함수 내부로 들어가지 않음) |
| `F11` | Step Into | 현재 줄 실행 (함수 내부로 들어감) |
| `Shift+F11` | Step Out | 현재 함수에서 빠져나옴 |
| `Ctrl+F10` | Run to Cursor | 커서 위치까지 실행 |

**브레이크포인트:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F9` | Toggle Breakpoint | 브레이크포인트 설정/해제 |
| `Ctrl+F9` | Enable/Disable Breakpoint | 브레이크포인트 활성화/비활성화 |
| `Ctrl+Shift+F9` | Remove All Breakpoints | 모든 브레이크포인트 제거 |

**패널 및 뷰:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `Ctrl+Shift+D` | Debug View | 디버그 패널 열기 |
| `Ctrl+Shift+Y` | Debug Console | 디버그 콘솔 열기 |
| `Ctrl+K Ctrl+I` | Show Hover | 변수 정보 팝업 표시 |

**편집 (디버깅 중):**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `Ctrl+Shift+B` | Run Build Task | 빌드 태스크 실행 |
| `Ctrl+.` | Quick Fix | 빠른 수정 제안 |

### 일반 단축키

**파일 탐색:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `Ctrl+P` | Quick Open | 파일 빠른 열기 |
| `Ctrl+Shift+P` | Command Palette | 명령 팔레트 |
| `Ctrl+Shift+E` | Explorer | 탐색기 패널 |
| `Ctrl+Shift+F` | Search | 전체 검색 |
| `Ctrl+Shift+G` | Source Control | Git 패널 |

**코드 편집:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `F12` | Go to Definition | 정의로 이동 |
| `Alt+F12` | Peek Definition | 정의 미리보기 |
| `Shift+F12` | Find All References | 모든 참조 찾기 |
| `F2` | Rename Symbol | 심볼 이름 변경 |
| `Ctrl+.` | Quick Actions | 빠른 작업 (리팩터링 등) |
| `Ctrl+Space` | Trigger Suggest | IntelliSense 표시 |
| `Ctrl+Shift+Space` | Parameter Hints | 매개변수 힌트 |

**터미널:**

| 단축키 | 동작 | 설명 |
|--------|------|------|
| `` Ctrl+` `` | Toggle Terminal | 터미널 열기/닫기 |
| `` Ctrl+Shift+` `` | New Terminal | 새 터미널 생성 |
| `Ctrl+C` | Cancel | 실행 중인 명령 취소 (터미널) |

### 기본 디버깅 (F5)

1. `F5` 키를 눌러 디버깅 시작
2. `.NET Core Launch (web)` 구성이 자동 선택됨
3. `build` 태스크가 먼저 실행됨
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

특정 조건에서만 중단하도록 설정합니다.

**설정 방법:**
1. 브레이크포인트 우클릭 → `Edit Breakpoint...`
2. 조건 입력

**조건 유형:**

| 유형 | 설명 | 예시 |
|------|------|------|
| Expression | 조건이 true일 때 중단 | `user.Id == 42` |
| Hit Count | N번째 실행 시 중단 | `= 10` (10번째에 중단) |
| Log Message | 중단 없이 로그만 출력 | `User: {user.Name}` |

**Expression 예시:**
```csharp
// 특정 ID일 때만 중단
user.Id == 42

// 컬렉션이 비어있을 때
items.Count == 0

// 문자열 조건
request.Path.Contains("/api/admin")

// 예외 조건
exception?.Message.Contains("timeout")
```

### 로그포인트 (Logpoint)

코드 수정 없이 디버깅 중 로그를 출력합니다.

**설정 방법:**
1. 코드 왼쪽 여백 우클릭 → `Add Logpoint...`
2. 메시지 입력 (변수는 `{변수명}` 형식)

**예시:**
```
Processing user: {user.Name}, Role: {user.Role}
Request received: {request.Method} {request.Path}
Loop iteration: {i}, Current value: {items[i]}
```

> **팁:** 로그포인트는 빨간 점 대신 다이아몬드(◆) 모양으로 표시됩니다.

### 예외 브레이크포인트

특정 예외 발생 시 자동으로 중단합니다.

**설정 방법:**
1. `Ctrl+Shift+D` → 디버그 패널
2. `BREAKPOINTS` 섹션에서 `Add...` 클릭
3. 예외 유형 선택

**launch.json 설정:**
```json
{
    "name": ".NET Core Launch (web)",
    "type": "coreclr",
    "request": "launch",
    // 예외 설정
    "justMyCode": false,  // 외부 코드에서도 예외 캐치
    "enableStepFiltering": false
}
```

**자주 사용하는 예외:**
- `System.NullReferenceException`
- `System.ArgumentException`
- `System.InvalidOperationException`
- `System.Net.Http.HttpRequestException`

### Watch와 변수 검사

**Watch 패널 사용:**
1. 디버깅 중 `Ctrl+Shift+D` → WATCH 섹션
2. `+` 클릭하여 표현식 추가

**유용한 Watch 표현식:**
```csharp
// 컬렉션 개수
users.Count

// LINQ 표현식
users.Where(u => u.IsActive).ToList()

// 속성 체이닝
response?.Content?.Headers?.ContentType

// 형변환
((HttpRequestMessage)request).RequestUri

// 조건 표현식
user?.Name ?? "Anonymous"
```

**변수 검사 단축키:**

| 단축키 | 동작 |
|--------|------|
| 마우스 호버 | 변수 값 미리보기 |
| `Ctrl+K Ctrl+I` | 변수 정보 팝업 |
| `Ctrl+Shift+D` | 디버그 패널 열기 |

### 디버그 콘솔

디버깅 중 표현식을 실행하고 결과를 확인합니다.

**사용 방법:**
1. 디버깅 중 `Ctrl+Shift+Y` → 디버그 콘솔
2. 표현식 입력 후 Enter

**유용한 명령:**
```csharp
// 변수 값 확인
user.Name

// 메서드 호출
user.GetFullName()

// LINQ 쿼리
users.Select(u => new { u.Id, u.Name }).ToList()

// 객체 JSON 직렬화 (보기 쉽게)
System.Text.Json.JsonSerializer.Serialize(user, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })

// 컬렉션 내용 확인
string.Join(", ", items.Select(i => i.Name))
```

### Edit and Continue (Hot Reload)

디버깅 중 코드를 수정하고 계속 실행합니다.

**지원되는 변경:**
- 메서드 본문 수정
- 지역 변수 추가/수정
- 람다 표현식 수정
- 속성 값 변경

**지원되지 않는 변경:**
- 메서드 시그니처 변경
- 새 클래스/인터페이스 추가
- 제네릭 타입 변경
- async/await 추가

**launch.json 설정:**
```json
{
    "name": ".NET Core Launch (web)",
    "type": "coreclr",
    "request": "launch",
    // Hot Reload 활성화
    "env": {
        "DOTNET_WATCH_RESTART_ON_RUDE_EDIT": "true"
    }
}
```

### 호출 스택 탐색

함수 호출 경로를 추적하여 문제 원인을 파악합니다.

**사용 방법:**
1. 브레이크포인트에서 중단
2. `CALL STACK` 패널 확인
3. 스택 프레임 클릭하여 해당 위치로 이동

**호출 스택 필터링:**
```json
{
    "justMyCode": true,  // 내 코드만 표시
    "enableStepFiltering": true,  // 시스템 코드 건너뛰기
    "symbolOptions": {
        "searchMicrosoftSymbolServer": true  // MS 심볼 서버 검색
    }
}
```

### 원격/컨테이너 디버깅

Docker 컨테이너에서 실행 중인 애플리케이션을 디버깅합니다.

**Dockerfile 설정:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# vsdbg 디버거 설치
RUN apt-get update && apt-get install -y curl procps
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg
```

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

### 프로세스 연결 (Attach)

실행 중인 프로세스에 연결:

1. `Ctrl+Shift+D` → 디버그 패널 열기
2. `.NET Core Attach` 선택
3. 연결할 프로세스 선택

### 동시에 여러 프로젝트 실행

여러 프로젝트를 동시에 실행하려면 `compounds` 구성을 사용합니다.

**launch.json 예시:**

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "API Server",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-api",
            "program": "${workspaceFolder}/Src/Api/bin/Debug/net10.0/Api.dll",
            "cwd": "${workspaceFolder}/Src/Api",
            "console": "integratedTerminal",
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development",
                "ASPNETCORE_URLS": "http://localhost:5000"
            }
        },
        {
            "name": "Worker Service",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-worker",
            "program": "${workspaceFolder}/Src/Worker/bin/Debug/net10.0/Worker.dll",
            "cwd": "${workspaceFolder}/Src/Worker",
            "console": "integratedTerminal",
            "env": {
                "DOTNET_ENVIRONMENT": "Development"
            }
        }
    ],
    "compounds": [
        {
            "name": "API + Worker",
            "configurations": ["API Server", "Worker Service"],
            "stopAll": true
        }
    ]
}
```

**compounds 속성:**

| 속성 | 설명 |
|------|------|
| `name` | 복합 구성 이름 (F5 시 선택 가능) |
| `configurations` | 동시에 실행할 구성 이름 배열 |
| `stopAll` | 하나가 종료되면 모두 종료 (`true` 권장) |

**실행 방법:**

1. `F5` 또는 `Ctrl+Shift+D` → 디버그 패널 열기
2. 드롭다운에서 `API + Worker` 선택
3. `F5`로 동시 실행

### Ctrl+C 입력 받기 (Graceful Shutdown)

애플리케이션이 `Ctrl+C` 신호를 받아 정상 종료하려면 `console` 설정이 중요합니다.

**console 옵션:**

| 옵션 | Ctrl+C 지원 | 설명 |
|------|-------------|------|
| `integratedTerminal` | O | 통합 터미널에서 실행, Ctrl+C 전달됨 |
| `externalTerminal` | O | 외부 터미널 창에서 실행 |
| `internalConsole` | X | 디버그 콘솔 (Ctrl+C 미지원) |

**권장 설정:**

```json
{
    "name": ".NET Core Launch (web)",
    "type": "coreclr",
    "request": "launch",
    "console": "integratedTerminal",  // Ctrl+C 지원
    // ...
}
```

**Graceful Shutdown 테스트:**

```csharp
// Program.cs에서 Ctrl+C 처리 확인
var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("애플리케이션 종료 중...");
});

app.Run();
```

> **참고:** `internalConsole`을 사용하면 `Ctrl+C`가 전달되지 않아 애플리케이션이 강제 종료됩니다. 리소스 정리가 필요한 경우 반드시 `integratedTerminal`을 사용하세요.

<br/>

## 태스크

### build

프로젝트를 빌드합니다.

```bash
# 수동 실행
Ctrl+Shift+B

# 또는
Ctrl+Shift+P → "Tasks: Run Task" → "build"
```

### publish

배포용으로 빌드합니다.

```bash
Ctrl+Shift+P → "Tasks: Run Task" → "publish"
```

### watch

Hot Reload 모드로 실행합니다. 코드 변경 시 자동으로 재빌드하고 재시작합니다.

```bash
Ctrl+Shift+P → "Tasks: Run Task" → "watch"
```

**특징:**
- 파일 저장 시 자동 재빌드
- 변경 감지 후 애플리케이션 재시작
- 개발 중 빠른 피드백 제공

<br/>

## 트러블슈팅

### 디버깅이 시작되지 않을 때

**증상:** F5를 눌러도 디버깅이 시작되지 않음

**원인:** C# 확장이 설치되지 않았거나 활성화되지 않음

**해결:**
```bash
# 1. 확장 설치 확인
Ctrl+Shift+X → "C# Dev Kit" 검색 → 설치

# 2. VSCode 재시작
```

### preLaunchTask 'build' 오류

**증상:** `preLaunchTask 'build' could not be found`

**원인:** tasks.json 파일이 없거나 build 태스크가 정의되지 않음

**해결:**
```bash
# tasks.json 파일 확인
.vscode/tasks.json

# build 태스크가 정의되어 있는지 확인
```

### 빌드 실패

**증상:** 빌드 태스크가 실패함

**원인:** 프로젝트 경로가 잘못되었거나 .NET SDK가 설치되지 않음

**해결:**
```bash
# 1. .NET SDK 설치 확인
dotnet --version

# 2. 프로젝트 경로 확인
# tasks.json의 args에서 경로가 올바른지 확인

# 3. 터미널에서 직접 빌드 시도
dotnet build Tutorials/Observability/Src/Observability/Observability.csproj
```

### Hot Reload가 작동하지 않을 때

**증상:** watch 태스크 실행 중 코드 변경이 반영되지 않음

**원인:** 일부 변경사항은 Hot Reload를 지원하지 않음

**해결:**
```bash
# watch 태스크 중지 (Ctrl+C) 후 재시작
Ctrl+Shift+P → "Tasks: Run Task" → "watch"
```

> **참고:** 메서드 시그니처 변경, 새 클래스 추가 등은 Hot Reload를 지원하지 않습니다.

<br/>

## FAQ

### Q1. launch.json과 tasks.json의 차이점은 무엇인가요?

| 파일 | 용도 | 실행 방법 |
|------|------|----------|
| `launch.json` | 디버깅 구성 정의 | F5 |
| `tasks.json` | 빌드/실행 태스크 정의 | Ctrl+Shift+B 또는 Run Task |

`launch.json`의 `preLaunchTask`가 `tasks.json`의 태스크를 참조합니다.

### Q2. 다른 프로젝트를 디버깅하려면 어떻게 하나요?

`launch.json`에 새 구성을 추가하세요:

```json
{
    "configurations": [
        // 기존 구성...
        {
            "name": "다른 프로젝트",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build-other",
            "program": "${workspaceFolder}/path/to/Other.dll",
            "cwd": "${workspaceFolder}/path/to"
        }
    ]
}
```

### Q3. 환경 변수를 추가하려면 어떻게 하나요?

`launch.json`의 `env` 섹션에 추가하세요:

```json
{
    "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "MY_CUSTOM_VAR": "value"
    }
}
```

### Q4. 브라우저를 자동으로 열고 싶습니다.

`launch.json`에서 `serverReadyAction` 주석을 해제하세요:

```json
{
    "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
    }
}
```

### Q5. 태스크를 키보드 단축키로 실행할 수 있나요?

`keybindings.json`에 단축키를 추가하세요:

```json
{
    "key": "ctrl+alt+w",
    "command": "workbench.action.tasks.runTask",
    "args": "watch"
}
```

### Q6. settings.json은 언제 사용하나요?

프로젝트별로 다른 VSCode 설정이 필요할 때 사용합니다:

```json
{
    // 저장 시 자동 포맷
    "editor.formatOnSave": true,

    // 기본 솔루션 파일 지정
    "dotnet.defaultSolution": "Functorium.slnx",

    // 특정 파일 제외
    "files.exclude": {
        "**/bin": true,
        "**/obj": true
    }
}
```

### Q7. 여러 프로젝트를 동시에 실행하려면 어떻게 하나요?

`launch.json`에 `compounds` 구성을 추가하세요:

```json
{
    "configurations": [
        { "name": "Project A", ... },
        { "name": "Project B", ... }
    ],
    "compounds": [
        {
            "name": "A + B 동시 실행",
            "configurations": ["Project A", "Project B"],
            "stopAll": true
        }
    ]
}
```

F5를 누르고 드롭다운에서 `A + B 동시 실행`을 선택하면 두 프로젝트가 동시에 시작됩니다.

### Q8. Ctrl+C로 애플리케이션이 종료되지 않습니다.

`console` 설정을 확인하세요:

```json
{
    "console": "integratedTerminal"  // ✓ Ctrl+C 지원
    // "console": "internalConsole"  // ✗ Ctrl+C 미지원
}
```

`internalConsole`은 디버그 콘솔을 사용하며 `Ctrl+C` 신호가 전달되지 않습니다. `integratedTerminal`로 변경하세요.

### Q9. 동시 실행 시 각 프로젝트의 포트가 충돌합니다.

각 구성의 `env`에서 다른 포트를 지정하세요:

```json
{
    "configurations": [
        {
            "name": "API Server",
            "env": {
                "ASPNETCORE_URLS": "http://localhost:5000"
            }
        },
        {
            "name": "Admin Server",
            "env": {
                "ASPNETCORE_URLS": "http://localhost:5001"
            }
        }
    ]
}
```

<br/>

## 참고 문서

- [VSCode Debugging](https://code.visualstudio.com/docs/editor/debugging)
- [VSCode Tasks](https://code.visualstudio.com/docs/editor/tasks)
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
