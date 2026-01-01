# 함수형으로 성공 주도 값 객체 구현하기

**C# LanguageExt로 타입 안전한 값 객체를 구현하는 실전 가이드**

---

## 이 책에 대하여

이 책은 **함수형 프로그래밍 원칙을 적용한 값 객체(Value Object) 구현**을 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 기본적인 나눗셈 함수에서 시작하여 완성된 패턴까지, **27개의 실습 프로젝트**를 통해 함수형 값 객체의 모든 측면을 체계적으로 학습할 수 있습니다.

> **단순한 예외 기반 함수에서 시작하여 타입 안전한 함수형 값 객체로 진화하는 과정을 함께 경험해보세요.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기본 문법을 알고 함수형 프로그래밍에 입문하려는 개발자 | Part 1 (1장~6장) |
| 🟡 **중급** | 함수형 개념을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 전체 |
| 🔴 **고급** | 프레임워크 설계와 아키텍처에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 책을 완료하면 다음을 할 수 있습니다:

1. **예외 대신 명시적 결과 타입**으로 안전한 코드 작성
2. **도메인 규칙을 타입으로 표현**하여 컴파일 타임 검증
3. **Bind/Apply 패턴**을 활용한 유연한 검증 로직 구현
4. **Functorium 프레임워크**를 활용한 실전 값 객체 개발

---

## 목차

### Part 0: 서론

서론에서는 성공 주도 개발의 개념과 환경 설정을 다룹니다.

- [0.1 이 책을 읽어야 하는 이유](Part0-Introduction/01-why-this-book.md)
- [0.2 성공 주도 개발이란?](Part0-Introduction/02-success-driven-development.md)
- [0.3 환경 설정](Part0-Introduction/03-environment-setup.md)

### Part 1: 값 객체 개념 이해

기본 개념부터 프레임워크까지 체계적으로 학습합니다.

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
| 14 | [아키텍처 테스트](Part1-ValueObject-Concepts/14-Architecture-Test/) | 아키텍처 테스트와 규칙 |

### Part 2: 검증 패턴 마스터

함수형 검증 패턴을 심화 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 15 | [순차 검증 (Bind)](Part2-Validation-Patterns/01-Bind-Sequential-Validation/) | Bind를 통한 순차 검증 |
| 16 | [병렬 검증 (Apply)](Part2-Validation-Patterns/02-Apply-Parallel-Validation/) | Apply를 통한 병렬 검증 |
| 17 | [Apply와 Bind 조합](Part2-Validation-Patterns/03-Apply-Bind-Combined-Validation/) | Apply와 Bind 조합 |
| 18 | [내부 Bind 외부 Apply](Part2-Validation-Patterns/04-Apply-Internal-Bind-Validation/) | 내부 Bind와 외부 Apply |
| 19 | [내부 Apply 외부 Bind](Part2-Validation-Patterns/05-Bind-Internal-Apply-Validation/) | 내부 Apply와 외부 Bind |

### Part 3: 값 객체 패턴 완성

완성된 값 객체 패턴을 실전 프로젝트로 적용합니다.

| 장 | 주제 | 프레임워크 타입 |
|:---:|------|----------------|
| 20 | [SimpleValueObject](Part3-ValueObject-Patterns/01-SimpleValueObject/) | `SimpleValueObject<T>` |
| 21 | [ComparableSimpleValueObject](Part3-ValueObject-Patterns/02-ComparableSimpleValueObject/) | `ComparableSimpleValueObject<T>` |
| 22 | [ValueObject (Primitive)](Part3-ValueObject-Patterns/03-ValueObject-Primitive/) | `ValueObject` |
| 23 | [ComparableValueObject (Primitive)](Part3-ValueObject-Patterns/04-ComparableValueObject-Primitive/) | `ComparableValueObject` |
| 24 | [ValueObject (Composite)](Part3-ValueObject-Patterns/05-ValueObject-Composite/) | `ValueObject` |
| 25 | [ComparableValueObject (Composite)](Part3-ValueObject-Patterns/06-ComparableValueObject-Composite/) | `ComparableValueObject` |
| 26 | [TypeSafeEnum](Part3-ValueObject-Patterns/07-TypeSafeEnum/) | `SmartEnum + IValueObject` |
| 27 | [아키텍처 테스트](Part3-ValueObject-Patterns/08-Architecture-Test/) | `ArchUnitNET` |

