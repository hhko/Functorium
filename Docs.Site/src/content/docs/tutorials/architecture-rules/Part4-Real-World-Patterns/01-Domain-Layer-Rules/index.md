---
title: "도메인 레이어 규칙"
---

## 개요

Entity, Value Object, Domain Event, Specification, Domain Service — DDD 전술 패턴의 각 요소에는 고유한 설계 규칙이 있습니다. AggregateRoot는 sealed이고 `[GenerateEntityId]`를 가져야 하며, Value Object는 불변이고 `Create`는 `Fin<T>`를, `Validate`는 `Validation<Error, T>`를 반환해야 합니다. Domain Event는 sealed record여야 하고, Specification은 도메인 레이어에만 존재해야 합니다.

이 챕터에서는 Functorium의 `DomainArchitectureTestSuite`가 검증하는 **21개 규칙**을 6개 카테고리로 나누어 직접 구현합니다.

> **"도메인 모델의 구조적 규칙을 테스트로 강제하면, 새로운 Entity를 추가할 때마다 '이것도 sealed인가?', '팩토리 메서드가 있는가?'를 묻는 코드 리뷰 코멘트가 사라집니다."**

## 학습 목표

### 핵심 학습 목표

1. **AggregateRoot와 Entity의 분리**
   - AggregateRoot: public sealed, 팩토리 메서드(`Create`/`CreateFromValidated`), `[GenerateEntityId]`, 비공개 생성자
   - Entity (AggregateRoot 제외): public sealed, 팩토리 메서드, 비공개 생성자

2. **Value Object의 Fin/Validation 반환타입 검증**
   - `Create` → `Fin<T>` (단일 오류, Railway-Oriented)
   - `Validate` → `Validation<Error, T>` (다중 오류 누적)

3. **DomainEvent: sealed record + Event 접미사**
   - `RequireRecord()` — 값 의미론과 불변성 보장
   - `RequireNameEndsWith("Event")` — 유비쿼터스 언어 일관성

4. **Specification: 도메인 레이어 한정**
   - `Specification<T>` 상속 검증
   - 도메인 레이어 외부 유출 방지

5. **IDomainService 마커 인터페이스 기반 검증**
   - `static class` 대신 `sealed class : IDomainService` 패턴
   - `RequireNoDependencyOn("IObservablePort")` — 아키텍처 경계 위반 탐지
   - public 인스턴스 메서드는 `Fin` 반환 강제

### 실습을 통해 확인할 내용
- **Order (AggregateRoot)**: sealed, `[GenerateEntityId]`, 팩토리 메서드, 비공개 생성자
- **Money, Address (Value Object)**: sealed, immutable, `Create → Fin<T>`, `Validate → Validation<Error, T>`
- **OrderCreatedEvent (Domain Event)**: sealed record, "Event" 접미사
- **ActiveOrderSpecification (Specification)**: sealed, `Specification<T>` 상속, 도메인 레이어 한정
- **OrderPricingService (Domain Service)**: sealed, IDomainService, stateless, Fin 반환

## 도메인 코드 구조

### 기반 타입

```
Domains/
├── Entity.cs                     # 엔티티 기반 추상 클래스
├── AggregateRoot.cs              # Aggregate Root 추상 클래스 (Entity<TId> 상속)
├── IValueObject.cs               # 값 객체 마커 인터페이스
├── DomainEvent.cs                # 도메인 이벤트 기반 추상 record
├── Specification.cs              # Specification 기반 추상 클래스
├── IDomainService.cs             # 도메인 서비스 마커 인터페이스
└── GenerateEntityIdAttribute.cs  # 소스 생성기 트리거 어트리뷰트
```

**`AggregateRoot<TId>`는** `Entity<TId>`를 상속하는 추상 클래스입니다. 도메인 이벤트 관리, 불변식 보호 등 Aggregate 전용 책임을 분리합니다.

**`IDomainService`는** 도메인 서비스를 식별하는 마커 인터페이스입니다. `static class` 대신 이 인터페이스를 사용하면 DI 컨테이너 등록, 아키텍처 테스트 필터링, 의존성 제어가 가능합니다.

**`Specification<T>`는** 비즈니스 규칙을 캡슐화하는 추상 클래스입니다. `IsSatisfiedBy(T)` 메서드로 조건을 표현합니다.

### 구현 타입

