---
title: "단순 값 객체"
---

> `SimpleValueObject<T>`

## 개요

Part 1~2에서 값 객체의 개념과 검증 패턴을 익혔습니다. 이제 매번 보일러플레이트를 반복하지 않고, 프레임워크 기본 클래스를 상속하여 값 객체를 빠르게 구현하는 방법을 학습합니다.

`SimpleValueObject<T>`는 값 객체 패턴의 가장 기본적인 형태로, 불변성(Immutable), 값 기반 동등성(Value-based Equality), 타입 안전성(Type Safety)을 기본 클래스 상속만으로 확보합니다.

## 학습 목표

- `SimpleValueObject<T>`를 상속하여 값 객체를 구현할 수 있습니다
- 값 기반 동등성(`==`, `!=`)과 해시코드(`GetHashCode()`)의 동작을 설명할 수 있습니다
- 명시적 타입 변환을 활용하여 내부 값을 추출할 수 있습니다
- 값 객체의 불변성 보장 메커니즘을 이해하고 적용할 수 있습니다

## 왜 필요한가?

기본 타입으로 도메인 값을 표현하면 세 가지 실질적인 문제가 생깁니다.

사용자 ID와 주문 ID가 모두 `int`라면, `userId = orderId` 같은 할당이 컴파일을 통과합니다. 타입 시스템이 논리적 오류를 잡아주지 못하는 것입니다. 또한 기본 타입은 데이터를 저장할 뿐 유효성 검증이나 비즈니스 로직을 포함하지 않으므로, 관련 로직이 여러 곳에 분산되어 유지보수가 어려워집니다. 마지막으로, 기본 타입은 값이 언제든 변경될 수 있어 예상치 못한 부작용이 발생할 수 있습니다.

값 객체 패턴은 이 세 문제를 한꺼번에 해결합니다. 의미 있는 타입으로 컴파일 타임 안전성을 확보하고, 데이터와 검증 로직을 한 곳에 캡슐화하며, 생성 후 값 변경을 원천 차단합니다.

## 핵심 개념

### 값 기반 동등성

일반 클래스는 두 인스턴스의 메모리 주소가 다르면 다른 객체로 취급합니다. 반면 값 객체는 내부 값이 같으면 동일한 객체로 판단합니다.

두 `BinaryData` 객체가 같은 바이트 배열을 가지고 있다면, 이들은 동일한 객체로 취급됩니다. 이 특성 덕분에 컬렉션에서의 중복 제거나 검색이 직관적으로 동작합니다.

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

### 불변성 보장

값 객체는 생성 후 값을 변경할 수 없습니다. 생성자를 `private`으로 선언하고 정적 팩토리 메서드를 통해서만 생성하도록 설계합니다. 이 구조 덕분에 여러 스레드가 동시에 값 객체를 읽어도 경쟁 상태가 발생하지 않습니다.

```csharp
// 값 객체: 불변성 보장
var data = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = (BinaryData)data;

// data의 값을 변경할 수 없음
// data.Value = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 }; // 컴파일 에러

// 새로운 값 객체를 생성해야 함
var newData = BinaryData.Create(new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 });
```

### 타입 안전성

기본 타입 대신 도메인에 맞는 타입을 사용하면, 컴파일러가 타입 불일치를 미리 발견합니다. 대규모 애플리케이션일수록 이 효과가 커집니다.

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

다음 표는 `SimpleValueObject<T>` 기반 값 객체를 구현할 때 필수적인 네 가지 요소를 정리합니다.

| 포인트 | 설명 |
|--------|------|
| **`SimpleValueObject<T>` 상속** | 기본적인 값 객체 기능을 상속받습니다 |
| **private 생성자** | 외부에서 직접 생성하지 못하도록 제한합니다 |
| **정적 Create 메서드** | 유효성 검증과 객체 생성을 담당합니다 |
| **DomainError.For\<T>()** | 구조화된 에러 처리를 위한 정적 메서드입니다 |

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

`BinaryData`는 `SimpleValueObject<byte[]>`를 상속하여 이진 데이터를 값 객체로 표현합니다.

**BinaryData.cs - 값 객체 구현**
```csharp
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), v => new BinaryData(v));

    public static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new(validatedValue);

    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value != null && value.Length > 0
            ? value
            : DomainError.For<BinaryData, byte[]>(new DomainErrorType.Empty(), value!,
                $"Binary data cannot be empty or null. Current value: '{(value == null ? "null" : $"{value.Length} bytes")}'");
}
```

동등성 비교, 타입 변환, 실패 케이스를 확인하는 데모 코드입니다.

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

일반 클래스와 `SimpleValueObject<T>` 기반 값 객체의 차이를 비교합니다.

### 비교 표
| 구분 | 일반 클래스 | 값 객체 (`SimpleValueObject<T>`) |
|------|------------|-------------------------------|
| **동등성** | 참조 동등성 | 값 동등성 |
| **불변성** | 변경 가능 | 변경 불가능 |
| **타입 안전성** | 기본 타입 | 의미 있는 타입 |
| **비교 연산자** | 해당 없음 | 미지원 (IComparable 미구현) |
| **용도** | 일반 객체 | 값 표현 |

## FAQ

### Q1: `SimpleValueObject<T>`는 어떤 경우에 사용해야 하나요?
**A**: 단일 기본 타입을 래핑하여 도메인 개념으로 표현하되, 크기 비교가 필요 없는 경우에 적합합니다. 사용자 ID, 이메일 주소, 전화번호 등이 대표적입니다.

### Q2: 값 객체와 일반 클래스의 차이점은 무엇인가요?
**A**: 일반 클래스는 참조 동등성을 사용하고 값을 변경할 수 있습니다. 값 객체는 내부 값이 같으면 동일한 객체로 취급하고, 생성 후 값 변경이 불가능합니다.

### Q3: 왜 비교 연산자를 지원하지 않나요?
**A**: 이진 데이터에서 "더 크다/작다"는 의미가 도메인마다 다르게 해석될 수 있어, 오히려 혼란을 유발합니다. 비교가 필요한 경우 `ComparableSimpleValueObject<T>`를 사용합니다.

다음 장에서는 `SimpleValueObject<T>`에 비교 기능을 추가한 `ComparableSimpleValueObject<T>`를 학습합니다. 값 객체에 자연스러운 순서가 있을 때 정렬과 비교 연산을 어떻게 지원하는지 살펴봅니다.
