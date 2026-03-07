---
title: ".NET 10 환경 설정"
---

릴리스 노트 자동화 시스템은 Git 로그를 분석하고 문서를 생성하는 여러 C# 스크립트로 구성되어 있습니다. 이 스크립트들은 `.csproj` 프로젝트 파일 없이 단일 `.cs` 파일만으로 실행되는 .NET 10의 **file-based app** 기능을 활용합니다. 프로젝트 설정 없이 곧바로 스크립트를 실행할 수 있어, 자동화 도구를 빠르게 만들고 수정하기에 적합하기 때문입니다.

이 절에서는 .NET 10 SDK를 설치하고, file-based app이 제대로 동작하는지 확인하는 과정을 살펴보겠습니다.

## file-based app이란?

.NET 10부터 지원되는 이 기능은 단일 `.cs` 파일만으로 애플리케이션을 실행할 수 있게 해줍니다. 기존에는 `dotnet new console`로 프로젝트를 생성한 뒤 `dotnet run`으로 실행해야 했지만, file-based app은 그 과정을 건너뜁니다.

```bash
# 기존 방식 (프로젝트 필요)
dotnet new console -n MyApp
cd MyApp
dotnet run

# file-based app (단일 파일)
dotnet MyScript.cs
```

이 튜토리얼의 자동화 스크립트들이 file-based app으로 작성된 이유는 명확합니다. 릴리스 노트 생성을 위한 스크립트는 빠른 프로토타이핑과 수정이 중요하고, 배포용 빌드가 필요 없으며, 프로젝트 간 참조 없이 독립적으로 동작하면 충분합니다. 반면 여러 파일로 구성된 대규모 애플리케이션이나, DLL/EXE 빌드가 필요하거나, 테스트 프로젝트와 연동해야 하는 경우에는 기존 프로젝트 방식이 여전히 적합합니다.

## .NET 10 SDK 설치

### Windows

1. [.NET 다운로드 페이지](https://dotnet.microsoft.com/download/dotnet/10.0)에서 SDK 다운로드
2. 설치 프로그램 실행
3. 설치 완료 후 터미널에서 확인:

```powershell
dotnet --version
# 출력: 10.0.100
```

### macOS

**Homebrew 사용:**
```bash
brew install dotnet-sdk
```

**수동 설치:**
1. [.NET 다운로드 페이지](https://dotnet.microsoft.com/download/dotnet/10.0)에서 macOS 버전 다운로드
2. `.pkg` 파일 실행
3. 확인:
```bash
dotnet --version
```

### Linux (Ubuntu/Debian)

```bash
# Microsoft 패키지 저장소 추가
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# SDK 설치
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# 확인
dotnet --version
```

## 설치 확인

터미널에서 다음 명령어를 실행하여 올바르게 설치되었는지 확인합니다.

```bash
# 버전 확인
dotnet --version
# 출력: 10.0.100 (또는 그 이상)

# SDK 정보 확인
dotnet --info
```

**예상 출력:**
```
.NET SDK:
 Version:           10.0.100
 Commit:            ...
 Workload version:  ...

런타임 환경:
 OS Name:     Windows
 OS Version:  10.0.22631
 OS Platform: Windows
 RID:         win-x64
 Base Path:   C:\Program Files\dotnet\sdk\10.0.100\
```

## 첫 번째 file-based app 실행

file-based app이 제대로 작동하는지 테스트해봅시다.

### 1. 테스트 파일 생성

`hello.cs` 파일을 생성합니다:

```csharp
// hello.cs
Console.WriteLine("Hello, .NET 10 file-based app!");
Console.WriteLine($"현재 시간: {DateTime.Now}");
```

### 2. 실행

```bash
dotnet hello.cs
```

**예상 출력:**
```
Hello, .NET 10 file-based app!
현재 시간: 2025-12-20 오전 10:30:45
```

### 3. NuGet 패키지 사용

file-based app에서도 NuGet 패키지를 사용할 수 있습니다. 파일 상단에 `#r` 지시자를 추가합니다:

```csharp
// nuget-test.cs
#r "nuget: Spectre.Console, 0.54.0"

using Spectre.Console;

AnsiConsole.MarkupLine("[green]Hello[/] from [blue]Spectre.Console[/]!");
```

**실행:**
```bash
dotnet nuget-test.cs
```

첫 실행 시 패키지를 다운로드하므로 시간이 걸릴 수 있습니다.

## 환경 변수 설정

### DOTNET_ROOT (선택사항)

일부 도구에서 .NET SDK 경로를 찾지 못할 경우 환경 변수를 설정합니다.

**Windows (PowerShell):**
```powershell
$env:DOTNET_ROOT = "C:\Program Files\dotnet"
[System.Environment]::SetEnvironmentVariable("DOTNET_ROOT", "C:\Program Files\dotnet", "User")
```

**macOS/Linux:**
```bash
export DOTNET_ROOT=/usr/share/dotnet
echo 'export DOTNET_ROOT=/usr/share/dotnet' >> ~/.bashrc
```

### PATH 확인

`dotnet` 명령어가 어디서든 실행되려면 PATH에 포함되어야 합니다.

**Windows:**
```powershell
$env:Path -split ';' | Where-Object { $_ -like '*dotnet*' }
```

**macOS/Linux:**
```bash
echo $PATH | tr ':' '\n' | grep dotnet
```

## file-based app의 특징

### 지원되는 기능

| 기능 | 지원 여부 | 예시 |
|------|----------|------|
| Top-level statements | O | `Console.WriteLine("Hello");` |
| NuGet 패키지 | O | `#r "nuget: PackageName, Version"` |
| 명령줄 인자 | O | `args` 배열 사용 |
| 여러 클래스 | O | 파일 내 여러 클래스 정의 가능 |
| async/await | O | `await Task.Delay(1000);` |
| 프로젝트 참조 | X | 단일 파일 전용 |

### 제한사항

file-based app은 단일 파일 전용이므로 다른 `.cs` 파일을 직접 참조할 수 없고, DLL/EXE 빌드도 불가능합니다. 대규모 프로젝트에는 부적합하지만, 이 튜토리얼의 자동화 스크립트처럼 독립적으로 동작하는 도구에는 이상적입니다.

## Directory.Build.props 활용

여러 file-based app에서 공통 설정을 사용하려면 `Directory.Build.props` 파일을 활용합니다.

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

이 파일이 있는 폴더(또는 상위 폴더)에서 실행되는 모든 file-based app에 설정이 적용됩니다.

## Directory.Packages.props 활용

NuGet 패키지 버전을 중앙에서 관리하려면 `Directory.Packages.props`를 사용합니다.

**Directory.Packages.props:**
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="System.CommandLine" Version="2.0.1" />
    <PackageVersion Include="Spectre.Console" Version="0.54.0" />
    <PackageVersion Include="PublicApiGenerator" Version="11.5.4" />
  </ItemGroup>
</Project>
```

이렇게 설정하면 스크립트에서 버전 없이 패키지를 참조할 수 있습니다:
```csharp
#r "nuget: Spectre.Console"  // 버전 생략 가능
```

이제 .NET 10 환경이 준비되었으니, 다음으로 이 스크립트들을 실행할 AI 도구인 Claude Code를 살펴보겠습니다.
