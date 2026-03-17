---
title: "애플리케이션 레이어 규칙"
---

## 개요

Command의 `Request`가 `record`가 아닌 일반 클래스로 작성되었다면? `Response`에 public setter가 있다면? Mediator 파이프라인은 이런 구조적 문제를 런타임에서야 드러냅니다. Command/Query 패턴의 규칙이 지켜지지 않으면, 파이프라인 전체가 불안정해집니다.

이 챕터에서는 Command/Query 패턴 기반의 애플리케이션 레이어를 아키텍처 테스트로 검증합니다. 각 유스케이스가 **중첩 클래스(Request, Response, Usecase)로 구성되는 패턴을 자동으로 강제**합니다.

> **"유스케이스 하나에 Request, Response, Usecase를 묶는 패턴은 강력하지만, 누군가 구조를 어기면 파이프라인이 깨집니다. 테스트가 구조를 지켜줍니다."**

## 학습 목표

### 핵심 학습 목표

1. **Command/Query 패턴의 구조적 규칙 정의**
   - 각 유스케이스가 하나의 sealed 클래스 안에 Request, Response, Usecase를 중첩
   - 관련 타입을 하나의 단위로 묶어 응집도를 높이는 패턴

2. **`RequireNestedClass`와 `RequireRecord`의 조합**
   - `RequireNestedClass`로 중첩 클래스 존재 여부 검증
   - `RequireRecord()`로 Request/Response가 record 타입인지 강제

3. **DTO 클래스의 프로퍼티 규칙 검증**
   - `RequireNoPublicSetters()`로 `init` 전용 프로퍼티를 강제
   - DTO의 불변성을 보장하는 패턴

### 실습을 통해 확인할 내용
- **CreateOrder (Command)**: sealed 클래스 안에 sealed record Request/Response + sealed Usecase
- **GetOrderById (Query)**: 동일한 중첩 구조 검증
- **OrderDto**: sealed, no public setters 규칙 검증

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

## 한눈에 보는 정리

다음 표는 애플리케이션 레이어의 각 검증 대상별 필터 전략과 규칙을 비교합니다.

### 애플리케이션 레이어 검증 규칙

| 대상 | 필터 전략 | 검증 규칙 | 핵심 의도 |
|------|-----------|-----------|-----------|
| **Command/Query** | `HaveName` (특정 클래스) | sealed, 중첩 Request/Response/Usecase | 유스케이스 구조 통일 |
| **Request/Response** | `RequireNestedClass` 내부 검증 | sealed record | 불변 DTO 보장 |
| **Usecase** | `HaveNameEndingWith("Usecase")` | sealed, 인터페이스 구현 | 파이프라인 호환성 |
| **DTO** | `ResideInNamespace(DtoNamespace)` | sealed, no public setters | 외부 노출 데이터 불변성 |

다음 표는 중첩 클래스 검증에서 `RequireRecord()`와 `RequireImmutable()`의 차이를 보여줍니다.

### Record vs Immutable 검증 비교

| 검증 메서드 | 검증 내용 | 적합한 대상 |
|-------------|-----------|-------------|
| **`RequireRecord()`** | C# record 타입 여부 | Request, Response (간결한 DTO) |
| **`RequireImmutable()`** | 6가지 차원의 불변성 | 도메인 객체 (복잡한 불변 클래스) |

## Functorium 사전 구축 테스트 스위트

Functorium은 도메인/애플리케이션 레이어의 아키텍처 규칙을 **abstract class로 사전 구축**하여 제공합니다. 프로젝트에서 상속만 하면 규칙이 자동 적용됩니다.

| 스위트 | 테스트 수 | 검증 대상 |
|--------|-----------|-----------|
| `DomainArchitectureTestSuite` | 21 | AggregateRoot, Entity, ValueObject, DomainEvent, Specification, DomainService |
| `ApplicationArchitectureTestSuite` | 4 | Command/Query의 Validator, Usecase 중첩 클래스 |

### 사용 방법

두 Suite 모두 `Architecture`와 네임스페이스만 오버라이드하면 됩니다:

