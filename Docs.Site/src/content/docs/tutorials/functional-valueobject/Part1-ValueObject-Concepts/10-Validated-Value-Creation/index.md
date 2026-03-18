---
title: "검증된 값 객체 생성"
---

## 개요

Address를 생성할 때 Street, City, PostalCode가 이미 검증된 상태라면, 다시 검증할 필요가 있을까요? 이 장에서는 이미 검증된 값에 대한 불필요한 재검증을 방지하는 **CreateFromValidated 메서드를** 도입하고, 복합 값 객체에서 Create, Validate, CreateFromValidated 세 가지 메서드가 각각 어떤 역할을 담당하는지 구현한다.

## 학습 목표

1. 이미 검증된 값으로 직접 객체를 생성하는 CreateFromValidated 메서드를 구현할 수 있습니다
2. 여러 값 객체의 검증 결과를 LanguageExt의 tuple Apply 패턴으로 조합할 수 있습니다
3. Create, Validate, CreateFromValidated 세 가지 메서드를 상황에 맞게 선택하여 성능과 안전성을 동시에 확보할 수 있습니다

## 왜 필요한가?

이전 단계인 `CreateValidateSeparation`에서는 Create와 Validate의 책임을 분리했습니다. 하지만 복합 값 객체를 다루면 새로운 문제가 드러납니다.

복합 객체를 생성할 때 각 구성 요소가 이미 검증된 상태라면, Create 메서드가 다시 검증을 수행하는 것은 불필요한 연산입니다. 또한 여러 값 객체의 검증 결과를 조합해야 하므로, 모든 검증이 성공했을 때만 복합 객체를 생성하는 안전한 합성이 필요합니다.

이를 해결하기 위해 Create, Validate, CreateFromValidated 세 가지 메서드 패턴을 도입합니다. 상황에 맞는 최적의 생성 방법을 선택하여 성능과 안전성을 동시에 확보할 수 있습니다.

## 핵심 개념

### CreateFromValidated 메서드 (Validated Value Creation)

이미 검증된 값으로 직접 객체를 생성합니다. 한 번 검증된 값은 다시 검증하지 않고 바로 사용하므로 중복 검증 비용이 제거됩니다.

다음 코드는 이전 방식의 중복 검증 문제와 개선된 방식을 보여줍니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 중복 검증 발생
public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue)
{
    // 각 필드를 다시 검증 (불필요한 중복)
    var streetResult = Street.Create(streetValue);  // 검증 수행
    var cityResult = City.Create(cityValue);        // 검증 수행
    var postalCodeResult = PostalCode.Create(postalCodeValue); // 검증 수행

    // 모든 결과를 조합하여 Address 생성
    return CombineResults(streetResult, cityResult, postalCodeResult);
}

// 개선된 방식 (현재 방식) - CreateFromValidated 활용
public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
    Validate(streetValue, cityValue, postalCodeValue)  // 한 번만 검증
        .Map(validatedValues => new Address(
            validatedValues.Street,    // 이미 검증된 값
            validatedValues.City,      // 이미 검증된 값
            validatedValues.PostalCode // 이미 검증된 값
        ))
        .ToFin();

// CreateFromValidated: 검증 없이 직접 생성
public static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
    new Address(street, city, postalCode);  // 검증 없이 바로 생성
```

### 복합 검증 조합 (Composite Validation Composition)

LanguageExt의 tuple Apply 패턴을 활용하면 각 구성 요소의 검증을 독립적으로 수행하고, 모든 검증이 성공했을 때만 복합 객체를 생성할 수 있습니다. 하나라도 실패하면 모든 오류가 수집되어 반환됩니다.

```csharp
// 복합 검증 조합 - tuple Apply 패턴
public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
    string streetValue, string cityValue, string postalCodeValue) =>
    (Street.Validate(streetValue), City.Validate(cityValue), PostalCode.Validate(postalCodeValue))
        .Apply((street, city, postalCode) =>
            (Street: Street.CreateFromValidated(street),       // 검증된 값으로 직접 생성
             City: City.CreateFromValidated(city),             // 검증된 값으로 직접 생성
             PostalCode: PostalCode.CreateFromValidated(postalCode))); // 검증된 값으로 직접 생성
