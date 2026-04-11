---
title: "가시성과 한정자"
---
## Overview

내부 구현용으로 만든 캐시 클래스가 실수로 `public`이 되면 어떤 일이 벌어질까요? 외부 모듈에서 직접 참조하기 시작하고, 나중에 내부 구현을 바꾸려 할 때 이미 여러 곳에서 의존하고 있어 변경이 불가능해집니다. In this chapter, 이런 문제를 방지하기 위해 **가시성과 한정자를** 아키텍처 테스트로 강제하는 방법을 학습합니다.

> **"클래스의 가시성과 한정자는 설계 의도의 표현입니다. 아키텍처 테스트는 이 의도가 코드베이스 전체에서 일관되게 유지되도록 guarantees."**

## Learning Objectives

### 핵심 학습 목표
1. **가시성(Visibility) 검증**
   - `RequirePublic()`: 외부에 공개해야 하는 클래스 검증
   - `RequireInternal()`: 내부 구현 클래스 검증
2. **한정자(Modifier) 검증**
   - `RequireSealed()` / `RequireNotSealed()`: sealed 여부 검증
   - `RequireAbstract()` / `RequireNotAbstract()`: abstract 여부 검증
   - `RequireStatic()` / `RequireNotStatic()`: static 여부 검증
3. **타입 종류 검증**
   - `RequireRecord()` / `RequireNotRecord()`: record 타입 여부 검증

### 실습을 통해 확인할 내용
- 네임스페이스별로 가시성 규칙을 분리하여 적용
- `abstract`, `sealed`, `static`, `record` 한정자를 각각 검증

## 프로젝트 구조

```
02-Visibility-And-Modifiers/
├── VisibilityAndModifiers/                   # 메인 프로젝트
│   ├── Domains/
│   │   ├── Order.cs                          # public sealed class
│   │   ├── OrderSummary.cs                   # public sealed record
│   │   ├── DomainEvent.cs                    # public abstract class
│   │   └── OrderCreatedEvent.cs              # sealed class (DomainEvent 상속)
│   ├── Services/
│   │   └── OrderFormatter.cs                 # public static class
│   ├── Internal/
│   │   └── OrderCache.cs                     # internal sealed class
│   ├── Program.cs
│   └── VisibilityAndModifiers.csproj
├── VisibilityAndModifiers.Tests.Unit/        # 테스트 프로젝트
│   ├── ArchitectureTests.cs
│   ├── VisibilityAndModifiers.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## 검증 대상 코드

프로젝트는 다양한 가시성과 한정자를 가진 클래스들로 구성됩니다:

| 클래스 | 네임스페이스 | 가시성 | 한정자 | 타입 |
|--------|-------------|--------|--------|------|
| `Order` | Domains | public | sealed | class |
| `OrderSummary` | Domains | public | sealed | record |
| `DomainEvent` | Domains | public | abstract | class |
| `OrderCreatedEvent` | Domains | public | sealed | class |
| `OrderFormatter` | Services | public | static | class |
| `OrderCache` | Internal | internal | sealed | class |

## 테스트 코드 설명

### 가시성 검증

```csharp
[Fact]
public void DomainClasses_ShouldBe_Public()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Visibility Rule");
}

