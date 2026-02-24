# Specification 패턴으로 도메인 규칙 구현하기

**C# Functorium으로 조합 가능한 비즈니스 규칙을 구현하는 실전 가이드**

---

## 이 책에 대하여

이 책은 **Specification 패턴을 활용한 도메인 규칙 구현**을 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 기본적인 Specification 클래스에서 시작하여 Expression Tree 기반 Repository 통합까지, **18개의 실습 프로젝트**를 통해 Specification 패턴의 모든 측면을 체계적으로 학습할 수 있습니다.

> **단순한 조건 분기에서 시작하여 조합 가능한 비즈니스 규칙으로 진화하는 과정을 함께 경험해보세요.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기본 문법을 알고 Specification 패턴에 입문하려는 개발자 | Part 1 (1장~4장) |
| 🟡 **중급** | 패턴을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 (1장~12장) |
| 🔴 **고급** | 아키텍처 설계와 도메인 모델링에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 책을 완료하면 다음을 할 수 있습니다:

1. **Specification 패턴의 개념과 필요성**을 이해하고 설명
2. **And, Or, Not 조합**으로 복합 비즈니스 규칙 구현
3. **Expression Tree**를 활용한 ORM 호환 Specification 구현
4. **Repository와 Specification 통합**으로 유연한 데이터 조회
5. **테스트 전략**을 적용한 신뢰할 수 있는 도메인 규칙 검증

---

## 목차

### Part 0: 서론

서론에서는 Specification 패턴의 개념과 환경 설정을 다룹니다.

- [0.1 이 책을 읽어야 하는 이유](Part0-Introduction/01-why-this-book.md)
- [0.2 사전 준비와 환경 설정](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 Specification 패턴 개요](Part0-Introduction/03-specification-pattern-overview.md)

### Part 1: Specification 기초

기본 Specification부터 연산자 오버로딩과 항등원까지 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [첫 번째 Specification](Part1-Specification-Basics/01-First-Specification/) | Specification<T> 상속, IsSatisfiedBy 구현 |
| 2 | [조합](Part1-Specification-Basics/02-Composition/) | And, Or, Not 메서드 조합 |
| 3 | [연산자](Part1-Specification-Basics/03-Operators/) | &, |, ! 연산자 오버로딩 |
| 4 | [All 항등원](Part1-Specification-Basics/04-All-Identity/) | All 항등원, 동적 필터 체이닝 |

### Part 2: Expression Specification

Expression Tree 기반 Specification으로 ORM 통합을 준비합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 5 | [Expression 소개](Part2-Expression-Specification/01-Expression-Introduction/) | Expression Tree 개념과 필요성 |
| 6 | [ExpressionSpecification 클래스](Part2-Expression-Specification/02-ExpressionSpecification-Class/) | sealed IsSatisfiedBy, 델리게이트 캐싱 |
| 7 | [Value Object 변환 패턴](Part2-Expression-Specification/03-ValueObject-Primitive-Conversion/) | Value Object→primitive 변환 |
| 8 | [Expression Resolver](Part2-Expression-Specification/04-Expression-Resolver/) | TryResolve, 재귀 합성 |

### Part 3: Repository 통합

Specification과 Repository를 통합하여 유연한 데이터 조회를 구현합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 9 | [Repository와 Specification](Part3-Repository-Integration/01-Repository-With-Specification/) | Repository 메서드 폭발 방지 |
| 10 | [InMemory 구현](Part3-Repository-Integration/02-InMemory-Implementation/) | InMemory 어댑터 |
| 11 | [PropertyMap](Part3-Repository-Integration/03-PropertyMap/) | PropertyMap, TranslatingVisitor |
| 12 | [EF Core 구현](Part3-Repository-Integration/04-EfCore-Implementation/) | TryResolve + Translate 조합 |

### Part 4: 실전 패턴

