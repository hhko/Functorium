---
title: "함수형으로 성공 주도 값 객체 구현하기"
---

**C# Functorium으로 타입 안전한 값 객체를 구현하는 실전 가이드**

---

## 이 튜토리얼에 대하여

`string email`에 `"not-an-email"`을 넣어도 컴파일러는 아무 말 하지 않습니다. `int age`에 `-1`을 넣어도 마찬가지입니다. 런타임이 되어서야 `ArgumentException`이 터지고, 그제야 "아, 여기서도 검증이 필요했구나"를 깨닫게 됩니다.

이 튜토리얼은 그 문제를 **타입 시스템으로** 해결합니다. `string` 대신 `Email`을, `int` 대신 `Age`를 쓰면 잘못된 값은 애초에 만들어지지 않습니다. 기본적인 나눗셈 함수에서 시작해 완성된 값 객체 프레임워크까지, **29개의 실습 프로젝트**를 통해 이 과정을 직접 경험합니다.

> **"string email에 잘못된 값이 들어가는 순간을 런타임이 아닌 컴파일 타임에 잡아내는 세상을 만들어 봅시다."**

### 대상 독자

다음 표는 경험 수준에 따른 권장 학습 범위를 안내합니다.

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 기본 문법을 알고 함수형 프로그래밍에 입문하려는 개발자 | Part 1 (1장~6장) |
| **중급** | 함수형 개념을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 전체 |
| **고급** | 프레임워크 설계와 아키텍처에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. **예외 대신 명시적 결과 타입**으로 안전한 코드를 작성할 수 있습니다
2. **도메인 규칙을 타입으로 표현**하여 컴파일 타임에 검증할 수 있습니다
3. **Bind/Apply 패턴**을 활용해 유연한 검증 로직을 구현할 수 있습니다
4. **Functorium 프레임워크**를 활용해 실전 값 객체를 개발할 수 있습니다

---

### Part 0: 서론

예외 기반 코드가 왜 문제인지, 성공 주도 개발이 어떤 대안을 제시하는지 살펴봅니다.

- [0.1 이 튜토리얼을 읽어야 하는 이유](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 성공 주도 개발이란?](Part0-Introduction/02-success-driven-development.md)
- [0.3 환경 설정](Part0-Introduction/03-environment-setup.md)

### Part 1: 값 객체 개념 이해

`10 / 0`이 터지는 단순한 예제에서 출발해, 예외 → 방어적 프로그래밍 → 함수형 결과 타입 → 항상 유효한 값 객체로 한 단계씩 진화합니다. 왜 각 단계가 필요한지를 코드로 직접 확인합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [기본 나눗셈](Part1-ValueObject-Concepts/01-Basic-Divide/) | 예외 vs 도메인 타입의 차이점 |
| 2 | [방어적 프로그래밍](Part1-ValueObject-Concepts/02-Defensive-Programming/) | 방어적 프로그래밍과 사전 검증 |
| 3 | [함수형 결과 타입](Part1-ValueObject-Concepts/03-Functional-Result/) | 함수형 결과 타입 (Fin, Validation) |
| 4 | [항상 유효한 값 객체](Part1-ValueObject-Concepts/04-Always-Valid/) | 항상 유효한 값 객체 구현 |
| 5 | [연산자 오버로딩](Part1-ValueObject-Concepts/05-Operator-Overloading/) | 연산자 오버로딩과 타입 변환 |
| 6 | [LINQ 표현식](Part1-ValueObject-Concepts/06-Linq-Expression/) | LINQ 표현식과 함수형 조합 |
| 7 | [값 동등성](Part1-ValueObject-Concepts/07-Value-Equality/) | 값 동등성과 해시코드 |
| 8 | [비교 가능성](Part1-ValueObject-Concepts/08-Value-Comparability/) | 비교 가능성과 정렬 |
| 9 | [생성과 검증 분리](Part1-ValueObject-Concepts/09-Create-Validate-Separation/) | 생성과 검증의 분리 |
| 10 | [검증된 값 생성](Part1-ValueObject-Concepts/10-Validated-Value-Creation/) | 검증된 값 생성 패턴 |
| 11 | [프레임워크 타입](Part1-ValueObject-Concepts/11-ValueObject-Framework/) | 프레임워크 타입 |
| 12 | [타입 안전한 열거형](Part1-ValueObject-Concepts/12-Type-Safe-Enums/) | 타입 안전한 열거형 |
| 13 | [에러 코드](Part1-ValueObject-Concepts/13-Error-Code/) | 구조화된 에러 코드 |
| 14 | [에러 코드 Fluent](Part1-ValueObject-Concepts/14-Error-Code-Fluent/) | DomainError 헬퍼 |
| 15 | [FluentValidation 검증](Part1-ValueObject-Concepts/15-Validation-Fluent/) | FluentValidation 기반 검증 패턴 |
| 16 | [아키텍처 테스트](Part1-ValueObject-Concepts/16-Architecture-Test/) | 아키텍처 테스트와 규칙 |

