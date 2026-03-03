---
title: "도메인 레이어 규칙"
---

## 소개

DDD(Domain-Driven Design) 전술 패턴의 도메인 레이어를 아키텍처 테스트로 검증합니다. **Entity, Value Object, Domain Event, Domain Service** 각 패턴에 맞는 구조적 규칙을 정의하고 자동으로 강제합니다.

## 학습 목표

- DDD 전술 패턴별 아키텍처 규칙을 정의할 수 있다
- `AreAssignableTo`와 `AreNotAbstract` 필터를 조합하여 상속 구조를 다룰 수 있다
- `RequireInherits`, `RequireImplements`, `RequireImmutable` 규칙을 활용할 수 있다
- 팩토리 메서드 패턴을 아키텍처 테스트로 강제할 수 있다

## 도메인 코드 구조

### 기반 타입

```
Domains/
├── Entity.cs          # 엔티티 기반 추상 클래스
├── IValueObject.cs    # 값 객체 마커 인터페이스
└── DomainEvent.cs     # 도메인 이벤트 기반 추상 클래스
```

**`Entity<TId>`는** 모든 엔티티의 기반 클래스입니다. `Id` 프로퍼티를 `protected set`으로 제공하여 하위 클래스에서만 설정할 수 있습니다.

**`IValueObject`는** 값 객체를 식별하는 마커 인터페이스입니다. `AreAssignableTo` 필터와 함께 사용하여 값 객체만 선택적으로 검증합니다.

**`DomainEvent`는** 도메인 이벤트의 기반 추상 클래스입니다. `OccurredAt` 프로퍼티로 이벤트 발생 시점을 기록합니다.

### 구현 타입

| 타입 | 패턴 | 핵심 규칙 |
|------|------|-----------|
| `Order` | Entity | public, sealed, 팩토리 메서드, 비공개 생성자 |
| `Money`, `Address` | Value Object | public, sealed, immutable, IValueObject 구현 |
| `OrderCreatedEvent` | Domain Event | public, sealed, DomainEvent 상속 |
| `OrderPricingService` | Domain Service | public, static |

## 테스트 코드 설명

### 추상 기반 클래스 제외하기

`AreAssignableTo(typeof(Entity<>))`로 엔티티를 필터링하면 추상 기반 클래스인 `Entity<>` 자체도 포함됩니다. `.And().AreNotAbstract()`를 추가하여 구체적인 엔티티만 검증합니다.

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And()
    .AreAssignableTo(typeof(Entity<>))
    .And()
    .AreNotAbstract()  // Entity<> 기반 클래스 제외
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed(),
        verbose: true)
    .ThrowIfAnyFailures("Entity Public Sealed Rule");
```

### Value Object 불변성 검증

```csharp
ArchRuleDefinition.Classes()
    .That()
    .AreAssignableTo(typeof(IValueObject))
    .ValidateAllClasses(Architecture, @class => @class
        .RequireImmutable(),
        verbose: true)
    .ThrowIfAnyFailures("Value Object Immutability Rule");
```

**`RequireImmutable()`은** setter가 없고 필드가 readonly인지 검증합니다. 값 객체의 핵심 특성인 불변성을 자동으로 강제합니다.

### Domain Service 정적 클래스 검증

```csharp
ArchRuleDefinition.Classes()
    .That()
    .HaveNameEndingWith("Service")
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireStatic(),
        verbose: true)
    .ThrowIfAnyFailures("Domain Service Public Static Rule");
```

## 핵심 정리

| 패턴 | 필터 전략 | 검증 규칙 |
|------|-----------|-----------|
| Entity | `AreAssignableTo` + `AreNotAbstract` | sealed, 팩토리 메서드, 비공개 생성자 |
| Value Object | `AreAssignableTo(IValueObject)` | sealed, immutable, 인터페이스 구현 |
| Domain Event | `AreAssignableTo` + `AreNotAbstract` | sealed, 상속 검증, 팩토리 메서드 |
| Domain Service | `HaveNameEndingWith` | static 클래스 |

---

[이전: Chapter 12 - Custom Rules](../../Part3-Advanced-Validation/04-Custom-Rules/) | [다음: Chapter 14 - Application Layer Rules](../02-Application-Layer-Rules/)