실전 프로젝트에서 Specification 패턴을 활용하는 방법을 학습합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 13 | [Usecase 패턴](Part4-Real-World-Patterns/01-Usecase-Patterns/) | CQRS에서 Spec 활용 |
| 14 | [동적 필터 빌더](Part4-Real-World-Patterns/02-Dynamic-Filter-Builder/) | All 시드 조건부 체이닝 |
| 15 | [테스트 전략](Part4-Real-World-Patterns/03-Testing-Strategies/) | Spec/조합/Usecase 테스트 |
| 16 | [아키텍처 규칙](Part4-Real-World-Patterns/04-Architecture-Rules/) | 네이밍, 폴더 배치, ArchUnitNET |

### Part 5: 도메인별 실전 예제

다양한 도메인에서 Specification 패턴을 적용하는 실전 예제입니다.

- [5.1 이커머스 상품 필터링](Part5-Domain-Examples/01-Ecommerce-Product-Filtering/)
- [5.2 고객 관리](Part5-Domain-Examples/02-Customer-Management/)

### [부록](Appendix/)

- [A. Specification vs 대안 비교](Appendix/A-specification-vs-alternatives.md)
- [B. 안티패턴](Appendix/B-anti-patterns.md)
- [C. 용어집](Appendix/C-glossary.md)
- [D. 참고 자료](Appendix/D-references.md)

---

## 핵심 진화 과정

```
1장: 첫 번째 Spec       →  2장: And/Or/Not 조합   →  3장: 연산자 오버로딩
     ↓
4장: All 항등원         →  5장: Expression 소개    →  6장: ExpressionSpec
     ↓
7장: VO 변환 패턴       →  8장: Expression Resolver
     ↓
9장: Repository 통합    →  10장: InMemory 구현     →  11장: PropertyMap
     ↓
12장: EF Core 구현      →  13장: Usecase 패턴      →  14장: 동적 필터
     ↓
15장: 테스트 전략       →  16장: 아키텍처 규칙
```

---

## Functorium Specification 타입 계층

```
Specification<T> (추상 클래스)
├── IsSatisfiedBy(T) : bool
├── And() / Or() / Not()
├── & / | / ! 연산자
└── All (항등원)

ExpressionSpecification<T> (Expression Tree 지원)
├── ToExpression() : Expression<Func<T, bool>>
└── sealed IsSatisfiedBy (컴파일 + 캐싱)

SpecificationExpressionResolver (Expression 합성)
PropertyMap<TEntity, TModel> (Entity→Model 변환)
```

---

## 필수 준비물

- .NET 10.0 SDK 이상
- VS Code + C# Dev Kit 확장
- C# 기초 문법 지식

---

## 프로젝트 구조

```
Implementing-Specification-Pattern/
├── Part0-Introduction/              # Part 0: 서론
├── Part1-Specification-Basics/      # Part 1: Specification 기초 (4개)
│   ├── 01-First-Specification/
│   ├── 02-Composition/
│   ├── 03-Operators/
│   └── 04-All-Identity/
├── Part2-Expression-Specification/  # Part 2: Expression Specification (4개)
│   ├── 01-Expression-Introduction/
│   ├── 02-ExpressionSpecification-Class/
│   ├── 03-ValueObject-Primitive-Conversion/
│   └── 04-Expression-Resolver/
├── Part3-Repository-Integration/    # Part 3: Repository 통합 (4개)
│   ├── 01-Repository-With-Specification/
│   ├── 02-InMemory-Implementation/
│   ├── 03-PropertyMap/
│   └── 04-EfCore-Implementation/
├── Part4-Real-World-Patterns/       # Part 4: 실전 패턴 (4개)
│   ├── 01-Usecase-Patterns/
│   ├── 02-Dynamic-Filter-Builder/
│   ├── 03-Testing-Strategies/
│   └── 04-Architecture-Rules/
├── Part5-Domain-Examples/           # Part 5: 도메인별 실전 예제 (2개)
│   ├── 01-Ecommerce-Product-Filtering/
│   └── 02-Customer-Management/
├── Appendix/                        # 부록
└── README.md                        # 이 문서
```

---

## 테스트

모든 Part의 예제 프로젝트에는 단위 테스트가 포함되어 있습니다. 테스트는 [Guide-01-Unit-Testing.md](../../Docs/Functorium/Guide-01-Unit-Testing.md) 가이드를 따릅니다.

