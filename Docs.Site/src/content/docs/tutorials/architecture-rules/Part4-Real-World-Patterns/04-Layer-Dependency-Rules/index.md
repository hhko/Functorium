---
title: "레이어 의존성 규칙"
---

## 개요

도메인 레이어가 인프라에 의존하는 순간 — 아키텍처의 근본이 무너집니다. "Domain은 아무것도 의존하지 않는다", "Application은 Domain에만 의존한다" — 이 간단한 규칙이 지켜지지 않으면, 레이어 분리는 폴더 구조에 불과합니다.

이 챕터에서는 ArchUnitNET의 네이티브 의존성 검사와 Functorium의 `ClassValidator`를 결합하여, **다중 레이어의 의존성 방향과 내부 구조를 동시에 검증**하는 방법을 학습합니다.

> **"아키텍처 다이어그램에 화살표를 그리는 것은 쉽습니다. 하지만 그 화살표가 코드에서도 지켜지는지 확인하려면, 테스트가 필요합니다."**

## 학습 목표

### 핵심 학습 목표

1. **3-레이어 아키텍처의 의존성 방향을 테스트로 강제**
   - Domain -> Application 의존 금지
   - Domain -> Adapter 의존 금지
   - Application -> Adapter 의존 금지

2. **ArchUnitNET 네이티브 API와 Functorium API의 결합**
   - 의존성 방향은 ArchUnitNET으로, 클래스 구조는 Functorium으로 검증
   - 하나의 테스트 스위트에서 두 도구를 함께 사용

3. **`DoNotResideInNamespace`로 특정 네임스페이스 제외**
   - Ports 하위 네임스페이스를 도메인 클래스 규칙에서 제외
   - 포트 인터페이스는 별도의 규칙으로 검증

### 실습을 통해 확인할 내용
- **Domain -> Application 의존 금지**: `NotDependOnAnyTypesThat` 검증
- **Domain -> Adapter 의존 금지**: 의존성 역전 원칙 보장
- **Application -> Adapter 의존 금지**: 애플리케이션 레이어 순수성 유지
- **레이어별 클래스 구조 검증**: `DoNotResideInNamespace`로 Ports 제외 후 검증

## 도메인 코드 구조

```
Domains/
├── Product.cs
└── Ports/
    └── IProductRepository.cs
Applications/
└── GetProduct.cs           # Request, Response, Usecase 중첩
Adapters/
├── Persistence/
│   └── ProductRepository.cs
└── Presentation/
    └── ProductEndpoint.cs
```

### 레이어 의존성 방향

```
Domain  <--  Application  <--  Adapter
  |                               |
  +-- Ports (인터페이스) ----------+
```

- **Domain은** 어떤 레이어에도 의존하지 않습니다
- **Application은** Domain에만 의존합니다 (Port 인터페이스 사용)
- **Adapter는** Domain과 Application 모두에 의존할 수 있습니다

## 테스트 코드 설명

### ArchUnitNET 레이어 의존성 검증

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

[Fact]
public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
{
    Types()
        .That()
        .ResideInNamespace(DomainNamespace)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespace(ApplicationNamespace)
        .Check(Architecture);
}

[Fact]
public void DomainLayer_ShouldNotDependOn_AdapterLayer()
{
    Types()
        .That()
        .ResideInNamespace(DomainNamespace)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespace(AdapterNamespace)
        .Check(Architecture);
}

