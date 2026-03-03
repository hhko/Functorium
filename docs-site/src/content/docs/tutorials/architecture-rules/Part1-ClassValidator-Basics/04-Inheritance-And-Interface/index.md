---
title: "4장: 상속과 인터페이스"
---

> **Part 1: ClassValidator 기초** | [← 이전: 3장 네이밍 규칙](../03-Naming-Rules/) | [다음: Part 2 →](../../Part2-Method-And-Property-Validation/)

---

## 개요

이 장에서는 클래스의 **상속 관계와 인터페이스 구현을** 아키텍처 테스트로 검증하는 방법을 학습합니다. 도메인 엔티티가 올바른 기본 클래스를 상속하는지, 필요한 인터페이스를 구현하는지를 강제하여 도메인 모델의 일관성을 보장합니다.

## 학습 목표

1. **상속 검증**
   - `RequireInherits(typeof(Entity<>))`: 엔티티 기본 클래스 상속 강제
2. **인터페이스 구현 검증**
   - `RequireImplements(typeof(IAggregate))`: 특정 인터페이스 구현 강제
   - `RequireImplements(typeof(IAuditable))`: 감사 인터페이스 구현 강제
3. **제네릭 인터페이스 검증**
   - `RequireImplementsGenericInterface("IRepository")`: 제네릭 인터페이스 구현 검증

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

**Product는** 애그리거트 루트이므로 `IAggregate`를 구현하고, **Category는** 일반 엔티티이므로 `IAuditable`만 구현합니다.

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

## 핵심 개념 정리

| 메서드 | 검증 내용 | 사용 시나리오 |
|--------|----------|--------------|
| `RequireInherits(Type)` | 특정 기본 클래스 상속 | 엔티티 기본 클래스, DDD 패턴 |
| `RequireImplements(Type)` | 특정 인터페이스 구현 | 마커 인터페이스, 계약 강제 |
| `RequireImplementsGenericInterface(string)` | 제네릭 인터페이스 구현 (이름 기반) | 리포지토리, 핸들러 패턴 |

### 오픈 제네릭 타입 처리

`typeof(Entity<>)`와 같은 오픈 제네릭 타입을 사용할 때, `RequireInherits`는 내부적으로 `FullName.StartsWith()`를 사용합니다. 이로 인해 `Entity<Guid>`, `Entity<int>` 등 모든 구체화된 타입을 매칭할 수 있습니다.

### 필터링과 규칙의 조합

ArchUnitNET의 필터링 메서드(`.AreNotAbstract()`, `.HaveNameEndingWith()` 등)와 ClassValidator 규칙을 결합하면, 특정 조건을 만족하는 클래스 집합에만 규칙을 적용할 수 있습니다.

---

> [← 이전: 3장 네이밍 규칙](../03-Naming-Rules/) | [다음: Part 2 →](../../Part2-Method-And-Property-Validation/)
