# 아키텍처 규칙 테스트 레퍼런스

## ArchUnitNET 기반 테스트

### ArchitectureTestBase 설정

```csharp
using ArchUnitNET.Loader;

internal static class ArchitectureTestBase
{
    internal static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Functorium.Domains.Specifications.Specification<>).Assembly,
                typeof(Functorium.Adapters.Repositories.InMemoryRepositoryBase<,>).Assembly,
                LayeredArch.Domain.AssemblyReference.Assembly,
                LayeredArch.Application.AssemblyReference.Assembly,
                LayeredArch.Adapters.Persistence.AssemblyReference.Assembly,
                LayeredArch.Adapters.Presentation.AssemblyReference.Assembly,
                LayeredArch.Adapters.Infrastructure.AssemblyReference.Assembly)
            .Build();

    internal static readonly string DomainNamespace =
        typeof(LayeredArch.Domain.AssemblyReference).Namespace!;
    internal static readonly string ApplicationNamespace =
        typeof(LayeredArch.Application.AssemblyReference).Namespace!;
    internal static readonly string PersistenceNamespace =
        typeof(LayeredArch.Adapters.Persistence.AssemblyReference).Namespace!;
    internal static readonly string PresentationNamespace =
        typeof(LayeredArch.Adapters.Presentation.AssemblyReference).Namespace!;
    internal static readonly string InfrastructureNamespace =
        typeof(LayeredArch.Adapters.Infrastructure.AssemblyReference).Namespace!;
}
```

## 레이어 의존성 규칙

### Domain → Application/Adapter 의존 금지

```csharp
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

public sealed class LayerDependencyArchitectureRuleTests
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }
}
```

### Adapter 간 의존 금지

```csharp
[Fact]
public void PresentationAdapter_ShouldNotDependOn_OtherAdapters()
{
    Types()
        .That()
        .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
        .Should().NotDependOnAnyTypesThat()
        .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
        .OrShould().NotDependOnAnyTypesThat()
        .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
        .Check(ArchitectureTestBase.Architecture);
}
```

## ClassValidator 패턴

### Functorium TestSuite 상속

```csharp
using ArchUnitNET.Fluent;
using Functorium.Domains.Entities;
using Functorium.Testing.Assertions.ArchitectureRules;
using Functorium.Testing.Assertions.ArchitectureRules.Suites;

public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string DomainNamespace => ArchitectureTestBase.DomainNamespace;

    // 프로젝트 고유 규칙 추가
    [Fact]
    public void AggregateRoot_ShouldInherit_AggregateRootBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(AggregateRoot<>)),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Inheritance Rule");
    }
}
```

### 사용 가능한 TestSuite 베이스 클래스

| TestSuite | 검증 대상 |
|-----------|----------|
| `DomainArchitectureTestSuite` | Domain 레이어 규칙 (sealed class, private 생성자 등) |
| `ApplicationArchitectureTestSuite` | Application 레이어 규칙 |
| `AdapterArchitectureTestSuite` | Adapter 레이어 규칙 (virtual 메서드 등) |
| `CqrsArchitectureTestSuite` | CQRS 규칙 (Command/Query 분리) |
| `DtoArchitectureTestSuite` | DTO 규칙 (sealed record) |
| `PortInterfaceArchitectureTestSuite` | Port 인터페이스 규칙 |

## 규칙 예제

### sealed class 검증

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreNotAbstract()
    .ValidateAllClasses(Architecture, @class => @class
        .RequireSealed(),
        verbose: true)
    .ThrowIfAnyFailures("Sealed Class Rule");
```

### private 생성자 검증

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePrivateConstructor(),
        verbose: true)
    .ThrowIfAnyFailures("Private Constructor Rule");
```

### virtual 메서드 검증

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(PersistenceNamespace)
    .And().HaveNameEndingWith("Repository")
    .ValidateAllClasses(Architecture, @class => @class
        .RequireVirtualMethods(),
        verbose: true)
    .ThrowIfAnyFailures("Virtual Methods Rule");
```

## 핵심 규칙 요약

| 규칙 | 설명 |
|------|------|
| Domain → App/Adapter 의존 금지 | 도메인 순수성 보장 |
| App → Adapter 의존 금지 | 포트 추상화 유지 |
| Adapter 간 의존 금지 | 어댑터 독립성 보장 |
| AggregateRoot sealed class | 상속 방지 |
| Repository virtual 메서드 | Source Generator 파이프라인 필수 |
