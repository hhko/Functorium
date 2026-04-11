---
title: "상속과 인터페이스"
---
## Overview

`Entity<TId>` 기반 클래스를 반드시 상속해야 하는 도메인 엔티티가 있습니다. 그런데 새로운 팀원이 `Entity<TId>`를 상속하지 않고 독자적인 `Product` 클래스를 만들었다면 어떻게 될까요? `Id` 속성도 없고, 동등성 비교도 깨지고, 나중에 리포지토리에서 문제가 발생합니다. In this chapter, **상속 관계와 인터페이스 구현을** 아키텍처 테스트로 검증하여 도메인 모델의 일관성을 보장하는 방법을 배웁니다.

> **"도메인 모델의 일관성은 올바른 상속과 인터페이스 구현에서 시작됩니다. 아키텍처 테스트는 이 계약이 코드베이스 전체에서 지켜지도록 guarantees."**

## Learning Objectives

### 핵심 학습 목표
1. **상속 검증**
   - `RequireInherits(typeof(Entity<>))`: 엔티티 기본 클래스 상속 강제
2. **인터페이스 구현 검증**
   - `RequireImplements(typeof(IAggregate))`: 특정 인터페이스 구현 강제
   - `RequireImplements(typeof(IAuditable))`: 감사 인터페이스 구현 강제
3. **제네릭 인터페이스 검증**
   - `RequireImplementsGenericInterface("IRepository")`: 제네릭 인터페이스 구현 검증

### 실습을 통해 확인할 내용
- `Entity<TId>` 상속 여부를 오픈 제네릭 타입으로 검증
- `IAggregate`, `IAuditable` 인터페이스 구현 강제
- 제네릭 인터페이스를 이름만으로 매칭하는 방법

## 프로젝트 구조

```
04-Inheritance-And-Interface/
├── InheritanceAndInterface/                  # 메인 프로젝트
│   ├── Domains/
│   │   ├── Entity.cs                         # abstract 기본 엔티티 클래스
│   │   ├── IAggregate.cs                     # 애그리거트 루트 마커 인터페이스
│   │   ├── IAuditable.cs                     # 감사 인터페이스
│   │   ├── Product.cs                        # Entity + IAggregate + IAuditable
│   │   └── Category.cs                       # Entity + IAuditable
│   ├── Services/
│   │   ├── IRepository.cs                    # 제네릭 리포지토리 인터페이스
│   │   └── ProductRepository.cs              # IRepository<Product> 구현
│   ├── Program.cs
│   └── InheritanceAndInterface.csproj
├── InheritanceAndInterface.Tests.Unit/       # 테스트 프로젝트
│   ├── ArchitectureTests.cs
│   ├── InheritanceAndInterface.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## 검증 대상 코드

### 도메인 계층 구조

```
Entity<TId> (abstract)
├── Product : Entity<Guid>, IAggregate, IAuditable
└── Category : Entity<Guid>, IAuditable
```

**Product는** 애그리거트 루트이므로 `IAggregate`를 구현하고, **Category는** 일반 엔티티이므로 `IAuditable`만 implements.

### 리포지토리 계층

```
IRepository<T> (interface)
└── ProductRepository : IRepository<Product>
```

## 테스트 코드 설명

### 상속 검증 (RequireInherits)

```csharp
[Fact]
public void Entities_ShouldInherit_EntityBase()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireInherits(typeof(Entity<>)),
            verbose: true)
        .ThrowIfAnyFailures("Entity Inheritance Rule");
}
```

`RequireInherits(typeof(Entity<>))`는 오픈 제네릭 타입 `Entity<>`를 기본 클래스로 지정합니다. 내부적으로 `FullName.StartsWith()`를 사용하여 `Entity`1[System.Guid]`와 같은 IL 수준의 타입명을 매칭합니다.

`.AreNotAbstract()` 필터를 사용하여 `Entity<TId>` 자체(abstract)는 검증 대상에서 제외합니다.

### 인터페이스 구현 검증 (RequireImplements)

```csharp
[Fact]
public void AuditableEntities_ShouldImplement_IAuditable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImplements(typeof(IAuditable)),
            verbose: true)
        .ThrowIfAnyFailures("Auditable Entity Rule");
}
```