### Part 4: 실전 가이드

실전 프로젝트에서 값 객체를 적용하는 방법을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 28 | [Functorium 프레임워크 통합](Part4-Practical-Guide/01-Functorium-Framework/) | Functorium 프레임워크와 값 객체 통합 |
| 29 | [ORM 통합 패턴](Part4-Practical-Guide/02-ORM-Integration/) | EF Core와 값 객체 통합 |
| 30 | [CQRS와 값 객체](Part4-Practical-Guide/03-CQRS-Integration/) | CQRS 패턴에서 값 객체 활용 |
| 31 | [테스트 전략](Part4-Practical-Guide/04-Testing-Strategies/) | 값 객체 테스트 전략 |

### Part 5: 도메인별 실전 예제

다양한 도메인에서 값 객체를 구현하는 실전 예제입니다.

| 장 | 주제 | 값 객체 예제 |
|:---:|------|-------------|
| 32 | [이커머스 도메인](Part5-Domain-Examples/01-Ecommerce-Domain/) | Money, ProductCode, Quantity, OrderStatus |
| 33 | [금융 도메인](Part5-Domain-Examples/02-Finance-Domain/) | AccountNumber, InterestRate, ExchangeRate |
| 34 | [사용자 관리 도메인](Part5-Domain-Examples/03-User-Management-Domain/) | Email, Password, PhoneNumber |
| 35 | [일정/예약 도메인](Part5-Domain-Examples/04-Scheduling-Domain/) | DateRange, TimeSlot, Duration |

### [부록](Appendix/)

- [A. LanguageExt 주요 타입 참조](Appendix/A-languageext-reference.md)
- [B. 프레임워크 타입 선택 가이드](Appendix/B-type-selection-guide.md)
- [C. 용어집](Appendix/C-glossary.md)
- [D. 참고 자료](Appendix/D-references.md)
- [E. FAQ](Appendix/E-faq.md)

---

## 핵심 진화 과정

```
1장: 예외 발생 함수     →  2장: 방어적 프로그래밍  →  3장: Fin<T> 도입
     ↓
4장: 항상 유효한 VO    →  5장: 연산자 오버로딩   →  6장: LINQ 지원
     ↓
7장: 값 동등성         →  8장: 비교 가능성       →  9장: 생성/검증 분리
     ↓
10장: 검증된 값 생성   →  11장: 프레임워크 타입  →  12장: 타입 안전 열거형
     ↓
13장: 에러 코드        →  14장: 아키텍처 테스트
```

---

## Bind vs Apply 비교

| 구분 | Bind (순차 검증) | Apply (병렬 검증) |
|------|------------------|-------------------|
| **실행 방식** | 순차 실행 | 병렬 실행 |
| **에러 처리** | 첫 번째 에러에서 중단 | 모든 에러 수집 |
| **사용 시기** | 의존성 있는 검증 | 독립적인 검증 |
| **성능** | 조기 중단으로 효율적 | 모든 검증 실행 |
| **UX** | 하나씩 오류 표시 | 모든 오류 한 번에 표시 |

---

## 프레임워크 타입 계층 구조

```
IValueObject (인터페이스 - 명명 규칙)
    │
    └── AbstractValueObject (기본 클래스 - 동등성, 해시코드)
        │
        ├── ValueObject (검증 헬퍼 메서드)
        │   │
        │   ├── SimpleValueObject<T> (단일 값 래퍼)
        │   │   └── ComparableSimpleValueObject<T> (비교 가능)
        │   │
        │   └── ComparableValueObject (복합 비교 가능)
        │
        └── SmartEnum<TValue, TKey> + IValueObject (열거형)
```

