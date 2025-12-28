# 단순 값 객체
> `SimpleValueObject<T>`

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 값 객체(Value Object) 패턴의 가장 기본적인 형태인 `SimpleValueObject<T>`를 이해하고 실습하는 것을 목표로 합니다. 값 객체의 핵심 개념인 불변성(Immutable), 값 기반 동등성(Value-based Equality), 타입 안전성(Type Safety)을 학습합니다.

## 학습 목표

### **핵심 학습 목표**
1. **값 객체의 기본 개념 이해**: 값 객체가 무엇인지, 왜 필요한지, 어떻게 구현하는지 학습합니다.
2. **불변성 원칙 실습**: 값 객체의 불변성(Immutability)이 어떻게 보장되는지 직접 확인합니다.
3. **값 기반 동등성 적용**: 참조 동등성이 아닌 값 동등성이 어떻게 구현되는지 실습합니다.
4. **타입 안전성 체험**: 컴파일 타임에 타입 오류를 방지하는 방법을 경험합니다.

### **실습을 통해 확인할 내용**
- `SimpleValueObject<T>`의 기본적인 사용 방법
- 동등성 비교(`==`, `!=`)와 해시코드(`GetHashCode()`)의 동작
- 명시적 타입 변환의 활용
- 값 객체의 불변성 보장 메커니즘

## 왜 필요한가?

이전 단계에서는 일반적인 클래스나 구조체를 사용하여 데이터를 표현했습니다. 하지만 실제 애플리케이션 개발에서 이러한 기본적인 데이터 타입을 사용할 때 몇 가지 문제가 발생했습니다.

**첫 번째 문제는 타입 안전성 부족입니다.** 예를 들어, 사용자 ID와 주문 ID가 모두 `int` 타입이라면, 실수로 `userId = orderId`와 같은 코드를 작성할 수 있습니다. 이는 마치 함수형 프로그래밍에서 타입 시스템이 부족할 때 발생하는 문제와 유사합니다.

**두 번째 문제는 의미 있는 동작의 부재입니다.** 기본 타입들은 단지 데이터를 저장할 뿐, 비즈니스 로직이나 유효성 검증과 같은 의미 있는 동작을 포함할 수 없습니다. 이는 마치 객체지향 프로그래밍에서 캡슐화가 제대로 이루어지지 않은 것처럼, 데이터와 관련된 로직이 분산되어 유지보수가 어려워집니다.

**세 번째 문제는 불변성 보장의 어려움입니다.** 기본 타입들은 값이 변경될 수 있어, 예상치 못한 부작용이 발생할 수 있습니다. 이는 마치 함수형 프로그래밍에서 순수 함수가 아닌 함수를 사용할 때 발생하는 문제와 유사합니다.

이러한 문제들을 해결하기 위해 값 객체 패턴을 도입했습니다. 값 객체를 사용하면 타입 안전성을 보장하고, 의미 있는 동작을 포함하며, 불변성을 유지할 수 있습니다. 이는 마치 디자인 패턴에서 팩토리 패턴을 사용하여 객체 생성을 캡슐화하는 것처럼, 데이터의 생성과 검증을 안전하게 관리할 수 있게 됩니다.

## 핵심 개념

이 프로젝트의 핵심은 값 객체의 가장 기본적인 형태인 `SimpleValueObject<T>`를 이해하는 것입니다. 값 객체는 크게 세 가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: 값 기반 동등성

값 객체의 가장 중요한 특징은 값 기반 동등성입니다. 일반적인 클래스와 달리, 값 객체는 참조가 아닌 값으로 동등성을 판단합니다.

**핵심 아이디어는 "내용이 같으면 동일한 객체"입니다.** 일반적인 클래스에서는 두 개의 인스턴스가 메모리 주소가 다르면 다른 객체로 취급됩니다. 하지만 값 객체에서는 내부 값이 같으면 동일한 객체로 취급됩니다.

