# 0.2 사전 준비와 환경 설정

> **Part 0: 서론** | [← 이전: 0.1 이 튜토리얼을 읽어야 하는 이유](01-why-this-tutorial.md) | [목차](../README.md) | [다음: 0.3 Specification 패턴 개요 →](03-specification-pattern-overview.md)

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

### 2. IDE 설정

#### VS Code + C# Dev Kit

- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) 확장 설치

---

## 프로젝트 설정

### 소스 코드 클론

```bash
# Functorium 프로젝트 클론
git clone https://github.com/your-org/functorium.git
cd functorium
```

### 빌드 확인

```bash
# 전체 솔루션 빌드
dotnet build Functorium.All.slnx

# 전체 테스트 실행
dotnet test --solution Functorium.All.slnx
```

### 개별 프로젝트 실행

```bash
# Part 1 첫 번째 장 테스트 실행
cd Docs/tutorials/Implementing-Specification-Pattern/Part1-Specification-Basics/01-First-Specification/FirstSpecification.Tests.Unit
dotnet test
```

---

## 기본 using 문

프로젝트에서 사용할 기본 using 문입니다:

```csharp
using Functorium.Domains.Specifications;
```

Expression Specification을 사용하는 경우:

```csharp
using System.Linq.Expressions;
using Functorium.Domains.Specifications;
```

---

## 각 프로젝트 실행 방법

### 테스트 실행

```bash
# 특정 프로젝트 테스트 실행
cd Docs/tutorials/Implementing-Specification-Pattern/Part1-Specification-Basics/01-First-Specification/FirstSpecification.Tests.Unit
dotnet test

# 특정 테스트만 실행
dotnet test --filter "IsSatisfiedBy_ReturnsTrue_WhenProductIsActive"
```

### 전체 솔루션 테스트

```bash
# 솔루션 루트에서
dotnet test --solution Functorium.All.slnx
```

---

## 프로젝트 구조

각 튜토리얼 프로젝트는 다음과 같은 구조를 가집니다:

```
01-First-Specification/
├── FirstSpecification/                    # 메인 프로젝트
│   ├── FirstSpecification.csproj          # 프로젝트 파일
│   └── Specifications/                    # Specification 클래스
│       └── ActiveProductSpec.cs
│
└── FirstSpecification.Tests.Unit/         # 테스트 프로젝트
    ├── FirstSpecification.Tests.Unit.csproj
    ├── xunit.runner.json
    └── ActiveProductSpecTests.cs
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

### IDE에서 IntelliSense가 작동하지 않는 경우

1. IDE 재시작
2. `dotnet restore` 실행
3. `.vs` 또는 `.vscode` 폴더 삭제 후 재시작

---

## 다음 단계

환경 설정이 완료되었다면, Specification 패턴의 개요를 살펴보세요.

→ [0.3 Specification 패턴 개요](03-specification-pattern-overview.md)
