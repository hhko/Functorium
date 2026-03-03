---
title: "ArchUnitNET과 Functorium"
---

## ArchUnitNET 소개

**ArchUnitNET은** Java의 ArchUnit을 .NET으로 포팅한 아키텍처 테스트 라이브러리입니다. 컴파일된 어셈블리를 리플렉션으로 분석하여 타입, 의존성, 네이밍 등의 규칙을 검증합니다.

### 핵심 개념

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

이런 규칙을 ArchUnitNET만으로 작성하려면 여러 개의 별도 테스트로 분산되거나, 복잡한 커스텀 로직이 필요합니다.

## Functorium ArchitectureRules 프레임워크

**Functorium ArchitectureRules는** ArchUnitNET 위에 구축된 fluent 아키텍처 검증 시스템입니다. 핵심 가치는 다음 세 가지입니다:

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

자주 사용되는 복합 규칙을 미리 구현하여 제공합니다:

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

## ArchUnitNET vs Functorium ArchitectureRules

| 구분 | ArchUnitNET | Functorium ArchitectureRules |
|------|------------|------------------------------|
| **검증 수준** | 타입/의존성/네이밍 | 타입 + 메서드 시그니처 + 불변성 |
| **규칙 표현** | Should 체인 | Validator 체이닝 |
| **복합 규칙** | 별도 테스트로 분산 | 하나의 Validator에서 통합 |
| **커스텀 규칙** | IArchRule 직접 구현 | DelegateArchRule/CompositeArchRule |
| **관계** | 독립 라이브러리 | ArchUnitNET 위에 구축 |

두 라이브러리는 대체 관계가 아닌 **보완 관계입니다.** 레이어 의존성 같은 규칙은 ArchUnitNET의 `Should().NotDependOnAnyTypesThat()`이 더 적합하고, 타입 내부 구조 검증은 Functorium ArchitectureRules가 더 적합합니다.

## 다음 장에서는

실습을 위한 개발 환경 설정 방법을 알아봅니다.