`RequireImplements`는 지정된 인터페이스를 구현하는지 검증합니다. 이 예제에서는 모든 구체 엔티티가 `IAuditable`을 구현해야 한다는 규칙을 강제합니다.

### 제네릭 인터페이스 검증 (RequireImplementsGenericInterface)

```csharp
[Fact]
public void Repositories_ShouldImplement_GenericIRepository()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ServiceNamespace)
        .And()
        .HaveNameEndingWith("Repository")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImplementsGenericInterface("IRepository"),
            verbose: true)
        .ThrowIfAnyFailures("Repository Interface Rule");
}
```

`RequireImplementsGenericInterface("IRepository")`는 제네릭 인터페이스의 이름만으로 구현 여부를 검증합니다. 타입 매개변수를 지정할 필요 없이 `IRepository<Product>`, `IRepository<Category>` 등 모든 구체 타입을 매칭합니다.

## Summary at a Glance

The following table 상속 및 인터페이스 검증 메서드를 정리합니다.

### 상속/인터페이스 검증 메서드

| 메서드 | 검증 내용 | 사용 시나리오 |
|--------|----------|--------------|
| `RequireInherits(Type)` | 특정 기본 클래스 상속 | 엔티티 기본 클래스, DDD 패턴 |
| `RequireImplements(Type)` | 특정 인터페이스 구현 | 마커 인터페이스, 계약 강제 |
| `RequireImplementsGenericInterface(string)` | 제네릭 인터페이스 구현 (이름 기반) | 리포지토리, 핸들러 패턴 |

### 오픈 제네릭 타입 처리

`typeof(Entity<>)`처럼 타입 매개변수가 비어 있는 오픈 제네릭 타입을 전달하면, `RequireInherits`는 내부적으로 `FullName.StartsWith()`를 uses. 이 방식은 `Entity<Guid>`, `Entity<int>`, `Entity<ProductId>` 등 **모든 구체화된 타입을** 매칭합니다. 반면 `typeof(Entity<Guid>)`처럼 닫힌 제네릭 타입을 전달하면 정확히 `Entity<Guid>`만 매칭됩니다.

### 필터링과 규칙의 조합

ArchUnitNET의 필터링 메서드(`.AreNotAbstract()`, `.HaveNameEndingWith()` 등)와 ClassValidator 규칙을 결합하면, 특정 조건을 만족하는 클래스 집합에만 규칙을 적용할 수 있습니다.

## FAQ

### Q1: `RequireInherits`와 `RequireImplements`의 차이는 무엇인가요?
**A**: `RequireInherits`는 **상속 계층(class hierarchy)을** 검증합니다. `class Product : Entity<Guid>`에서 `Entity<Guid>`와의 관계를 verifies. `RequireImplements`는 **인터페이스 구현을** 검증합니다. `class Product : IAggregate`에서 `IAggregate`와의 관계를 verifies.

### Q2: `RequireImplements`와 `RequireImplementsGenericInterface`는 언제 구분해서 사용하나요?
**A**: 비제네릭 인터페이스(`IAggregate`, `IAuditable`)에는 `RequireImplements(typeof(IAggregate))`를 uses. 제네릭 인터페이스(`IRepository<Product>`)에는 타입 매개변수를 무시하고 이름만으로 매칭하는 `RequireImplementsGenericInterface("IRepository")`가 편리합니다.

### Q3: 다중 인터페이스 구현을 한꺼번에 검증할 수 있나요?
**A**: 네, Validator 체이닝으로 가능합니다. `@class.RequireImplements(typeof(IAggregate)).RequireImplements(typeof(IAuditable))`처럼 여러 인터페이스 구현을 하나의 체인에서 검증할 수 있습니다.

### Q4: `AreNotAbstract()` 필터를 빼면 어떻게 되나요?
**A**: `Entity<TId>` 추상 클래스 자체도 검증 대상에 포함됩니다. `Entity<TId>`는 자기 자신을 상속하지 않으므로 `RequireInherits(typeof(Entity<>))` 검증에 실패합니다. 추상 기본 클래스를 필터에서 제외하는 것이 일반적인 패턴입니다.

---

다음 Part에서는 MethodValidator를 통한 메서드 시그니처 검증과 프로퍼티/필드 immutability 검증으로 범위를 확장합니다.

→ [Part 2: 메서드와 프로퍼티 검증](../../Part2-Method-And-Property-Validation/)