예를 들어, 두 개의 `BinaryData` 객체가 같은 바이트 배열을 가지고 있다면, 이들은 동일한 객체로 취급되어야 합니다. 이는 마치 데이터베이스에서 기본 키가 아닌 내용으로 레코드를 비교하는 것과 유사합니다.

```csharp
// 일반적인 클래스: 참조 동등성
var obj1 = new SomeClass { Value = 42 };
var obj2 = new SomeClass { Value = 42 };
Console.WriteLine(obj1 == obj2); // false (참조가 다름)

// 값 객체: 값 동등성
var data1 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
Console.WriteLine(data1 == data2); // true (값이 같음)
```

이러한 값 기반 동등성은 컬렉션에서의 검색이나 비교 연산에서 매우 유용합니다. 값이 같으면 같은 것으로 취급되므로, 중복 제거나 검색이 직관적으로 작동합니다.

### 두 번째 개념: 불변성 보장

값 객체는 생성 후에는 그 값을 변경할 수 없습니다. 이는 예상치 못한 부작용을 방지하고, 코드의 예측 가능성을 높여줍니다.

**핵심 아이디어는 "한 번 생성된 값은 영원히 유지"입니다.** 값 객체는 생성자를 `private`으로 선언하고, 정적 팩토리 메서드를 통해서만 생성할 수 있도록 설계합니다.

예를 들어, `BinaryData` 객체를 생성한 후에는 그 내부의 바이트 배열을 변경할 수 없습니다. 이는 마치 함수형 프로그래밍에서 불변 데이터 구조를 사용하는 것과 유사합니다.

```csharp
// 값 객체: 불변성 보장
var data = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = (BinaryData)data;

// data의 값을 변경할 수 없음
// data.Value = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 }; // 컴파일 에러

// 새로운 값 객체를 생성해야 함
var newData = BinaryData.Create(new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 });
```

이러한 불변성은 동시성 프로그래밍에서 특히 유용합니다. 여러 스레드가 동시에 값 객체를 읽어도 경쟁 상태가 발생하지 않습니다.

### 세 번째 개념: 타입 안전성

값 객체는 컴파일 타임에 타입 오류를 방지합니다. 기본 타입 대신 의미 있는 타입을 사용하여 실수를 줄입니다.

**핵심 아이디어는 "의미 있는 타입으로 실수 방지"입니다.** 기본 타입 대신 도메인에 맞는 의미 있는 타입을 사용하면, 컴파일러가 타입 불일치를 미리 발견할 수 있습니다.

예를 들어, 사용자 ID와 상품 ID가 모두 `int`라면 실수로 혼용할 수 있지만, `UserId`와 `ProductId`라는 별도의 값 객체 타입을 사용하면 이러한 실수를 컴파일 타임에 방지할 수 있습니다.

```csharp
// 기본 타입: 타입 안전성 부족
int userId = 123;
int productId = 456;
userId = productId; // 컴파일은 되지만 논리적 오류

// 값 객체: 타입 안전성 보장
UserId userId = UserId.Create(123);
ProductId productId = ProductId.Create(456);
// userId = productId; // 컴파일 에러 - 타입 불일치
```

이러한 타입 안전성은 대규모 애플리케이션 개발에서 특히 중요합니다. 컴파일러가 타입 오류를 미리 발견해주므로, 런타임 오류를 크게 줄일 수 있습니다.

## 실전 지침

