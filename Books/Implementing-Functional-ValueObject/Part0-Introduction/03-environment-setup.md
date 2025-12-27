# 0.3 환경 설정

> **Part 0: 서론** | [← 이전: 0.2 성공 주도 개발이란?](02-success-driven-development.md) | [목차](../README.md) | [다음: 1장 기본 나눗셈에서 시작하기 →](../01-Concept/01-Basic-Divide/BasicDivide/README.md)

---

## 필수 요구사항

### 1. .NET 10.0 SDK

```bash
# 버전 확인
dotnet --version
# 출력 예: 10.0.100

# 설치 (Windows)
winget install Microsoft.DotNet.SDK.10

# 설치 (macOS)
brew install --cask dotnet-sdk

# 설치 (Linux - Ubuntu)
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
```

### 2. IDE 선택

#### Visual Studio 2022 (권장)

- 버전: 17.12 이상
- 워크로드: ".NET 데스크톱 개발" 또는 "ASP.NET 및 웹 개발"

#### VS Code + C# 확장

- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) 확장 설치

---

## 프로젝트 설정

### 새 프로젝트 생성

```bash
# 새 프로젝트 생성
dotnet new console -n MyValueObjectProject

# LanguageExt 패키지 설치
cd MyValueObjectProject
dotnet add package LanguageExt.Core
```

### 기본 using 문

프로젝트에서 사용할 기본 using 문입니다:

```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
```

### GlobalUsings.cs (선택사항)

프로젝트 전체에서 사용할 using 문을 한 곳에서 관리할 수 있습니다:

```csharp
// GlobalUsings.cs
global using LanguageExt;
global using LanguageExt.Common;
global using static LanguageExt.Prelude;
```

---

## 각 프로젝트 실행 방법

### 프로젝트 실행

```bash
# 특정 프로젝트로 이동
cd Books/Functional-ValueObject/01-Concept/01-Basic-Divide/BasicDivide

# 프로젝트 실행
dotnet run
```

### 테스트 실행

```bash
# 테스트 프로젝트로 이동
cd Books/Functional-ValueObject/01-Concept/01-Basic-Divide/BasicDivide.Tests.Unit

# 테스트 실행
dotnet test
```

### 전체 솔루션 빌드

```bash
# 솔루션 루트에서
dotnet build

# 전체 테스트 실행
dotnet test
```

---

## 프로젝트 구조

각 튜토리얼 프로젝트는 다음과 같은 구조를 가집니다:

```
01-Basic-Divide/
├── BasicDivide/                    # 메인 프로젝트
│   ├── Program.cs                  # 메인 실행 파일
│   ├── MathOperations.cs           # 핵심 로직
│   ├── BasicDivide.csproj          # 프로젝트 파일
│   └── README.md                   # 프로젝트 설명
│
└── BasicDivide.Tests.Unit/         # 테스트 프로젝트
    ├── MathOperationsTests.cs      # 테스트 파일
    ├── BasicDivide.Tests.Unit.csproj
    └── README.md
```

---

## 첫 번째 예제 실행

환경 설정이 완료되었다면, 첫 번째 예제를 실행해보세요:

```bash
cd Books/Functional-ValueObject/01-Concept/01-Basic-Divide/BasicDivide
dotnet run
```

### 예상 출력

```
=== 기본 나눗셈 함수 ===

정상 케이스:
10 / 2 = 5

예외 케이스:
10 / 0 = System.DivideByZeroException: Attempted to divide by zero.
```

---

## 문제 해결

### .NET SDK가 인식되지 않는 경우

```bash
# PATH 환경 변수 확인
echo $PATH

# Windows의 경우 시스템 환경 변수에 다음 경로 추가
# C:\Program Files\dotnet
```

### LanguageExt 패키지 설치 실패

```bash
# NuGet 소스 확인
dotnet nuget list source

# 공식 NuGet 소스 추가
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### IDE에서 IntelliSense가 작동하지 않는 경우

1. IDE 재시작
2. `dotnet restore` 실행
3. `.vs` 또는 `.vscode` 폴더 삭제 후 재시작

---

## 다음 단계

환경 설정이 완료되었다면, Part 1의 첫 번째 장으로 이동하세요.

→ [1장: 기본 나눗셈에서 시작하기](../01-Concept/01-Basic-Divide/BasicDivide/README.md)
