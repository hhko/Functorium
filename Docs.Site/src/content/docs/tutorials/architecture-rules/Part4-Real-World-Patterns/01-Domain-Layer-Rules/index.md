---
title: "도메인 레이어 규칙"
---

## 개요

Entity, Value Object, Domain Event, Domain Service — DDD 전술 패턴의 각 요소에는 고유한 설계 규칙이 있습니다. Entity는 sealed이어야 하고, Value Object는 불변이어야 하며, Domain Event는 기반 클래스를 상속해야 합니다. 하지만 이 규칙들이 코드 리뷰에만 의존한다면, 프로젝트가 커질수록 위반은 늘어납니다.

이 챕터에서는 DDD 전술 패턴별 아키텍처 규칙을 정의하고, **하나의 테스트 스위트로 도메인 레이어 전체를 자동 검증**하는 방법을 학습합니다.

> **"도메인 모델의 구조적 규칙을 테스트로 강제하면, 새로운 Entity를 추가할 때마다 '이것도 sealed인가?', '팩토리 메서드가 있는가?'를 묻는 코드 리뷰 코멘트가 사라집니다."**

## 학습 목표

### 핵심 학습 목표

1. **DDD 전술 패턴별 아키텍처 규칙 정의**
   - Entity: public, sealed, 팩토리 메서드, 비공개 생성자
   - Value Object: sealed, immutable, `IValueObject` 구현
   - Domain Event: sealed, `DomainEvent` 상속
   - Domain Service: public, static

2. **`AreAssignableTo`와 `AreNotAbstract` 필터 조합**
   - 추상 기반 클래스를 제외하고 구체 구현체만 검증
   - 마커 인터페이스(`IValueObject`)를 활용한 필터링

3. **`RequireInherits`, `RequireImplements`, `RequireImmutable` 규칙 활용**
   - 상속 관계와 인터페이스 구현을 자동으로 검증
   - 팩토리 메서드 패턴을 아키텍처 테스트로 강제

### 실습을 통해 확인할 내용
- **Order (Entity)**: public, sealed, 팩토리 메서드 검증
- **Money, Address (Value Object)**: sealed, immutable, `IValueObject` 구현 검증
- **OrderCreatedEvent (Domain Event)**: sealed, `DomainEvent` 상속 검증
- **OrderPricingService (Domain Service)**: public, static 클래스 검증

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

## 한눈에 보는 정리

다음 표는 DDD 전술 패턴별 필터 전략과 검증 규칙을 비교합니다.

### DDD 전술 패턴별 아키텍처 규칙

| 패턴 | 필터 전략 | 검증 규칙 | 핵심 의도 |
|------|-----------|-----------|-----------|
| **Entity** | `AreAssignableTo` + `AreNotAbstract` | sealed, 팩토리 메서드, 비공개 생성자 | 상속 방지, 생성 제어 |
| **Value Object** | `AreAssignableTo(IValueObject)` | sealed, immutable, 인터페이스 구현 | 불변성 보장 |
| **Domain Event** | `AreAssignableTo` + `AreNotAbstract` | sealed, 상속 검증, 팩토리 메서드 | 이벤트 구조 통일 |
| **Domain Service** | `HaveNameEndingWith` | static 클래스 | 상태 없는 서비스 보장 |

다음 표는 추상 기반 클래스를 제외하는 필터 패턴을 정리합니다.

### 추상 클래스 제외 패턴

| 상황 | 필터 조합 | 이유 |
|------|-----------|------|
| Entity 검증 | `AreAssignableTo(typeof(Entity<>))` + `AreNotAbstract()` | `Entity<>` 자체 제외 |
| Domain Event 검증 | `AreAssignableTo(typeof(DomainEvent))` + `AreNotAbstract()` | `DomainEvent` 자체 제외 |
| Value Object 검증 | `AreAssignableTo(typeof(IValueObject))` | 인터페이스이므로 추상 제외 불필요 |

## FAQ

### Q1: `AreAssignableTo`와 `AreOfType`의 차이는 무엇인가요?
**A**: `AreAssignableTo(typeof(Entity<>))`는 `Entity<>`를 상속하는 모든 클래스를 포함합니다 (기반 클래스 자체도 포함). `AreOfType`은 정확히 해당 타입만 매칭합니다. 상속 계층을 검증할 때는 `AreAssignableTo`를 사용하고, `AreNotAbstract()`로 기반 클래스를 제외합니다.

### Q2: Value Object에 마커 인터페이스(`IValueObject`)를 사용하는 이유는 무엇인가요?
**A**: Value Object는 특정 기반 클래스를 상속하지 않으므로 `AreAssignableTo`로 직접 필터링할 수 없습니다. 마커 인터페이스를 사용하면 "이 클래스는 Value Object입니다"라고 명시적으로 선언할 수 있고, 아키텍처 테스트에서 쉽게 필터링할 수 있습니다.

### Q3: Domain Service를 static 클래스로 강제하는 이유는 무엇인가요?
**A**: Domain Service는 상태를 가지지 않고 순수한 도메인 로직만 포함해야 합니다. static 클래스로 강제하면 인스턴스 생성이 불가능하므로 상태를 가질 수 없고, DI 컨테이너에 등록할 필요도 없어집니다. 상태가 필요한 서비스는 Application 레이어에 속합니다.

### Q4: 새로운 DDD 패턴(예: Aggregate Root)을 추가하려면 어떻게 하나요?
**A**: 기반 타입(예: `AggregateRoot<TId>`)을 정의하고, 동일한 패턴으로 테스트를 추가하면 됩니다: `AreAssignableTo(typeof(AggregateRoot<>)).And().AreNotAbstract()`로 필터링한 후 원하는 규칙을 적용합니다.

---

도메인 레이어의 DDD 전술 패턴을 테스트로 강제하면, 새로운 도메인 객체가 추가될 때마다 자동으로 규칙 준수가 검증됩니다. 다음 장에서는 Command/Query 기반의 애플리케이션 레이어 규칙을 살펴봅니다.

> [다음: 2장 - Application Layer Rules](../02-Application-Layer-Rules/)
