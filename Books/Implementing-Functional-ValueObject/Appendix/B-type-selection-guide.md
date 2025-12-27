# 부록 B. 프레임워크 타입 선택 가이드

> **부록** | [← 이전: A. LanguageExt 참조](A-languageext-reference.md) | [목차](../README.md) | [다음: C. 용어집 →](C-glossary.md)

---

## 개요

값 객체를 구현할 때 어떤 프레임워크 타입을 선택해야 하는지 안내하는 의사결정 가이드입니다.

---

## 의사결정 트리

```
값 객체 구현 시작
       │
       ▼
┌─────────────────┐
│ 값이 하나인가?   │
│ (단일 값 래퍼)   │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
   Yes        No
    │         │
    ▼         ▼
┌────────┐  ┌─────────────────┐
│비교 필요?│  │ 열거형인가?      │
└────┬───┘  └────────┬────────┘
     │               │
 ┌───┴───┐      ┌────┴────┐
 │       │      │         │
Yes      No    Yes        No
 │       │      │         │
 ▼       ▼      ▼         ▼
┌────────────┐ ┌──────────┐ ┌──────────┐ ┌───────────┐
│Comparable  │ │Simple    │ │SmartEnum │ │ 비교 필요?  │
│SimpleValue │ │ValueObject│ │+IValueObj│ │           │
│Object<T>   │ │<T>       │ │          │ └─────┬─────┘
└────────────┘ └──────────┘ └──────────┘       │
                                          ┌────┴────┐
                                          │         │
                                         Yes        No
                                          │         │
                                          ▼         ▼
                                    ┌──────────┐ ┌──────────┐
                                    │Comparable│ │ValueObject│
                                    │ValueObj  │ │          │
                                    └──────────┘ └──────────┘
```

---

## 타입별 상세 가이드

### 1. SimpleValueObject<T>

**언제 사용?**
- 단일 값을 래핑할 때
- 비교(정렬)가 필요 없을 때
- 가장 일반적인 값 객체

**예시**
```
- Email (문자열)
- ProductCode (문자열)
- Password (해시된 문자열)
- UserId (GUID)
```

**구현 예시**
```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string value)
    {
        // 검증 로직
        return new Email(normalized);
    }
}
```

---

### 2. ComparableSimpleValueObject<T>

**언제 사용?**
- 단일 값을 래핑할 때
- 정렬이나 비교가 필요할 때
- 내부 값이 `IComparable<T>`일 때

**예시**
```
- Age (정수 - 나이 비교)
- Quantity (정수 - 수량 비교)
- Amount (decimal - 금액 비교)
- InterestRate (decimal - 이율 비교)
- DateOfBirth (DateOnly - 날짜 비교)
```

**구현 예시**
```csharp
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public static Fin<Age> Create(int value)
    {
        if (value < 0 || value > 150)
            return Error.New("유효하지 않은 나이");
        return new Age(value);
    }
}
```

---

### 3. ValueObject (복합)

**언제 사용?**
- 여러 속성을 가진 값 객체
- 비교(정렬)가 필요 없을 때
- 복합 값이 필요할 때

**예시**
```
- Address (도시, 거리, 우편번호)
- FullName (성, 이름)
- Coordinate (위도, 경도)
- DateTimeRange (시작, 종료)
```

**구현 예시**
```csharp
public sealed class Address : ValueObject
{
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }
}
```

---

### 4. ComparableValueObject (복합 비교 가능)

**언제 사용?**
- 여러 속성을 가진 값 객체
- 정렬이나 비교가 필요할 때
- 복합 키로 정렬해야 할 때

**예시**
```
- Money (금액, 통화 - 동일 통화 내 비교)
- DateRange (시작일, 종료일 - 시작일 기준 정렬)
- ExchangeRate (기준통화, 견적통화, 환율)
- TimeSlot (시작시간, 종료시간)
```

**구현 예시**
```csharp
public sealed class Money : ComparableValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    protected override IEnumerable<IComparable> GetComparableComponents()
    {
        yield return Currency;
        yield return Amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

### 5. SmartEnum + IValueObject (타입 안전 열거형)

**언제 사용?**
- 제한된 값 집합
- 각 값에 행위나 속성이 있을 때
- 상태 전이 로직이 필요할 때

**예시**
```
- OrderStatus (대기, 확정, 배송중, 완료)
- PaymentMethod (카드, 현금, 계좌이체)
- UserRole (관리자, 사용자, 게스트)
- TransactionType (입금, 출금, 이체)
```

**구현 예시**
```csharp
public sealed class OrderStatus : SmartEnum<OrderStatus, string>, IValueObject
{
    public static readonly OrderStatus Pending = new("PENDING", "대기중", canCancel: true);
    public static readonly OrderStatus Shipped = new("SHIPPED", "배송중", canCancel: false);

    public string DisplayName { get; }
    public bool CanCancel { get; }

    private OrderStatus(string value, string displayName, bool canCancel)
        : base(displayName, value)
    {
        DisplayName = displayName;
        CanCancel = canCancel;
    }
}
```

---

## 빠른 선택 표

| 특성 | SimpleValueObject | ComparableSimple | ValueObject | ComparableValue | SmartEnum |
|------|:-----------------:|:----------------:|:-----------:|:---------------:|:---------:|
| 단일 값 | ✅ | ✅ | ❌ | ❌ | ✅ |
| 복합 값 | ❌ | ❌ | ✅ | ✅ | ❌ |
| 비교 가능 | ❌ | ✅ | ❌ | ✅ | ❌ |
| 정렬 가능 | ❌ | ✅ | ❌ | ✅ | ❌ |
| 열거형 | ❌ | ❌ | ❌ | ❌ | ✅ |
| 상태 전이 | ❌ | ❌ | ❌ | ❌ | ✅ |

---

## 일반적인 실수

### 1. 불필요한 비교 가능성

```csharp
// ❌ 이메일은 정렬할 필요 없음
public sealed class Email : ComparableSimpleValueObject<string> { }

// ✅ 단순 값 객체 사용
public sealed class Email : SimpleValueObject<string> { }
```

### 2. 단일 값에 복합 타입 사용

```csharp
// ❌ 불필요하게 복잡함
public sealed class ProductCode : ValueObject
{
    public string Value { get; }
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

// ✅ 단순하게
public sealed class ProductCode : SimpleValueObject<string> { }
```

### 3. 열거형에 SmartEnum 미사용

```csharp
// ❌ 일반 enum의 한계
public enum OrderStatus { Pending, Shipped }

// ✅ 행위를 가진 SmartEnum
public sealed class OrderStatus : SmartEnum<OrderStatus, string>, IValueObject
{
    public bool CanCancel { get; }
    public Fin<OrderStatus> TransitionTo(OrderStatus next) { ... }
}
```

---

## 체크리스트

값 객체 구현 시 다음을 확인하세요:

- [ ] 타입이 sealed로 선언되었는가?
- [ ] public 생성자 대신 팩토리 메서드(Create)를 사용하는가?
- [ ] 검증 로직이 Create 메서드에 있는가?
- [ ] DomainErrors 내부 클래스가 있는가?
- [ ] 불변성이 보장되는가?
- [ ] 필요한 경우 암시적 변환 연산자가 있는가?
- [ ] ToString()이 적절히 오버라이드되었는가?

---

## 다음 단계

용어집을 확인합니다.

→ [C. 용어집](C-glossary.md)
