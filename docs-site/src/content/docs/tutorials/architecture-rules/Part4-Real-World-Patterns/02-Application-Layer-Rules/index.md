---
title: "Chapter 14: 애플리케이션 레이어 규칙"
---

## 소개

Command/Query 패턴 기반의 애플리케이션 레이어를 아키텍처 테스트로 검증합니다. 각 유스케이스가 **중첩 클래스(Request, Response, Usecase)로** 구성되는 패턴을 강제합니다.

## 학습 목표

- Command/Query 패턴의 구조적 규칙을 정의할 수 있다
- `RequireNestedClass`로 중첩 클래스 존재 여부를 검증할 수 있다
- `RequireRecord`로 record 타입을 강제할 수 있다
- DTO 클래스의 프로퍼티 규칙을 검증할 수 있다

## 도메인 코드 구조

### Command/Query 패턴

```
Applications/
├── ICommandUsecase.cs    # Command 인터페이스
├── IQueryUsecase.cs      # Query 인터페이스
├── CreateOrder.cs        # Command (중첩: Request, Response, Usecase)
├── GetOrderById.cs       # Query (중첩: Request, Response, Usecase)
└── Dtos/
    └── OrderDto.cs       # DTO
```

각 유스케이스는 하나의 sealed 클래스 안에 관련 타입을 중첩합니다:

```csharp
public sealed class CreateOrder
{
    public sealed record Request(string CustomerName);
    public sealed record Response(Guid OrderId, bool Success);

    public sealed class Usecase : ICommandUsecase<Request>
    {
        public Task ExecuteAsync(Request request) => Task.CompletedTask;
    }
}
```

이 패턴은 **관련된 타입을 하나의 단위로 묶어** 응집도를 높입니다.

## 테스트 코드 설명

### 중첩 클래스 구조 검증

`HaveName`으로 특정 클래스를 선택한 후, `RequireNestedClass`로 내부 구조를 검증합니다:

```csharp
ArchRuleDefinition.Classes()
    .That()
    .HaveName("CreateOrder")
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNestedClass("Request", nested => nested
            .RequireSealed()
            .RequireRecord())
        .RequireNestedClass("Response", nested => nested
            .RequireSealed()
            .RequireRecord())
        .RequireNestedClass("Usecase", nested => nested
            .RequireSealed()),
        verbose: true)
    .ThrowIfAnyFailures("Command Structure Rule");
```

### DTO 프로퍼티 규칙

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DtoNamespace)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNoPublicSetters(),
        verbose: true)
    .ThrowIfAnyFailures("DTO Rule");
```

**`RequireNoPublicSetters()`는** DTO가 `init` 전용 프로퍼티만 가지도록 강제합니다. `set`이 아닌 `init`을 사용하면 객체 초기화 시에만 값을 설정할 수 있습니다.

## 핵심 정리

| 대상 | 필터 전략 | 검증 규칙 |
|------|-----------|-----------|
| Command/Query | `HaveName` (특정 클래스) | sealed, 중첩 Request/Response/Usecase |
| Request/Response | `RequireNestedClass` 내부 검증 | sealed record |
| Usecase | `HaveNameEndingWith("Usecase")` | sealed, 인터페이스 구현 |
| DTO | `ResideInNamespace(DtoNamespace)` | sealed, no public setters |

---

[이전: Chapter 13 - Domain Layer Rules](../01-Domain-Layer-Rules/) | [다음: Chapter 15 - Adapter Layer Rules](../03-Adapter-Layer-Rules/)