| 타입 | 패턴 | 핵심 규칙 |
|------|------|-----------|
| `Order` | AggregateRoot | public, sealed, `[GenerateEntityId]`, 팩토리 메서드, 비공개 생성자 |
| `Money`, `Address` | Value Object | public, sealed, immutable, `Create → Fin<T>`, `Validate → Validation<Error, T>` |
| `OrderCreatedEvent` | Domain Event | sealed record, "Event" 접미사 |
| `ActiveOrderSpecification` | Specification | public, sealed, `Specification<T>` 상속 |
| `OrderPricingService` | Domain Service | public, sealed, IDomainService, stateless, Fin 반환 |

## 테스트 코드 설명

### AggregateRoot vs Entity 분리

`DomainArchitectureTestSuite`는 AggregateRoot와 Entity를 별도 카테고리로 검증합니다. AggregateRoot는 트랜잭션 경계이므로 `[GenerateEntityId]`로 강타입 ID를 보장하고, Entity는 AggregateRoot 내부의 하위 엔티티이므로 ID 생성 규칙이 다릅니다.

```csharp
// AggregateRoot: Entity<> 중 AggregateRoot<>를 상속하는 클래스
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreAssignableTo(typeof(AggregateRoot<>))
    .And().AreNotAbstract()
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNotStatic(),
        verbose: true)
    .ThrowIfAnyFailures("AggregateRoot Visibility Rule");

// Entity: Entity<>를 상속하지만 AggregateRoot<>는 제외
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreAssignableTo(typeof(Entity<>))
    .And().AreNotAbstract()
    .And().AreNotAssignableTo(typeof(AggregateRoot<>))  // AggregateRoot 제외
    // ...
```

### 팩토리 메서드 반환타입 검증

**`RequireReturnTypeOfDeclaringClass()`는** 팩토리 메서드가 자기 타입을 반환하는지 검증합니다. `Order.Create()`가 `Order`를 반환하지 않으면 위반입니다. 이 규칙은 팩토리 메서드가 잘못된 타입을 반환하는 실수를 방지합니다.

```csharp
.RequireMethod("Create", m => m
    .RequireVisibility(Visibility.Public)
    .RequireStatic()
    .RequireReturnTypeOfDeclaringClass())
.RequireMethod("CreateFromValidated", m => m
    .RequireVisibility(Visibility.Public)
    .RequireStatic()
    .RequireReturnTypeOfDeclaringClass())
```

### Value Object Create/Validate 반환타입

`Create`는 `Fin<T>`, `Validate`는 `Validation<Error, T>`를 반환해야 합니다. 두 메서드의 역할이 다릅니다:
- **`Create`** — "하나의 오류가 있으면 즉시 실패" (Railway-Oriented)
- **`Validate`** — "모든 오류를 누적 수집" (Applicative)

```csharp
// Create → Fin<T>
.RequireMethod("Create", m => m
    .RequireStatic()
    .RequireReturnType(typeof(Fin<>)))

// Validate → Validation<Error, T>
.RequireMethod("Validate", m => m
    .RequireStatic()
    .RequireReturnType(typeof(Validation<,>)))
```

### DomainEvent sealed record 검증

DomainEvent는 값 의미론이 필요합니다. 같은 주문 ID로 발생한 `OrderCreatedEvent` 두 개는 같은 이벤트입니다. `record`는 이 동등성을 자동으로 보장하고, `sealed`는 이벤트 계약 변경을 방지합니다.

```csharp
.ValidateAllClasses(Architecture, @class => @class
    .RequireSealed()
    .RequireRecord(),      // record 타입 강제
    verbose: true)
```

### Specification 도메인 레이어 한정

Specification은 비즈니스 규칙을 캡슐화하므로 도메인 레이어에만 존재해야 합니다. Application이나 Infrastructure 레이어에 Specification이 생기면 비즈니스 규칙이 유출된 것입니다.

```csharp
ArchRuleDefinition.Classes()
    .That()
    .AreAssignableTo(typeof(Specification<>))
    .And().AreNotAbstract()
    .And().ResideInNamespace(DomainNamespace)
    .Should().ResideInNamespace(DomainNamespace)
    .Check(Architecture);
```

### IDomainService 아키텍처 경계 검증

`RequireNoDependencyOn("IObservablePort")`는 도메인 서비스가 관측 관심사에 의존하지 않도록 강제합니다. 로깅, 메트릭, 트레이싱은 Application 레이어의 Usecase Pipeline에서 처리해야 합니다.

