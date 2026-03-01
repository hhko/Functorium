# 0.2 환경 설정

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
| Standalone | Part 1, 2(6~7장), 3 | LanguageExt.Core만 | 개념 학습용, 독립 실행 가능 |
| Functorium 참조 | Part 2(5장), 4, 5 | Functorium.csproj | 실전 적용, Pipeline/Usecase 사용 |

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

---

[← 이전: 0.1 왜 타입 안전한 파이프라인인가](01-why-this-book.md) | [다음: 0.3 Usecase Pipeline 아키텍처 개요 →](03-usecase-pipeline-overview.md)
