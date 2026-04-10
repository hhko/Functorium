---
title: "어댑터 레이어 규칙"
---

## 개요

포트 인터페이스는 도메인이 외부와 소통하는 계약입니다. 그런데 어댑터 구현체가 포트를 구현하지 않거나, 포트 인터페이스에 `I` 접두사가 빠져 있거나, 도메인이 어댑터에 직접 의존하고 있다면 — Hexagonal Architecture의 근본이 무너집니다.

이 챕터에서는 Functorium의 `InterfaceValidator`와 ArchUnitNET의 레이어 의존성 규칙을 함께 활용하여, **포트 인터페이스와 어댑터 구현체의 구조적 관계를 자동으로 검증**하는 방법을 학습합니다.

> **"포트와 어댑터의 관계는 아키텍처 다이어그램에만 존재하는 것이 아닙니다. 테스트가 이 관계를 코드 수준에서 강제해야, 다이어그램과 실제 코드가 일치합니다."**

## 학습 목표

### 핵심 학습 목표

1. **`ValidateAllInterfaces`로 포트 인터페이스 규칙 검증**
   - `RequireNameStartsWith("I")`로 네이밍 규칙 강제
   - `Domains.Ports` 네임스페이스 기반 필터링

2. **ArchUnitNET의 `NotDependOnAnyTypesThat`로 레이어 간 의존성 검증**
   - 도메인이 어댑터에 의존하지 않음을 자동으로 검증
   - `.Check(Architecture)`로 규칙 위반 시 테스트 실패

3. **`RequireVirtual()`로 Port 구현체의 확장성 보장**
   - 데코레이터 패턴 지원을 위해 `IObservablePort` 구현체에 virtual 메서드 강제
   - `RequireNotSealed()`과 `RequireVirtual()` 조합

4. **Functorium API와 ArchUnitNET 네이티브 API의 역할 분담**
   - Functorium: 타입 내부 구조 검증 (네이밍, 멤버, 불변성)
   - ArchUnitNET: 타입 간 관계 검증 (의존성, 상속)

### 실습을 통해 확인할 내용
- **IOrderRepository, INotificationService**: 포트 인터페이스의 `I` 접두사 검증
- **OrderRepository**: `IObservablePort` 구현체의 virtual 메서드, not sealed 검증
- **레이어 의존성**: Domain -> Adapter 의존 금지 검증

## 도메인 코드 구조

```
Domains/
├── Order.cs
└── Ports/
    ├── IObservablePort.cs        # 관측성 마커 인터페이스
    ├── IOrderRepository.cs       # 포트 인터페이스
    └── INotificationService.cs   # 포트 인터페이스
Adapters/
├── Persistence/
│   └── OrderRepository.cs        # IObservablePort 구현 (non-sealed, virtual)
└── Infrastructure/
    └── EmailNotificationService.cs  # 어댑터 구현체 (sealed)
```

**포트(Port)는** 도메인이 외부와 소통하는 인터페이스입니다. `Domains.Ports` 네임스페이스에 위치합니다.

**어댑터(Adapter)는** 포트의 구체적인 구현입니다. `Adapters` 하위 네임스페이스에 위치하며, 반드시 포트 인터페이스를 구현해야 합니다.

## 테스트 코드 설명

### 포트 인터페이스 네이밍 규칙

`ValidateAllInterfaces`와 `RequireNameStartsWith`를 사용하여 모든 포트가 `I` 접두사를 가지는지 검증합니다:

```csharp
Interfaces()
    .That()
    .ResideInNamespace(PortNamespace)
    .ValidateAllInterfaces(Architecture, @interface => @interface
        .RequireNameStartsWith("I"),
        verbose: true)
    .ThrowIfAnyFailures("Port Interface Naming Rule");
```

### 레이어 의존성 규칙

ArchUnitNET의 네이티브 API를 사용하여 도메인이 어댑터에 의존하지 않음을 검증합니다:

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

Types()
    .That()
    .ResideInNamespace(DomainNamespace)
    .Should()
    .NotDependOnAnyTypesThat()
    .ResideInNamespace(AdapterNamespace)
    .Check(Architecture);
