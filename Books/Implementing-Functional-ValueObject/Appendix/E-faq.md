# 부록 E. FAQ

> **부록** | [← 이전: D. 참고 자료](D-references.md) | [목차](../README.md)

---

## 일반 질문

### Q: 값 객체와 엔티티의 차이점은 무엇인가요?

**A:** 핵심 차이는 **식별자**입니다.

| 특성 | 값 객체 | 엔티티 |
|------|---------|--------|
| 식별 | 값으로 식별 | 고유 ID로 식별 |
| 동등성 | 모든 속성이 같으면 동등 | ID가 같으면 동등 |
| 불변성 | 항상 불변 | 변경 가능 |
| 생명주기 | 없음 | 있음 |

```csharp
// 값 객체: 값이 같으면 동등
var email1 = Email.Create("user@example.com");
var email2 = Email.Create("user@example.com");
// email1 == email2 (true)

// 엔티티: ID가 같으면 동등
var user1 = new User(id: 1, name: "Alice");
var user2 = new User(id: 1, name: "Bob");
// user1 == user2 (true, 이름이 달라도)
```

---

### Q: Fin<T>와 Validation<Error, T>는 언제 사용하나요?

**A:** 검증 방식에 따라 선택합니다.

| 타입 | 실행 방식 | 오류 처리 | 사용 시기 |
|------|----------|----------|----------|
| `Fin<T>` | 순차 (Bind) | 첫 번째 오류에서 중단 | 의존성 있는 검증 |
| `Validation<Error, T>` | 병렬 (Apply) | 모든 오류 수집 | 독립적인 검증 |

```csharp
// Fin<T>: 순차 검증 - A가 실패하면 B는 실행 안 함
ValidateA().Bind(_ => ValidateB()).Bind(_ => ValidateC());

// Validation: 병렬 검증 - 모든 검증 실행, 오류 수집
(ValidateA(), ValidateB(), ValidateC()).Apply((a, b, c) => new Result(a, b, c));
```

---

### Q: 값 객체에 비즈니스 로직을 넣어도 되나요?

**A:** 네, 해당 값에 관련된 로직은 값 객체에 포함하는 것이 좋습니다.

```csharp
public sealed class Money : ComparableValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    // ✅ 적절함: 금액 관련 연산
    public Money Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : throw new InvalidOperationException("다른 통화");

    // ✅ 적절함: 포맷팅
    public string ToFormattedString() => $"{Amount:N2} {Currency}";

    // ❌ 부적절함: 외부 시스템 의존
    public async Task<decimal> GetExchangeRate() { /* API 호출 */ }
}
```

---

### Q: private 생성자와 Create 팩토리 메서드를 사용하는 이유는?

**A:** **항상 유효한 상태**를 보장하기 위해서입니다.

```csharp
// ❌ public 생성자: 유효하지 않은 객체 생성 가능
public class Email
{
    public Email(string value) { Value = value; }
}
var invalid = new Email("not-an-email"); // 유효하지 않음!

// ✅ private 생성자 + Create: 검증 후에만 생성
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string value)
    {
        if (!IsValid(value))
            return Error.New("유효하지 않은 이메일");
        return new Email(value);
    }
}
var result = Email.Create("not-an-email"); // Fail 반환
```

---

## 구현 질문

### Q: EF Core에서 값 객체를 어떻게 저장하나요?

**A:** 세 가지 방법이 있습니다.

**1. OwnsOne (권장)**
```csharp
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Email, email =>
    {
        email.Property(e => e.Value).HasColumnName("Email");
    });
```

**2. Value Converter**
```csharp
modelBuilder.Entity<User>()
    .Property(u => u.Email)
    .HasConversion(
        e => (string)e,
        s => Email.CreateFromValidated(s));
```

**3. OwnsMany (컬렉션)**
```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems);
```

---

### Q: JSON 직렬화에서 값 객체를 어떻게 처리하나요?

**A:** JsonConverter를 구현합니다.

```csharp
public class EmailJsonConverter : JsonConverter<Email>
{
    public override Email Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return Email.Create(value!)
            .IfFail(e => throw new JsonException(e.Message));
    }

    public override void Write(Utf8JsonWriter writer, Email email, JsonSerializerOptions options)
    {
        writer.WriteStringValue((string)email);
    }
}
```

---

### Q: 값 객체 생성 시 예외를 던져도 되나요?

**A:** 가능하면 피하고, `Fin<T>`나 `Validation`을 반환하세요.

```csharp
// ❌ 예외 사용
public static Email Create(string value)
{
    if (!IsValid(value))
        throw new ArgumentException("Invalid email");
    return new Email(value);
}

// ✅ 결과 타입 사용
public static Fin<Email> Create(string value)
{
    if (!IsValid(value))
        return Error.New("Invalid email");
    return new Email(value);
}

// ⚠️ 검증된 값에서만 예외 허용 (내부용)
internal static Email CreateFromValidated(string value) => new(value);
```