### 예상 출력
```
=== 1. 비교 불가능한 primitive 값 객체 - SimpleValueObject<T> ===
부모 클래스: SimpleValueObject<byte[]>
예시: BinaryData (이진 데이터)

📋 특징:
   ✅ 기본적인 동등성 비교와 해시코드 제공
   ❌ 비교 연산자는 지원하지 않음 (IComparable<T> 미구현)
   ✅ 명시적 타입 변환 지원
   ✅ 단순한 값 래핑에 적합

🔍 성공 케이스:
   ✅ BinaryData(Hello): BinaryData[5 bytes: 48 65 6C 6C 6F]
   ✅ BinaryData(World): BinaryData[5 bytes: 57 6F 72 6C 64]
   ✅ BinaryData(Hello): BinaryData[5 bytes: 48 65 6C 6C 6F]

📊 동등성 비교:
   BinaryData[5 bytes: 48 65 6C 6C 6F] == BinaryData[5 bytes: 57 6F 72 6C 64] = False
   BinaryData[5 bytes: 48 65 6C 6C 6F] == BinaryData[5 bytes: 48 65 6C 6C 6F] = True

🔄 타입 변환:
   (byte[])BinaryData[5 bytes: 48 65 6C 6C 6F] = [0x48, 0x65, 0x6C, 0x6C, 0x6F]

🔢 해시코드:
   BinaryData[5 bytes: 48 65 6C 6C 6F].GetHashCode() = -1711187277
   BinaryData[5 bytes: 48 65 6C 6C 6F].GetHashCode() = -1711187277
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   BinaryData(null): DomainErrors.BinaryData.Empty
   BinaryData(empty): DomainErrors.BinaryData.Empty

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트
1. **`SimpleValueObject<T>` 상속**: 기본적인 값 객체 기능을 상속받습니다.
2. **private 생성자**: 외부에서 직접 생성하지 못하도록 제한합니다.
3. **정적 Create 메서드**: 유효성 검증과 객체 생성을 담당합니다.
4. **DomainErrors**: 구조화된 에러 처리를 위한 중첩 클래스입니다.

## 프로젝트 설명

### 프로젝트 구조
```
01-SimpleValueObject/
├── Program.cs                    # 메인 실행 파일
├── SimpleValueObject.csproj     # 프로젝트 파일
├── ValueObjects/
│   └── BinaryData.cs           # 이진 데이터 값 객체
└── README.md                   # 프로젝트 문서
```

### 핵심 코드

**BinaryData.cs - 값 객체 구현**
```csharp
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), val => new BinaryData(val));

    internal static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new BinaryData(validatedValue);

    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value != null && value.Length > 0
            ? value
            : DomainErrors.Empty(value);

    internal static class DomainErrors
    {
        public static Error Empty(byte[] value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(BinaryData)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Binary data cannot be empty or null. Current value: '{(value == null ? "null" : value.Length.ToString())}'");
    }
}
```

**Program.cs - 데모 코드**
```csharp
// 성공 케이스
var data1 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = BinaryData.Create(new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 });
var data3 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });

// 동등성 비교
Console.WriteLine($"   {(BinaryData)data1} == {(BinaryData)data2} = {(BinaryData)data1 == (BinaryData)data2}");
Console.WriteLine($"   {(BinaryData)data1} == {(BinaryData)data3} = {(BinaryData)data1 == (BinaryData)data3}");