[Fact]
public void InternalClasses_ShouldBe_Internal()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(InternalNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireInternal(),
            verbose: true)
        .ThrowIfAnyFailures("Internal Class Visibility Rule");
}
```

`RequirePublic()`과 `RequireInternal()`은 클래스의 가시성을 검증합니다. 네임스페이스별로 규칙을 분리하여 도메인 클래스는 공개, 내부 구현 클래스는 비공개로 유지하는 규칙을 강제합니다.

### Abstract vs Sealed 검증

```csharp
[Fact]
public void AbstractClasses_ShouldBe_Abstract()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .AreAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireAbstract(),
            verbose: true)
        .ThrowIfAnyFailures("Abstract Class Rule");
}
```

`AreAbstract()`은 ArchUnitNET의 필터링 메서드로, 검증 대상을 먼저 좁힌 후 `RequireAbstract()`로 규칙을 적용합니다.

### Static 클래스 검증

```csharp
[Fact]
public void ServiceClasses_ShouldBe_Static()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ServiceNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireStatic(),
            verbose: true)
        .ThrowIfAnyFailures("Service Class Static Rule");
}
```

C#에서 `static` 클래스는 IL 수준에서 `abstract sealed`로 표현됩니다. 즉, CLR 관점에서 `static` 클래스는 상속할 수 없고(`sealed`) 인스턴스화할 수 없는(`abstract`) 클래스입니다. **ClassValidator는** 이 차이를 내부적으로 처리하여 `RequireStatic()`과 `RequireAbstract()`을 올바르게 구분합니다.

> **"C#의 `static` 클래스는 IL에서 `abstract sealed`입니다. ClassValidator는 이 IL 수준의 차이를 자동으로 처리하므로, `RequireStatic()`과 `RequireAbstract()`이 서로 간섭하지 않습니다."**

### Record 타입 검증

```csharp
[Fact]
public void RecordTypes_ShouldBe_Record()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameEndingWith("Summary")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireRecord(),
            verbose: true)
        .ThrowIfAnyFailures("Record Type Rule");
}
```

`RequireRecord()`는 C# `record` 타입인지 검증합니다. DTO나 불변 데이터 전달 객체에 `record` 사용을 강제할 때 유용합니다.

## Summary at a Glance

The following table ClassValidator의 가시성 및 한정자 검증 메서드를 정리합니다.

### ClassValidator 가시성/한정자 검증 메서드

| 메서드 | 검증 내용 | 사용 시나리오 |
|--------|----------|--------------|
| `RequirePublic()` | public 가시성 | 도메인 모델, API 계약 |
| `RequireInternal()` | internal 가시성 | 내부 구현, 인프라 코드 |
| `RequireSealed()` | sealed 한정자 | 상속 방지, 불변 계약 보호 |
| `RequireNotSealed()` | sealed 아님 | 기본 클래스, 확장 가능 클래스 |
| `RequireAbstract()` | abstract 한정자 | 기본 클래스, 템플릿 패턴 |
| `RequireNotAbstract()` | abstract 아님 | 구체 구현 클래스 |
| `RequireStatic()` | static 클래스 | 유틸리티, 확장 메서드 |
| `RequireNotStatic()` | static 아님 | 인스턴스 클래스 |
| `RequireRecord()` | record 타입 | DTO, value object |
| `RequireNotRecord()` | record 아님 | 일반 클래스 |

## FAQ

### Q1: `RequirePublic()`과 `RequireInternal()` 외에 `protected`나 `private` 검증도 있나요?
**A**: 최상위 클래스는 C#에서 `public` 또는 `internal`만 가능합니다. 중첩 클래스(nested class)는 `protected`나 `private`이 될 수 있지만, ArchUnitNET은 최상위 타입을 주로 다루므로 `RequirePublic()`과 `RequireInternal()`이 핵심 메서드입니다.

### Q2: `static` 클래스에 `RequireSealed()`을 적용하면 어떻게 되나요?
**A**: IL 수준에서 `static` 클래스는 `abstract sealed`이므로 `RequireSealed()`은 통과하지만 `RequireAbstract()`도 통과합니다. **ClassValidator는** `static`을 별도 카테고리로 인식하므로 `RequireStatic()`을 사용하는 것이 의도를 정확하게 표현합니다.

### Q3: `RequireRecord()`는 `record class`와 `record struct` 모두를 검증하나요?
**A**: `ValidateAllClasses`에서 사용하는 `RequireRecord()`는 `record class`만 검증합니다. `record struct`는 값 타입이므로 ArchUnitNET의 클래스 필터에 포함되지 않습니다.

---

Next chapter에서는 접미사, 접두사, 정규식 패턴을 사용하여 클래스와 인터페이스의 네이밍 규칙을 검증하는 방법을 배웁니다.

→ [3장: 네이밍 규칙](../03-Naming-Rules/)
