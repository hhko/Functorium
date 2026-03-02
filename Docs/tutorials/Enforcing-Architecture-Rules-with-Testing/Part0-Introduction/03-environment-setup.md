# 0.3 환경 설정

## 필요 환경

| 항목 | 최소 버전 | 비고 |
|------|----------|------|
| .NET SDK | 10.0 | `dotnet --version`으로 확인 |
| IDE | VS 2022 / Rider / VS Code | C# 지원 IDE |

## 필요 패키지

아키텍처 테스트 프로젝트에는 다음 NuGet 패키지가 필요합니다:

### 테스트 프레임워크

| 패키지 | 용도 |
|--------|------|
| `xunit.v3` | 테스트 프레임워크 |
| `xunit.runner.visualstudio` | IDE 테스트 탐색기 지원 |
| `Microsoft.NET.Test.Sdk` | .NET 테스트 SDK |

### 아키텍처 테스트

| 패키지 | 용도 |
|--------|------|
| `TngTech.ArchUnitNET.xUnitV3` | ArchUnitNET xUnit 통합 |
| `Shouldly` | Assertion 라이브러리 |

### 프로젝트 참조

| 프로젝트 | 용도 |
|----------|------|
| `Functorium.Testing` | ArchitectureRules 프레임워크 (ClassValidator 등) |

## 프로젝트 구조

각 챕터는 다음 구조를 따릅니다:

```txt
01-Chapter-Name/
├── README.md                           # 챕터 설명
├── ProjectName/                        # 검증 대상 프로젝트
│   ├── ProjectName.csproj
│   ├── Program.cs
│   └── Domains/                        # 도메인 클래스
│       └── ...
└── ProjectName.Tests.Unit/             # 아키텍처 테스트
    ├── ProjectName.Tests.Unit.csproj
    ├── xunit.runner.json
    ├── ArchitectureTestBase.cs         # 공통 설정
    └── XxxArchitectureTests.cs         # 테스트 파일
```

## 테스트 프로젝트 설정

### .csproj 파일

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="TngTech.ArchUnitNET.xUnitV3" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectName\ProjectName.csproj" />
    <ProjectReference Include="..\..\..\..\..\..\Src\Functorium.Testing\Functorium.Testing.csproj" />
  </ItemGroup>

</Project>
```

> 패키지 버전은 `Directory.Packages.props`에서 중앙 관리됩니다.

### xunit.runner.json

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "methodDisplay": "method",
  "methodDisplayOptions": "replaceUnderscoreWithSpace",
  "diagnosticMessages": true
}
```

## Architecture 로딩 패턴

모든 아키텍처 테스트의 기반이 되는 `ArchitectureTestBase` 클래스입니다:

```csharp
using ArchUnitNET.Loader;

public abstract class ArchitectureTestBase
{
    protected static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(SomeClassInTargetAssembly).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(SomeClassInTargetAssembly).Namespace!;
}
```

**핵심 포인트:**

1. `ArchLoader`로 검증 대상 어셈블리를 로딩합니다
2. `typeof(...).Assembly`로 어셈블리를 참조합니다
3. 네임스페이스 문자열은 `typeof(...).Namespace!`로 안전하게 추출합니다
4. `static readonly` 필드로 선언하여 테스트 간 재사용합니다

## 테스트 실행

```bash
# 개별 프로젝트 테스트
dotnet test --project Path/To/ProjectName.Tests.Unit

# 전체 솔루션 테스트
dotnet test --solution Functorium.All.slnx
```

## 다음 단계

환경 설정이 완료되었으면 [Part 1: ClassValidator 기초](../Part1-ClassValidator-Basics/01-First-Architecture-Test/)에서 첫 아키텍처 테스트를 작성해봅니다.
