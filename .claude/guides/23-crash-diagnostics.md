# 크래시 덤프 핸들러 가이드

프로덕션 환경의 크래시는 로그와 메트릭만으로 원인을 파악하기 어려운 경우가 있습니다. 특히 `AccessViolationException`이나 `StackOverflowException`처럼 `try-catch`로 잡을 수 없는 예외는 일반적인 Observability 도구로는 진단이 불가능합니다.
크래시 덤프는 프로세스 종료 시점의 메모리 스냅샷으로, 스택 트레이스와 힙 상태 등을 사후에 분석할 수 있게 해주는 최후 수단의 진단 도구입니다.

이 가이드는 `Functorium.Abstractions.Diagnostics.CrashDumpHandler`를 사용하여 .NET 애플리케이션의 크래시 덤프를 생성하고 분석하는 방법을 설명합니다.

## CrashDumpHandler 개요

`CrashDumpHandler`는 `AccessViolationException`과 같은 **Corrupted State Exception(CSE)** 을 처리합니다. CSE는 `try-catch`로 잡을 수 없으며, `AppDomain.UnhandledException` 이벤트를 통해 프로세스 종료 직전에 덤프를 생성합니다.

| 예외 유형 | try-catch 가능 | CrashDumpHandler 처리 |
|-----------|:--------------:|:---------------------:|
| 일반 예외 (Exception) | O | O |
| AccessViolationException | X | O |
| StackOverflowException | X | O |
| ExecutionEngineException | X | O |

### 소스 위치

```
Src/Functorium/Abstractions/Diagnostics/CrashDumpHandler.cs
```

## Program.cs 설정

`CrashDumpHandler.Initialize()`는 **반드시 `Program.cs`의 첫 번째 줄**에서 호출해야 합니다.

```csharp
using Functorium.Abstractions.Diagnostics;

// 가장 먼저 초기화 (다른 코드보다 앞에)
CrashDumpHandler.Initialize();

var builder = WebApplication.CreateBuilder(args);
// ... 나머지 코드
```

### 커스텀 경로 지정

```csharp
// 명시적 경로 지정 (Linux/macOS)
CrashDumpHandler.Initialize("/var/log/myapp/dumps");

// 명시적 경로 지정 (Windows)
CrashDumpHandler.Initialize(@"C:\Logs\MyApp\Dumps");

// 환경 변수 사용
var dumpDir = Environment.GetEnvironmentVariable("CRASH_DUMP_DIR");
CrashDumpHandler.Initialize(dumpDir);
```

### 기본 덤프 경로

`dumpDirectory` 파라미터를 생략하면 `{LocalApplicationData}/{EntryAssemblyName}/CrashDumps`를 사용합니다.

| 플랫폼 | 기본 경로 예시 |
|--------|---------------|
| Windows | `%LOCALAPPDATA%\MyApp\CrashDumps\` |
| Linux | `~/.local/share/MyApp/CrashDumps/` |
| macOS | `~/Library/Application Support/MyApp/CrashDumps/` |

### DumpDirectory 프로퍼티

초기화 후 `CrashDumpHandler.DumpDirectory`로 경로를 조회할 수 있습니다.

```csharp
CrashDumpHandler.Initialize();
Console.WriteLine(CrashDumpHandler.DumpDirectory);
```

## 생성되는 파일

### 미니 덤프 파일 (`.dmp`)

```
crash_AccessViolationException_20240115_143052.dmp
```

- 파일명 형식: `crash_{예외타입}_{날짜시간}.dmp`
- Windows: `MiniDumpWriteDump` (Full Memory)
- Linux/macOS: `createdump` 도구 사용

### 예외 정보 파일 (`.txt`)

```
crash_info_20240115_143052.txt
```

프로세스 정보, 예외 상세, 스택 트레이스, Inner Exception을 텍스트로 저장합니다.

## 프로덕션 배포

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0

RUN mkdir -p /app/dumps && chmod 777 /app/dumps
VOLUME ["/app/dumps"]
ENV CRASH_DUMP_DIR=/app/dumps

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

```yaml
# docker-compose.yml
services:
  api:
    environment:
      - CRASH_DUMP_DIR=/app/dumps
    volumes:
      - crash-dumps:/app/dumps
    cap_add:
      - SYS_PTRACE  # createdump에 필요