---

## 필수 준비물

- .NET 10.0 SDK 이상
- VS Code + C# Dev Kit 확장
- C# 기초 문법 지식

---

## 프로젝트 구조

```
Implementing-Functional-ValueObject/
├── Part0-Introduction/        # Part 0: 서론
├── Part1-ValueObject-Concepts/  # Part 1: 값 객체 개념 이해 (14개)
│   ├── 01-Basic-Divide/
│   ├── 02-Defensive-Programming/
│   ├── ...
│   └── 14-Architecture-Test/
├── Part2-Validation-Patterns/   # Part 2: 검증 패턴 마스터 (5개)
│   ├── 01-Bind-Sequential-Validation/
│   ├── ...
│   └── 05-Bind-Internal-Apply-Validation/
├── Part3-ValueObject-Patterns/  # Part 3: 값 객체 패턴 완성 (8개)
│   ├── 01-SimpleValueObject/
│   ├── ...
│   └── 08-Architecture-Test/
├── Part4-Practical-Guide/       # Part 4: 실전 가이드
│   ├── 01-Functorium-Framework/
│   │   ├── FunctoriumFramework/
│   │   └── FunctoriumFramework.Tests.Unit/
│   ├── 02-ORM-Integration/
│   │   ├── OrmIntegration/
│   │   └── OrmIntegration.Tests.Unit/
│   ├── 03-CQRS-Integration/
│   │   ├── CqrsIntegration/
│   │   └── CqrsIntegration.Tests.Unit/
│   └── 04-Testing-Strategies/
│       ├── TestingStrategies/
│       └── TestingStrategies.Tests.Unit/
├── Part5-Domain-Examples/       # Part 5: 도메인별 실전 예제
│   ├── 01-Ecommerce-Domain/
│   │   ├── EcommerceDomain/
│   │   └── EcommerceDomain.Tests.Unit/
│   ├── 02-Finance-Domain/
│   │   ├── FinanceDomain/
│   │   └── FinanceDomain.Tests.Unit/
│   ├── 03-User-Management-Domain/
│   │   ├── UserManagementDomain/
│   │   └── UserManagementDomain.Tests.Unit/
│   └── 04-Scheduling-Domain/
│       ├── SchedulingDomain/
│       └── SchedulingDomain.Tests.Unit/
├── Appendix/                    # 부록
└── README.md                    # 이 문서
```

---

## 테스트

모든 Part의 예제 프로젝트에는 단위 테스트가 포함되어 있습니다. 테스트는 [Guide-01-Unit-Testing.md](../../Docs/Functorium/Guide-01-Unit-Testing.md) 가이드를 따릅니다.

### 테스트 실행 방법

```bash
# Part 1 테스트 실행
cd Books/Implementing-Functional-ValueObject/Part1-ValueObject-Concepts/01-Basic-Divide/BasicDivide.Tests.Unit
dotnet test

# Part 2 테스트 실행
cd Books/Implementing-Functional-ValueObject/Part2-Validation-Patterns/01-Bind-Sequential-Validation/BindSequentialValidation.Tests.Unit
dotnet test

# Part 3 테스트 실행
cd Books/Implementing-Functional-ValueObject/Part3-ValueObject-Patterns/01-SimpleValueObject/SimpleValueObject.Tests.Unit
dotnet test

# Part 4 테스트 실행
cd Books/Implementing-Functional-ValueObject/Part4-Practical-Guide/01-Functorium-Framework/FunctoriumFramework.Tests.Unit
dotnet test

# Part 5 테스트 실행
cd Books/Implementing-Functional-ValueObject/Part5-Domain-Examples/01-Ecommerce-Domain/EcommerceDomain.Tests.Unit
dotnet test
```

### 테스트 프로젝트 구조