### Part 2: 검증 패턴 마스터

값 객체 하나를 검증하는 것은 어렵지 않습니다. 하지만 여러 필드를 동시에 검증하면서 모든 오류를 한번에 수집하려면 Bind와 Apply의 차이를 이해해야 합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [순차 검증 (Bind)](Part2-Validation-Patterns/01-Bind-Sequential-Validation/) | Bind를 통한 순차 검증 |
| 2 | [병렬 검증 (Apply)](Part2-Validation-Patterns/02-Apply-Parallel-Validation/) | Apply를 통한 병렬 검증 |
| 3 | [Apply와 Bind 조합](Part2-Validation-Patterns/03-Apply-Bind-Combined-Validation/) | Apply와 Bind 조합 |
| 4 | [내부 Bind 외부 Apply](Part2-Validation-Patterns/04-Apply-Internal-Bind-Validation/) | 내부 Bind와 외부 Apply |
| 5 | [내부 Apply 외부 Bind](Part2-Validation-Patterns/05-Bind-Internal-Apply-Validation/) | 내부 Apply와 외부 Bind |
| 6 | [컨텍스트 기반 검증](Part2-Validation-Patterns/06-Contextual-Validation/) | ContextualValidation — 필드 이름 기반 검증 |

### Part 3: 값 객체 패턴 완성

Part 1~2에서 익힌 개념을 Functorium 프레임워크의 기본 클래스로 조립합니다. 단일 값 래퍼부터 복합 값 객체, 타입 안전 열거형까지 실전에서 바로 쓸 수 있는 패턴을 완성합니다.

| 장 | 주제 | 프레임워크 타입 |
|:---:|------|----------------|
| 1 | [SimpleValueObject](Part3-ValueObject-Patterns/01-SimpleValueObject/) | `SimpleValueObject<T>` |
| 2 | [ComparableSimpleValueObject](Part3-ValueObject-Patterns/02-ComparableSimpleValueObject/) | `ComparableSimpleValueObject<T>` |
| 3 | [ValueObject (Primitive)](Part3-ValueObject-Patterns/03-ValueObject-Primitive/) | `ValueObject` |
| 4 | [ComparableValueObject (Primitive)](Part3-ValueObject-Patterns/04-ComparableValueObject-Primitive/) | `ComparableValueObject` |
| 5 | [ValueObject (Composite)](Part3-ValueObject-Patterns/05-ValueObject-Composite/) | `ValueObject` |
| 6 | [ComparableValueObject (Composite)](Part3-ValueObject-Patterns/06-ComparableValueObject-Composite/) | `ComparableValueObject` |
| 7 | [TypeSafeEnum](Part3-ValueObject-Patterns/07-TypeSafeEnum/) | `SmartEnum + IValueObject` |
| 8 | [아키텍처 테스트](Part3-ValueObject-Patterns/08-Architecture-Test/) | `ArchUnitNET` |
| 9 | [UnionValueObject](Part3-ValueObject-Patterns/09-UnionValueObject/) | `UnionValueObject` (Discriminated Union) |

### Part 4: 실전 가이드

값 객체를 EF Core, CQRS 같은 인프라와 통합할 때 발생하는 실전 문제를 다룹니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [Functorium 프레임워크 통합](Part4-Practical-Guide/01-Functorium-Framework/) | Functorium 프레임워크와 값 객체 통합 |
| 2 | [ORM 통합 패턴](Part4-Practical-Guide/02-ORM-Integration/) | EF Core와 값 객체 통합 |
| 3 | [CQRS와 값 객체](Part4-Practical-Guide/03-CQRS-Integration/) | CQRS 패턴에서 값 객체 활용 |
| 4 | [테스트 전략](Part4-Practical-Guide/04-Testing-Strategies/) | 값 객체 테스트 전략 |

### Part 5: 도메인별 실전 예제

이커머스, 금융, 사용자 관리, 일정 예약 등 실제 도메인에서 값 객체가 어떻게 쓰이는지 확인합니다.

| 장 | 주제 | 값 객체 예제 |
|:---:|------|-------------|
| 1 | [이커머스 도메인](Part5-Domain-Examples/01-Ecommerce-Domain/) | Money, ProductCode, Quantity, OrderStatus |
| 2 | [금융 도메인](Part5-Domain-Examples/02-Finance-Domain/) | AccountNumber, InterestRate, ExchangeRate |
| 3 | [사용자 관리 도메인](Part5-Domain-Examples/03-User-Management-Domain/) | Email, Password, PhoneNumber |
| 4 | [일정/예약 도메인](Part5-Domain-Examples/04-Scheduling-Domain/) | DateRange, TimeSlot, Duration |