```

### 3가지 메서드 패턴 (Three-Method Pattern)

입력 데이터의 상태에 따라 가장 적합한 생성 방법을 선택합니다.

- **Create**: 원시 데이터로부터 검증 후 생성 (일반적인 사용)
- **Validate**: 검증만 수행하고 객체 생성은 하지 않음 (검증만 필요한 경우)
- **CreateFromValidated**: 이미 검증된 값으로 직접 생성 (성능 최적화)

```csharp
// 3가지 메서드 패턴의 사용 예시
// 1. Create: 원시 데이터로부터 생성
var address1 = Address.Create("123 Main St", "Seoul", "12345");

// 2. Validate: 검증만 수행
var validation = Address.Validate("123 Main St", "Seoul", "12345");
if (validation.IsSucc) { /* 검증 성공 처리 */ }

// 3. CreateFromValidated: 이미 검증된 값으로 직접 생성
var street = Street.CreateFromValidated("123 Main St");
var city = City.CreateFromValidated("Seoul");
var postalCode = PostalCode.CreateFromValidated("12345");
var address2 = Address.CreateFromValidated(street, city, postalCode);
```

## 실전 지침

### 예상 출력
```
=== 복합 값 객체의 3가지 메서드 패턴 ===

1. Create: 검증 후 생성

  성공 케이스:
    성공: 123 Main St, Seoul 12345

  실패 케이스들:
    실패: 거리명은 비어있을 수 없습니다
    성공: 123 Main St, Seoul 123

2. Validate: 검증 메서드

  검증 성공 케이스:
    검증 성공: 123 Main St, Seoul 12345

  검증 실패 케이스:
    검증 실패: 거리명은 비어있을 수 없습니다

3. CreateFromValidated: 이미 검증된 값 객체들로 직접 생성
  생성된 주소: 123 Main St, Seoul 12345
```

### 핵심 구현 포인트
1. **CreateFromValidated 메서드 구현**: internal 접근자로 외부에서는 사용할 수 없지만 내부에서는 활용 가능하도록 설계
2. **복합 검증 조합**: LanguageExt의 tuple Apply 패턴을 활용한 함수형 조합으로 안전한 복합 검증 구현
3. **3가지 메서드 패턴**: 상황에 맞는 최적의 생성 방법을 제공하여 성능과 유연성 확보

## 프로젝트 설명

### 프로젝트 구조
```
ValidatedValueCreation/                    # 메인 프로젝트
├── Program.cs                            # 메인 실행 파일
├── ValueObjects/
│   ├── Address.cs                        # 복합 주소 값 객체 (3가지 메서드 패턴)
│   ├── Street.cs                         # 거리명 값 객체
│   ├── City.cs                           # 도시명 값 객체
│   └── PostalCode.cs                     # 우편번호 값 객체
├── ValidatedValueCreation.csproj         # 프로젝트 파일
└── README.md                            # 메인 문서
```

### 핵심 코드

#### Address.cs - 3가지 메서드 패턴 구현
```csharp
/// <summary>
/// Address 인스턴스를 생성하는 팩토리 메서드
/// 검증 책임을 분리하여 단일 책임 원칙 준수
/// </summary>
public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
    Validate(streetValue, cityValue, postalCodeValue)
        .Map(validatedValues => new Address(
            validatedValues.Street,
            validatedValues.City,
            validatedValues.PostalCode))
        .ToFin();

/// <summary>
/// 이미 검증된 값 객체들로부터 Address 인스턴스를 생성하는 internal 메서드
/// 외부(부모)에서만 사용하며, 자기 자신의 Create에서는 사용하지 않음
/// </summary>
public static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
    new Address(street, city, postalCode);

/// <summary>
/// 검증 책임 - 단일 책임 원칙
/// 각 구성 요소들의 검증을 조합하여 복합 검증 수행
/// </summary>
public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
    string streetValue, string cityValue, string postalCodeValue) =>
    (Street.Validate(streetValue), City.Validate(cityValue), PostalCode.Validate(postalCodeValue))
        .Apply((street, city, postalCode) =>
            (Street: Street.CreateFromValidated(street),
             City: City.CreateFromValidated(city),
             PostalCode: PostalCode.CreateFromValidated(postalCode)));
