---
title: "Architecture Test Suites"
---

## Overview

Part 1~4에서는 아키텍처 규칙을 하나씩 직접 작성하는 방법을 배웠습니다. Entity는 sealed인지, ValueObject는 불변인지, DomainService는 상태가 없는지 — 각각의 규칙을 이해하고 작성하는 것은 중요합니다.

하지만 실전 프로젝트에서 매번 21개의 domain rule을 새로 작성해야 한다면 어떨까요? Functorium은 **사전 구축된 테스트 스위트(Test Suite)를** provides. 추상 클래스를 상속하고 두 개의 프로퍼티만 오버라이드하면, 검증된 규칙이 즉시 적용됩니다.

> **"규칙을 이해하는 것과 규칙을 매번 작성하는 것은 다릅니다. Suite는 이해한 규칙을 즉시 적용하는 가장 빠른 방법입니다."**

## Learning Objectives

### 핵심 학습 목표

1. **DomainArchitectureTestSuite 상속**
   - `Architecture`와 `DomainNamespace`만 오버라이드하면 21개 규칙 자동 적용
   - Entity, ValueObject, DomainEvent, Specification, DomainService 규칙 포함

2. **커스텀 규칙 추가**
   - Suite를 상속한 후 프로젝트별 고유 규칙을 `[Fact]` 메서드로 추가
   - 기존 Suite 규칙과 새 규칙이 함께 실행

3. **Virtual 프로퍼티로 동작 커스터마이징**
   - `ValueObjectExcludeFromFactoryMethods`: factory method 검증에서 특정 ValueObject 제외
   - `DomainServiceAllowedFieldTypes`: DomainService의 허용 필드 타입 지정

### 실습을 통해 확인할 내용
- **DomainArchitectureTestSuite 상속**: DomainLayerRules 도메인 코드를 Suite로 검증
- **커스텀 규칙 추가**: AggregateRoot 상속 규칙을 프로젝트별 규칙으로 추가

## 프로젝트 구조

```
05-Architecture-Test-Suites/
├── ArchitectureTestSuites.Tests.Unit/
│   ├── ArchitectureTestSuites.Tests.Unit.csproj   # DomainLayerRules 프로젝트 참조
│   ├── xunit.runner.json
│   └── ArchitectureTests.cs                       # Suite 상속 테스트
└── index.md
```

이 챕터는 별도 도메인 프로젝트를 생성하지 않습니다. **Part 4-01의 DomainLayerRules 프로젝트를 참조**하여 동일한 도메인 코드에 Suite 기반 검증을 적용합니다.

## DomainArchitectureTestSuite (21 tests)

### 기본 사용법

Suite 상속은 두 단계입니다:

**Step 1**: 추상 프로퍼티 오버라이드

```csharp
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader()
            .LoadAssemblies(typeof(Order).Assembly)
            .Build();

    protected override string DomainNamespace { get; } =
        typeof(Order).Namespace!;
}
```

이것만으로 21개의 `[Fact]` 테스트가 자동 실행됩니다.

### 자동 적용되는 21개 규칙

| 카테고리 | 테스트 수 | 검증 내용 |
|----------|:---------:|-----------|
| **Entity** | 7 | AggregateRoot/Entity — public sealed, Create/CreateFromValidated 팩토리, GenerateEntityId 어트리뷰트, private 생성자 |
| **ValueObject** | 4 | public sealed + private 생성자, immutability(ImmutabilityRule), Create → `Fin<T>`, Validate → `Validation<Error, T>` |
| **DomainEvent** | 2 | sealed record, "Event" 접미사 |
| **Specification** | 3 | public sealed, `Specification<T>` 상속, 도메인 레이어 위치 |
| **DomainService** | 5 | public sealed, 상태 없음(인스턴스 필드 금지), IObservablePort 의존 금지, public 메서드 `Fin<T>` 반환, record 아님 |

### 추상 프로퍼티

| 프로퍼티 | 타입 | Description |
|----------|------|------|
| `Architecture` | `Architecture` | ArchLoader로 로딩한 어셈블리 아키텍처 |
| `DomainNamespace` | `string` | 도메인 타입이 위치하는 루트 네임스페이스 |

### 가상 프로퍼티 (커스터마이징)

| 프로퍼티 | 기본값 | Description |
|----------|--------|------|
| `ValueObjectExcludeFromFactoryMethods` | `[]` | Create/Validate factory method 검증에서 제외할 ValueObject 타입 |
| `DomainServiceAllowedFieldTypes` | `[]` | DomainService의 `RequireNoInstanceFields`에서 허용할 필드 타입 |

커스터마이징 예시:

```csharp
public sealed class DomainArchTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } = ...;
    protected override string DomainNamespace { get; } = ...;

    // UnitOfMeasure는 열거형 스타일이라 Create/Validate가 없음
    protected override IReadOnlyList<Type> ValueObjectExcludeFromFactoryMethods =>
        [typeof(UnitOfMeasure)];

    // DomainService에서 ILogger 필드 허용
    protected override string[] DomainServiceAllowedFieldTypes =>
        ["ILogger"];
}
```

## 커스텀 규칙 추가

Suite를 상속한 후 프로젝트 고유 규칙을 `[Fact]` 메서드로 추가합니다. Suite의 21개 규칙과 함께 실행됩니다.

