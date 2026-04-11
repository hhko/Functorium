---
title: "ArchUnitNET 소개"
---

## 아키텍처 테스트를 위한 도구

앞 장에서 설계 규칙을 수동으로 검증하는 것의 한계를 확인했습니다. 그렇다면 이 규칙들을 자동화하려면 어떤 도구가 필요할까요? .NET 생태계에서 가장 널리 쓰이는 아키텍처 테스트 도구는 **ArchUnitNET입니다.**

> **"아키텍처 테스트는 리플렉션으로 어셈블리를 분석하여, 컴파일된 코드의 구조가 팀의 규칙을 따르는지 자동으로 검증합니다."**

## ArchUnitNET 소개

**ArchUnitNET은** Java의 ArchUnit을 .NET으로 포팅한 아키텍처 테스트 라이브러리입니다. 컴파일된 어셈블리를 리플렉션으로 분석하여 타입, 의존성, 네이밍 등의 규칙을 검증합니다.

### Core Concepts

**Architecture 객체는** 검증할 어셈블리들을 로딩한 결과입니다:

```csharp
using ArchUnitNET.Loader;

static readonly Architecture Architecture =
    new ArchLoader()
        .LoadAssemblies(typeof(MyDomain.Order).Assembly)
        .Build();
```

**ArchRuleDefinition은** 규칙 정의의 진입점입니다:

```csharp
using ArchUnitNET.Fluent;

// 클래스 규칙
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace("MyApp.Domains")
    .Should().BePublic()
    .Check(Architecture);

// 의존성 규칙
ArchRuleDefinition.Types()
    .That()
    .ResideInNamespace("MyApp.Domains")
    .Should().NotDependOnAnyTypesThat()
    .ResideInNamespace("MyApp.Infrastructure")
    .Check(Architecture);
```

### ArchUnitNET의 한계

ArchUnitNET은 강력하지만, 다음과 같은 복합 규칙을 표현하기 어렵습니다:

```txt
"모든 도메인 엔티티는:
  1. public sealed 클래스이고
  2. 불변이며
  3. Create 팩토리 메서드가 있고
  4. 해당 메서드는 static이고 Fin<T>를 반환해야 한다"
```

이런 규칙을 ArchUnitNET만으로 작성하려면 여러 개의 별도 테스트로 분산되거나, 복잡한 커스텀 로직이 필요합니다. 여러 속성을 한꺼번에 검증해야 하는 domain rule에서는 이러한 분산이 규칙의 **응집도를** 떨어뜨립니다.

## Functorium ArchitectureRules 프레임워크

**Functorium ArchitectureRules는** ArchUnitNET 위에 구축된 fluent 아키텍처 검증 시스템입니다. ArchUnitNET이 어셈블리를 로드하고 타입을 필터링하는 기반을 제공하면, Functorium ArchitectureRules는 그 위에 **Validator 패턴으로** 복합 규칙을 응집도 있게 표현합니다.

### 1. Validator 패턴

**ClassValidator/InterfaceValidator/MethodValidator를** 통해 타입 수준에서 체이닝 방식으로 규칙을 검증합니다:

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNoPublicSetters()
        .RequireMethod("Create", m => m
            .RequireStatic()
            .RequireReturnType(typeof(Fin<>))),
        verbose: true)
    .ThrowIfAnyFailures("Domain Entity Rule");
```

하나의 테스트에서 여러 규칙을 fluent하게 표현할 수 있습니다.

### 2. 내장 규칙

자주 사용되는 복합 규칙을 미리 구현하여 provides:

```csharp
// ImmutabilityRule: 6차원 불변성 검증
@class.RequireImmutable()

// 6차원: 쓰기 가능성, 생성자, 속성, 필드, 컬렉션, 메서드
```

### 3. 커스텀 규칙 합성

팀 고유의 규칙을 `IArchRule<T>` 인터페이스로 정의하고, `DelegateArchRule`과 `CompositeArchRule`로 합성합니다:

```csharp
// 람다로 커스텀 규칙 정의
var namingRule = new DelegateArchRule<Class>(
    "Forbids Dto suffix in domain",
    (target, _) => target.Name.EndsWith("Dto")
        ? [new RuleViolation(target.FullName, "NamingRule", "Dto suffix not allowed")]
        : []);

// 여러 규칙을 AND로 합성
var compositeRule = new CompositeArchRule<Class>(
    new ImmutabilityRule(),
    namingRule);

// Validator에 적용
@class.Apply(compositeRule)
```

> **"ArchUnitNET이 '어떤 클래스를 검증할지'를 결정한다면, Functorium ArchitectureRules는 '그 클래스가 어떤 조건을 만족해야 하는지'를 표현합니다."**

## ArchUnitNET vs Functorium ArchitectureRules

The following table 두 라이브러리의 역할과 특성을 compares.

| Aspect | ArchUnitNET | Functorium ArchitectureRules |
|------|------------|------------------------------|
| **검증 수준** | 타입/의존성/네이밍 | 타입 + 메서드 시그니처 + immutability |
| **규칙 표현** | Should 체인 | Validator 체이닝 |
| **복합 규칙** | 별도 테스트로 분산 | 하나의 Validator에서 통합 |
| **커스텀 규칙** | IArchRule 직접 구현 | DelegateArchRule/CompositeArchRule |
| **관계** | 독립 라이브러리 | ArchUnitNET 위에 구축 |

두 라이브러리는 대체 관계가 아닌 **보완 관계입니다.** 레이어 의존성 같은 규칙은 ArchUnitNET의 `Should().NotDependOnAnyTypesThat()`이 더 적합하고, 타입 내부 구조 검증은 Functorium ArchitectureRules가 더 적합합니다.

## FAQ

### Q1: ArchUnitNET 없이 Functorium ArchitectureRules만 사용할 수 있나요?
**A**: 아닙니다. Functorium ArchitectureRules는 ArchUnitNET 위에 구축되어 있습니다. 어셈블리 로딩(`ArchLoader`)과 타입 필터링(`ArchRuleDefinition.Classes().That()...`)은 ArchUnitNET이 담당하고, 그 위에서 Validator 패턴으로 세밀한 규칙을 표현하는 것이 Functorium의 역할입니다.

### Q2: ArchUnitNET의 `Should()` 체인과 Functorium의 `ValidateAllClasses`를 같은 테스트에서 섞어 써도 되나요?
**A**: 네, 가능합니다. 레이어 의존성처럼 ArchUnitNET이 더 적합한 규칙은 `Should()` 체인으로, 타입 내부 구조 검증은 `ValidateAllClasses`로 각각 작성하면 됩니다. 같은 테스트 클래스 안에 두 방식의 테스트를 공존시킬 수 있습니다.

### Q3: Functorium의 Validator 패턴이 ArchUnitNET의 커스텀 규칙보다 나은 점은 무엇인가요?
**A**: 가장 큰 차이는 **응집도입니다.** ArchUnitNET에서 "public + sealed + 불변 + factory method" 규칙을 표현하려면 4개의 별도 테스트가 필요하지만, Functorium의 Validator에서는 하나의 체인으로 모든 조건을 한눈에 볼 수 있습니다.

---

## Next Steps

도구의 역할을 이해했으니, 실습 환경을 준비하겠습니다. Next chapter에서는 NuGet 패키지 설치부터 테스트 프로젝트 설정까지, 아키텍처 테스트를 작성하기 위한 환경을 구성합니다.

→ [0.3 환경 설정](03-environment-setup.md)