[Fact]
public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
{
    Types()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .Should()
        .NotDependOnAnyTypesThat()
        .ResideInNamespace(AdapterNamespace)
        .Check(Architecture);
}
```

### Functorium 클래스 검증과 결합

레이어 의존성 규칙과 함께 각 레이어의 클래스 구조도 검증합니다:

```csharp
[Fact]
public void DomainClasses_ShouldBe_PublicAndSealed()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .DoNotResideInNamespace(DomainNamespace + ".Ports")
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic()
            .RequireSealed(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Rule");
}
```

`DoNotResideInNamespace(DomainNamespace + ".Ports")`로 **Ports 하위 네임스페이스를 제외합니다.** 포트 인터페이스는 별도의 규칙으로 검증하므로 도메인 클래스 규칙에서 제외해야 합니다.

## 한눈에 보는 정리

다음 표는 3-레이어 아키텍처의 의존성 규칙을 요약합니다.

### 레이어 의존성 규칙 매트릭스

| 의존하는 레이어 | Domain | Application | Adapter |
|-----------------|--------|-------------|---------|
| **Domain** | - | 금지 | 금지 |
| **Application** | 허용 | - | 금지 |
| **Adapter** | 허용 | 허용 | - |

다음 표는 각 검증 유형별 사용 도구를 정리합니다.

### 두 도구의 역할 분담

| 검증 유형 | 도구 | 사용 방법 |
|-----------|------|-----------|
| **레이어 의존성** | ArchUnitNET `.Check()` | `NotDependOnAnyTypesThat().ResideInNamespace()` |
| **클래스 구조** | Functorium `ValidateAllClasses` | `RequirePublic().RequireSealed()` |
| **네임스페이스 제외** | ArchUnitNET 필터 | `DoNotResideInNamespace()` |

## FAQ

### Q1: Domain이 Application에 의존하면 구체적으로 어떤 문제가 발생하나요?
**A**: Domain이 Application에 의존하면 순환 의존성이 생깁니다. Domain을 단독으로 테스트할 수 없게 되고, Domain의 변경이 Application에 영향을 주고 그 반대도 성립합니다. 결국 두 레이어가 하나의 덩어리가 되어 독립적인 배포와 테스트가 불가능해집니다.

### Q2: `DoNotResideInNamespace`로 Ports를 제외하는 이유는 무엇인가요?
**A**: Ports 네임스페이스에는 인터페이스만 존재합니다. `RequirePublic().RequireSealed()` 같은 클래스 규칙은 인터페이스에 적용할 수 없으므로 제외합니다. 포트 인터페이스는 `ValidateAllInterfaces`로 별도 검증합니다.

### Q3: Adapter가 다른 Adapter에 의존하는 것은 허용되나요?
**A**: 이 예제에서는 Adapter 간 의존성을 별도로 제한하지 않습니다. 하지만 프로젝트 규칙에 따라 `Adapters.Persistence`가 `Adapters.Presentation`에 의존하지 않도록 추가 규칙을 정의할 수 있습니다.

### Q4: ArchUnitNET과 Functorium을 같은 테스트 클래스에서 사용할 수 있나요?
**A**: 네, 둘 다 동일한 `Architecture` 인스턴스를 공유합니다. 하나의 테스트 클래스에서 `Types().That()...Check(Architecture)`와 `Classes().That()...ValidateAllClasses(Architecture, ...)`를 함께 사용할 수 있습니다.

### Q5: 레이어가 3개 이상(예: Infrastructure, Presentation 분리)일 때는 어떻게 하나요?
**A**: 동일한 패턴을 확장하면 됩니다. 각 레이어 쌍에 대해 `NotDependOnAnyTypesThat().ResideInNamespace()` 규칙을 추가합니다. 레이어가 많아질수록 의존성 매트릭스를 먼저 정의하고, 각 금지 조합에 대해 테스트를 작성하는 것이 좋습니다.

---

레이어 의존성 규칙은 아키텍처의 가장 기본적인 보호장치입니다. ArchUnitNET으로 의존성 방향을, Functorium으로 내부 구조를 동시에 검증하면, 아키텍처 다이어그램과 실제 코드가 항상 일치하게 됩니다.

다음 Part 5에서는 이 튜토리얼에서 배운 모든 내용을 정리하고, 실전 프로젝트에 적용하는 전략을 다룹니다.

> [다음: Part 5 - Conclusion](../../Part5-Conclusion/)