```csharp
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } = ...;
    protected override string DomainNamespace { get; } = ...;

    // 프로젝트별 추가 규칙
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

## ApplicationArchitectureTestSuite (4 tests)

애플리케이션 레이어의 Command/Query 구조를 검증하는 Suite입니다.

```csharp
public sealed class ApplicationArchitectureRuleTests : ApplicationArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader()
            .LoadAssemblies(typeof(CreateOrderCommand).Assembly)
            .Build();

    protected override string ApplicationNamespace { get; } =
        "MyApp.Application";
}
```

### 자동 적용되는 4개 규칙

| 테스트 | 검증 내용 |
|--------|-----------|
| `Command_ShouldHave_ValidatorNestedClass` | Command에 Validator가 있으면 sealed + `AbstractValidator` 구현 |
| `Command_ShouldHave_UsecaseNestedClass` | Command에 Usecase 필수, sealed + `ICommandUsecase` 구현 |
| `Query_ShouldHave_ValidatorNestedClass` | Query에 Validator가 있으면 sealed + `AbstractValidator` 구현 |
| `Query_ShouldHave_UsecaseNestedClass` | Query에 Usecase 필수, sealed + `IQueryUsecase` 구현 |

### 추상 프로퍼티

| 프로퍼티 | 타입 | Description |
|----------|------|------|
| `Architecture` | `Architecture` | ArchLoader로 로딩한 어셈블리 아키텍처 |
| `ApplicationNamespace` | `string` | 애플리케이션 타입이 위치하는 루트 네임스페이스 |

## 수동 규칙 vs Suite 비교

| 관점 | 수동 규칙 (Part 4-01~04) | Suite 상속 (이 챕터) |
|------|--------------------------|---------------------|
| **규칙 작성** | 규칙을 직접 하나씩 구현 | 상속만으로 즉시 적용 |
| **학습 가치** | 각 규칙의 동작 원리 이해 | 실전 프로젝트 빠른 적용 |
| **커스터마이징** | 완전한 자유도 | virtual 프로퍼티 + 추가 `[Fact]` |
| **유지보수** | 프레임워크 변경 시 직접 수정 | 프레임워크 업데이트로 자동 반영 |
| **권장 시나리오** | 규칙 학습, 특수한 검증 요구 | 새 프로젝트, 팀 표준 적용 |

## 실전 예제 참조

Suite 패턴이 실전에서 어떻게 사용되는지 확인할 수 있는 프로젝트입니다.

| 프로젝트 | 경로 | 테스트 수 |
|----------|------|:---------:|
| LayeredArch 호스트 | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture/` | 42+ |
| ECommerce DDD 예제 | `Docs.Site/src/content/docs/samples/ecommerce-ddd/Tests/ECommerce.Tests.Unit/Architecture/` | 26+ |

두 프로젝트 모두 `DomainArchitectureTestSuite`와 `ApplicationArchitectureTestSuite`를 상속하고, 프로젝트별 커스텀 규칙을 추가하는 패턴을 uses.

## FAQ

### Q1: Suite와 수동 규칙을 함께 사용해도 되나요?
**A**: 네, Suite 상속과 수동 규칙 작성을 동일 테스트 프로젝트에서 결합할 수 있습니다. Suite가 제공하지 않는 규칙(예: 레이어 의존성, Adapter 규칙)은 별도 테스트 클래스로 작성합니다. 실전에서는 Suite + 수동 규칙 + ArchUnitNET 네이티브 규칙을 모두 사용하는 것이 일반적입니다.

### Q2: Suite의 특정 테스트를 비활성화할 수 있나요?
**A**: xUnit의 `[Fact(Skip = "reason")]`은 상속된 테스트에 적용할 수 없습니다. 특정 테스트를 건너뛰려면 virtual 프로퍼티를 활용하세요. 예를 들어 `ValueObjectExcludeFromFactoryMethods`로 특정 ValueObject를 제외하거나, `DomainServiceAllowedFieldTypes`로 허용 필드 타입을 지정할 수 있습니다.

### Q3: Suite를 사용하면 Part 1~4의 학습이 불필요한가요?
**A**: 아닙니다. Suite는 검증된 규칙을 빠르게 적용하는 도구이지만, 규칙이 위반됐을 때 원인을 파악하려면 각 API의 동작 원리를 이해해야 합니다. Part 1~4의 학습은 Suite 사용 여부와 관계없이 필수입니다.

### Q4: 새 프로젝트에서 어떤 순서로 아키텍처 테스트를 도입하나요?
**A**: 1) `DomainArchitectureTestSuite` 상속으로 domain rule 즉시 적용, 2) `ApplicationArchitectureTestSuite` 상속으로 애플리케이션 규칙 적용, 3) 프로젝트별 커스텀 규칙 추가, 4) ArchUnitNET 네이티브 API로 레이어 의존성 규칙 추가. Suite부터 시작하면 최소한의 코드로 최대의 검증을 확보할 수 있습니다.

---

Suite를 활용하면 새 프로젝트에서 아키텍처 규칙을 도입하는 비용이 크게 줄어듭니다. 규칙을 이해하고(Part 1~4), Suite로 즉시 적용하고(이 챕터), 필요에 따라 확장하는 것이 실전에서 가장 효과적인 패턴입니다.

→ [Part 5의 1장: 베스트 프랙티스](../../Part5-Conclusion/01-best-practices.md)
