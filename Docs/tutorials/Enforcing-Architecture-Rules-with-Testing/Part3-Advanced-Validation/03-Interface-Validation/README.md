# Chapter 11: 인터페이스 검증 (Interface Validation)

## 소개

도메인 계층에서 **인터페이스(Interface)는** 외부 의존성과의 경계를 정의하는 포트(Port) 역할을 합니다.
이 챕터에서는 `ValidateAllInterfaces()`와 `InterfaceValidator`를 사용하여 인터페이스의 네이밍 규칙과 메서드 시그니처를 검증하는 방법을 학습합니다.

## 학습 목표

- `ValidateAllInterfaces()`로 인터페이스 검증 시작
- `InterfaceValidator`의 네이밍 규칙 검증 메서드 사용
- 인터페이스 메서드의 반환 타입 검증

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
제네릭 인터페이스(`IRepository<T>`)는 ArchUnitNET에서 `IRepository`1`로 표현되므로 별도로 처리해야 합니다.

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

## 핵심 정리

| 개념 | 설명 |
|------|------|
| `ValidateAllInterfaces()` | 인터페이스 검증의 진입점 |
| `InterfaceValidator` | `TypeValidator`를 상속하여 네이밍, 메서드 검증 제공 |
| `RequireNameStartsWith("I")` | 인터페이스 `I` 접두사 규칙 |
| `RequireNameEndsWith("Repository")` | Repository 인터페이스 접미사 규칙 |
| `RequireMethod()` + `RequireReturnTypeContaining()` | 메서드 시그니처 검증 |

---

[이전: Chapter 10 - Nested Class Validation](../02-Nested-Class-Validation/) | [다음: Chapter 12 - Custom Rules](../04-Custom-Rules/)
