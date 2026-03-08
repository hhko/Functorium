---
title: "인터페이스 검증"
---

## 개요

인터페이스 이름에서 `I` 접두사가 빠져 있거나, Repository 인터페이스가 `Repository`로 끝나지 않거나, 비동기 메서드가 `Task`를 반환하지 않는다면 — 이런 규칙 위반은 컴파일러가 잡아주지 않습니다. 팀의 네이밍 컨벤션은 코드 리뷰에만 의존하면 점점 무너집니다.

이 챕터에서는 `ValidateAllInterfaces()`와 `InterfaceValidator`를 사용하여 인터페이스의 네이밍 규칙과 메서드 시그니처를 **자동화된 테스트로** 검증하는 방법을 학습합니다.

> **"네이밍 컨벤션은 문서에 적어두는 것이 아니라, 테스트로 강제하는 것입니다. 'I 접두사를 붙이세요'라는 코드 리뷰 코멘트는 이제 테스트가 대신합니다."**

## 학습 목표

### 핵심 학습 목표

1. **`ValidateAllInterfaces()`로 인터페이스 검증 시작**
   - `ValidateAllClasses()`와 동일한 패턴으로 인터페이스를 대상으로 검증
   - `InterfaceValidator`를 통한 네이밍, 메서드 검증 제공

2. **`InterfaceValidator`의 네이밍 규칙 검증**
   - `RequireNameStartsWith("I")` — 접두사 규칙
   - `RequireNameEndsWith("Repository")` — 접미사 규칙

3. **인터페이스 메서드의 반환 타입 검증**
   - `RequireMethod()` + `RequireReturnTypeContaining()`으로 비동기 메서드 시그니처 검증
   - 제네릭 인터페이스의 ArchUnitNET 이름 표현 방식 이해

### 실습을 통해 확인할 내용
- **IRepository\<T\>**: 제네릭 기반 Repository 인터페이스
- **IOrderRepository / IProductRepository**: 특화된 Repository 인터페이스의 네이밍 검증
- **GetByIdAsync**: 비동기 메서드의 `Task` 반환 타입 검증

## 도메인 코드

### IRepository - 기본 Repository 인터페이스

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task SaveAsync(T entity);
}
```

### IOrderRepository / IProductRepository - 특화된 Repository

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerName);
}

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category);
}
```

모든 Repository 인터페이스는 `I` 접두사로 시작하고, `Repository`로 끝나며, 비동기 메서드는 `Task`를 반환합니다.

## 테스트 코드

### 인터페이스 네이밍 규칙 검증

`ValidateAllInterfaces()`는 `ValidateAllClasses()`와 동일한 패턴으로, `InterfaceValidator`를 통해 인터페이스를 검증합니다.

```csharp
[Fact]
public void AllInterfaces_ShouldHave_NameStartingWithI()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllInterfaces(Architecture, iface => iface
            .RequireNameStartsWith("I"),
            verbose: true)
        .ThrowIfAnyFailures("Interface Naming Convention Rule");
}
```

### Repository 인터페이스 이름 검증