### 테스트 실행 방법

```bash
# Part 1 테스트 실행
cd Books/Implementing-Specification-Pattern/Part1-Specification-Basics/01-First-Specification/FirstSpecification.Tests.Unit
dotnet test

# Part 2 테스트 실행
cd Books/Implementing-Specification-Pattern/Part2-Expression-Specification/01-Expression-Introduction/ExpressionIntroduction.Tests.Unit
dotnet test

# Part 3 테스트 실행
cd Books/Implementing-Specification-Pattern/Part3-Repository-Integration/01-Repository-With-Specification/RepositoryWithSpecification.Tests.Unit
dotnet test

# Part 4 테스트 실행
cd Books/Implementing-Specification-Pattern/Part4-Real-World-Patterns/01-Usecase-Patterns/UsecasePatterns.Tests.Unit
dotnet test

# Part 5 테스트 실행
cd Books/Implementing-Specification-Pattern/Part5-Domain-Examples/01-Ecommerce-Product-Filtering/EcommerceProductFiltering.Tests.Unit
dotnet test
```

### 테스트 프로젝트 구조

**Part 1: Specification 기초** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 1 | `FirstSpecification.Tests.Unit` | IsSatisfiedBy 동작 검증 |
| 2 | `Composition.Tests.Unit` | And, Or, Not 조합 검증 |
| 3 | `Operators.Tests.Unit` | 연산자 오버로딩 검증 |
| 4 | `AllIdentity.Tests.Unit` | All 항등원, 동적 체이닝 |

**Part 2: Expression Specification** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 5 | `ExpressionIntroduction.Tests.Unit` | Expression Tree 기본 |
| 6 | `ExpressionSpecificationClass.Tests.Unit` | sealed IsSatisfiedBy, 캐싱 |
| 7 | `ValueObjectPrimitiveConversion.Tests.Unit` | VO→primitive 변환 |
| 8 | `ExpressionResolver.Tests.Unit` | TryResolve, 재귀 합성 |

**Part 3: Repository 통합** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 9 | `RepositoryWithSpecification.Tests.Unit` | Repository + Spec 통합 |
| 10 | `InMemoryImplementation.Tests.Unit` | InMemory 어댑터 |
| 11 | `PropertyMap.Tests.Unit` | PropertyMap, TranslatingVisitor |
| 12 | `EfCoreImplementation.Tests.Unit` | EF Core TryResolve + Translate |

**Part 4: 실전 패턴** (4개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 13 | `UsecasePatterns.Tests.Unit` | CQRS + Spec 활용 |
| 14 | `DynamicFilterBuilder.Tests.Unit` | 동적 필터 체이닝 |
| 15 | `TestingStrategies.Tests.Unit` | Spec 테스트 패턴 |
| 16 | `ArchitectureRules.Tests.Unit` | 아키텍처 규칙 검증 |

**Part 5: 도메인별 실전 예제** (2개)

| 장 | 테스트 프로젝트 | 주요 테스트 내용 |
|:---:|----------------|-----------------|
| 17 | `EcommerceProductFiltering.Tests.Unit` | 상품 필터링 Spec |
| 18 | `CustomerManagement.Tests.Unit` | 고객 관리 Spec |

### 테스트 명명 규칙

T1_T2_T3 명명 규칙을 따릅니다:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void IsSatisfiedBy_ReturnsTrue_WhenProductIsActive()
{
    // Arrange
    var spec = new ActiveProductSpec();
    var product = new Product { IsActive = true };
    // Act
    var actual = spec.IsSatisfiedBy(product);
    // Assert
    actual.ShouldBeTrue();
}
```

---

## 소스 코드

이 책의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

- 프레임워크 타입: `Src/Functorium/Domains/Specifications/`
- 튜토리얼 프로젝트: `Books/Implementing-Specification-Pattern/`

---

이 책은 Functorium 프로젝트의 실제 Specification 프레임워크 개발 경험을 바탕으로 작성되었습니다.
