---
title: "아키텍처 규칙 테스트"
---

**Functorium ArchitectureRules 프레임워크를 활용한 아키텍처 테스트 실전 가이드**

---

## 이 튜토리얼에 대하여

코드 리뷰에서 반복되는 지적 — `sealed` 누락, 의존성 방향 위반, 네이밍 규칙 불일치. 매번 사람이 눈으로 확인해야 할까요? 이런 설계 규칙을 **컴파일 직후 자동으로 검증하는 테스트**가 있다면 어떨까요?

이 튜토리얼은 **Functorium ArchitectureRules 프레임워크를 활용한 아키텍처 테스트 구현**을 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 기본적인 클래스 검증에서 시작하여 실전 레이어 아키텍처 규칙까지, **16개의 실습 프로젝트**를 통해 아키텍처 테스트의 모든 측면을 체계적으로 학습할 수 있습니다.

> **"이 클래스는 sealed여야 합니다" — 코드 리뷰 코멘트가 아니라, 실패하는 테스트가 알려주는 세상을 만들어 봅시다.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 단위 테스트 경험이 있고 아키텍처 테스트에 입문하려는 개발자 | Part 0~1 |
| **중급** | 아키텍처 테스트 기본을 이해하고 심화 검증을 원하는 개발자 | Part 2~3 |
| **고급** | 실전 프로젝트에 아키텍처 규칙을 도입하려는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. **타입 수준 아키텍처 규칙 검증**
   - ClassValidator/InterfaceValidator로 가시성, 한정자, 네이밍, 상속 규칙 강제
   - 어셈블리를 로드하여 검증 대상 타입을 자동으로 수집
2. **멤버 수준 시그니처 검증**
   - MethodValidator로 메서드 시그니처, 반환 타입, 파라미터 검증
   - 프로퍼티 불변성과 필드 접근 규칙 강제
3. **팀 고유의 커스텀 규칙 합성**
   - DelegateArchRule/CompositeArchRule로 재사용 가능한 규칙 조합
4. **DDD 레이어 아키텍처 설계 일관성 자동화**
   - 도메인/애플리케이션/어댑터 레이어별 규칙 적용
   - ArchUnitNET과 Functorium을 결합한 의존성 방향 검증

---

### Part 0: 서론

아키텍처 테스트의 필요성과 프레임워크를 소개합니다.

- [0.1 왜 아키텍처 테스트인가?](Part0-Introduction/01-why-architecture-testing.md)
- [0.2 ArchUnitNET과 Functorium](Part0-Introduction/02-archunitnet-and-functorium.md)
- [0.3 환경 설정](Part0-Introduction/03-environment-setup.md)

### Part 1: ClassValidator 기초

ClassValidator의 핵심 검증 메서드를 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [첫 아키텍처 테스트](Part1-ClassValidator-Basics/01-First-Architecture-Test/) | ArchRuleDefinition, ValidateAllClasses, RequirePublic, RequireSealed |
| 2 | [가시성과 수정자](Part1-ClassValidator-Basics/02-Visibility-And-Modifiers/) | RequireInternal, RequireStatic, RequireAbstract, RequireRecord |
| 3 | [네이밍 규칙](Part1-ClassValidator-Basics/03-Naming-Rules/) | RequireNameStartsWith, RequireNameEndsWith, RequireNameMatching |
| 4 | [상속과 인터페이스](Part1-ClassValidator-Basics/04-Inheritance-And-Interface/) | RequireInherits, RequireImplements, RequireImplementsGenericInterface |

### Part 2: 메서드와 속성 검증

MethodValidator를 통한 메서드 시그니처 검증을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [메서드 검증](Part2-Method-And-Property-Validation/01-Method-Validation/) | RequireMethod, RequireAllMethods, RequireVisibility, RequireExtensionMethod |
| 2 | [반환 타입 검증](Part2-Method-And-Property-Validation/02-Return-Type-Validation/) | RequireReturnType, RequireReturnTypeOfDeclaringClass, RequireReturnTypeContaining |
| 3 | [파라미터 검증](Part2-Method-And-Property-Validation/03-Parameter-Validation/) | RequireParameterCount, RequireFirstParameterTypeContaining |
| 4 | [속성과 필드 검증](Part2-Method-And-Property-Validation/04-Property-And-Field-Validation/) | RequireProperty, RequireNoPublicSetters, RequireNoInstanceFields |

### Part 3: 고급 검증

불변성 규칙, 중첩 클래스, 인터페이스 검증, 커스텀 규칙을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [불변성 규칙](Part3-Advanced-Validation/01-Immutability-Rule/) | RequireImmutable, ImmutabilityRule 6차원 검증 |
| 2 | [중첩 클래스 검증](Part3-Advanced-Validation/02-Nested-Class-Validation/) | RequireNestedClass, RequireNestedClassIfExists |
| 3 | [인터페이스 검증](Part3-Advanced-Validation/03-Interface-Validation/) | ValidateAllInterfaces, InterfaceValidator |
| 4 | [커스텀 규칙](Part3-Advanced-Validation/04-Custom-Rules/) | DelegateArchRule, CompositeArchRule, Apply |

### Part 4: 실전 패턴

