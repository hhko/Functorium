---
title: "비교 가능한 단순 값 객체"
---

> `ComparableSimpleValueObject<T>`

## 개요

`SimpleValueObject<T>`는 동등성 비교만 지원합니다. 그런데 사용자 ID를 정렬하거나, 우선순위를 비교하려면 어떻게 해야 할까요? `ComparableSimpleValueObject<T>`는 값 객체의 기본 기능에 `IComparable<T>` 구현과 비교 연산자 오버로딩을 자동으로 추가하여, 정렬과 크기 비교를 기본 타입처럼 사용할 수 있게 합니다.

## 학습 목표

- `ComparableSimpleValueObject<T>`가 `SimpleValueObject<T>`와 어떻게 다른지 설명할 수 있습니다
- `IComparable<T>` 인터페이스가 자동으로 구현되는 원리를 이해할 수 있습니다
- 값 객체를 컬렉션에서 자연스럽게 정렬할 수 있습니다
- `<`, `<=`, `>`, `>=` 연산자를 값 객체에 적용할 수 있습니다

## 왜 필요한가?

`SimpleValueObject<T>`로 값 객체를 만들면 동등성 비교는 되지만, 정렬이나 크기 비교가 필요할 때 한계에 부딪힙니다.

값 객체들을 컬렉션에 담아 정렬하려면 별도의 비교 로직을 매번 구현해야 합니다. `<`, `>` 같은 비교 연산자도 사용할 수 없어 조건문에서 직관적이지 않습니다. SortedSet이나 우선순위 큐처럼 비교가 필수인 자료구조에서는 값 객체를 키로 사용하는 것 자체가 불가능합니다.

`ComparableSimpleValueObject<T>`는 이 모든 문제를 기본 클래스 상속만으로 해결합니다. 내부 값의 자연스러운 순서를 그대로 활용하여, C#의 기본 타입처럼 비교하고 정렬할 수 있습니다.

## 핵심 개념

### 자동 비교 기능

`ComparableSimpleValueObject<T>`는 부모 클래스에서 비교 기능을 자동으로 상속받습니다. 별도의 구현 없이도 내부 값을 기준으로 크기를 비교할 수 있습니다.

```csharp
// 비교 가능한 값 객체
UserId id1 = UserId.Create(123);
UserId id2 = UserId.Create(456);

// 자연스러운 비교 연산
bool isLess = id1 < id2;      // true
bool isGreater = id1 > id2;   // false
bool isEqual = id1 == id2;    // false

// 이는 마치 기본 타입처럼 작동
int num1 = 123;
int num2 = 456;
bool numLess = num1 < num2;   // true
```

### IComparable\<T> 자동 구현

`ComparableSimpleValueObject<T>`는 `IComparable<T>` 인터페이스를 부모 클래스에서 자동으로 구현합니다. 이 덕분에 `List<T>.Sort()`나 LINQ의 `OrderBy()` 같은 .NET 표준 정렬 API에서 별도의 비교 함수 없이 바로 사용할 수 있습니다.

```csharp
// 자동 정렬 가능
List<UserId> userIds = new List<UserId>
{
    UserId.Create(456),
    UserId.Create(123),
    UserId.Create(789)
};

userIds.Sort(); // 별도의 비교 함수 필요 없음

// LINQ에서도 자연스럽게 사용
var sorted = userIds.OrderBy(id => id); // IComparable<T> 덕분에 가능
```

### 모든 비교 연산자 오버로딩

`IComparable<T>` 구현뿐 아니라 `<`, `<=`, `>`, `>=` 연산자까지 자동으로 오버로딩됩니다. 조건문이나 범위 체크에서 직관적인 표현이 가능합니다.

```csharp
// 자연스러운 비교 표현
UserId currentId = UserId.Create(500);
UserId minId = UserId.Create(100);
UserId maxId = UserId.Create(1000);

// 범위 체크
bool isValid = minId <= currentId && currentId <= maxId;

// 이는 마치 수학처럼 읽힘
// 100 <= 500 <= 1000
```

## 실전 지침