// 타입 변환
var binaryData = (BinaryData)data1;
var bytes = (byte[])binaryData;
```

## 한눈에 보는 정리

### 비교 표
| 구분 | 일반 클래스 | 값 객체 (`SimpleValueObject<T>`) |
|------|------------|-------------------------------|
| **동등성** | 참조 동등성 | 값 동등성 |
| **불변성** | 변경 가능 | 변경 불가능 |
| **타입 안전성** | 기본 타입 | 의미 있는 타입 |
| **비교 연산자** | 해당 없음 | 미지원 (IComparable 미구현) |
| **용도** | 일반 객체 | 값 표현 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **값 동등성 보장** | 비교 연산자 미지원 |
| **불변성으로 인한 안전성** | 구현이 다소 복잡 |
| **타입 안전성 향상** | 기본 타입보다 메모리 사용 증가 |
| **의미 있는 도메인 표현** | 학습 곡선 존재 |

## FAQ

### Q1: `SimpleValueObject<T>`는 어떤 경우에 사용해야 하나요?
**A**: `SimpleValueObject<T>`는 단일 기본 타입을 래핑하여 의미 있는 도메인 개념으로 표현하고자 할 때 사용합니다. 값 기반 동등성이 필요하고, 비교 연산이 필요하지 않은 경우에 적합합니다.

예를 들어, 사용자 ID, 이메일 주소, 전화번호와 같은 단일 값이면서 비교 연산이 크게 중요하지 않은 도메인 개념을 표현할 때 사용합니다. 이는 마치 데이터베이스에서 기본 키를 래핑하는 것처럼, 단순하지만 의미 있는 타입을 생성하는 역할을 합니다.

이러한 경우에 `SimpleValueObject<T>`를 사용하면 타입 안전성을 보장하면서도 불필요한 비교 연산자를 구현하지 않아도 됩니다. 이는 마치 객체지향 디자인에서 필요한 기능만을 구현하는 인터페이스 분리 원칙을 따르는 것과 유사합니다.

### Q2: 값 객체와 일반 클래스의 차이점은 무엇인가요?
**A**: 값 객체와 일반 클래스의 가장 큰 차이점은 동등성 비교 방식과 불변성 보장입니다. 일반 클래스는 참조 동등성을 사용하지만 값 객체는 값 동등성을 사용합니다.

일반 클래스에서는 두 인스턴스의 메모리 주소가 같아야 동일한 객체로 취급됩니다. 하지만 값 객체에서는 내부 값이 같으면 동일한 객체로 취급됩니다. 이는 마치 집합론에서 원소의 동일성을 값으로 판단하는 것과 유사합니다.

또한 값 객체는 생성 후에는 그 값을 변경할 수 없도록 불변성을 보장합니다. 이는 예상치 못한 부작용을 방지하고 코드의 예측 가능성을 높여줍니다. 이는 마치 함수형 프로그래밍에서 불변 데이터 구조를 사용하는 것처럼 안전한 프로그래밍을 가능하게 합니다.

### Q3: 왜 비교 연산자를 지원하지 않나요?
**A**: `SimpleValueObject<T>`는 의도적으로 비교 연산자를 지원하지 않습니다. 이는 값 객체의 설계 원칙과 관련이 있습니다. 단순한 값 래핑의 경우, 비교 연산이 크게 의미가 없거나 오히려 혼란을 일으킬 수 있습니다.

예를 들어, 이진 데이터를 비교한다고 해서 어느 것이 "더 크다"거나 "더 작다"는 의미가 있을까요? 이러한 비교는 도메인에 따라 다르게 해석될 수 있어, 오히려 잘못된 사용을 유도할 수 있습니다.

이는 마치 프로그래밍 언어에서 `==`와 `===`을 구분하는 것처럼, 필요한 경우에만 특정 기능을 제공하는 최소주의 설계 원칙을 따르는 것입니다. 비교가 필요한 경우에는 `ComparableSimpleValueObject<T>`를 사용해야 합니다.

### Q4: DomainErrors는 왜 internal로 선언되나요?
**A**: `DomainErrors`는 내부 구현 세부사항이므로 `internal`로 선언하여 같은 어셈블리 내에서만 접근할 수 있도록 제한합니다. 이는 캡슐화를 강화하고 외부에서의 잘못된 사용을 방지합니다.

이는 마치 private 멤버를 외부에서 접근하지 못하게 하는 것처럼, 내부 구현 세부사항을 보호하는 역할을 합니다. 외부 코드에서 에러를 직접 생성하는 것을 방지하여 도메인의 무결성을 보장할 수 있습니다.

또한 `internal` 키워드를 사용함으로써 API의 의도를 명확히 표현할 수 있습니다. 이는 마치 접근 제한자를 통해 메서드의 사용 범위를 명확히 하는 것처럼, 이 클래스가 내부용이라는 것을 명확히 알려주는 역할을 합니다.