```

`.Check(Architecture)`는 ArchUnitNET xUnitV3 패키지가 제공하는 확장 메서드로, 규칙 위반 시 xUnit 테스트를 실패시킵니다.

## Port 구현체의 확장성 보장

### 왜 virtual 메서드가 필요한가?

Observability(관측성) 패턴에서는 어댑터를 **데코레이터(Decorator)로 감싸** 로깅, 메트릭, 트레이싱을 투명하게 추가합니다. 이때 원본 어댑터의 메서드가 `virtual`이어야 데코레이터가 오버라이드할 수 있습니다.

`IObservablePort` 마커 인터페이스를 구현하는 어댑터는 sealed가 아니고, 모든 메서드가 virtual이어야 합니다:

```csharp
// IObservablePort를 구현하는 어댑터는 데코레이터 패턴을 지원
public class OrderRepository : IOrderRepository, IObservablePort
{
    public virtual Task<Order?> GetByIdAsync(string id) => ...;
    public virtual Task SaveAsync(Order order) => ...;
}
```

### RequireVirtual 테스트

`RequireNotSealed()`과 `RequireVirtual()`을 조합하여 데코레이터 패턴 지원을 강제합니다:

```csharp
[Fact]
public void ObservablePortAdapters_ShouldHave_VirtualMethods()
{
    ArchRuleDefinition.Classes()
        .That()
        .ImplementInterface(typeof(IObservablePort))
        .And().AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNotSealed()
            .RequireAllMethods(method => method
                .RequireVirtual()),
            verbose: true)
        .ThrowIfAnyFailures("Observable Port Adapter Virtual Methods Rule");
}
```

`IObservablePort`를 구현하지 않는 단순 어댑터(`EmailNotificationService`)는 여전히 sealed로 유지됩니다. sealed/non-sealed 구분은 데코레이터 패턴 지원 여부에 의해 결정됩니다.

## 한눈에 보는 정리

다음 표는 어댑터 레이어 검증의 대상별 도구와 규칙을 비교합니다.

### 어댑터 레이어 검증 규칙

| 대상 | 검증 도구 | 검증 규칙 | 핵심 의도 |
|------|-----------|-----------|-----------|
| **Port Interface** | Functorium `ValidateAllInterfaces` | `I` 접두사 네이밍 | 네이밍 컨벤션 통일 |
| **Adapter** | Functorium `ValidateAllClasses` | public | 구현체 구조 통일 |
| **Observable Port Adapter** | Functorium `ValidateAllClasses` | not sealed, virtual 메서드 | 데코레이터 패턴 지원 |
| **레이어 의존성** | ArchUnitNET `.Check()` | Domain -> Adapter 의존 금지 | 의존성 역전 보장 |

다음 표는 두 도구의 역할 분담을 정리합니다.

### Functorium vs ArchUnitNET 역할 분담

| 검증 유형 | 적합한 도구 | 예시 |
|-----------|-------------|------|
| **타입 내부 구조** | Functorium | sealed, immutable, 네이밍, 멤버 검증 |
| **타입 간 관계** | ArchUnitNET 네이티브 API | 의존성 방향, 상속 관계 |
| **복합 검증** | 두 도구 결합 | 구조 + 의존성 동시 검증 |

## FAQ

### Q1: Functorium의 `ValidateAllInterfaces`와 ArchUnitNET의 네이티브 API를 함께 사용하는 이유는 무엇인가요?
**A**: Functorium은 타입 내부 구조(네이밍, 멤버, 불변성)를 검증하는 데 특화되어 있고, ArchUnitNET 네이티브 API는 타입 간 의존성 관계를 검증하는 데 특화되어 있습니다. 어댑터 레이어는 두 가지 모두 필요하므로 함께 사용합니다.

### Q2: `.Check(Architecture)`와 `.ThrowIfAnyFailures()`의 차이는 무엇인가요?
**A**: `.Check(Architecture)`는 ArchUnitNET 네이티브 API의 검증 실행 메서드입니다. `.ThrowIfAnyFailures()`는 Functorium의 `ValidateAllClasses`/`ValidateAllInterfaces` 체인의 종료 메서드입니다. 각각 자신의 API 체인에서 사용됩니다.

### Q3: 포트 인터페이스에 `I` 접두사 외에 다른 네이밍 규칙도 적용할 수 있나요?
**A**: 네, `RequireNameEndsWith("Repository")`나 `RequireNameContains("Service")` 등을 추가로 체이닝할 수 있습니다. 포트의 역할에 따라 접미사 규칙을 더 세분화할 수 있습니다.

### Q4: 왜 IObservablePort 구현체는 sealed가 아닌가요?
**A**: 데코레이터 패턴을 지원하기 위해서입니다. Observability(관측성) 레이어가 원본 어댑터를 감싸 로깅, 메트릭, 트레이싱을 투명하게 추가하려면, 원본 메서드를 오버라이드할 수 있어야 합니다. sealed 클래스는 상속이 불가능하고, virtual이 아닌 메서드는 오버라이드할 수 없으므로, IObservablePort 구현체는 `RequireNotSealed()`과 `RequireVirtual()`로 확장성을 강제합니다.

### Q5: 어댑터가 다른 어댑터에 의존해도 되나요?
**A**: 일반적으로 어댑터 간 직접 의존은 권장하지 않습니다. 하지만 기술적으로 같은 레이어이므로 ArchUnitNET 규칙에서 금지하지 않습니다. 필요하다면 `Types().That().ResideInNamespace(AdapterNamespace).Should().NotDependOnAnyTypesThat().ResideInNamespace(OtherAdapterNamespace)`로 제한할 수 있습니다.

---

포트와 어댑터의 관계를 테스트로 강제하면, Hexagonal Architecture의 의존성 역전 원칙이 코드 수준에서 보장됩니다. 다음 장에서는 모든 레이어 간 의존성 방향을 종합적으로 검증하는 방법을 살펴봅니다.

→ [4장: 레이어 의존성 규칙](../04-Layer-Dependency-Rules/)
