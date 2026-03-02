# Chapter 15: 어댑터 레이어 규칙

## 소개

포트 & 어댑터(Hexagonal) 아키텍처에서 **포트 인터페이스와 어댑터 구현체의** 구조적 규칙을 아키텍처 테스트로 검증합니다. Functorium의 `InterfaceValidator`와 ArchUnitNET의 레이어 의존성 규칙을 함께 활용합니다.

## 학습 목표

- `ValidateAllInterfaces`로 인터페이스 규칙을 검증할 수 있다
- `RequireNameStartsWith`로 네이밍 규칙을 강제할 수 있다
- ArchUnitNET의 `NotDependOnAnyTypesThat`로 레이어 간 의존성을 검증할 수 있다
- 도메인 레이어가 어댑터 레이어에 의존하지 않음을 자동으로 검증할 수 있다

## 도메인 코드 구조

```
Domains/
├── Order.cs
└── Ports/
    ├── IOrderRepository.cs       # 포트 인터페이스
    └── INotificationService.cs   # 포트 인터페이스
Adapters/
├── Persistence/
│   └── OrderRepository.cs        # 어댑터 구현체
└── Infrastructure/
    └── EmailNotificationService.cs  # 어댑터 구현체
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

## 핵심 정리

| 대상 | 검증 도구 | 검증 규칙 |
|------|-----------|-----------|
| Port Interface | `ValidateAllInterfaces` | `I` 접두사 네이밍 |
| Adapter | `ValidateAllClasses` | public, sealed |
| 레이어 의존성 | ArchUnitNET `.Check()` | Domain -> Adapter 의존 금지 |

---

[이전: Chapter 14 - Application Layer Rules](../02-Application-Layer-Rules/README.md) | [다음: Chapter 16 - Layer Dependency Rules](../04-Layer-Dependency-Rules/README.md)