### [부록](Appendix/)

- [A. LanguageExt 주요 타입 참조](Appendix/A-languageext-reference.md)
- [B. 프레임워크 타입 선택 가이드](Appendix/B-type-selection-guide.md)
- [C. 용어집](Appendix/C-glossary.md)
- [D. 참고 자료](Appendix/D-references.md)
- [E. FAQ](Appendix/E-faq.md)

---

## 핵심 진화 과정

[Part 1] 값 객체 개념 이해
1장: 기본 나눗셈  →  2장: 방어적 프로그래밍  →  3장: 함수형 결과 타입  →  4장: 항상 유효한 값 객체  →  5장: 연산자 오버로딩  →  6장: LINQ 표현식  →  7장: 값 동등성  →  8장: 비교 가능성  →  9장: 생성과 검증 분리  →  10장: 검증된 값 생성  →  11장: 프레임워크 타입  →  12장: 타입 안전한 열거형  →  13장: 에러 코드  →  14장: 에러 코드 Fluent  →  15장: FluentValidation 검증  →  16장: 아키텍처 테스트

[Part 2] 검증 패턴 마스터
1장: 순차 검증 (Bind)  →  2장: 병렬 검증 (Apply)  →  3장: Apply와 Bind 조합  →  4장: 내부 Bind 외부 Apply  →  5장: 내부 Apply 외부 Bind  →  6장: 컨텍스트 기반 검증

[Part 3] 값 객체 패턴 완성
1장: SimpleValueObject  →  2장: ComparableSimpleValueObject  →  3장: ValueObject (Primitive)  →  4장: ComparableValueObject (Primitive)  →  5장: ValueObject (Composite)  →  6장: ComparableValueObject (Composite)  →  7장: TypeSafeEnum  →  8장: 아키텍처 테스트  →  9장: UnionValueObject

[Part 4] 실전 가이드
1장: Functorium 프레임워크 통합  →  2장: ORM 통합 패턴  →  3장: CQRS와 값 객체  →  4장: 테스트 전략

[Part 5] 도메인별 실전 예제
1장: 이커머스 도메인  →  2장: 금융 도메인  →  3장: 사용자 관리 도메인  →  4장: 일정/예약 도메인

---

## 필수 준비물

- .NET 10.0 SDK 이상
- VS Code + C# Dev Kit 확장
- C# 기초 문법 지식

---

## 프로젝트 구조

```
functional-valueobject/
├── Part0-Introduction/                # Part 0: 서론
├── Part1-ValueObject-Concepts/        # Part 1: 값 객체 개념 이해 (16개)
│   ├── 01-Basic-Divide/
│   ├── 02-Defensive-Programming/
│   ├── ...
│   └── 16-Architecture-Test/
├── Part2-Validation-Patterns/         # Part 2: 검증 패턴 마스터 (6개)
│   ├── 01-Bind-Sequential-Validation/
│   ├── 02-Apply-Parallel-Validation/
│   ├── ...
│   └── 06-Contextual-Validation/
├── Part3-ValueObject-Patterns/        # Part 3: 값 객체 패턴 완성 (9개)
│   ├── 01-SimpleValueObject/
│   ├── 02-ComparableSimpleValueObject/
│   ├── ...
│   └── 09-UnionValueObject/
├── Part4-Practical-Guide/             # Part 4: 실전 가이드 (4개)
│   ├── 01-Functorium-Framework/
│   ├── 02-ORM-Integration/
│   ├── 03-CQRS-Integration/
│   └── 04-Testing-Strategies/
├── Part5-Domain-Examples/             # Part 5: 도메인별 실전 예제 (4개)
│   ├── 01-Ecommerce-Domain/
│   ├── 02-Finance-Domain/
│   ├── 03-User-Management-Domain/
│   └── 04-Scheduling-Domain/
├── Appendix/                          # 부록
└── index.md                           # 이 문서
```

---

## 테스트

모든 Part의 예제 프로젝트에는 단위 테스트가 포함되어 있습니다. 테스트는 [단위 테스트 가이드](../../guides/testing/15a-unit-testing.md)를 따릅니다.

### 테스트 실행 방법

```bash
# 튜토리얼 전체 빌드
dotnet build functional-valueobject.slnx

# 튜토리얼 전체 테스트
dotnet test --solution functional-valueobject.slnx
```

### 테스트 프로젝트 구조