```

### Kubernetes

```yaml
spec:
  containers:
  - name: myapp
    env:
    - name: CRASH_DUMP_DIR
      value: /dumps
    volumeMounts:
    - name: dump-volume
      mountPath: /dumps
    securityContext:
      capabilities:
        add: ["SYS_PTRACE"]
  volumes:
  - name: dump-volume
    persistentVolumeClaim:
      claimName: dump-pvc
```

### Windows 서비스 (WER)

```powershell
# Windows Error Reporting 자동 덤프 설정
$werKey = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\MyApp.exe"
New-Item -Path $werKey -Force
Set-ItemProperty -Path $werKey -Name "DumpFolder" -Value "C:\Dumps\MyApp"
Set-ItemProperty -Path $werKey -Name "DumpType" -Value 2  # Full dump
Set-ItemProperty -Path $werKey -Name "DumpCount" -Value 10
```

## 덤프 분석 도구

| 도구 | 플랫폼 | 용도 |
|------|--------|------|
| Visual Studio | Windows | GUI 기반 분석, .NET 네이티브 지원 |
| WinDbg | Windows | 고급 디버깅, 스크립트 지원 |
| dotnet-dump | Cross-platform | CLI 기반, 컨테이너 환경에 적합 |
| lldb | Linux/macOS | 네이티브 디버깅 |

### dotnet-dump 주요 명령어

```bash
# 설치
dotnet tool install -g dotnet-dump

# 분석 시작
dotnet-dump analyze crash.dmp

# 주요 명령어
> clrstack          # 현재 스레드 스택 트레이스
> clrstack -all     # 모든 스레드 스택 트레이스
> pe                # 예외 정보 확인
> dumpheap -stat    # 힙 통계
> dumpobj <addr>    # 특정 객체 덤프
> gcroot <addr>     # GC 루트 찾기
> threads           # 스레드 목록
> syncblk           # 동기화 블록 (데드락 분석)
```

### Visual Studio 분석

1. `File > Open > File` 에서 `.dmp` 파일 열기
2. "Debug with Managed Only" 클릭
3. Call Stack 창에서 예외 발생 위치 확인
4. Locals/Watch 창에서 변수 값 확인

### 심볼(PDB) 관리

| 분석 수준 | PDB 필요 |
|-----------|:--------:|
| 기본 스택 트레이스 (메서드명만) | X |
| 메서드명 + 라인 번호 | O |
| 소스 코드 보며 디버깅 | O + 소스 |
| 변수/파라미터 값 | O |
| 힙/메모리 분석 | X |

Source Link 사용을 권장합니다:

```xml
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <DebugType>embedded</DebugType>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
</ItemGroup>
```

## 트러블슈팅

### 덤프 파일이 생성되지 않음

| 원인 | 해결 |
|------|------|
| 권한 부족 | `chmod 755 /var/log/myapp/dumps` |
| 디스크 공간 부족 | 오래된 덤프 정리: `find ... -mtime +7 -delete` |
| 핸들러 초기화 전 크래시 | `Program.cs` 첫 줄에서 `Initialize()` 호출 |

### 덤프 파일을 열 수 없음

| 원인 | 해결 |
|------|------|
| 비트니스 불일치 | 64비트 덤프는 64비트 디버거 사용 |
| 심볼 파일 없음 | `dotnet publish -c Release -p:DebugType=full` |

### 컨테이너에서 덤프 생성 실패

Docker: `cap_add: SYS_PTRACE` + `security_opt: seccomp:unconfined`
Kubernetes: `securityContext.capabilities.add: ["SYS_PTRACE"]`

## Observability와의 관계

Observability(Logging, Metrics, Tracing)는 **실행 중인** 프로세스의 동작을 관찰하는 도구입니다. 크래시 덤프는 프로세스가 **비정상 종료된 후**의 사후 분석 도구로, 성격이 다릅니다.

문제 진단 시 권장하는 순서:

1. **Logging** -- 구조화된 로그로 오류 코드와 컨텍스트 확인
2. **Metrics** -- 에러율, 응답 시간 등 추이 변화 확인
3. **Tracing** -- 분산 요청 흐름에서 병목/실패 지점 추적
4. **크래시 덤프** -- 위 도구로 해결할 수 없는 프로세스 크래시 분석 (최후 수단)

> Observability 사양은 [18-observability-spec.md](./18-observability-spec.md)을 참조하세요.

## 참고

- 원본 상세 문서: `Tests.Hosts/01-SingleHost/CRASH-DUMP.md`
- [Microsoft: Collect and analyze memory dumps](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dumps)
- [dotnet-dump 공식 문서](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)