**Part 1: 값 객체 개념 이해** (14개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `BasicDivide.Tests.Unit` | 나눗셈 예외 vs 결과 타입 |
| 2 | `DefensiveProgramming.Tests.Unit` | 방어적 프로그래밍 검증 |
| 3 | `FunctionalResult.Tests.Unit` | Fin/Validation 타입 테스트 |
| 4 | `AlwaysValid.Tests.Unit` | 항상 유효한 값 객체 |
| 5 | `OperatorOverloading.Tests.Unit` | 연산자 오버로딩 |
| 6 | `LinqExpression.Tests.Unit` | LINQ 표현식 |
| 7 | `ValueEquality.Tests.Unit` | 값 동등성 |
| 8 | `ValueComparability.Tests.Unit` | 비교 가능성 |
| 9 | `CreateValidateSeparation.Tests.Unit` | 생성/검증 분리 |
| 10 | `ValidatedValueCreation.Tests.Unit` | 검증된 값 생성 |
| 11 | `ValueObjectFramework.Tests.Unit` | 프레임워크 타입 |
| 12 | `TypeSafeEnums.Tests.Unit` | 타입 안전 열거형 |
| 13 | `ErrorCode.Tests.Unit` | 에러 코드 |
| 14 | `ArchitectureTest.Tests.Unit` | 아키텍처 테스트 |

**Part 2: 검증 패턴 마스터** (5개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------:|
| 15 | `BindSequentialValidation.Tests.Unit` | Bind 순차 검증 |
| 16 | `ApplyParallelValidation.Tests.Unit` | Apply 병렬 검증 |
| 17 | `ApplyBindCombinedValidation.Tests.Unit` | Apply와 Bind 조합 |
| 18 | `ApplyInternalBindValidation.Tests.Unit` | 내부 Bind 외부 Apply |
| 19 | `BindInternalApplyValidation.Tests.Unit` | 내부 Apply 외부 Bind |

**Part 3: 값 객체 패턴 완성** (7개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------:|
| 20 | `SimpleValueObject.Tests.Unit` | 단일 값 래퍼 테스트 |
| 21 | `ComparableSimpleValueObject.Tests.Unit` | 비교 가능 단일 값 테스트 |
| 22 | `ValueObjectPrimitive.Tests.Unit` | 기본 타입 값 객체 |
| 23 | `ComparableValueObjectPrimitive.Tests.Unit` | 비교 가능 기본 타입 |
| 24 | `ValueObjectComposite.Tests.Unit` | 복합 값 객체 |
| 25 | `ComparableValueObjectComposite.Tests.Unit` | 비교 가능 복합 값 객체 |
| 26 | `TypeSafeEnum.Tests.Unit` | 타입 안전 열거형 |

**Part 4: 실전 가이드** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 28 | `FunctoriumFramework.Tests.Unit` | 프레임워크 타입 통합 테스트 |
| 29 | `OrmIntegration.Tests.Unit` | EF Core ORM 패턴 테스트 |
| 30 | `CqrsIntegration.Tests.Unit` | CQRS 핸들러 테스트 |
| 31 | `TestingStrategies.Tests.Unit` | 테스트 패턴 메타 테스트 |

**Part 5: 도메인별 실전 예제** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 32 | `EcommerceDomain.Tests.Unit` | 이커머스 값 객체 테스트 |
| 33 | `FinanceDomain.Tests.Unit` | 금융 값 객체 테스트 |
| 34 | `UserManagementDomain.Tests.Unit` | 사용자 관리 값 객체 테스트 |
| 35 | `SchedulingDomain.Tests.Unit` | 일정 관리 값 객체 테스트 |

### 테스트 명명 규칙

T1_T2_T3 명명 규칙을 따릅니다:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Create_ReturnsSuccess_WhenInputIsValid()
{
    // Arrange
    // Act
    var actual = Money.Create(10000, "KRW");
    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

---

## 소스 코드

이 책의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- 프레임워크 타입: `Src/Functorium/Domains/ValueObjects/`
- 튜토리얼 프로젝트: `Books/Implementing-Functional-ValueObject/`

---

이 책은 Functorium 프로젝트의 실제 값 객체 프레임워크 개발 경험을 바탕으로 작성되었습니다.