DDD 레이어 아키텍처에 아키텍처 테스트를 적용합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [도메인 레이어 규칙](Part4-Real-World-Patterns/01-Domain-Layer-Rules/) | Entity, ValueObject, DomainEvent, DomainService 종합 검증 |
| 2 | [애플리케이션 레이어 규칙](Part4-Real-World-Patterns/02-Application-Layer-Rules/) | Command/Query, Usecase, DTO 규칙 |
| 3 | [어댑터 레이어 규칙](Part4-Real-World-Patterns/03-Adapter-Layer-Rules/) | Port Interface, Adapter Implementation 규칙 |
| 4 | [레이어 의존성 규칙](Part4-Real-World-Patterns/04-Layer-Dependency-Rules/) | ArchUnitNET 의존성 규칙 + Functorium 규칙 통합 |

### Part 5: 결론

베스트 프랙티스와 다음 단계를 안내합니다.

- [5.1 베스트 프랙티스](Part5-Conclusion/01-best-practices.md)
- [5.2 다음 단계](Part5-Conclusion/02-next-steps.md)

### [부록](Appendix/)

- [A. API 레퍼런스](Appendix/A-api-reference.md)
- [B. ArchUnitNET 치트시트](Appendix/B-archunitnet-cheatsheet.md)
- [C. FAQ](Appendix/C-faq.md)

---

## 핵심 진화 과정

```
[Part 1] ClassValidator 기초
1장: 첫 아키텍처 테스트  →  2장: 가시성과 한정자  →  3장: 네이밍 규칙  →  4장: 상속과 인터페이스

[Part 2] 메서드와 속성 검증
1장: 메서드 검증  →  2장: 반환 타입 검증  →  3장: 파라미터 검증  →  4장: 속성과 필드 검증

[Part 3] 고급 검증
1장: 불변성 규칙  →  2장: 중첩 클래스  →  3장: 인터페이스 검증  →  4장: 커스텀 규칙

[Part 4] 실전 패턴
1장: 도메인 레이어  →  2장: 애플리케이션 레이어  →  3장: 어댑터 레이어  →  4장: 레이어 의존성
```

---

## 프로젝트 구조

```txt
Enforcing-Architecture-Rules-with-Testing/
├── README.md
├── Part0-Introduction/
│   ├── 01-why-architecture-testing.md
│   ├── 02-archunitnet-and-functorium.md
│   └── 03-environment-setup.md
├── Part1-ClassValidator-Basics/
│   ├── 01-First-Architecture-Test/
│   ├── 02-Visibility-And-Modifiers/
│   ├── 03-Naming-Rules/
│   └── 04-Inheritance-And-Interface/
├── Part2-Method-And-Property-Validation/
│   ├── 01-Method-Validation/
│   ├── 02-Return-Type-Validation/
│   ├── 03-Parameter-Validation/
│   └── 04-Property-And-Field-Validation/
├── Part3-Advanced-Validation/
│   ├── 01-Immutability-Rule/
│   ├── 02-Nested-Class-Validation/
│   ├── 03-Interface-Validation/
│   └── 04-Custom-Rules/
├── Part4-Real-World-Patterns/
│   ├── 01-Domain-Layer-Rules/
│   ├── 02-Application-Layer-Rules/
│   ├── 03-Adapter-Layer-Rules/
│   └── 04-Layer-Dependency-Rules/
├── Part5-Conclusion/
│   ├── 01-best-practices.md
│   └── 02-next-steps.md
└── Appendix/
    ├── A-api-reference.md
    ├── B-archunitnet-cheatsheet.md
    └── C-faq.md
```

## 테스트 실행

```bash
# 개별 챕터 테스트
dotnet test --project Docs/tutorials/Enforcing-Architecture-Rules-with-Testing/Part1-ClassValidator-Basics/01-First-Architecture-Test/FirstArchitectureTest.Tests.Unit

# 전체 솔루션 테스트
dotnet test --solution Functorium.All.slnx
```

## 테스트 프로젝트 목록

| Part | 장 | 테스트 프로젝트 |
|:----:|:---:|----------------|
| 1 | 1 | `FirstArchitectureTest.Tests.Unit` |
| 1 | 2 | `VisibilityAndModifiers.Tests.Unit` |
| 1 | 3 | `NamingRules.Tests.Unit` |
| 1 | 4 | `InheritanceAndInterface.Tests.Unit` |
| 2 | 1 | `MethodValidation.Tests.Unit` |
| 2 | 2 | `ReturnTypeValidation.Tests.Unit` |
| 2 | 3 | `ParameterValidation.Tests.Unit` |
| 2 | 4 | `PropertyAndFieldValidation.Tests.Unit` |
| 3 | 1 | `ImmutabilityRule.Tests.Unit` |
| 3 | 2 | `NestedClassValidation.Tests.Unit` |
| 3 | 3 | `InterfaceValidation.Tests.Unit` |
| 3 | 4 | `CustomRules.Tests.Unit` |
| 4 | 1 | `DomainLayerRules.Tests.Unit` |
| 4 | 2 | `ApplicationLayerRules.Tests.Unit` |
| 4 | 3 | `AdapterLayerRules.Tests.Unit` |
| 4 | 4 | `LayerDependencyRules.Tests.Unit` |

## 테스트 네이밍 규칙

```txt
T1_T2_T3
│  │  └─ T3: 조건/시나리오
│  └──── T2: 기대 동작 (ShouldBe, ShouldHave, ShouldNotDependOn)
└─────── T1: 검증 대상 (DomainClasses, ValueObject, Entity)

예시: DomainClasses_ShouldBe_PublicAndSealed
```
