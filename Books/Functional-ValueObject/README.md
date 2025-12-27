# 함수형 값 객체 튜토리얼 (Functional Value Object Tutorial)

## 목차
- [개요](#개요)<br/>
- [학습 목표](#학습-목표)<br/>
- [왜 필요한가?](#왜-필요한가)<br/>
- [핵심 개념](#핵심-개념)<br/>
- [학습 경로](#학습-경로)<br/>
- [프로젝트 구조](#프로젝트-구조)<br/>
- [실전 지침](#실전-지침)<br/>
- [한눈에 보는 정리](#한눈에-보는-정리)<br/>
- [FAQ](#faq)

## 개요

이 튜토리얼은 **함수형 프로그래밍 원칙을 적용한 값 객체(Value Object) 구현**을 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 기본적인 나눗셈 함수에서 시작하여 완성된 패턴까지, **27개의 실습 프로젝트**를 통해 함수형 값 객체의 모든 측면을 체계적으로 학습할 수 있습니다.

> **단순한 예외 기반 함수에서 시작하여 타입 안전한 함수형 값 객체로 진화하는 과정을 함께 경험해보세요.**

## 학습 목표

### **핵심 학습 목표**
1. **함수형 프로그래밍 원칙 이해**
   - 순수 함수와 부작용 제거
   - 예외 vs 도메인 타입의 차이점
   - 타입 안전성을 통한 조기 오류 발견

2. **값 객체 패턴 마스터**
   - 프레임워크 타입 활용
   - 복합 값 객체 설계와 구현
   - 검증 패턴 (Bind, Apply, 조합)

### **실습을 통해 확인할 내용**
- **27개 프로젝트**: 기본 개념부터 완성된 패턴까지 단계별 학습
- **3개 섹션**: 개념 이해 → 검증 패턴 → 패턴 완성
- **실전 예제**: 이메일, 주소, 좌표, 날짜 범위 등 실제 도메인 모델

## 왜 필요한가?

현대 소프트웨어 개발에서 **타입 안전성**과 **예측 가능한 동작**은 매우 중요합니다. 하지만 대부분의 프로젝트에서는 여전히 **예외 기반 오류 처리**와 **원시 타입 남용**으로 인한 문제들이 발생하고 있습니다.

**첫 번째 문제는 예외 기반 오류 처리입니다.** 예상 가능한 실패(사용자 입력 오류, 비즈니스 규칙 위반)를 예외로 처리하면 프로그램이 갑자기 중단되어 사용자 경험이 나빠집니다. 이는 마치 **시스템 리소스 부족**이나 **네트워크 연결 실패**와 같은 예외적인 상황이 아니라, **예상 가능한 도메인 규칙 위반**임에도 불구하고 예외를 발생시키는 것입니다.

**두 번째 문제는 원시 타입의 남용입니다.** `string`, `int`, `decimal` 같은 원시 타입으로는 도메인의 제약 조건과 비즈니스 규칙을 표현할 수 없습니다. 이는 **도메인 주도 설계(DDD)** 관점에서 도메인 개념이 코드에 명확하게 반영되지 않는 문제입니다.

**세 번째 문제는 함수형 프로그래밍 원칙의 부재입니다.** 부작용이 있는 함수, 예측 불가능한 동작, 조합하기 어려운 코드는 유지보수성과 테스트 가능성을 크게 저하시킵니다.

이러한 문제들을 해결하기 위해 **함수형 값 객체**를 도입해야 합니다. 함수형 값 객체를 사용하면 예상 가능한 실패를 명시적으로 처리하고, 도메인 규칙을 코드에 명확하게 표현하며, 순수 함수의 장점을 활용할 수 있습니다.

## 핵심 개념

이 튜토리얼의 핵심은 크게 4가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 1. 함수형 프로그래밍 원칙 (Functional Programming Principles)

**핵심 아이디어는 "순수 함수와 부작용 제거를 통한 예측 가능한 코드 작성"입니다.**

순수 함수는 마치 수학 공식처럼 예측 가능하게 동작하는 함수입니다. **동일한 입력에 대해 항상 동일한 출력을 반환하고, 부작용이 없다**는 함수형 프로그래밍의 핵심 원칙을 따릅니다.

```csharp
// 문제가 있는 함수 (순수하지 않음) - 예외 발생으로 프로그램 중단
public int Divide(int x, int y)
{
    // y가 0이면 예외 발생! (부작용)
    return x / y;
}

// 개선된 함수 (순수 함수) - 예외 없이 안전한 연산
public int Divide(int x, Denominator y)
{
    // y는 항상 유효함을 보장 (부작용 없음)
    return x / y.Value;
}
```

이렇게 개선하면 함수가 **예측 가능하게 동작**하고, **예외로 인한 프로그램 중단을 방지**할 수 있습니다. 이는 **함수형 프로그래밍의 합성성(Composability)**을 가능하게 만듭니다.

### 2. 값 객체 패턴 (Value Object Pattern)

**핵심 아이디어는 "도메인 개념을 타입으로 표현하여 컴파일 타임에 오류를 방지"입니다.**

값 객체는 도메인의 개념을 타입으로 표현하는 DDD의 핵심 패턴입니다. **불변성(Immutability)**, **값 동등성(Value Equality)**, **타입 안전성(Type Safety)**을 보장합니다.

```csharp
// 원시 타입 사용 (문제가 있는 방식)
public class User
{
    public string Email { get; set; }  // 어떤 문자열이든 허용
    public int Age { get; set; }       // 음수도 허용
}

// 값 객체 사용 (개선된 방식)
public class User
{
    public Email Email { get; }        // 유효한 이메일만 허용
    public Age Age { get; }            // 유효한 나이만 허용
}
```

이렇게 하면 **컴파일러가 컴파일 시점에 잘못된 사용을 방지**할 수 있어서, 런타임에 예상치 못한 오류가 발생할 가능성을 줄일 수 있습니다.

### 3. 함수형 검증 패턴 (Functional Validation Patterns)

**핵심 아이디어는 "모나드 체이닝을 통한 선언적이고 조합 가능한 검증 로직"입니다.**

함수형 프로그래밍의 모나드 개념을 활용하여 검증 로직을 체이닝으로 조합할 수 있습니다. **Bind**, **Apply**, **조합 패턴**을 통해 복잡한 검증 규칙을 선언적으로 표현할 수 있습니다.

```csharp
// 명령형 검증 (문제가 있는 방식)
public static User CreateUser(string email, int age)
{
    if (string.IsNullOrEmpty(email)) throw new ArgumentException();
    if (!IsValidEmail(email)) throw new ArgumentException();
    if (age < 0) throw new ArgumentException();
    if (age > 150) throw new ArgumentException();
    // ... 복잡한 중첩 조건문
}

// 함수형 검증 (개선된 방식)
public static Validation<Error, User> CreateUser(string email, int age) =>
    from validEmail in Email.Validate(email)
    from validAge in Age.Validate(age)
    select new User(validEmail, validAge);
```

이 방식의 장점은 **선언적이고 읽기 쉬운 코드**를 작성할 수 있고, **에러 처리가 자동화**되어 실수 가능성이 크게 줄어든다는 것입니다.


## 학습 경로

이 튜토리얼은 **3개의 주요 섹션**으로 구성되어 있으며, 각 섹션은 **단계별로 진행**되어야 합니다.

### 📚 **1단계: 값 객체 개념 이해 (01-Concept)**

기본 개념부터 프레임워크까지 체계적으로 학습합니다.

| 순서 | 프로젝트 | 핵심 학습 내용 | 예상 소요 시간 |
|------|----------|----------------|----------------|
| **01** | `01-Basic-Divide` | 예외 vs 도메인 타입의 차이점 | 30분 |
| **02** | `02-Defensive-Programming` | 방어적 프로그래밍과 사전 검증 | 30분 |
| **03** | `03-Functional-Result` | 함수형 결과 타입 (Fin, Validation) | 45분 |
| **04** | `04-Always-Valid` | 항상 유효한 값 객체 구현 | 45분 |
| **05** | `05-Operator-Overloading` | 연산자 오버로딩과 타입 변환 | 45분 |
| **06** | `06-Linq-Expression` | LINQ 표현식과 함수형 조합 | 45분 |
| **07** | `07-Value-Equality` | 값 동등성과 해시코드 | 30분 |
| **08** | `08-Value-Comparability` | 비교 가능성과 정렬 | 45분 |
| **09** | `09-Create-Validate-Separation` | 생성과 검증의 분리 | 30분 |
| **10** | `10-Validated-Value-Creation` | 검증된 값 생성 패턴 | 45분 |
| **11** | `11-ValueObject-Framework` | 프레임워크 타입 | 60분 |
| **12** | `12-Type-Safe-Enums` | 타입 안전한 열거형 | 45분 |
| **13** | `13-Error-Code` | 구조화된 에러 코드 | 45분 |
| **14** | `14-Architecture-Test` | 아키텍처 테스트와 규칙 | 30분 |

### 🔍 **2단계: 검증 패턴 마스터 (02-Validation)**

함수형 검증 패턴을 심화 학습합니다.

| 순서 | 프로젝트 | 핵심 학습 내용 | 예상 소요 시간 |
|------|----------|----------------|----------------|
| **01** | `01-Bind-Sequential-Validation` | Bind를 통한 순차 검증 | 45분 |
| **02** | `02-Apply-Parallel-Validation` | Apply를 통한 병렬 검증 | 45분 |
| **03** | `03-Apply-Bind-Combined-Validation` | Apply와 Bind 조합 | 60분 |
| **04** | `04-Apply-Internal-Bind-Validation` | 내부 Bind와 외부 Apply | 45분 |
| **05** | `05-Bind-Internal-Apply-Validation` | 내부 Apply와 외부 Bind | 45분 |

### 🏗️ **3단계: 값 객체 패턴 완성 (03-Patterns)**

완성된 값 객체 패턴을 실전 프로젝트로 적용해봅니다.

| 순서 | 프로젝트 | 핵심 학습 내용 | 예상 소요 시간 |
|------|----------|----------------|----------------|
| **01** | `01-SimpleValueObject` | 비교 불가능한 단일 값 객체 | 45분 |
| **02** | `02-ComparableSimpleValueObject` | 비교 가능한 단일 값 객체 | 45분 |
| **03** | `03-ValueObject-Primitive` | 비교 불가능한 복합 원시 타입 | 45분 |
| **04** | `04-ComparableValueObject-Primitive` | 비교 가능한 복합 원시 타입 | 45분 |
| **05** | `05-ValueObject-Composite` | 비교 불가능한 복합 값 객체 | 45분 |
| **06** | `06-ComparableValueObject-Composite` | 비교 가능한 복합 값 객체 | 60분 |
| **07** | `07-TypeSafeEnum` | 타입 안전한 열거형 | 45분 |
| **08** | `08-Architecture-Test` | 아키텍처 테스트와 규칙 | 30분 |


## 프로젝트 구조

```
FunctionalValueObjectTutorial/
├── 01-Concept/                 # 값 객체 개념 이해 (14개 프로젝트)
│   ├── 01-Basic-Divide/                    # 기본 나눗셈에서 시작
│   ├── 02-Defensive-Programming/           # 방어적 프로그래밍
│   ├── 03-Functional-Result/               # 함수형 결과 타입
│   ├── 04-Always-Valid/                    # 항상 유효한 값 객체
│   ├── 05-Operator-Overloading/            # 연산자 오버로딩
│   ├── 06-Linq-Expression/                 # LINQ 표현식
│   ├── 07-Value-Equality/                  # 값 동등성
│   ├── 08-Value-Comparability/             # 비교 가능성
│   ├── 09-Create-Validate-Separation/      # 생성과 검증 분리
│   ├── 10-Validated-Value-Creation/        # 검증된 값 생성
│   ├── 11-ValueObject-Framework/           # 값 객체 프레임워크
│   ├── 12-Type-Safe-Enums/                 # 타입 안전한 열거형
│   ├── 13-Error-Code/                      # 구조화된 에러 코드
│   └── 14-Architecture-Test/               # 아키텍처 테스트
├── 02-Validation/              # 검증 패턴 마스터 (5개 프로젝트)
│   ├── 01-Bind-Sequential-Validation/      # Bind 순차 검증
│   ├── 02-Apply-Parallel-Validation/       # Apply 병렬 검증
│   ├── 03-Apply-Bind-Combined-Validation/  # Apply와 Bind 조합
│   ├── 04-Apply-Internal-Bind-Validation/  # 내부 Bind, 외부 Apply
│   └── 05-Bind-Internal-Apply-Validation/  # 내부 Apply, 외부 Bind
├── 03-Patterns/                # 값 객체 패턴 완성 (8개 프로젝트)
│   ├── 01-SimpleValueObject/               # 비교 불가능한 단일 값 객체
│   ├── 02-ComparableSimpleValueObject/     # 비교 가능한 단일 값 객체
│   ├── 03-ValueObject-Primitive/           # 비교 불가능한 복합 원시 타입
│   ├── 04-ComparableValueObject-Primitive/ # 비교 가능한 복합 원시 타입
│   ├── 05-ValueObject-Composite/           # 비교 불가능한 복합 값 객체
│   ├── 06-ComparableValueObject-Composite/ # 비교 가능한 복합 값 객체
│   ├── 07-TypeSafeEnum/                    # 타입 안전한 열거형
│   └── 08-Architecture-Test/               # 아키텍처 테스트
├── Framework/                              # 공통 프레임워크
│   ├── Abstractions/                       # 추상화 인터페이스
│   └── Layers/                             # 계층별 구현
└── README.md                               # 이 문서
```

## 실전 지침

### 시작하기 전 준비사항

1. **.NET 9.0 SDK** 설치
2. **Visual Studio 2022** 또는 **VS Code** 설치
3. **LanguageExt.Core** 패키지 이해 (함수형 프로그래밍 라이브러리)

### 학습 방법

1. **순차적 학습**: 각 프로젝트를 순서대로 진행
2. **실습 중심**: 코드를 직접 실행하고 결과 확인
3. **이해 중심**: 왜 그렇게 구현하는지 이해
4. **실전 적용**: 학습한 내용을 실제 프로젝트에 적용

### 각 프로젝트 실행 방법

```bash
# 특정 프로젝트로 이동
cd 01-Concept/01-Basic-Divide/BasicDivide

# 프로젝트 실행
dotnet run

# 테스트 실행
cd ../BasicDivide.Tests.Unit
dotnet test
```

### 예상 출력 예시

```
=== 기본 나눗셈 함수 테스트 ===

정상 케이스:
10 / 2 = 5

예외 케이스:
10 / 0 = System.DivideByZeroException: Attempted to divide by zero.
```


## 한눈에 보는 정리

### 학습 진화 과정

| 단계 | 핵심 개념 | 주요 기술 | 예시 |
|------|-----------|------------|------|
| **1단계** | 예외 → 도메인 타입 | `Fin<T>`, `Validation<Error, T>` | `Denominator`, `Email` |
| **2단계** | 검증 패턴 | `Bind`, `Apply`, 조합 | `Address`, `UserRegistration` |
| **3단계** | 완성된 패턴 | 7가지 프레임워크 타입 | `BinaryData`, `Coordinate`, `PriceRange`, `Currency` |

### 프레임워크 타입

| 타입 | 베이스 클래스 | `IComparable<T>` | 특징 | 예시 |
|------|---------------|----------------|------|------|
| **비교 불가능한 primitive** | `SimpleValueObject<T>` | ❌ | 단일 값, 동등성만 | `BinaryData` |
| **비교 불가능한 복합 primitive** | `ValueObject` | ❌ | 여러 primitive, 동등성만 | `Coordinate` |
| **비교 불가능한 복합** | `ValueObject` | ❌ | 여러 값 객체, 동등성만 | `Address` |
| **비교 가능한 primitive** | `ComparableSimpleValueObject<T>` | ✅ | 단일 값, 비교 기능 | `Denominator` |
| **비교 가능한 복합 primitive** | `ComparableValueObject` | ✅ | 여러 primitive, 비교 기능 | `DateRange` |
| **비교 가능한 복합** | `ComparableValueObject` | ✅ | 여러 값 객체, 비교 기능 | `PriceRange` |
| **열거형** | `SmartEnum<TValue, TKey>` + `IValueObject` | ✅ | 타입 안전한 열거형, 도메인 로직 | `Currency` |

### 검증 패턴 비교

| 패턴 | 실행 방식 | 에러 처리 | 사용 시기 |
|------|-----------|-----------|-----------|
| **Bind** | 순차 실행, 조기 중단 | 첫 번째 에러만 | 의존성 검증 |
| **Apply** | 병렬 실행, 모든 에러 수집 | 모든 에러 수집 | 독립적 검증 |
| **조합** | 혼합 사용 | 상황에 따라 | 복잡한 검증 |

## FAQ

### Q1: 함수형 프로그래밍 경험이 없어도 따라할 수 있나요?
**A**: 네, 가능합니다. 이 튜토리얼은 함수형 프로그래밍 경험이 없는 개발자도 따라할 수 있도록 설계되었습니다. 각 개념을 단계별로 설명하고, 실제 코드 예제를 통해 이해할 수 있도록 구성되어 있습니다. 특히 LanguageExt 라이브러리의 `Fin<T>`와 `Validation<Error, T>` 타입을 중심으로 설명하므로, 함수형 프로그래밍의 복잡한 이론보다는 실용적인 활용에 집중할 수 있습니다.

### Q2: 실제 프로젝트에서 바로 적용할 수 있나요?
**A**: 네, 바로 적용할 수 있습니다. 이 튜토리얼의 모든 예제는 실제 프로젝트에서 자주 사용되는 도메인 모델(이메일, 주소, 좌표, 날짜 범위 등)을 기반으로 구성되어 있습니다. 특히 1단계의 **ValueObject Framework**(11번 프로젝트)와 3단계의 **완성된 패턴들**(03-Patterns)을 사용하면 기존 프로젝트에 점진적으로 도입할 수 있습니다.

### Q3: 성능 최적화는 언제 고려해야 하나요?
**A**: 성능 최적화는 **성능 문제가 실제로 발생했을 때** 고려해야 합니다. 일반적인 CRUD 애플리케이션에서는 1-2단계의 내용만으로도 충분합니다. 대량의 데이터 처리나 실시간 시스템에서 성능 문제가 발생할 때 추가적인 최적화 기법을 고려하는 것이 좋습니다.

### Q4: 기존 코드를 값 객체로 리팩토링하는 방법은?
**A**: 점진적 리팩토링을 권장합니다:

1. **1단계**: 새로운 기능부터 값 객체 적용
2. **2단계**: 기존 코드의 중요한 부분부터 점진적 변경
3. **3단계**: 테스트 코드를 통해 안전성 확보

### Q5: 값 객체와 엔티티의 차이점은 무엇인가요?
**A**: 값 객체와 엔티티는 DDD의 핵심 개념으로, 다음과 같이 구분됩니다:

**값 객체 (Value Object)**:
- **식별성**: 값으로 식별 (예: 이메일 주소)
- **불변성**: 생성 후 변경 불가
- **동등성**: 값이 같으면 같은 객체
- **예시**: `Email`, `Address`, `Money`

**엔티티 (Entity)**:
- **식별성**: 고유 ID로 식별
- **가변성**: 생명주기 동안 상태 변경 가능
- **동등성**: ID가 같으면 같은 객체
- **예시**: `User`, `Order`, `Product`

### Q6: 값 객체를 데이터베이스에 저장하는 방법은?
**A**: 값 객체는 일반적으로 **임베디드 값(Embedded Value)**으로 저장합니다:

```csharp
// 값 객체
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
}

// 엔티티에서 사용
public class User : Entity
{
    public Address Address { get; set; }  // 임베디드 값으로 저장
}
```

Entity Framework에서는 `OwnsOne` 또는 `OwnsMany`를 사용하여 매핑할 수 있습니다.

### Q7: 값 객체의 메모리 사용량이 걱정됩니다. 괜찮을까요?
**A**: 값 객체는 일반적으로 **메모리 효율적**입니다. 값 객체는 **불변성**을 보장하므로 가비지 컬렉션 부담도 줄어듭니다. 또한 프레임워크를 사용하면 공통 기능을 재사용하여 메모리 사용량을 최적화할 수 있습니다.

### Q8: 팀에서 값 객체를 도입할 때 주의사항은?
**A**: 팀 도입 시 다음 사항들을 고려하세요:

1. **교육**: 팀원들에게 함수형 프로그래밍과 값 객체 개념 교육
2. **가이드라인**: 코딩 표준과 네이밍 컨벤션 정립
3. **점진적 도입**: 새로운 기능부터 시작하여 점진적 확산
4. **리뷰**: 코드 리뷰를 통한 일관성 확보
5. **문서화**: 팀 내부 문서와 예제 코드 작성

### Q9: 3단계(03-Patterns)의 역할은 무엇인가요?
**A**: 3단계는 **완성된 값 객체 패턴을 실전 적용**하는 단계입니다. 1-2단계에서 학습한 이론과 프레임워크를 실제 프로젝트 구조로 구현하여 실무에서 바로 사용할 수 있는 형태로 제공합니다.

**3단계의 특징:**
- **프레임워크 타입 완전 구현**: 단순 값 객체에서 복합 값 객체, 열거형까지 모든 패턴 제공
- **실전 예제 중심**: BinaryData, Coordinate, DateRange, Email, Address, Currency 등 실제 도메인 적용
- **비교 기능 체계적 학습**: 비교 불가능/가능한 각 타입별로 구분하여 학습
- **패턴 완성**: 값 객체의 모든 측면(단일/복합, 비교 가능성, 열거형)을 아우르는 완전한 패턴 제공
- **아키텍처 테스트**: ArchUnitNET을 활용한 자동화된 아키텍처 규칙 검증

이 단계는 특히 **기존 프로젝트에 값 객체를 도입하려는 개발자**에게 유용하며, 각 패턴의 장단점과 적용 시기를 이해할 수 있습니다.

### Q10: 08번 아키텍처 테스트 프로젝트의 역할은 무엇인가요?
**A**: 08번 프로젝트는 **아키텍처 테스트를 통한 품질 보장**을 담당합니다. ArchUnitNET을 활용하여 01-07번 프로젝트의 모든 값 객체가 ValueObject 규칙을 올바르게 준수하고 있는지 자동으로 검증합니다.

**08번 프로젝트의 특징:**
- **자동화된 품질 보장**: 수동 검토 없이 아키텍처 규칙 준수 확인
- **다중 어셈블리 검증**: 7개 프로젝트의 모든 값 객체를 동시에 검증
- **지속적 통합 지원**: CI/CD 파이프라인에서 자동 실행 가능
- **확장 가능성**: 새로운 프로젝트 추가 시 쉽게 확장

이 프로젝트를 통해 **값 객체 구현의 일관성과 품질을 지속적으로 보장**할 수 있습니다.
