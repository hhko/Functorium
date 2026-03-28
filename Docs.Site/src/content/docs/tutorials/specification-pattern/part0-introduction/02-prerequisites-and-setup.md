---
title: "환경 설정"
---
코드를 직접 실행하며 학습하기 위한 환경을 준비합니다. 각 단계는 몇 분이면 충분합니다.

## 필수 요구사항

### 1. .NET 10.0 SDK

.NET SDK는 모든 프로젝트의 빌드와 실행에 필요합니다.

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
git clone https://github.com/hhko/Functorium.git
cd functorium
```

### 빌드 확인

```bash
# 전체 솔루션 빌드
dotnet build Functorium.slnx

# 전체 테스트 실행
dotnet test --solution Functorium.slnx
```

### 개별 프로젝트 실행

```bash
# 튜토리얼 전체 빌드
dotnet build specification-pattern.slnx

# 튜토리얼 전체 테스트
dotnet test --solution specification-pattern.slnx
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
# 튜토리얼 전체 테스트
dotnet test --solution specification-pattern.slnx

# 특정 테스트만 실행
dotnet test --solution specification-pattern.slnx --filter "IsSatisfiedBy_ReturnsTrue_WhenProductIsActive"
```

### 전체 솔루션 테스트

```bash
# 솔루션 루트에서
dotnet test --solution specification-pattern.slnx
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

## FAQ

### Q1: .NET 10.0 SDK가 필수인가요?
**A**: 네. 이 튜토리얼의 모든 프로젝트는 .NET 10.0을 대상으로 빌드됩니다. Functorium 라이브러리의 일부 API가 .NET 10.0 이상을 필요로 하므로, 이전 버전에서는 빌드가 실패할 수 있습니다.

### Q2: 개별 프로젝트만 빌드/테스트할 수 있나요?
**A**: 네, 각 장의 프로젝트 폴더에서 `dotnet test`를 실행하면 해당 프로젝트만 독립적으로 테스트할 수 있습니다. 전체 솔루션 빌드는 `dotnet build Functorium.slnx`로 수행합니다.

### Q3: IDE는 어떤 것을 사용해야 하나요?
**A**: VS Code + C# Dev Kit, JetBrains Rider, Visual Studio 2022 중 편한 것을 사용하면 됩니다. C# 개발 환경이 설정되어 있고 .NET 10.0 SDK가 설치되어 있다면 어떤 IDE든 동작합니다.

---

## 다음 단계

환경 설정이 완료되었습니다. 이제 Specification 패턴이 어떤 문제를 해결하는지, 전체 그림을 살펴보겠습니다.

→ [0.3 Specification 패턴 개요](03-specification-pattern-overview.md)