`HaveNameEndingWith("Repository")`로 구체적 Repository 인터페이스만 필터링합니다.
제네릭 인터페이스(`IRepository<T>`)는 ArchUnitNET에서 `` IRepository`1 ``로 표현되므로 별도로 처리해야 합니다.

```csharp
[Fact]
public void ConcreteRepositoryInterfaces_ShouldHave_NameEndingWithRepository()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameEndingWith("Repository")
        .ValidateAllInterfaces(Architecture, iface => iface
            .RequireNameEndsWith("Repository"),
            verbose: true)
        .ThrowIfAnyFailures("Repository Interface Naming Rule");
}
```

### 기반 인터페이스의 메서드 반환 타입 검증

인터페이스 메서드에서도 `RequireMethod()`와 `RequireReturnTypeContaining()`을 사용할 수 있습니다.
상속받은 인터페이스는 직접 선언한 멤버만 가지므로, 기반 인터페이스를 직접 대상으로 검증합니다.

```csharp
[Fact]
public void BaseRepositoryInterface_ShouldHave_GetByIdAsyncReturningTask()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameStartingWith("IRepository")
        .ValidateAllInterfaces(Architecture, iface => iface
            .RequireMethod("GetByIdAsync", m => m
                .RequireReturnTypeContaining("Task")),
            verbose: true)
        .ThrowIfAnyFailures("Repository GetByIdAsync Rule");
}
```

## 한눈에 보는 정리

다음 표는 인터페이스 검증에 사용하는 주요 API를 정리합니다.

### 인터페이스 검증 API 요약

| API | 역할 | 적용 대상 |
|-----|------|-----------|
| **`ValidateAllInterfaces()`** | 인터페이스 검증의 진입점 | 모든 인터페이스 |
| **`InterfaceValidator`** | `TypeValidator`를 상속하여 네이밍, 메서드 검증 제공 | 검증 콜백 내부 |
| **`RequireNameStartsWith("I")`** | 인터페이스 `I` 접두사 규칙 | 네이밍 컨벤션 |
| **`RequireNameEndsWith("Repository")`** | Repository 인터페이스 접미사 규칙 | 특정 역할 인터페이스 |
| **`RequireMethod()` + `RequireReturnTypeContaining()`** | 메서드 시그니처 검증 | 비동기 메서드 패턴 |

다음 표는 ArchUnitNET에서 제네릭 타입의 이름 표현을 보여줍니다.

### ArchUnitNET 제네릭 이름 표현

| C# 코드 | ArchUnitNET 이름 | 비고 |
|----------|------------------|------|
| `IRepository<T>` | `` IRepository`1 `` | 제네릭 매개변수 수가 백틱 뒤에 표시 |
| `IOrderRepository` | `IOrderRepository` | 비제네릭은 그대로 |

## FAQ

### Q1: `ValidateAllInterfaces()`와 `ValidateAllClasses()`의 차이는 무엇인가요?
**A**: 진입점만 다릅니다. `ValidateAllInterfaces()`는 `ArchRuleDefinition.Interfaces()`로 시작하며 `InterfaceValidator`를 콜백에 제공합니다. `ValidateAllClasses()`는 `ArchRuleDefinition.Classes()`로 시작하며 `ClassValidator`를 제공합니다. 사용 패턴은 동일합니다.

### Q2: 제네릭 인터페이스를 `HaveNameEndingWith("Repository")`로 필터링하면 `IRepository<T>`도 포함되나요?
**A**: 아닙니다. ArchUnitNET에서 `IRepository<T>`는 `` IRepository`1 ``로 표현되므로 `HaveNameEndingWith("Repository")` 필터에 매칭되지 않습니다. `IOrderRepository`, `IProductRepository` 같은 비제네릭 인터페이스만 필터링됩니다.

### Q3: 상속받은 인터페이스에서 부모의 메서드를 검증할 수 있나요?
**A**: 아닙니다. ArchUnitNET에서 인터페이스는 직접 선언한 멤버만 가집니다. `IOrderRepository`에서 `GetByIdAsync`를 검증하려면 `IRepository<T>`를 직접 대상으로 지정해야 합니다.

### Q4: 인터페이스에도 `RequireImmutable()`을 적용할 수 있나요?
**A**: `RequireImmutable()`은 클래스의 구조적 불변성을 검증하는 규칙이므로 인터페이스에는 적용되지 않습니다. 인터페이스는 메서드 시그니처만 정의하므로, `RequireMethod()`와 `RequireReturnTypeContaining()` 등으로 검증합니다.

---

인터페이스의 네이밍 규칙과 메서드 시그니처를 자동으로 검증하면, 팀 컨벤션이 코드 리뷰 없이도 일관되게 유지됩니다. 다음 장에서는 프레임워크가 제공하지 않는 팀 고유의 규칙을 직접 만드는 방법을 살펴봅니다.

> [다음: 4장 - Custom Rules](../04-Custom-Rules/)
