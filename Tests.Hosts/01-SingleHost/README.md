# SingleHost 크래시 덤프 가이드

이 문서는 .NET 애플리케이션에서 `AccessViolationException`과 같은 CLR 수준의 치명적 예외를 처리하고, 크래시 덤프를 생성하여 원인을 분석하는 방법을 설명합니다.

## 목차

1. [AccessViolationException 이해하기](#1-accessviolationexception-이해하기)
2. [생성되는 덤프 파일 정보](#2-생성되는-덤프-파일-정보)
3. [프로덕션 배포 방법](#3-프로덕션-배포-방법)
4. [덤프 파일 원인 분석](#4-덤프-파일-원인-분석)
5. [실전 사례 분석](#5-실전-사례-분석)
6. [트러블슈팅](#6-트러블슈팅)

---

## 1. AccessViolationException 이해하기

### 1.1 CSE(Corrupted State Exception)란?

`AccessViolationException`은 **Corrupted State Exception(CSE)** 의 일종으로, 프로세스 상태가 손상되었을 때 발생합니다.

| 예외 유형 | 설명 | try-catch 가능 |
|-----------|------|:--------------:|
| 일반 예외 (Exception) | 애플리케이션 레벨 오류 | ✅ |
| AccessViolationException | 잘못된 메모리 접근 | ❌ |
| StackOverflowException | 스택 오버플로우 | ❌ |
| ExecutionEngineException | CLR 내부 오류 | ❌ |

### 1.2 .NET Framework vs .NET Core/.NET 5+

```csharp
// .NET Framework에서는 이 특성으로 CSE를 catch할 수 있었음
[HandleProcessCorruptedStateExceptions]
[SecurityCritical]
public void HandleCSE()
{
    try
    {
        // CSE 발생 코드
    }
    catch (AccessViolationException ex)
    {
        // .NET Framework에서만 동작
    }
}
```

**.NET Core/.NET 5+ 에서는 이 방식이 작동하지 않습니다.** 대신 `AppDomain.UnhandledException` 이벤트를 사용하여 프로세스 종료 직전에 덤프를 생성해야 합니다.

### 1.3 왜 catch할 수 없는가?

CSE가 발생하면 프로세스의 메모리 상태가 이미 손상되었을 가능성이 높습니다. 손상된 상태에서 코드를 계속 실행하면:
- 데이터 손상
- 보안 취약점
- 예측 불가능한 동작

이러한 이유로 .NET Core/.NET 5+는 CSE 발생 시 프로세스를 즉시 종료합니다.

---

## 2. 생성되는 덤프 파일 정보

### 2.1 덤프 파일 저장 위치

| 플랫폼 | 기본 경로 |
|--------|-----------|
| Windows | `%LOCALAPPDATA%\LayeredArch\CrashDumps\` |
| Linux | `~/.local/share/LayeredArch/CrashDumps/` |
| macOS | `~/Library/Application Support/LayeredArch/CrashDumps/` |

### 2.2 생성되는 파일 종류

#### 미니 덤프 파일 (`.dmp`)

```
crash_AccessViolationException_20240115_143052.dmp
```

| 항목 | 설명 |
|------|------|
| 파일명 형식 | `crash_{예외타입}_{날짜시간}.dmp` |
| 크기 | 수 MB ~ 수백 MB (Full Memory 덤프 기준) |
| 내용 | 프로세스 메모리, 스레드 정보, 스택 트레이스 |

#### 덤프 타입별 크기

```csharp
// CrashDumpHandler.cs에서 설정
MiniDumpType.MiniDumpWithFullMemory  // 전체 메모리 (가장 큼, 가장 상세)
MiniDumpType.MiniDumpNormal          // 최소 정보 (가장 작음)
MiniDumpType.MiniDumpWithDataSegs    // 데이터 세그먼트 포함
```

#### 예외 정보 파일 (`.txt`)

```
crash_info_20240115_143052.txt
```

```
================================================================================
Crash Report
================================================================================
Time: 2024-01-15 14:30:52.123
Process: LayeredArch (PID: 12345)
Machine: PROD-SERVER-01
OS: Microsoft Windows 10.0.19045
.NET Version: 10.0.0
Working Directory: C:\app

================================================================================
Exception Details
================================================================================
Type: System.AccessViolationException
Message: Attempted to read or write protected memory.
Source: LayeredArch
TargetSite: Void TriggerAccessViolation()

================================================================================
Stack Trace
================================================================================
   at LayeredArch.CrashDiagnostics.AccessViolationDemo.TriggerAccessViolation()
   at LayeredArch.CrashDiagnostics.CrashDiagnosticsEndpoints.<>c.<TriggerAccessViolation>b__1_0()
   ...

================================================================================
Inner Exception
================================================================================
None
================================================================================
```

---

## 3. 프로덕션 배포 방법

### 3.1 코드 배포

#### Step 1: CrashDumpHandler 파일 복사

```
CrashDiagnostics/
├── AccessViolationDemo.cs      # 프로덕션에서는 제외 가능
├── CrashDumpHandler.cs         # 필수
└── CrashDiagnosticsEndpoints.cs # 프로덕션에서는 제외 권장
```

#### Step 2: Program.cs 수정

```csharp
using YourApp.CrashDiagnostics;

// 가장 먼저 초기화 (다른 코드보다 앞에)
CrashDumpHandler.Initialize("/var/log/yourapp/dumps");

var builder = WebApplication.CreateBuilder(args);
// ... 나머지 코드
```

#### Step 3: 프로덕션 전용 설정

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    // 진단 엔드포인트는 개발 환경에서만 노출
    app.MapCrashDiagnosticsEndpoints();
}
```

### 3.2 프로덕션 덤프 경로 설정

#### appsettings.Production.json

```json
{
  "CrashDump": {
    "Directory": "/var/log/myapp/dumps",
    "DumpType": "MiniDumpWithFullMemory",
    "Enabled": true
  }
}
```

#### 환경 변수 사용

```bash
# Linux
export CRASH_DUMP_DIR=/var/log/myapp/dumps

# Windows
set CRASH_DUMP_DIR=C:\Logs\MyApp\Dumps
```

```csharp
var dumpDir = Environment.GetEnvironmentVariable("CRASH_DUMP_DIR")
    ?? "/var/log/myapp/dumps";
CrashDumpHandler.Initialize(dumpDir);
```

### 3.3 Docker 환경 배포

#### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# 덤프 디렉터리 생성 및 권한 설정
RUN mkdir -p /app/dumps && chmod 777 /app/dumps

# 덤프 생성을 위한 도구 설치 (선택사항)
RUN apt-get update && apt-get install -y --no-install-recommends \
    procps \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# 덤프 디렉터리를 볼륨으로 노출
VOLUME ["/app/dumps"]

ENV CRASH_DUMP_DIR=/app/dumps

ENTRYPOINT ["dotnet", "LayeredArch.dll"]
```

#### docker-compose.yml

```yaml
version: '3.8'
services:
  api:
    image: myapp:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - CRASH_DUMP_DIR=/app/dumps
    volumes:
      - crash-dumps:/app/dumps
    deploy:
      resources:
        limits:
          memory: 512M

volumes:
  crash-dumps:
    driver: local
```

### 3.4 Kubernetes 환경 배포

#### deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        env:
        - name: CRASH_DUMP_DIR
          value: /dumps
        volumeMounts:
        - name: dump-volume
          mountPath: /dumps
      volumes:
      - name: dump-volume
        persistentVolumeClaim:
          claimName: dump-pvc
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: dump-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
```

### 3.5 Windows 서비스 배포

#### Windows Error Reporting (WER) 설정

레지스트리를 통해 자동 덤프 생성을 설정할 수 있습니다:

```powershell
# 관리자 권한으로 실행
$dumpPath = "C:\Dumps\MyApp"
New-Item -Path $dumpPath -ItemType Directory -Force

# WER 설정
$werKey = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\LayeredArch.exe"
New-Item -Path $werKey -Force
Set-ItemProperty -Path $werKey -Name "DumpFolder" -Value $dumpPath
Set-ItemProperty -Path $werKey -Name "DumpType" -Value 2  # Full dump
Set-ItemProperty -Path $werKey -Name "DumpCount" -Value 10
```

---

## 4. 덤프 파일 원인 분석

### 4.1 분석 도구

| 도구 | 플랫폼 | 용도 |
|------|--------|------|
| Visual Studio | Windows | GUI 기반 분석, .NET 네이티브 지원 |
| WinDbg | Windows | 고급 디버깅, 스크립트 지원 |
| dotnet-dump | Cross-platform | CLI 기반, 컨테이너 환경에 적합 |
| lldb | Linux/macOS | 네이티브 디버깅 |

### 4.2 심볼(PDB)과 소스 코드 요구사항

#### 분석 수준별 필요 조건

| 분석 수준 | PDB 파일 | 소스 코드 | 빌드 경로 일치 |
|-----------|:--------:|:---------:|:--------------:|
| 기본 스택 트레이스 | ❌ | ❌ | ❌ |
| 메서드명 + 라인 번호 | ✅ | ❌ | ❌ |
| 소스 코드 보며 디버깅 | ✅ | ✅ | ⚠️ 매핑 가능 |
| 변수/파라미터 값 확인 | ✅ | ❌ | ❌ |
| 힙/메모리 분석 | ❌ | ❌ | ❌ |

#### PDB 파일 없이도 가능한 분석

```bash
# dotnet-dump는 PDB 없이도 기본 정보 제공
$ dotnet-dump analyze crash.dmp

> clrstack
OS Thread Id: 0x1234
        Child SP               IP Call Site
00007FFE12340000 00007FFE12345678 LayeredArch.CrashDiagnostics.AccessViolationDemo.TriggerAccessViolation()
# ↑ 메서드명은 보이지만 라인 번호는 없음
```

#### PDB 파일이 있으면 추가되는 정보

```bash
> clrstack
OS Thread Id: 0x1234
        Child SP               IP Call Site
00007FFE12340000 00007FFE12345678 LayeredArch.CrashDiagnostics.AccessViolationDemo.TriggerAccessViolation() [C:\build\src\AccessViolationDemo.cs @ 22]
# ↑ 소스 파일 경로와 라인 번호가 추가됨
```

#### 빌드 경로가 다를 때 소스 매핑

PDB에는 빌드 시점의 소스 경로가 저장됩니다. 분석 환경에서 경로가 다르면 매핑이 필요합니다.

**Visual Studio에서 소스 경로 매핑:**

```
Debug → Options → Debugging → General → "Enable source server support" 체크

Debug → Options → Debugging → Symbols →
  "Symbol file (.pdb) locations"에 PDB 경로 추가
```

**소스 서버 없이 수동 매핑:**

```
1. 덤프 열기
2. 스택에서 메서드 더블클릭
3. "Browse and find source" 선택
4. 로컬 소스 파일 선택
5. "Look in these paths" 설정으로 전체 프로젝트 매핑
```

**WinDbg에서 소스 경로 매핑:**

```
// 소스 경로 설정
.srcpath+ C:\MyLocalSource

// 빌드 경로를 로컬 경로로 매핑
.srcfix
```

#### Source Link 사용 (권장)

Source Link를 사용하면 PDB에 GitHub/Azure DevOps URL이 포함되어 자동으로 소스를 다운로드합니다.

```xml
<!-- .csproj -->
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <DebugType>embedded</DebugType>
</PropertyGroup>

<ItemGroup>
  <!-- GitHub용 -->
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>

  <!-- Azure DevOps용 -->
  <!-- <PackageReference Include="Microsoft.SourceLink.AzureRepos.Git" Version="8.0.0" PrivateAssets="All"/> -->
</ItemGroup>
```

**Source Link의 장점:**
- 빌드 경로 무관
- 소스 코드 배포 불필요
- 정확한 커밋의 소스 자동 매칭
- Visual Studio에서 자동 다운로드

#### 프로덕션 배포 시 심볼 관리

```bash
# 배포 구조 예시
/app/
├── LayeredArch.dll          # 실행 파일
├── LayeredArch.pdb          # 심볼 파일 (선택적 배포)
└── ...

/symbols/                     # 별도 심볼 서버
├── LayeredArch.pdb/
│   └── <GUID>/
│       └── LayeredArch.pdb
└── ...
```

**옵션 1: PDB 함께 배포**
```bash
dotnet publish -c Release
# bin/Release/net10.0/publish/ 에 dll과 pdb 모두 포함
```

**옵션 2: PDB 별도 보관 (권장)**
```bash
# 빌드 후 PDB를 심볼 서버로 이동
dotnet publish -c Release -o ./publish
mv ./publish/*.pdb /symbols/archive/$(git rev-parse HEAD)/
```

**옵션 3: Embedded PDB (단일 파일)**
```xml
<PropertyGroup>
  <DebugType>embedded</DebugType>
</PropertyGroup>
```

### 4.3 Visual Studio로 분석

#### Step 1: 덤프 파일 열기

```
File → Open → File → crash_AccessViolationException_20240115_143052.dmp
```

#### Step 2: 디버깅 시작

"Debug with Managed Only" 또는 "Debug with Mixed" 클릭

#### Step 3: 콜 스택 확인

```
Call Stack 창에서 예외 발생 위치 확인:

LayeredArch.dll!LayeredArch.CrashDiagnostics.AccessViolationDemo.TriggerAccessViolation() Line 22
LayeredArch.dll!LayeredArch.CrashDiagnostics.CrashDiagnosticsEndpoints.<>c.<TriggerAccessViolation>b__1_0() Line 52
System.Private.CoreLib.dll!System.Threading.Tasks.Task.InnerInvoke()
...
```

#### Step 4: 변수 값 확인

Locals, Watch 창에서 예외 발생 시점의 변수 값 확인

### 4.4 dotnet-dump로 분석 (Cross-platform)

#### 설치

```bash
dotnet tool install -g dotnet-dump
```

#### 덤프 분석

```bash
# 덤프 파일 분석 시작
dotnet-dump analyze crash_AccessViolationException_20240115_143052.dmp
```

#### 주요 명령어

```bash
# 모든 스레드의 스택 트레이스
> clrstack -all

# 현재 스레드의 스택 트레이스
> clrstack

# 예외 정보 확인
> pe

# 모든 예외 객체 확인
> dumpheap -type Exception

# 특정 객체 덤프
> dumpobj <address>

# 힙 통계
> dumpheap -stat

# GC 루트 찾기
> gcroot <address>

# 스레드 목록
> threads

# 종료
> exit
```

#### 분석 예시

```bash
$ dotnet-dump analyze crash.dmp

> pe
Exception object: 00007f8a12345678
Exception type:   System.AccessViolationException
Message:          Attempted to read or write protected memory.
InnerException:   <none>
StackTrace (generated):
    SP               IP               Function
    00007FFE12340000 00007FFE12345678 LayeredArch.CrashDiagnostics.AccessViolationDemo.TriggerAccessViolation()

> clrstack
OS Thread Id: 0x1234 (0)
        Child SP               IP Call Site
00007FFE12340000 00007FFE12345678 LayeredArch.CrashDiagnostics.AccessViolationDemo.TriggerAccessViolation()
00007FFE12340100 00007FFE12345700 LayeredArch.CrashDiagnostics.CrashDiagnosticsEndpoints+<>c.<TriggerAccessViolation>b__1_0()
...
```

### 4.5 WinDbg로 분석 (고급)

#### 설치

Microsoft Store에서 "WinDbg Preview" 설치 또는 Windows SDK 설치

#### 심볼 서버 설정

```
.sympath srv*C:\Symbols*https://msdl.microsoft.com/download/symbols
.reload
```

#### 주요 명령어

```
// SOS 확장 로드 (.NET 분석용)
.loadby sos coreclr

// 또는 .NET Core용
.load C:\path\to\sos.dll

// 모든 스레드 스택 트레이스
~*e !clrstack

// 예외 분석
!pe

// 마지막 예외 분석
!analyze -v

// 힙 분석
!dumpheap -stat

// 특정 타입 인스턴스 찾기
!dumpheap -type System.AccessViolationException

// 객체 상세 정보
!dumpobj <address>

// 메모리 읽기
dd <address>
```

### 4.6 분석 체크리스트

#### 1. 기본 정보 수집

```
[ ] 예외 타입 확인
[ ] 예외 메시지 확인
[ ] 발생 시간 확인
[ ] 영향받은 스레드 확인
```

#### 2. 스택 트레이스 분석

```
[ ] 예외 발생 메서드 확인
[ ] 호출 체인 추적
[ ] 외부 라이브러리 관련 여부 확인
[ ] 비동기 컨텍스트 확인
```

#### 3. 메모리 상태 분석

```
[ ] 힙 메모리 사용량 확인
[ ] Large Object Heap(LOH) 상태 확인
[ ] 메모리 누수 징후 확인
[ ] GC 세대별 객체 분포 확인
```

#### 4. 스레드 상태 분석

```
[ ] 스레드 수 확인
[ ] 데드락 여부 확인
[ ] 스레드 풀 고갈 여부 확인
[ ] 동기화 객체 상태 확인
```

### 4.7 일반적인 원인과 해결책

| 원인 | 증상 | 해결책 |
|------|------|--------|
| 네이티브 라이브러리 버그 | 특정 P/Invoke 호출 후 크래시 | 라이브러리 업데이트, 호출 방식 검토 |
| 버퍼 오버플로우 | Marshal 관련 코드에서 발생 | 버퍼 크기 검증, SafeHandle 사용 |
| 멀티스레드 경합 | 간헐적 크래시, 재현 어려움 | 동기화 메커니즘 추가, lock 검토 |
| 메모리 손상 | 무작위 위치에서 발생 | 메모리 분석, 네이티브 코드 검토 |
| 스택 오버플로우 | 재귀 호출에서 발생 | 재귀 깊이 제한, 반복문으로 변환 |

---

## 5. 실전 사례 분석

### 5.1 사례 1: AccessViolationException 분석

#### 상황

프로덕션 서버에서 간헐적으로 API 서버가 크래시됩니다. 로그에는 아무 정보가 없고, 덤프 파일만 생성되었습니다.

```
crash_AccessViolationException_20240115_143052.dmp
crash_info_20240115_143052.txt
```

#### Step 1: crash_info 텍스트 파일 확인

```
================================================================================
Exception Details
================================================================================
Type: System.AccessViolationException
Message: Attempted to read or write protected memory. This is often an indication
         that other memory is corrupt.

================================================================================
Stack Trace
================================================================================
   at MyApp.Services.ImageProcessor.ResizeImage(Byte[] imageData, Int32 width, Int32 height)
   at MyApp.Controllers.ImageController.Upload(IFormFile file)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor...
```

**초기 분석**: `ImageProcessor.ResizeImage`에서 문제 발생. 이미지 처리 라이브러리 의심.

#### Step 2: dotnet-dump로 상세 분석

```bash
$ dotnet-dump analyze crash_AccessViolationException_20240115_143052.dmp

# 예외 정보 확인
> pe
Exception object: 00007f8a12345678
Exception type:   System.AccessViolationException
Message:          Attempted to read or write protected memory.

# 스택 트레이스 확인
> clrstack
OS Thread Id: 0x1a2b (5)
        Child SP               IP Call Site
00007FFE12340000 00007FFE12345678 MyApp.Services.ImageProcessor.ResizeImage(Byte[], Int32, Int32)
00007FFE12340100 00007FFE12345800 MyApp.Controllers.ImageController.Upload(IFormFile)

# 해당 스레드의 로컬 변수 확인
> clrstack -l
OS Thread Id: 0x1a2b (5)
        Child SP               IP Call Site
00007FFE12340000 00007FFE12345678 MyApp.Services.ImageProcessor.ResizeImage(Byte[], Int32, Int32)
    LOCALS:
        0x00007FFE12340008 = 0x00007f8a11111111  # imageData
        0x00007FFE12340010 = 0x00000000000007d0  # width = 2000
        0x00007FFE12340018 = 0x00000000000005dc  # height = 1500

# imageData 배열 확인
> dumpobj 0x00007f8a11111111
Name:        System.Byte[]
Size:        52428824(0x3200008) bytes  # 50MB 이미지!
Array:       Rank 1, Number of elements 52428800
```

#### Step 3: 원인 파악

```bash
# 힙에서 대용량 객체 확인
> dumpheap -stat -min 10000000
              MT    Count    TotalSize Class Name
00007f8a00001234        3    157286472 System.Byte[]

# 메모리 사용량 확인
> eeheap -gc
GC Heap Size:    Size: 0x2f000000 (788529152) bytes  # 750MB+
```

**근본 원인**:
- 50MB 이상의 대용량 이미지 업로드 시 네이티브 이미지 라이브러리에서 크래시
- 이미지 라이브러리(ImageSharp 구버전)의 버퍼 오버플로우 버그

#### Step 4: 해결책

```csharp
// 1. 이미지 크기 제한 추가
public async Task<IActionResult> Upload(IFormFile file)
{
    if (file.Length > 10 * 1024 * 1024) // 10MB 제한
        return BadRequest("이미지 크기는 10MB 이하여야 합니다.");

    // ...
}

// 2. 라이브러리 업데이트
// ImageSharp 3.0+ 버전으로 업그레이드

// 3. try-catch는 효과 없음 (CSE이므로)
// 대신 입력 검증으로 방어
```

---

### 5.2 사례 2: 메모리 누수 분석

#### 상황

서비스 운영 중 메모리가 점진적으로 증가하다가 OOM(Out of Memory)으로 크래시됩니다.

```bash
# 메모리 증가 패턴
시작: 200MB → 1시간 후: 500MB → 4시간 후: 1.5GB → 크래시
```

#### Step 1: 라이브 프로세스에서 덤프 생성

```bash
# 크래시 전에 수동으로 덤프 생성
dotnet-dump collect -p <PID> -o memory_leak.dmp

# 또는 Windows에서
procdump -ma <PID> memory_leak.dmp
```

#### Step 2: 힙 분석

```bash
$ dotnet-dump analyze memory_leak.dmp

# 힙 통계 확인 (메모리 많이 사용하는 타입 순)
> dumpheap -stat
              MT    Count    TotalSize Class Name
00007f8a00002222   500000     64000000 MyApp.Models.OrderDto
00007f8a00003333   500000     40000000 System.String
00007f8a00001111   500000     32000000 MyApp.Services.CacheEntry
00007f8a00004444        1     20000000 System.Collections.Generic.Dictionary...
```

**발견**: `CacheEntry`가 50만 개, `OrderDto`가 50만 개 존재

#### Step 3: 특정 타입 인스턴스 분석

```bash
# CacheEntry 인스턴스들의 주소 확인
> dumpheap -type MyApp.Services.CacheEntry
         Address               MT     Size
00007f8a20000000 00007f8a00001111       64
00007f8a20000040 00007f8a00001111       64
00007f8a20000080 00007f8a00001111       64
... (50만 개)

# 첫 번째 인스턴스 상세 확인
> dumpobj 00007f8a20000000
Name:        MyApp.Services.CacheEntry
MethodTable: 00007f8a00001111
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
00007f8a00005555  4000001        8        System.String  0 instance 00007f8a30000000 Key
00007f8a00006666  4000002       10        System.Object  0 instance 00007f8a40000000 Value
00007f8a00007777  4000003       18      System.DateTime  1 instance 2024-01-01 CreatedAt

# 이 객체가 왜 수집되지 않는지 확인 (GC Root 찾기)
> gcroot 00007f8a20000000
Thread 1a2b:
    00007FFE12340000 00007FFE12345678 MyApp.Services.CacheService._cache
        -> 00007f8a50000000 System.Collections.Concurrent.ConcurrentDictionary
            -> 00007f8a20000000 MyApp.Services.CacheEntry

HandleTable:
    00007f8a60000000 (strong handle)
        -> 00007f8a50000000 System.Collections.Concurrent.ConcurrentDictionary
```

#### Step 4: 원인 파악

```csharp
// 문제가 되는 코드
public class CacheService
{
    // 만료 정책 없이 무한히 쌓이는 캐시!
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public void Add(string key, object value)
    {
        _cache[key] = new CacheEntry(key, value, DateTime.UtcNow);
        // 제거 로직 없음!
    }
}
```

#### Step 5: 해결책

```csharp
// 1. 메모리 캐시에 만료 정책 추가
public class CacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache) => _cache = cache;

    public void Add(string key, object value)
    {
        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2))
            .SetSize(1);  // 크기 제한

        _cache.Set(key, value, options);
    }
}

// 2. DI 등록 시 크기 제한
services.AddMemoryCache(options =>
{
    options.SizeLimit = 10000;  // 최대 1만 개 항목
});
```

---

### 5.3 사례 3: 데드락 분석

#### 상황

특정 API 호출 시 응답이 무한 대기됩니다. CPU 사용률은 낮지만 요청이 처리되지 않습니다.

#### Step 1: 행(Hang) 상태에서 덤프 생성

```bash
# 프로세스가 멈춘 상태에서 덤프 생성
dotnet-dump collect -p <PID> -o deadlock.dmp
```

#### Step 2: 스레드 상태 분석

```bash
$ dotnet-dump analyze deadlock.dmp

# 모든 스레드 목록
> threads
   ID    OSID ThreadOBJ           State GC Mode     GC Alloc Context  Domain
    0       1 00007f8a10000000    28220 Preemptive  00007f8a20000000 00007f8a30000000
    5    1a2b 00007f8a10000100    28220 Preemptive  00007f8a20000100 00007f8a30000000  <- 요청 처리 스레드
    6    1a2c 00007f8a10000200    28220 Preemptive  00007f8a20000200 00007f8a30000000  <- 요청 처리 스레드

# 대기 중인 스레드 확인 (State가 20이면 Wait 상태)
> threads
  Lock Count가 있는 스레드들...

# 각 스레드의 스택 확인
> setthread 5
> clrstack
OS Thread Id: 0x1a2b (5)
        Child SP               IP Call Site
00007FFE12340000 00007FFE12345678 System.Threading.Monitor.Enter(Object)
00007FFE12340100 00007FFE12345800 MyApp.Services.OrderService.ProcessOrder(Int32)
00007FFE12340200 00007FFE12345900 MyApp.Controllers.OrderController.Create(OrderRequest)

> setthread 6
> clrstack
OS Thread Id: 0x1a2c (6)
        Child SP               IP Call Site
00007FFE22340000 00007FFE22345678 System.Threading.Monitor.Enter(Object)
00007FFE22340100 00007FFE22345800 MyApp.Services.InventoryService.DeductStock(Int32)
00007FFE22340200 00007FFE22345900 MyApp.Services.OrderService.ProcessOrder(Int32)
```

#### Step 3: 락 분석

```bash
# 동기화 블록 테이블 확인
> syncblk
Index SyncBlock MonitorHeld Recursion Owning Thread Info  SyncBlock Owner
    5 00007f8a70000000            1         1 00007f8a10000100 0x1a2b MyApp.Services.OrderService
    6 00007f8a70000100            1         1 00007f8a10000200 0x1a2c MyApp.Services.InventoryService

# 데드락 상태:
# - 스레드 5 (0x1a2b): OrderService 락을 보유, InventoryService 락 대기 중
# - 스레드 6 (0x1a2c): InventoryService 락을 보유, OrderService 락 대기 중
```

#### Step 4: 원인 파악

```csharp
// 문제가 되는 코드
public class OrderService
{
    private readonly object _orderLock = new();
    private readonly InventoryService _inventory;

    public void ProcessOrder(int orderId)
    {
        lock (_orderLock)  // 1. OrderService 락 획득
        {
            // 주문 처리...
            _inventory.DeductStock(orderId);  // 2. InventoryService 락 요청 → 대기!
        }
    }
}

public class InventoryService
{
    private readonly object _inventoryLock = new();
    private readonly OrderService _order;

    public void DeductStock(int orderId)
    {
        lock (_inventoryLock)  // 1. InventoryService 락 획득
        {
            // 재고 차감...
            _order.ValidateOrder(orderId);  // 2. OrderService 락 요청 → 대기!
        }
    }
}
```

#### Step 5: 해결책

```csharp
// 해결책 1: 락 순서 통일
// 항상 OrderService → InventoryService 순서로 획득

// 해결책 2: 락 범위 최소화
public void ProcessOrder(int orderId)
{
    Order order;
    lock (_orderLock)
    {
        order = GetOrder(orderId);  // 락 범위 최소화
    }

    _inventory.DeductStock(order);  // 락 밖에서 호출
}

// 해결책 3: 비동기 처리로 전환
public async Task ProcessOrderAsync(int orderId)
{
    await using var _ = await _semaphore.WaitAsync();
    // SemaphoreSlim + async/await 사용
}
```

---

### 5.4 사례 4: 스레드 풀 고갈 분석

#### 상황

부하가 높아지면 응답 시간이 급격히 증가합니다. 타임아웃 에러가 빈발합니다.

```
System.TimeoutException: The operation has timed out.
```

#### Step 1: 스레드 상태 확인

```bash
$ dotnet-dump analyze threadpool_starvation.dmp

# 스레드 풀 상태 확인
> threadpool
CPU utilization: 85%
Worker Thread: Total: 100 Running: 100 Idle: 0 MaxLimit: 100
Completion Port Thread: Total: 10 Running: 10 Idle: 0 MaxLimit: 1000

# 모든 스레드 스택 확인
> clrstack -all
```

**발견**: Worker Thread 100개가 모두 사용 중, Idle 0개

#### Step 2: 스레드들이 무엇을 하는지 확인

```bash
# 모든 스레드의 스택을 파일로 출력
> clrstack -all

# 공통 패턴 발견
Thread 1: System.Net.Http.HttpClient.SendAsync() → Task.Wait()
Thread 2: System.Net.Http.HttpClient.SendAsync() → Task.Wait()
Thread 3: System.Net.Http.HttpClient.SendAsync() → Task.Wait()
... (100개 스레드 모두 동일)
```

#### Step 3: 원인 파악

```csharp
// 문제가 되는 코드 - async/await를 동기적으로 차단
public string GetData()
{
    // 스레드 풀 스레드를 차단하는 동기 대기!
    var result = _httpClient.GetStringAsync("https://api.example.com/data").Result;
    return result;
}

// 또는
public string GetData()
{
    var task = _httpClient.GetStringAsync("https://api.example.com/data");
    task.Wait();  // 동일한 문제!
    return task.Result;
}
```

#### Step 4: 해결책

```csharp
// 해결책 1: 완전한 async/await 사용
public async Task<string> GetDataAsync()
{
    // 스레드를 차단하지 않음
    var result = await _httpClient.GetStringAsync("https://api.example.com/data");
    return result;
}

// 해결책 2: 불가피하게 동기가 필요한 경우
public string GetData()
{
    // 별도 스레드에서 실행하여 데드락 방지
    return Task.Run(async () =>
        await _httpClient.GetStringAsync("https://api.example.com/data")
    ).GetAwaiter().GetResult();
}

// 해결책 3: 스레드 풀 최소 크기 증가 (임시 방편)
ThreadPool.SetMinThreads(200, 200);
```

---

### 5.5 사례 5: .NET 10 Preview 비동기 예외 크래시

#### 상황

.NET 10 Preview에서 CI 테스트가 간헐적으로 `AccessViolationException`으로 크래시됩니다.
로컬 Windows 환경에서는 재현되지 않고, GitHub Actions Ubuntu 환경에서만 발생합니다.

```
Process terminated. Couldn't find a valid ICU package installed on the system...
SIGABRT: abort
PC=0x7f8a12345678
   at System.AccessViolationException...
```

#### Step 1: 크래시 패턴 분석

```bash
# crash_info.txt 패턴 분석
Exception Type: System.AccessViolationException
발생 위치: 비동기 핸들러에서 예외를 throw할 때
환경: Ubuntu + .NET 10 Preview
재현율: 약 30%
```

#### Step 2: 테스트 코드 확인

```csharp
// 문제가 되는 테스트
[Fact]
public async Task Should_Throw_When_Handler_Fails()
{
    var handler = new ThrowingHandler();  // 예외를 던지는 핸들러

    // 이 호출이 AccessViolationException을 유발
    await Assert.ThrowsAsync<AggregateException>(
        () => _publisher.Publish(handler, event).AsTask());
}

public class ThrowingHandler : INotificationHandler<TestEvent>
{
    public ValueTask Handle(TestEvent notification, CancellationToken ct)
    {
        throw new InvalidOperationException("Test");  // 이 예외가 문제
    }
}
```

#### Step 3: 원인 분석

```
근본 원인:
1. .NET 10 Preview의 비동기 예외 처리 코드에 버그 존재
2. ValueTask에서 예외가 throw될 때 특정 조건에서 메모리 손상 발생
3. try-catch로 잡을 수 없는 CLR 레벨 크래시

특징:
- Windows에서는 재현 안됨
- Linux에서만 재현
- Release 모드에서 더 자주 발생
- GC 타이밍에 따라 간헐적
```

#### Step 4: 해결책

```csharp
// 해결책: .NET 10 정식 릴리즈까지 해당 테스트 Skip
[Fact(Skip = ".NET 10 preview: AccessViolationException on async exception")]
public async Task Should_Throw_When_Handler_Fails()
{
    // ...
}

// 또는 조건부 Skip
public class SkipOnNet10PreviewFact : FactAttribute
{
    public SkipOnNet10PreviewFact()
    {
        if (Environment.Version.Major == 10 &&
            Environment.Version.Build < 100)  // Preview 버전
        {
            Skip = ".NET 10 Preview에서 비동기 예외 처리 버그";
        }
    }
}

[SkipOnNet10PreviewFact]
public async Task Should_Throw_When_Handler_Fails()
{
    // ...
}
```

#### 교훈

```
1. Preview 버전의 런타임은 프로덕션 워크로드에 사용하지 않는다
2. CSE는 try-catch로 잡을 수 없으므로 방어적 프로그래밍 필요
3. 크래시 덤프 분석이 불가능한 경우도 있음 (런타임 버그)
4. CI 환경(Linux)과 로컬 환경(Windows)의 차이 인지
```

---

## 6. 트러블슈팅

### 6.1 덤프 파일이 생성되지 않음

#### 원인 1: 권한 부족

```bash
# Linux에서 권한 확인
ls -la /var/log/myapp/dumps

# 권한 부여
chmod 755 /var/log/myapp/dumps
```

#### 원인 2: 디스크 공간 부족

```bash
# 디스크 사용량 확인
df -h /var/log/myapp/dumps

# 오래된 덤프 정리
find /var/log/myapp/dumps -name "*.dmp" -mtime +7 -delete
```

#### 원인 3: 핸들러 초기화 전 크래시

```csharp
// Program.cs의 가장 첫 줄에서 초기화
CrashDumpHandler.Initialize();  // 다른 코드보다 먼저!

var builder = WebApplication.CreateBuilder(args);
```

### 6.2 덤프 파일을 열 수 없음

#### 원인 1: 비트니스 불일치

```
32비트 프로세스 덤프 → 32비트 디버거 사용
64비트 프로세스 덤프 → 64비트 디버거 사용
```

#### 원인 2: 심볼 파일 없음

```bash
# 빌드 시 심볼 파일 생성
dotnet publish -c Release -p:DebugType=full

# 또는 pdb 포함
dotnet publish -c Release -p:DebugSymbols=true
```

### 6.3 스택 트레이스가 보이지 않음

#### 원인: 최적화된 릴리즈 빌드

```xml
<!-- .csproj -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <DebugType>pdbonly</DebugType>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

### 6.4 컨테이너 환경에서 덤프 생성 실패

#### Docker에서 ptrace 권한 필요

```yaml
# docker-compose.yml
services:
  api:
    cap_add:
      - SYS_PTRACE
    security_opt:
      - seccomp:unconfined
```

#### Kubernetes에서 설정

```yaml
spec:
  containers:
  - name: myapp
    securityContext:
      capabilities:
        add: ["SYS_PTRACE"]
```

---

## 참고 자료

- [Microsoft Docs: Collect and analyze memory dumps](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dumps)
- [dotnet-dump 공식 문서](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)
- [WinDbg 사용 가이드](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/)
- [.NET 메모리 덤프 분석](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-memory-leak)
