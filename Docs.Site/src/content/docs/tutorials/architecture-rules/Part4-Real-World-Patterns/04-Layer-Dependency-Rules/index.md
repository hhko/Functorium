---
title: "레이어 의존성 규칙"
---

## 소개

다중 레이어 프로젝트에서 **레이어 간 의존성 방향을** 아키텍처 테스트로 강제합니다. ArchUnitNET의 네이티브 의존성 검사와 Functorium의 `ClassValidator`를 결합하여 각 레이어의 구조와 의존성을 동시에 검증합니다.

## 학습 목표

- 3-레이어 아키텍처의 의존성 방향을 테스트로 강제할 수 있다
- ArchUnitNET 네이티브 API와 Functorium API를 결합할 수 있다
- 각 레이어별 클래스 규칙을 정의할 수 있다
- `DoNotResideInNamespace`로 특정 네임스페이스를 제외할 수 있다

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

## 핵심 정리

| 검증 유형 | 도구 | 사용 방법 |
|-----------|------|-----------|
| 레이어 의존성 | ArchUnitNET `.Check()` | `NotDependOnAnyTypesThat().ResideInNamespace()` |
| 클래스 구조 | Functorium `ValidateAllClasses` | `RequirePublic().RequireSealed()` |
| 네임스페이스 제외 | ArchUnitNET 필터 | `DoNotResideInNamespace()` |

### 두 도구의 역할 분담

- **ArchUnitNET 네이티브 API:** 레이어 간 의존성 방향 검증
- **Functorium ClassValidator:** 각 레이어 내 클래스 구조 검증

두 도구를 결합하면 의존성 방향과 내부 구조를 모두 자동으로 강제할 수 있습니다.

---

[이전: Chapter 15 - Adapter Layer Rules](../03-Adapter-Layer-Rules/) | [다음: Part 5 - Conclusion](../../Part5-Conclusion/)