```

#### Program.cs - 3가지 메서드 패턴 사용 예제
```csharp
// 1. Create: 검증 후 생성
var successResult = Address.Create("123 Main St", "Seoul", "12345");
successResult.Match(
    Succ: address => Console.WriteLine($"    성공: {address}"),
    Fail: error => Console.WriteLine($"    실패: {error}")
);

// 2. Validate: 검증만 수행
var successValidation = Address.Validate("123 Main St", "Seoul", "12345");
successValidation.Match(
    Succ: validatedValues => Console.WriteLine($"    검증 성공: {validatedValues.Street}, {validatedValues.City} {validatedValues.PostalCode}"),
    Fail: error => Console.WriteLine($"    검증 실패: {error}")
);

// 3. CreateFromValidated: 이미 검증된 값으로 직접 생성
var street = Street.CreateFromValidated("123 Main St");
var city = City.CreateFromValidated("Seoul");
var postalCode = PostalCode.CreateFromValidated("12345");
var address = Address.CreateFromValidated(street, city, postalCode);
```

## 한눈에 보는 정리

다음 표는 이전 방식(Create + Validate)과 현재 방식(3가지 메서드 패턴)의 차이를 비교합니다.

| 구분 | 이전 방식 | 현재 방식 |
|------|-----------|-----------|
| **메서드 수** | Create, Validate 2개 | Create, Validate, CreateFromValidated 3개 |
| **중복 검증** | 복합 객체 생성 시 각 구성 요소 재검증 | CreateFromValidated로 중복 검증 방지 |
| **성능** | 모든 경우에 검증 수행 | 상황에 맞는 최적화된 생성 |
| **복합 검증** | 단순한 순차 검증 | tuple Apply 패턴으로 안전한 조합 |
| **유연성** | 제한적인 생성 방법 | 3가지 상황별 최적화된 생성 방법 |

## FAQ

### Q1: CreateFromValidated 메서드는 언제 사용해야 하나요?
**A**: 값이 이미 검증된 것이 확실한 경우에만 사용합니다. 대표적으로 데이터베이스에서 조회한 값(저장 시점에 검증 완료), 복합 값 객체의 Validate 내부에서 자식 객체 생성, 비즈니스 로직에서 검증 완료 후 객체 생성이 해당됩니다.

### Q2: Validate 메서드에서 왜 CreateFromValidated를 사용하나요?
**A**: 성능 최적화와 타입 일치를 위해서입니다. Validate 성공 시 반환 타입이 값 객체여야 하는데, Create를 호출하면 `Fin<T>`를 반환하므로 다시 `T`로 변환해야 합니다. CreateFromValidated는 이미 검증된 원시 값을 받아 바로 값 객체를 반환하므로 불필요한 재검증과 타입 변환을 모두 피할 수 있습니다.

### Q3: CreateFromValidated가 internal인 이유는 무엇인가요?
**A**: 외부 코드에서 검증되지 않은 값으로 객체를 생성하는 실수를 방지하기 위해서입니다. `internal` 접근자로 같은 어셈블리 내에서만 사용하도록 제한하면, 도메인 무결성을 보장하면서도 내부적으로는 성능 최적화를 활용할 수 있습니다.

```csharp
// 올바른 사용: Address 내부 어셈블리에서만 사용
public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(...) =>
    (Street.Validate(streetValue), City.Validate(cityValue), PostalCode.Validate(postalCodeValue))
        .Apply((street, city, postalCode) =>
            (Street: Street.CreateFromValidated(street),              // 내부 어셈블리에서만 사용
             City: City.CreateFromValidated(city),                    // 내부 어셈블리에서만 사용
             PostalCode: PostalCode.CreateFromValidated(postalCode))); // 내부 어셈블리에서만 사용

// 잘못된 사용: 외부 어셈블리에서 직접 사용 (컴파일 에러)
// var street = Street.CreateFromValidated("123 Main St"); // 접근 불가
```

3가지 메서드 패턴으로 복합 값 객체의 성능과 안전성을 모두 확보했습니다. 다음 장에서는 이 패턴들을 재사용 가능한 프레임워크 타입으로 추상화하는 방법을 다룹니다.