```csharp
// 아키텍처 경계 위반 탐지
.ValidateAllClasses(Architecture, @class => @class
    .RequireNoDependencyOn("IObservablePort"),
    verbose: true)

// public 인스턴스 메서드는 Fin 반환 강제
.RequireAllMethods(
    m => m.Visibility == Visibility.Public
         && m.IsStatic != true
         && m.MethodForm == MethodForm.Normal,
    method => method.RequireReturnTypeContaining("Fin"))
```

## 한눈에 보는 정리

### 6개 카테고리 × 21개 규칙

| 카테고리 | 테스트 | 핵심 규칙 |
|---------|--------|-----------|
| **AggregateRoot (4)** | PublicSealed, Create/CreateFromValidated, GenerateEntityId, PrivateCtors | 트랜잭션 경계, 소스 생성기 연동 |
| **Entity (3)** | PublicSealed, Create/CreateFromValidated, PrivateCtors | AggregateRoot 제외 필터 |
| **ValueObject (4)** | PublicSealed+PrivateCtors, Immutable, Create→`Fin<>`, Validate→`Validation<,>` | 이중 반환타입 검증 |
| **DomainEvent (2)** | SealedRecord, NameEndsWith("Event") | record + 명명 규칙 |
| **Specification (3)** | PublicSealed, InheritsBase, ResideInDomain | 도메인 한정 |
| **DomainService (5)** | PublicSealed, Stateless, NoDependencyOn, ReturnFin, NotRecord | 마커 인터페이스 기반 |

### 추상 클래스 제외 패턴

| 상황 | 필터 조합 | 이유 |
|------|-----------|------|
| AggregateRoot 검증 | `AreAssignableTo(typeof(AggregateRoot<>))` + `AreNotAbstract()` | `AggregateRoot<>` 자체 제외 |
| Entity 검증 | `AreAssignableTo(typeof(Entity<>))` + `AreNotAbstract()` + `AreNotAssignableTo(typeof(AggregateRoot<>))` | Entity + AggregateRoot 분리 |
| Value Object 검증 | `ImplementInterface(typeof(IValueObject))` + `AreNotAbstract()` | 마커 인터페이스 필터링 |
| DomainService 검증 | `ImplementInterface(typeof(IDomainService))` + `AreNotAbstract()` | 마커 인터페이스 필터링 |

## FAQ

### Q1: AggregateRoot와 Entity를 왜 구분하나요?
**A**: AggregateRoot는 트랜잭션 경계이므로 `[GenerateEntityId]`로 강타입 ID를 반드시 갖춰야 합니다. Entity는 AggregateRoot 내부의 하위 엔티티(예: `OrderItem`)로, 독립적인 ID 생성이 필요 없을 수 있습니다. Suite가 두 카테고리를 분리하여 각각 다른 규칙을 적용합니다.

### Q2: Domain Service를 `static class` 대신 `IDomainService`를 쓰는 이유는 무엇인가요?
**A**: `static class`는 DI 컨테이너에 등록할 수 없고, `ImplementInterface` 필터로 선택할 수 없습니다. `IDomainService` 마커 인터페이스를 사용하면: (1) 아키텍처 테스트에서 정확히 도메인 서비스만 필터링, (2) `RequireNoDependencyOn`으로 아키텍처 경계 검증, (3) DI 등록이 필요한 경우 확장 가능합니다.

### Q3: `RequireNoDependencyOn("IObservablePort")`는 어떤 문제를 방지하나요?
**A**: 도메인 서비스가 로깅/메트릭/트레이싱 인터페이스에 의존하면, 순수한 도메인 로직이 인프라 관심사에 오염됩니다. Observability는 Application 레이어의 Usecase Pipeline에서 Cross-Cutting Concern으로 처리해야 합니다.

### Q4: `Create → Fin<T>`와 `Validate → Validation<Error, T>`를 왜 둘 다 요구하나요?
**A**: `Create`는 단일 오류로 즉시 실패하는 Railway-Oriented 패턴이고, `Validate`는 모든 오류를 누적 수집하는 Applicative 패턴입니다. Command Usecase에서는 `Create`로 빠르게 실패하고, Application 레이어 DTO 검증에서는 `Validate`로 모든 오류를 한 번에 사용자에게 보여줍니다.

---

도메인 레이어의 6개 카테고리 21개 규칙을 테스트로 강제하면, 새로운 도메인 객체가 추가될 때마다 자동으로 규칙 준수가 검증됩니다. 다음 장에서는 Command/Query 기반의 애플리케이션 레이어 규칙을 살펴봅니다.

→ [2장: 애플리케이션 레이어 규칙](../02-Application-Layer-Rules/)
