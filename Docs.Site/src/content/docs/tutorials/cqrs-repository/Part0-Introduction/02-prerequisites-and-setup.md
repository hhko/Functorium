---
title: "환경 설정"
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

#### JetBrains Rider

- .NET 10.0 SDK가 자동 감지됩니다
- File -> Settings -> Build, Execution, Deployment -> Toolset and Build에서 SDK 경로 확인

#### Visual Studio 2022

- Visual Studio 2022 17.12 이상 권장
- 워크로드: ".NET 데스크톱 개발" 또는 "ASP.NET 및 웹 개발"

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
# Part 1 첫 번째 장 테스트 실행
dotnet test --project Docs.Site/src/content/docs/tutorials/cqrs-repository/Part1-Domain-Entity-Foundations/01-Entity-And-Identity/EntityAndIdentity.Tests.Unit

# 튜토리얼 전체 빌드/테스트
dotnet build Docs.Site/src/content/docs/tutorials/cqrs-repository/cqrs-repository.slnx
dotnet test --solution Docs.Site/src/content/docs/tutorials/cqrs-repository/cqrs-repository.slnx
```

---

## 기본 using 문

프로젝트에서 사용할 기본 using 문입니다:

```csharp
// 도메인 엔티티
using Functorium.Domains.Entities;

// Repository
using Functorium.Domains.Repositories;

// Query 어댑터
using Functorium.Applications.Queries;

// Usecase
using Functorium.Applications.Usecases;

// Specification
using Functorium.Domains.Specifications;
```

LanguageExt 함수형 타입을 사용하는 경우:

```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
```

---

## 각 프로젝트 실행 방법

### 테스트 실행

```bash
# 특정 프로젝트 테스트 실행
dotnet test --project Docs.Site/src/content/docs/tutorials/cqrs-repository/Part1-Domain-Entity-Foundations/01-Entity-And-Identity/EntityAndIdentity.Tests.Unit

# 특정 테스트만 실행
dotnet test --filter "Create_ReturnsAggregate_WhenValid"
```

### 전체 솔루션 테스트

```bash
# 솔루션 루트에서
dotnet test --solution Functorium.slnx
```

---

## 프로젝트 구조

각 튜토리얼 프로젝트는 다음과 같은 구조를 가집니다:

```
01-Entity-And-Identity/
├── EntityAndIdentity/                       # 메인 프로젝트
│   ├── EntityAndIdentity.csproj             # 프로젝트 파일
│   └── Domains/                             # 도메인 클래스
│       ├── Product.cs
│       └── ProductId.cs
│
└── EntityAndIdentity.Tests.Unit/            # 테스트 프로젝트
    ├── EntityAndIdentity.Tests.Unit.csproj
    ├── xunit.runner.json
    └── ProductTests.cs
```

---

## Functorium 의존성

각 프로젝트는 Functorium 라이브러리를 참조합니다. 프로젝트 파일에서 다음과 같이 설정됩니다:

```xml
<ItemGroup>
    <ProjectReference Include="../../../../../../../../../Src/Functorium/Functorium.csproj" />
</ItemGroup>
```

Functorium이 제공하는 CQRS 관련 주요 타입:

| 네임스페이스 | 주요 타입 | 용도 |
|-------------|----------|------|
| `Functorium.Domains.Entities` | Entity\<TId\>, AggregateRoot\<TId\>, IEntityId | 도메인 엔티티 |
| `Functorium.Domains.Repositories` | IRepository\<TAggregate, TId\> | Command 측 Repository |
| `Functorium.Applications.Queries` | IQueryPort\<TEntity, TDto\> | Query 측 어댑터 |
| `Functorium.Applications.Usecases` | ICommandRequest, IQueryRequest | Usecase 인터페이스 |
| `Functorium.Applications.Persistence` | IUnitOfWork, IUnitOfWorkTransaction | 영속화 |

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

### LanguageExt 관련 빌드 오류

LanguageExt.Core 패키지가 올바르게 복원되었는지 확인합니다:

```bash
dotnet restore
dotnet build
```

---

## FAQ

### Q1: .NET 10.0 SDK 대신 이전 버전을 사용해도 되나요?
**A**: 이 튜토리얼의 프로젝트는 .NET 10.0을 대상으로 빌드됩니다. 이전 버전에서는 LanguageExt와 Functorium의 일부 API가 호환되지 않을 수 있으므로, .NET 10.0 SDK 설치를 권장합니다.

### Q2: 전체 솔루션 빌드 없이 개별 프로젝트만 테스트할 수 있나요?
**A**: 네, 각 장의 테스트 프로젝트 폴더에서 `dotnet test`를 실행하면 해당 프로젝트만 빌드/테스트됩니다. 전체 솔루션 빌드는 모든 프로젝트의 정합성을 확인할 때 사용합니다.

### Q3: LanguageExt 관련 빌드 오류가 발생하면 어떻게 해야 하나요?
**A**: 먼저 `dotnet restore`로 패키지를 복원하세요. 그래도 해결되지 않으면 `bin`과 `obj` 폴더를 삭제한 뒤 다시 빌드합니다. LanguageExt.Core 패키지가 NuGet 소스에서 정상적으로 다운로드되는지도 확인하세요.

---

## 다음 단계

환경 설정이 완료되었다면, CQRS 패턴의 개요를 살펴보세요.
