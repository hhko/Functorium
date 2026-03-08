---
title: "환경 설정"
---

튜토리얼을 시작하기 전에 다음 항목을 확인하세요.

## 필수 도구

| 도구 | 버전 | 용도 |
|------|------|------|
| .NET SDK | 10.0 이상 | 빌드 및 실행 |
| VS Code | 최신 | 코드 편집 |
| C# Dev Kit | 최신 | C# 개발 지원 |

## 사전 지식

이 튜토리얼을 학습하기 위해 다음 개념을 알고 있어야 합니다:

| 개념 | 수준 | 설명 |
|------|------|------|
| C# 제네릭 | 기초 | `List<T>`, `where T : class` 등 기본 제네릭 문법 |
| 인터페이스 | 기초 | 인터페이스 정의, 구현, 다형성 |
| Record 타입 | 기초 | `record class`, sealed record, positional record |
| Mediator 패턴 | 선택 | Part 2부터 필요 (Part 1은 불필요) |

## 프로젝트 빌드

```bash
# 저장소 클론
git clone https://github.com/your-repo/Functorium.git
cd Functorium

# Part 1 빌드 (standalone)
cd Docs/tutorials/Designing-TypeSafe-Usecase-Pipeline-Constraints/Part1-Generic-Variance-Foundations/01-Covariance/Covariance
dotnet build

# Part 1 테스트
cd ../Covariance.Tests.Unit
dotnet test

# 전체 솔루션 빌드
cd /path/to/Functorium
dotnet build Functorium.All.slnx
```

## 프로젝트 구분

이 튜토리얼의 프로젝트는 두 가지 유형으로 나뉩니다:

| 유형 | Part | 참조 | 설명 |
|------|------|------|------|
| Standalone | Part 1, 2(2~3장), 3 | LanguageExt.Core만 | 개념 학습용, 독립 실행 가능 |
| Functorium 참조 | Part 2(1장), 4, 5 | Functorium.csproj | 실전 적용, Pipeline/Usecase 사용 |

### Standalone 프로젝트

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" />
  </ItemGroup>
</Project>
```

### Functorium 참조 프로젝트

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
  </ItemGroup>
</Project>
```

## 학습 순서 권장

Part 1~3은 순서대로 학습하는 것을 권장합니다. Part 4~5는 Part 3 완료 후 자유롭게 선택하여 학습할 수 있습니다.

## FAQ

### Q1: Part 1은 Mediator 패턴 지식 없이도 학습 가능한가요?
**A**: 네. Part 1은 C# 제네릭 변성(공변/반공변/불변)만 다루므로 Mediator 패턴 지식이 필요하지 않습니다. Part 2부터 `IPipelineBehavior`가 등장하므로, 그때까지 Mediator 패턴의 기본 개념을 알아두면 충분합니다.

### Q2: Standalone 프로젝트와 Functorium 참조 프로젝트의 차이는 무엇인가요?
**A**: Standalone 프로젝트는 `LanguageExt.Core`만 참조하며, 개념 학습용으로 독립 실행이 가능합니다. Functorium 참조 프로젝트는 `Functorium.csproj`를 참조하며, 실전 Pipeline과 Usecase 구현에 사용됩니다. Part 1~3은 주로 Standalone, Part 4~5는 Functorium 참조 프로젝트입니다.

### Q3: .NET SDK 10.0 이전 버전으로도 튜토리얼을 따라갈 수 있나요?
**A**: 이 튜토리얼은 C# 11의 `static abstract` 멤버와 최신 record 구문을 사용합니다. .NET 7 이상이면 `static abstract`를 지원하지만, 프로젝트 빌드 설정이 .NET 10을 기준으로 되어 있으므로 .NET SDK 10.0 이상을 권장합니다.

다음 장에서는 이 튜토리얼이 해결하려는 Pipeline 아키텍처의 전체 그림을 먼저 살펴봅니다.

