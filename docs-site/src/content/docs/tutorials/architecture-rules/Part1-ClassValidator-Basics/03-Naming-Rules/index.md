---
title: "네이밍 규칙"
---

> **Part 1: ClassValidator 기초** | [← 이전: 2장 가시성과 한정자](../02-Visibility-And-Modifiers/) | [다음: 4장 상속과 인터페이스 →](../04-Inheritance-And-Interface/)

---

## 개요

이 장에서는 **클래스와 인터페이스의 네이밍 규칙을** 아키텍처 테스트로 강제하는 방법을 학습합니다. 접두사, 접미사, 정규식 패턴을 사용하여 일관된 명명 규칙을 코드베이스 전체에 적용합니다.

## 학습 목표

1. **접미사 규칙**
   - `RequireNameEndsWith("Service")`: 서비스 클래스 네이밍 강제
   - `RequireNameEndsWith("Event")`: 이벤트 클래스 네이밍 강제
   - `RequireNameEndsWith("Dto")`: DTO 클래스 네이밍 강제
2. **접두사 규칙**
   - `RequireNameStartsWith("I")`: 인터페이스 네이밍 규칙 강제
3. **정규식 패턴 규칙**
   - `RequireNameMatching(".*Repository$")`: 복잡한 네이밍 패턴 검증
4. **인터페이스 검증**
   - `ValidateAllInterfaces`로 인터페이스 대상 검증

## 프로젝트 구조

```
03-Naming-Rules/
├── NamingRules/                              # 메인 프로젝트
│   ├── Domains/
│   │   ├── OrderService.cs
│   │   ├── ProductService.cs
│   │   ├── OrderCreatedEvent.cs
│   │   └── ProductUpdatedEvent.cs
│   ├── Dtos/
│   │   ├── OrderDto.cs
│   │   └── ProductDto.cs
│   ├── Repositories/
│   │   ├── IOrderRepository.cs
│   │   └── IProductRepository.cs
│   ├── Program.cs
│   └── NamingRules.csproj
├── NamingRules.Tests.Unit/                   # 테스트 프로젝트
│   ├── ArchitectureTests.cs
│   ├── NamingRules.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## 테스트 코드 설명

### 접미사 검증 (RequireNameEndsWith)

```csharp
[Fact]
public void ServiceClasses_ShouldEndWith_Service()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameEndingWith("Service")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNameEndsWith("Service"),
            verbose: true)
        .ThrowIfAnyFailures("Service Naming Rule");
}
```

`HaveNameEndingWith("Service")`로 대상을 필터링한 후, `RequireNameEndsWith("Service")`로 규칙을 검증합니다. 이 패턴은 "Service 네임스페이스에 있는 모든 클래스는 Service로 끝나야 한다"와 같은 규칙에 활용할 수 있습니다.

### DTO 네이밍 검증

```csharp
[Fact]
public void DtoClasses_ShouldEndWith_Dto()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DtoNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNameEndsWith("Dto"),
            verbose: true)
        .ThrowIfAnyFailures("DTO Naming Rule");
}
```

`Dtos` 네임스페이스에 있는 **모든 클래스가** `Dto`로 끝나는지 검증합니다. 네임스페이스 기반 필터링과 네이밍 규칙을 결합하면 강력한 규칙을 정의할 수 있습니다.

### 인터페이스 접두사 검증 (ValidateAllInterfaces)

```csharp
[Fact]
public void Interfaces_ShouldStartWith_I()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(RepositoryNamespace)
        .ValidateAllInterfaces(Architecture, @interface => @interface
            .RequireNameStartsWith("I"),
            verbose: true)
        .ThrowIfAnyFailures("Interface Naming Rule");
}
```

인터페이스를 검증할 때는 `ArchRuleDefinition.Interfaces()`와 `ValidateAllInterfaces`를 사용합니다. **InterfaceValidator도** ClassValidator와 동일한 네이밍 검증 메서드를 제공합니다.

### 정규식 패턴 검증 (RequireNameMatching)

```csharp
[Fact]
public void RepositoryInterfaces_ShouldMatch_RepositoryPattern()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(RepositoryNamespace)
        .ValidateAllInterfaces(Architecture, @interface => @interface
            .RequireNameMatching(".*Repository$"),
            verbose: true)
        .ThrowIfAnyFailures("Repository Naming Rule");
}
```

`RequireNameMatching`은 정규식 패턴을 사용하여 복잡한 네이밍 규칙을 검증합니다. 접두사/접미사 검증으로 표현하기 어려운 규칙에 활용합니다.

## 핵심 개념 정리

| 메서드 | 검증 내용 | 사용 시나리오 |
|--------|----------|--------------|
| `RequireNameEndsWith(suffix)` | 이름이 지정된 접미사로 끝남 | Service, Event, Dto 등 |
| `RequireNameStartsWith(prefix)` | 이름이 지정된 접두사로 시작 | I (인터페이스), Abstract 등 |
| `RequireNameMatching(regex)` | 정규식 패턴과 일치 | 복합 네이밍 규칙 |
| `ValidateAllInterfaces` | 인터페이스 대상 검증 진입점 | 인터페이스 규칙 적용 |

### ClassValidator vs InterfaceValidator

- **`ValidateAllClasses`** -- `ClassValidator`를 사용하여 클래스 검증
- **`ValidateAllInterfaces`** -- `InterfaceValidator`를 사용하여 인터페이스 검증
- 두 Validator 모두 `TypeValidator`를 상속하여 네이밍 규칙(`RequireNameStartsWith`, `RequireNameEndsWith`, `RequireNameMatching`)과 인터페이스 구현 규칙을 공유합니다.

---

> [← 이전: 2장 가시성과 한정자](../02-Visibility-And-Modifiers/) | [다음: 4장 상속과 인터페이스 →](../04-Inheritance-And-Interface/)