**Part 1: 값 객체 개념 이해** (16개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `BasicDivide.Tests.Unit` | 예외 vs 도메인 타입 나눗셈 |
| 2 | `DefensiveProgramming.Tests.Unit` | 방어적 프로그래밍, 사전 검증 |
| 3 | `FunctionalResult.Tests.Unit` | Fin, Validation 결과 타입 |
| 4 | `AlwaysValid.Tests.Unit` | 항상 유효한 값 객체 생성 |
| 5 | `OperatorOverloading.Tests.Unit` | 연산자 오버로딩, 타입 변환 |
| 6 | `LinqExpression.Tests.Unit` | LINQ 표현식, 함수형 조합 |
| 7 | `ValueEquality.Tests.Unit` | 값 동등성, 해시코드 |
| 8 | `ValueComparability.Tests.Unit` | 비교 가능성, 정렬 |
| 9 | `CreateValidateSeparation.Tests.Unit` | 생성과 검증 분리 |
| 10 | `ValidatedValueCreation.Tests.Unit` | 검증된 값 생성 패턴 |
| 11 | `ValueObjectFramework.Tests.Unit` | 프레임워크 타입 검증 |
| 12 | `TypeSafeEnums.Tests.Unit` | 타입 안전한 열거형 |
| 13 | `ErrorCode.Tests.Unit` | 구조화된 에러 코드 |
| 14 | `ErrorCodeFluent.Tests.Unit` | DomainError 헬퍼 |
| 15 | `ValidationFluent.Tests.Unit` | FluentValidation 기반 검증 |
| 16 | `ArchitectureTest.Tests.Unit` | 아키텍처 규칙 검증 |

**Part 2: 검증 패턴 마스터** (6개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `BindSequentialValidation.Tests.Unit` | Bind 순차 검증 |
| 2 | `ApplyParallelValidation.Tests.Unit` | Apply 병렬 검증 |
| 3 | `ApplyBindCombinedValidation.Tests.Unit` | Apply+Bind 조합 검증 |
| 4 | `ApplyInternalBindValidation.Tests.Unit` | 내부 Bind 외부 Apply |
| 5 | `BindInternalApplyValidation.Tests.Unit` | 내부 Apply 외부 Bind |
| 6 | `ContextualValidation.Tests.Unit` | 컨텍스트 기반 검증 |

**Part 3: 값 객체 패턴 완성** (8개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `SimpleValueObject.Tests.Unit` | SimpleValueObject 검증 |
| 2 | `ComparableSimpleValueObject.Tests.Unit` | ComparableSimpleValueObject 검증 |
| 3 | `ValueObjectPrimitive.Tests.Unit` | ValueObject (Primitive) 검증 |
| 4 | `ComparableValueObjectPrimitive.Tests.Unit` | ComparableValueObject (Primitive) 검증 |
| 5 | `ValueObjectComposite.Tests.Unit` | ValueObject (Composite) 검증 |
| 6 | `ComparableValueObjectComposite.Tests.Unit` | ComparableValueObject (Composite) 검증 |
| 7 | `TypeSafeEnum.Tests.Unit` | SmartEnum + IValueObject 검증 |
| 9 | `UnionValueObject.Tests.Unit` | UnionValueObject (Discriminated Union) 검증 |

**Part 4: 실전 가이드** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `FunctoriumFramework.Tests.Unit` | Functorium 프레임워크 통합 |
| 2 | `OrmIntegration.Tests.Unit` | EF Core 값 객체 통합 |
| 3 | `CqrsIntegration.Tests.Unit` | CQRS 패턴 값 객체 활용 |
| 4 | `TestingStrategies.Tests.Unit` | 값 객체 테스트 전략 |

**Part 5: 도메인별 실전 예제** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `EcommerceDomain.Tests.Unit` | Money, ProductCode, Quantity, OrderStatus |
| 2 | `FinanceDomain.Tests.Unit` | AccountNumber, InterestRate, ExchangeRate |
| 3 | `UserManagementDomain.Tests.Unit` | Email, Password, PhoneNumber |
| 4 | `SchedulingDomain.Tests.Unit` | DateRange, TimeSlot, Duration |

### 테스트 명명 규칙

T1_T2_T3 명명 규칙을 따릅니다:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Create_ReturnsSuccess_WhenValueIsValid()
{
    // Arrange
    var input = "user@example.com";
    // Act
    var actual = Email.Create(input);
    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

---

## 소스 코드

이 튜토리얼의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- 프레임워크 타입: `Src/Functorium/Domains/ValueObjects/`
- 튜토리얼 프로젝트: `Docs.Site/src/content/docs/tutorials/functional-valueobject/`

### 관련 튜토리얼

- **[명세 패턴](../specification-pattern/)**: ValueObject와 Specification을 결합하여 도메인 규칙을 표현하는 방법을 학습합니다.
- **[아키텍처 규칙 테스트](../architecture-rules/)**: ValueObject의 아키텍처 규칙 검증을 자동화하는 방법을 학습합니다.

---

이 튜토리얼은 Functorium 프로젝트의 실제 값 객체 프레임워크 개발 경험을 바탕으로 작성되었습니다.