### 예상 출력
```
=== 2. 비교 가능한 primitive 값 객체 - ComparableSimpleValueObject<T> ===
부모 클래스: ComparableSimpleValueObject<int>
예시: UserId (사용자 ID)

📋 특징:
   ✅ 자동으로 IComparable<UserId> 구현
   ✅ 모든 비교 연산자 오버로딩 (<, <=, >, >=)
   ✅ 명시적 타입 변환 지원
   ✅ 동등성 비교와 해시코드 자동 제공

🔍 성공 케이스:
   ✅ UserId(123): 123
   ✅ UserId(456): 456
   ✅ UserId(123): 123

📊 동등성 비교:
   123 == 456 = False
   123 == 123  =  True

📊 비교 기능 (IComparable<T>):
   123 < 456 = True
   123 <= 456 = True
   123 > 456 = False
   123 >= 456 = False

🔄 타입 변환:
   (int)123 = 123

🔢 해시코드:
   123.GetHashCode() = 123
   123.GetHashCode() = 123
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   UserId(0): DomainErrors.UserId.NotPositive
   UserId(-1): DomainErrors.UserId.NotPositive

📈 정렬 데모:
   정렬된 UserId 목록:
     123
     234
     456
     567
     789

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트

`SimpleValueObject<T>`와 비교했을 때 추가되는 핵심 요소를 정리합니다.

| 포인트 | 설명 |
|--------|------|
| **`ComparableSimpleValueObject<T>` 상속** | 자동 비교 기능 상속 |
| **`IComparable<T>` 자동 구현** | 부모 클래스에서 제공 |
| **비교 연산자 자동 오버로딩** | `<`, `<=`, `>`, `>=` 사용 가능 |
| **컬렉션 정렬 지원** | Sort(), OrderBy() 등에서 별도 비교 함수 불필요 |

## 프로젝트 설명

### 프로젝트 구조
```
02-ComparableSimpleValueObject/
├── Program.cs                          # 메인 실행 파일
├── ComparableSimpleValueObject.csproj # 프로젝트 파일
├── ValueObjects/
│   └── UserId.cs                      # 사용자 ID 값 객체
└── README.md                          # 프로젝트 문서
```

### 핵심 코드

`UserId`는 `ComparableSimpleValueObject<int>`를 상속하여 비교 가능한 사용자 ID를 표현합니다.

**UserId.cs - 비교 가능한 값 객체 구현**
```csharp
public sealed class UserId : ComparableSimpleValueObject<int>
{
    private UserId(int value) : base(value) { }

    public int Id => Value; // public 접근자 제공

    public static Fin<UserId> Create(int value) =>
        CreateFromValidation(Validate(value), v => new UserId(v));

    public static UserId CreateFromValidated(int validatedValue) =>
        new(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<UserId>.Positive(value);

    public static implicit operator int(UserId userId) => userId.Value;
}
```

비교 연산자와 자동 정렬을 확인하는 데모 코드입니다.

**Program.cs - 비교 기능 데모**
```csharp
// 비교 연산자 사용
var userId1 = (UserId)id1;
var userId2 = (UserId)id2;
Console.WriteLine($"   {userId1} < {userId2} = {userId1 < userId2}");
Console.WriteLine($"   {userId1} <= {userId2} = {userId1 <= userId2}");

// 자동 정렬
userIds.Sort(); // IComparable<T> 덕분에 자동 정렬 가능

Console.WriteLine("   정렬된 UserId 목록:");
foreach (var userId in userIds)
{
    Console.WriteLine($"     {userId}");
}
```

## 한눈에 보는 정리

`SimpleValueObject<T>`에서 추가되는 비교 기능을 비교합니다.

### 비교 표
| 구분 | `SimpleValueObject<T>` | `ComparableSimpleValueObject<T>` |
|------|---------------------|-------------------------------|
| **동등성 비교** | 지원 | 지원 |
| **비교 연산자** | 미지원 | 자동 지원 (`<`, `<=`, `>`, `>=`) |
| **`IComparable<T>`** | 미구현 | 자동 구현 |
| **컬렉션 정렬** | 수동 구현 필요 | 자동 지원 |
| **LINQ 정렬** | 별도 키 필요 | 자연스러운 정렬 |

## FAQ

### Q1: 언제 `ComparableSimpleValueObject<T>`를 사용해야 하나요?
**A**: 값 객체에 자연스러운 순서 관계가 있을 때 사용합니다. ID, 버전 번호, 우선순위처럼 크기 비교가 의미 있는 경우에 적합합니다. 이메일 주소나 전화번호처럼 순서가 중요하지 않은 값에는 `SimpleValueObject<T>`가 적절합니다.

### Q2: `IComparable<T>`는 어떻게 자동으로 구현되나요?
**A**: 부모 클래스가 내부 `Value`의 비교를 위임합니다. `int` 기반이면 정수의 자연 순서를, `string` 기반이면 알파벳 순서를 따릅니다.

### Q3: 비교 로직을 커스터마이징할 수 있나요?
**A**: `ComparableSimpleValueObject<T>`는 기본 타입의 자연 순서만 지원합니다. 버전 번호에서 "1.10" > "1.2" 같은 커스텀 비교가 필요하면 `ValueObject`를 직접 상속하여 `IComparable<T>`를 수동 구현해야 합니다.

다음 장에서는 단일 값이 아닌 여러 primitive 타입을 조합하는 `ValueObject` 패턴을 학습합니다. 2D 좌표처럼 복합 데이터를 하나의 값 객체로 표현하는 방법을 살펴봅니다.

---

→ [3장: ValueObject (Primitive)](../03-ValueObject-Primitive/)