---

### Q: 값 객체에 ID를 포함해도 되나요?

**A:** 아니요, ID가 있으면 엔티티입니다.

```csharp
// ❌ 값 객체에 ID 포함
public sealed class Email : SimpleValueObject<string>
{
    public Guid Id { get; } // 이러면 엔티티!
}

// ✅ 값 객체는 값만 포함
public sealed class Email : SimpleValueObject<string>
{
    // ID 없음, 값으로만 식별
}
```

---

## 성능 질문

### Q: 값 객체를 많이 생성하면 성능 문제가 있나요?

**A:** 일반적으로 걱정할 필요 없습니다.

- 값 객체는 대부분 작은 객체입니다
- .NET의 GC는 작은 객체를 효율적으로 처리합니다
- 필요시 `record struct`로 스택 할당 가능

```csharp
// 힙 할당 (class 기반)
public sealed class Email : SimpleValueObject<string> { }

// 스택 할당 가능 (struct 기반) - 고성능 필요시
public readonly record struct EmailStruct(string Value);
```

---

### Q: GetHashCode()가 자주 호출되면 문제가 되나요?

**A:** 해시 코드를 캐싱할 수 있습니다.

```csharp
public abstract class ValueObject
{
    private int? _cachedHashCode;

    public override int GetHashCode()
    {
        return _cachedHashCode ??= ComputeHashCode();
    }

    private int ComputeHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(17, (hash, obj) =>
                HashCode.Combine(hash, obj?.GetHashCode() ?? 0));
    }
}
```

---

## 테스트 질문

### Q: 값 객체 테스트에서 무엇을 검증해야 하나요?

**A:** 다음 항목을 검증하세요.

1. **생성 검증**
   - 유효한 입력 → 성공
   - 유효하지 않은 입력 → 실패 + 적절한 오류

2. **동등성**
   - 같은 값 → 동등
   - 다른 값 → 비동등
   - 해시코드 일관성

3. **불변성**
   - 연산 후 원본 변경 없음

4. **비교 (해당시)**
   - 정렬 순서
   - 비교 연산자

```csharp
[Fact]
public void Create_WithValidEmail_ShouldSucceed()
{
    var result = Email.Create("user@example.com");
    result.IsSucc.ShouldBeTrue();
}

[Fact]
public void Equals_WithSameValue_ShouldBeTrue()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");
    email1.ShouldBe(email2);
}
```

---

### Q: 아키텍처 테스트로 값 객체 규칙을 강제할 수 있나요?

**A:** 네, ArchUnitNET을 사용합니다.

```csharp
[Fact]
public void ValueObjects_ShouldBeSealed()
{
    var rule = Classes()
        .That().AreAssignableTo(typeof(ValueObject))
        .Should().BeSealed();

    rule.Check(Architecture);
}

[Fact]
public void ValueObjects_ShouldNotHavePublicConstructors()
{
    var rule = Classes()
        .That().AreAssignableTo(typeof(ValueObject))
        .Should().NotHavePublicConstructors();

    rule.Check(Architecture);
}
```

---

## 설계 질문

### Q: 값 객체가 너무 많아지면 복잡해지지 않나요?

**A:** 적절한 수준에서 사용하세요.

**값 객체로 만들면 좋은 경우:**
- 검증 규칙이 있는 값
- 여러 곳에서 재사용되는 값
- 비즈니스 의미가 있는 값

**과도한 경우:**
```csharp
// ❌ 과도함: 단순 문자열에 별도 타입
public sealed class FirstName : SimpleValueObject<string> { }
public sealed class LastName : SimpleValueObject<string> { }
public sealed class MiddleName : SimpleValueObject<string> { }

// ✅ 적절함: 복합 값 객체로 그룹화
public sealed class FullName : ValueObject
{
    public string First { get; }
    public string Last { get; }
    public string? Middle { get; }
}
```

---

### Q: 값 객체 간의 의존성은 어떻게 처리하나요?

**A:** 합성을 사용합니다.

```csharp
public sealed class Order : ValueObject
{
    public OrderId Id { get; }           // 다른 값 객체
    public Money TotalAmount { get; }    // 다른 값 객체
    public ShippingAddress Address { get; } // 다른 값 객체

    public static Validation<Error, Order> Create(
        OrderId id,
        Money totalAmount,
        ShippingAddress address)
    {
        // 각 값 객체는 이미 유효함
        return new Order(id, totalAmount, address);
    }
}
```

---

## 마무리

더 많은 질문이 있다면:
- GitHub Issues에 질문 등록
- Stack Overflow에 태그 `value-objects`, `languageext` 사용
- 커뮤니티 토론 참여

---

> **끝** | [← 목차로 돌아가기](../README.md)