```csharp
public sealed class DomainArchTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader().LoadAssemblies(typeof(Order).Assembly).Build();

    protected override string DomainNamespace { get; } =
        typeof(Order).Namespace!;
}

public sealed class ApplicationArchTests : ApplicationArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader().LoadAssemblies(typeof(CreateOrderCommand).Assembly).Build();

    protected override string ApplicationNamespace { get; } =
        "MyApp.Application";
}
```

### ApplicationArchitectureTestSuite (4 tests)

`ApplicationArchitectureTestSuite`는 Command/Query 패턴의 구조를 자동 검증합니다:

1. **Command_ShouldHave_ValidatorNestedClass** — Command에 Validator가 있으면 sealed + `AbstractValidator` 구현
2. **Command_ShouldHave_UsecaseNestedClass** — Command에 Usecase 필수, sealed + `ICommandUsecase` 구현
3. **Query_ShouldHave_ValidatorNestedClass** — Query에 Validator가 있으면 sealed + `AbstractValidator` 구현
4. **Query_ShouldHave_UsecaseNestedClass** — Query에 Usecase 필수, sealed + `IQueryUsecase` 구현

`RequireImplementsGenericInterface("ICommandUsecase")` / `RequireImplementsGenericInterface("IQueryUsecase")`로 제네릭 인터페이스 구현을 검증합니다. `RequireNestedClassIfExists`는 Validator처럼 선택적 중첩 클래스에, `RequireNestedClass`는 Usecase처럼 필수 중첩 클래스에 사용합니다.

Suite의 상세한 사용법, virtual 프로퍼티 커스터마이징, 수동 규칙과의 비교는 [4-05 아키텍처 테스트 스위트](../05-Architecture-Test-Suites/)에서 실습합니다.

### 커스텀 규칙 추가

Suite를 상속한 후 프로젝트별 추가 규칙을 자유롭게 정의할 수 있습니다:

```csharp
public sealed class DomainArchTests : DomainArchitectureTestSuite
{
    // Suite의 21개 규칙 자동 상속

    // 프로젝트별 추가 규칙
    [Fact]
    public void Entity_ShouldNotDependOn_ExternalHttpClient()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoDependencyOn("HttpClient"),
                verbose: true)
            .ThrowIfAnyFailures("Entity No HttpClient Rule");
    }
}
```

## FAQ

### Q1: Request/Response를 record로 강제하는 이유는 무엇인가요?
**A**: record는 값 기반 동등성, `ToString()`, 디컨스트럭션을 자동으로 제공합니다. Request/Response처럼 단순한 데이터 전달 객체에는 record가 가장 적합합니다. 또한 `sealed record`는 기본적으로 `init` 전용 프로퍼티를 생성하므로 불변성도 보장됩니다.

### Q2: 중첩 클래스 패턴 대신 별도 파일로 분리하면 안 되나요?
**A**: 기술적으로 가능하지만, `CreateOrder.Request`, `CreateOrder.Response`, `CreateOrder.Usecase`처럼 중첩하면 관련 타입이 하나의 네임스페이스 아래 응집됩니다. IDE에서 `CreateOrder.`만 입력하면 관련 타입이 모두 나타나는 것이 큰 장점입니다.

### Q3: `RequireNoPublicSetters()`와 `RequireImmutable()`의 차이는 무엇인가요?
**A**: `RequireNoPublicSetters()`는 public setter가 없는지만 검사합니다. DTO처럼 `init` 프로퍼티를 사용하는 경우에 적합합니다. `RequireImmutable()`은 생성자, 필드, 가변 컬렉션, 상태 변경 메서드까지 6가지 차원을 검증하므로 도메인 객체에 더 적합합니다.

### Q4: Usecase 클래스에 인터페이스 구현 검증은 어떻게 하나요?
**A**: `RequireNestedClass("Usecase", nested => nested.RequireSealed().RequireImplements("ICommandUsecase"))`처럼 `RequireImplements()`를 체이닝하면 특정 인터페이스 구현을 강제할 수 있습니다.

---

애플리케이션 레이어의 Command/Query 구조를 테스트로 강제하면, 새로운 유스케이스를 추가할 때마다 동일한 패턴이 자동으로 보장됩니다. 다음 장에서는 포트 인터페이스와 어댑터 구현체의 관계를 검증하는 방법을 살펴봅니다.

> [다음: 3장 - Adapter Layer Rules](../03-Adapter-Layer-Rules/)
