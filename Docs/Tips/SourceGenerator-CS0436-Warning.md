# Source Generator CS0436 타입 충돌 경고

## 개요

Source Generator를 사용하는 프로젝트에서 `CS0436` 경고가 발생하는 원인과 해결 방법을 설명합니다.

```
warning CS0436: 'AssemblyReference' 형식이 'ProjectA'에서 가져온 형식 'AssemblyReference'과(와) 충돌합니다.
```

## 원인

Source Generator는 속성(Attribute) 기반 기능을 위해 타입 정의를 컴파일 유닛에 주입합니다. 이 타입들은 `internal`로 생성되는데, 다음 조건에서 CS0436 경고가 발생합니다:

1. **프로젝트 A**가 Source Generator를 사용
2. **프로젝트 B**가 A를 참조하면서 동일한 Source Generator를 사용
3. A가 B에 `InternalsVisibleTo` 설정이 있는 경우

```
ProjectA (Source Generator 사용)
    ↓ 참조
ProjectB (Source Generator 사용) → CS0436 발생!
```

### 충돌하는 타입 예시 (Mediator.SourceGenerator)

| 타입 | 설명 |
|------|------|
| `Mediator.MediatorOptions` | Mediator 옵션 클래스 |
| `Mediator.MediatorOptionsAttribute` | Mediator 옵션 속성 |
| `Mediator.AssemblyReference` | 어셈블리 참조 마커 |
| `Mediator.IMessageHandlerBase` | 메시지 핸들러 기본 인터페이스 |
| `Mediator.ContainerMetadata` | 컨테이너 메타데이터 |

## 해결 방법

### 방법 1: NoWarn 추가 (권장)

프로젝트 파일(`.csproj`)에 경고 억제를 추가합니다:

```xml
<PropertyGroup>
  <!-- CS0436: Source Generator 타입 충돌 경고 억제 -->
  <NoWarn>$(NoWarn);CS0436</NoWarn>
</PropertyGroup>
```

### 방법 2: Generator 비활성화

테스트 프로젝트에서 Source Generator를 비활성화합니다:

```xml
<PropertyGroup>
  <!-- Mediator SourceGenerator 비활성화 -->
  <Mediator_DisableGenerator>true</Mediator_DisableGenerator>
</PropertyGroup>
```

### 방법 3: ExcludeAssets 설정

프로젝트 참조에서 analyzer를 제외합니다:

```xml
<ItemGroup>
  <ProjectReference Include="..\ProjectA\ProjectA.csproj">
    <ExcludeAssets>analyzers</ExcludeAssets>
  </ProjectReference>
</ItemGroup>
```

### 방법 1 + 2 + 3 조합 (가장 안전)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <!-- Source Generator 비활성화 -->
    <Mediator_DisableGenerator>true</Mediator_DisableGenerator>
    <!-- 타입 충돌 경고 억제 -->
    <NoWarn>$(NoWarn);CS0436</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MainProject\MainProject.csproj">
      <ExcludeAssets>analyzers</ExcludeAssets>
    </ProjectReference>
  </ItemGroup>

</Project>
```

## 영향받는 라이브러리

이 문제는 Source Generator 패턴 자체의 한계로, 여러 라이브러리에서 동일하게 발생합니다:

| 라이브러리 | 관련 이슈 |
|------------|-----------|
| Mediator | [conflict in generated code](https://lightrun.com/answers/martinothamar-mediator-conflict-in-generated-code-if-imported-in-multiple-projects) |
| CommunityToolkit.Maui | [#1185](https://github.com/CommunityToolkit/Maui/issues/1185), [#814](https://github.com/CommunityToolkit/Maui/issues/814) |
| StronglyTypedId | [#38](https://github.com/andrewlock/StronglyTypedId/issues/38) |
| xUnit | [#3036](https://github.com/xunit/xunit/issues/3036) |

## 참고 자료

- [Microsoft Learn - CS0436 Warning](https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0436)
- [dotnet/csharplang - Proposal: Disable CS0436 by default](https://github.com/dotnet/csharplang/issues/5528)

## 결론

CS0436 경고는 기능에 영향을 주지 않으며, `NoWarn`으로 안전하게 억제할 수 있습니다. 다만 `TreatWarningsAsErrors`가 활성화된 프로젝트에서는 빌드 실패를 유발하므로 반드시 처리해야 합니다.
