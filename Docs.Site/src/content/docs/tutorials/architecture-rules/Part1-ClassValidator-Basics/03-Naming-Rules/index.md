---
title: "네이밍 규칙"
---
## 개요

팀에서 서비스 클래스는 `Service` 접미사, 이벤트 클래스는 `Event` 접미사를 붙이기로 합의했습니다. 그런데 어느 날 누군가 `OrderSvc`라는 클래스를 만들었고, 다른 누군가는 `ProductChanged`라는 이벤트를 만들었습니다. 코드 리뷰에서 잡힐 수도 있지만, 잡히지 않으면 점점 일관성이 무너집니다. 이 장에서는 **네이밍 규칙을** 아키텍처 테스트로 자동화하여 이런 불일치를 원천 차단하는 방법을 배웁니다.

> **"네이밍 규칙은 코드의 가독성과 탐색성을 결정합니다. 자동화된 검증이 없으면, 규칙은 시간이 지나면서 점차 흐려집니다."**

## 학습 목표

### 핵심 학습 목표
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

### 실습을 통해 확인할 내용
- 네임스페이스별 접미사 규칙 적용 (Service, Event, Dto)
- `ValidateAllInterfaces`를 사용한 인터페이스 접두사 검증
- 정규식 패턴을 사용한 복합 네이밍 규칙

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

## 한눈에 보는 정리

다음 표는 네이밍 규칙 검증 메서드를 정리합니다.

### 네이밍 검증 메서드

| 메서드 | 검증 내용 | 사용 시나리오 |
|--------|----------|--------------|
| `RequireNameEndsWith(suffix)` | 이름이 지정된 접미사로 끝남 | Service, Event, Dto 등 |
| `RequireNameStartsWith(prefix)` | 이름이 지정된 접두사로 시작 | I (인터페이스), Abstract 등 |
| `RequireNameMatching(regex)` | 정규식 패턴과 일치 | 복합 네이밍 규칙 |
| `ValidateAllInterfaces` | 인터페이스 대상 검증 진입점 | 인터페이스 규칙 적용 |

### ClassValidator vs InterfaceValidator

두 Validator는 **공통 기반 클래스인 `TypeValidator`를** 상속합니다. 네이밍 규칙(`RequireNameStartsWith`, `RequireNameEndsWith`, `RequireNameMatching`)과 인터페이스 구현 규칙은 `TypeValidator`에 정의되어 있으므로 두 Validator에서 동일하게 사용할 수 있습니다. 차이점은 진입점입니다:

- **`ValidateAllClasses`** -- `ClassValidator`를 사용하여 클래스 검증
- **`ValidateAllInterfaces`** -- `InterfaceValidator`를 사용하여 인터페이스 검증

## FAQ

### Q1: `RequireNameEndsWith`와 `HaveNameEndingWith`의 차이는 무엇인가요?
**A**: `HaveNameEndingWith`는 ArchUnitNET의 **필터링** 메서드로, 검증 대상을 좁히는 역할입니다. `RequireNameEndsWith`는 Functorium의 **규칙 검증** 메서드로, 선택된 대상이 조건을 만족하는지 확인합니다. 필터링은 "어떤 클래스를 검사할지", 규칙은 "그 클래스가 어떤 조건을 만족해야 하는지"를 결정합니다.

### Q2: 대소문자를 구분하지 않는 네이밍 검증도 가능한가요?
**A**: `RequireNameMatching`에서 정규식 옵션으로 대소문자 무시를 지정할 수 있습니다. 예를 들어 `RequireNameMatching("(?i).*service$")`는 `OrderService`, `orderservice` 모두 매칭합니다.

### Q3: 네이밍 규칙과 가시성 규칙을 하나의 체인으로 결합할 수 있나요?
**A**: 네, 가능합니다. `@class.RequirePublic().RequireNameEndsWith("Service")`처럼 가시성과 네이밍 규칙을 하나의 Validator 체인에서 결합할 수 있습니다. 관련된 규칙을 하나로 묶으면 규칙의 의도가 더 명확해집니다.

### Q4: 다음 장에서는 무엇을 배우나요?
**A**: 4장에서는 클래스의 **상속 관계와 인터페이스 구현을** 검증합니다. `RequireInherits(typeof(Entity<>))`로 기본 클래스 상속을 강제하고, `RequireImplements`로 필수 인터페이스 구현을 검증합니다.

---

다음 장에서는 클래스의 상속 관계와 인터페이스 구현을 아키텍처 테스트로 검증하는 방법을 배웁니다.
