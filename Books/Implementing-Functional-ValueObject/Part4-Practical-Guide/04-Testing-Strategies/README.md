# 값 객체 테스트 전략

> **Part 4: 실전 가이드** | [← 이전: CQRS 통합](../03-CQRS-Integration/README.md) | [목차](../../README.md) | [다음: Part 5 도메인 예제 →](../../05-domain-examples/01-Ecommerce-Domain/README.md)

---

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

이 프로젝트는 값 객체를 효과적으로 테스트하는 전략과 패턴을 학습합니다. 값 객체의 생성 검증, 동등성 비교, 비교 가능성, 그리고 `Fin<T>` 결과를 테스트하기 위한 헬퍼 메서드를 구현하고 활용합니다.

값 객체는 도메인 모델의 기초이므로 철저한 테스트가 필수입니다. 유효한 입력, 유효하지 않은 입력, 경계값, 동등성, 정렬 등 다양한 측면에서 값 객체의 동작을 검증하는 방법을 익힙니다.

## 학습 목표

### **핵심 학습 목표**
1. **생성 테스트 패턴**: 유효한 입력과 유효하지 않은 입력에 대한 값 객체 생성 테스트를 학습합니다.
2. **동등성 테스트 패턴**: 값 기반 동등성(`Equals`, `GetHashCode`, `==`)을 검증하는 방법을 익힙니다.
3. **비교 가능성 테스트 패턴**: `IComparable<T>` 구현과 정렬 동작을 테스트합니다.
4. **Fin<T> 테스트 헬퍼**: `ShouldBeSuccess()`, `ShouldBeFail()` 등 테스트 유틸리티를 구현합니다.

### **실습을 통해 확인할 내용**
- 유효한 입력 → 성공(`IsSucc`) 검증
- 유효하지 않은 입력 → 실패(`IsFail`) 및 에러 메시지 검증
- 경계값 테스트 (빈 문자열, null, 최대/최소 값)
- 해시코드 일관성과 동등성 연관 규칙
- 정렬 동작과 비교 연산자 테스트

## 왜 필요한가?

값 객체 테스트는 도메인 모델의 신뢰성을 보장하는 핵심입니다.

**첫 번째 이유는 비즈니스 규칙 검증입니다.** 값 객체는 도메인 불변식을 캡슐화합니다. "이메일은 @ 기호를 포함해야 한다", "나이는 0~150 사이여야 한다" 같은 규칙이 항상 지켜지는지 테스트해야 합니다.

**두 번째 이유는 리팩토링 안전망입니다.** 값 객체의 구현을 변경하더라도 테스트가 통과하면 기존 동작이 보존됨을 확신할 수 있습니다. 특히 동등성과 해시코드는 미묘한 버그가 발생하기 쉬운 영역입니다.

**세 번째 이유는 문서화 역할입니다.** 테스트 코드는 값 객체의 사용법과 제약 조건을 보여주는 살아있는 문서입니다. 새로운 팀원이 `Email.Create()`가 어떤 입력을 허용하는지 테스트만 보면 알 수 있습니다.

## 핵심 개념

### 첫 번째 개념: 생성 테스트 패턴

값 객체 생성의 성공과 실패를 검증합니다. `Fin<T>`의 `IsSucc`와 `IsFail` 속성을 활용합니다.

```csharp
// 유효한 입력 테스트
[Fact]
public void Create_WithValidEmail_ReturnsSuccess()
{
    var result = Email.Create("user@example.com");

    result.IsSucc.Should().BeTrue();
    result.GetSuccessValue().Value.Should().Be("user@example.com");
}

// 유효하지 않은 입력 테스트
[Fact]
public void Create_WithInvalidEmail_ReturnsFailure()
{
    var result = Email.Create("invalid-email");

    result.IsFail.Should().BeTrue();
    result.GetFailError().Message.Should().Contain("Email.InvalidFormat");
}

// 경계값 테스트
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public void Create_WithEmptyOrNull_ReturnsFailure(string? input)
{
    var result = Email.Create(input!);

    result.IsFail.Should().BeTrue();
}
```

**핵심 아이디어는 "모든 검증 경로를 커버"하는 것입니다.** 값 객체의 `Create()` 메서드에 있는 모든 `if` 조건에 대해 성공과 실패 케이스를 테스트합니다.

### 두 번째 개념: 동등성 테스트 패턴

값 객체의 동등성 구현을 철저히 검증합니다. `Equals()`, `GetHashCode()`, `==`, `!=` 모두 테스트해야 합니다.

```csharp
[Fact]
public void Equals_SameValue_ReturnsTrue()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.Equals(email2).Should().BeTrue();
    (email1 == email2).Should().BeTrue();
    (email1 != email2).Should().BeFalse();
}

[Fact]
public void Equals_DifferentValue_ReturnsFalse()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("other@example.com");

    email1.Equals(email2).Should().BeFalse();
    (email1 == email2).Should().BeFalse();
    (email1 != email2).Should().BeTrue();
}

[Fact]
public void GetHashCode_SameValue_ReturnsSameHash()
{
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.GetHashCode().Should().Be(email2.GetHashCode());
}
```

**핵심 아이디어는 "동등성 계약 검증"입니다.** 동등한 객체는 같은 해시코드를 가져야 합니다. 이 규칙이 깨지면 Dictionary나 HashSet에서 예기치 않은 동작이 발생합니다.

### 세 번째 개념: 비교 가능성 테스트 패턴

`IComparable<T>`을 구현한 값 객체의 정렬 동작을 테스트합니다.

```csharp
[Fact]
public void CompareTo_ReturnsCorrectOrder()
{
    var age20 = Age.CreateFromValidated(20);
    var age25 = Age.CreateFromValidated(25);
    var age30 = Age.CreateFromValidated(30);

    age20.CompareTo(age25).Should().BeNegative();
    age30.CompareTo(age25).Should().BePositive();
    age25.CompareTo(age25).Should().Be(0);
}

[Fact]
public void ComparisonOperators_WorkCorrectly()
{
    var age20 = Age.CreateFromValidated(20);
    var age25 = Age.CreateFromValidated(25);

    (age20 < age25).Should().BeTrue();
    (age25 > age20).Should().BeTrue();
    (age20 <= age20).Should().BeTrue();
    (age25 >= age25).Should().BeTrue();
}

[Fact]
public void Sort_OrdersCorrectly()
{
    var ages = new[] {
        Age.CreateFromValidated(30),
        Age.CreateFromValidated(20),
        Age.CreateFromValidated(25)
    };

    Array.Sort(ages);

    ages[0].Value.Should().Be(20);
    ages[1].Value.Should().Be(25);
    ages[2].Value.Should().Be(30);
}
```

**핵심 아이디어는 "정렬 계약 검증"입니다.** `CompareTo()`의 결과와 비교 연산자들이 일관되게 동작하는지 확인합니다.

### 네 번째 개념: Fin<T> 테스트 헬퍼

`Fin<T>` 결과를 테스트하기 위한 확장 메서드를 구현합니다.

```csharp
public static class FinTestExtensions
{
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
        {
            throw new Exception("Expected Fail but got Succ");
        }
    }

    public static T GetSuccessValue<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: value => value,
            Fail: error => throw new Exception($"Expected Succ but got Fail: {error.Message}")
        );
    }

    public static Error GetFailError<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: _ => throw new Exception("Expected Fail but got Succ"),
            Fail: error => error
        );
    }
}
```

**핵심 아이디어는 "테스트 가독성 향상"입니다.** `result.ShouldBeSuccess()`가 `result.IsSucc.Should().BeTrue()`보다 의도를 명확하게 표현합니다.

## 실전 지침

### 예상 출력
```
=== 값 객체 테스트 전략 ===

1. 생성 테스트 패턴
────────────────────────────────────────
   [유효한 입력 테스트] user@example.com → PASS
   [유효하지 않은 입력 테스트] invalid-email → PASS
   [에러 코드 검증] 'Email.InvalidFormat' 포함 → PASS
   [경계값 테스트] 빈 문자열/null → PASS

2. 동등성 테스트 패턴
────────────────────────────────────────
   [같은 값 동등성] email1 == email2 → PASS
   [다른 값 비동등성] email1 != email3 → PASS
   [해시코드 일관성] hash(email1) == hash(email2) → PASS
   [연산자 테스트] == 및 != → PASS

3. 비교 가능성 테스트 패턴
────────────────────────────────────────
   [CompareTo 테스트] 20 < 25 < 30 → PASS
   [비교 연산자 테스트] < 연산자 → PASS
   [정렬 테스트] 정렬 후 순서 → PASS

4. 테스트 헬퍼 사용
────────────────────────────────────────
   [ShouldBeSuccess 헬퍼] → PASS
   [ShouldBeFail 헬퍼] → PASS
   [GetSuccessValue 헬퍼] → PASS
   [GetFailError 헬퍼] → PASS
```

### 테스트 클래스 구조 예시

```csharp
public class EmailTests
{
    public class CreateMethod
    {
        [Fact]
        public void WithValidEmail_ReturnsSuccess() { ... }

        [Theory]
        [InlineData("invalid")]
        [InlineData("no-at-sign")]
        public void WithInvalidFormat_ReturnsFailure(string input) { ... }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void WithEmptyOrNull_ReturnsEmptyError(string? input) { ... }
    }

    public class Equality
    {
        [Fact]
        public void SameValue_AreEqual() { ... }

        [Fact]
        public void DifferentValue_AreNotEqual() { ... }

        [Fact]
        public void HashCode_ConsistentWithEquals() { ... }
    }
}
```

## 프로젝트 설명

### 프로젝트 구조
```
04-Testing-Strategies/
├── TestingStrategies/
│   ├── Program.cs                    # 메인 실행 파일 (테스트 데모)
│   └── TestingStrategies.csproj      # 프로젝트 파일
└── README.md                         # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### 핵심 코드

**테스트 대상 값 객체**
```csharp
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");
        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Email? left, Email? right) { ... }
    public static bool operator !=(Email? left, Email? right) => !(left == right);
}
```

**테스트 헬퍼 확장 메서드**
```csharp
public static class FinTestExtensions
{
    public static void ShouldBeSuccess<T>(this Fin<T> fin)
    {
        if (fin.IsFail)
        {
            var message = fin.Match(_ => "", e => e.Message);
            throw new Exception($"Expected Succ but got Fail: {message}");
        }
    }

    public static void ShouldBeFail<T>(this Fin<T> fin)
    {
        if (fin.IsSucc)
            throw new Exception("Expected Fail but got Succ");
    }

    public static T GetSuccessValue<T>(this Fin<T> fin) { ... }
    public static Error GetFailError<T>(this Fin<T> fin) { ... }
}
```

## 한눈에 보는 정리

### 테스트 유형별 체크리스트

| 테스트 유형 | 검증 항목 |
|------------|----------|
| **생성 테스트** | 유효한 입력 → 성공, 유효하지 않은 입력 → 실패 |
| **경계값 테스트** | null, 빈 문자열, 최대/최소 값 |
| **에러 검증** | 에러 코드, 에러 메시지 내용 |
| **동등성 테스트** | `Equals()`, `==`, `!=`, `GetHashCode()` |
| **비교 테스트** | `CompareTo()`, `<`, `>`, `<=`, `>=`, 정렬 |

### Fin<T> 테스트 헬퍼 요약

| 헬퍼 메서드 | 용도 |
|------------|------|
| `ShouldBeSuccess()` | 성공 상태 확인 (실패 시 예외) |
| `ShouldBeFail()` | 실패 상태 확인 (성공 시 예외) |
| `GetSuccessValue()` | 성공 값 추출 (실패 시 예외) |
| `GetFailError()` | 에러 정보 추출 (성공 시 예외) |

### 동등성 계약 규칙

| 규칙 | 설명 |
|------|------|
| 반사성 | `x.Equals(x)` → true |
| 대칭성 | `x.Equals(y)` ↔ `y.Equals(x)` |
| 추이성 | `x.Equals(y)` ∧ `y.Equals(z)` → `x.Equals(z)` |
| 일관성 | 같은 입력이면 항상 같은 결과 |
| 해시코드 | `x.Equals(y)` → `x.GetHashCode() == y.GetHashCode()` |

## FAQ

### Q1: 모든 값 객체에 대해 어떤 테스트를 작성해야 하나요?
**A**: 최소한 다음 테스트를 작성해야 합니다.

1. **생성 테스트**: 유효한 입력 케이스, 각 검증 규칙별 실패 케이스
2. **경계값 테스트**: null, 빈 값, 최대/최소 값
3. **동등성 테스트**: 같은 값, 다른 값, null과의 비교
4. **해시코드 테스트**: 동등한 객체의 해시코드 일치

비교 가능한 값 객체(`IComparable<T>`)는 추가로:
- `CompareTo()` 테스트
- 비교 연산자 테스트
- 정렬 동작 테스트

### Q2: Theory와 Fact 중 언제 무엇을 사용하나요?
**A**: 단일 케이스는 `[Fact]`, 여러 입력 데이터는 `[Theory]`를 사용합니다.

```csharp
// Fact: 단일 시나리오
[Fact]
public void Create_WithValidEmail_ReturnsSuccess()
{
    var result = Email.Create("user@example.com");
    result.ShouldBeSuccess();
}

// Theory: 여러 입력 데이터
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
[InlineData("no-at-sign")]
public void Create_WithInvalidInput_ReturnsFailure(string? input)
{
    var result = Email.Create(input!);
    result.ShouldBeFail();
}
```

`[Theory]`는 같은 검증 로직에 다양한 입력을 테스트할 때 코드 중복을 줄여줍니다.

### Q3: FluentAssertions와 테스트 헬퍼를 함께 사용해도 되나요?
**A**: 물론입니다. 둘을 조합하면 더 표현력 있는 테스트를 작성할 수 있습니다.

```csharp
[Fact]
public void Create_WithValidEmail_ReturnsExpectedValue()
{
    var result = Email.Create("User@Example.com");

    result.ShouldBeSuccess();                          // 테스트 헬퍼
    result.GetSuccessValue().Value
        .Should().Be("user@example.com");              // FluentAssertions
}

[Fact]
public void Create_WithInvalidEmail_ReturnsCorrectErrorCode()
{
    var result = Email.Create("invalid");

    result.ShouldBeFail();                             // 테스트 헬퍼
    result.GetFailError().Message
        .Should().Contain("Email.InvalidFormat");      // FluentAssertions
}
```

테스트 헬퍼는 `Fin<T>` 관련 검증을, FluentAssertions는 값 비교를 담당하면 역할이 명확해집니다.

### Q4: 해시코드 테스트는 왜 중요한가요?
**A**: 해시코드는 `Dictionary`, `HashSet` 등 해시 기반 컬렉션에서 사용됩니다. 동등성과 해시코드가 일관되지 않으면 예기치 않은 버그가 발생합니다.

```csharp
// 위험한 상황: Equals는 true인데 해시코드가 다른 경우
var dict = new Dictionary<Email, string>();
var email1 = Email.CreateFromValidated("user@example.com");
var email2 = Email.CreateFromValidated("user@example.com");

dict[email1] = "값";
var exists = dict.ContainsKey(email2);  // 해시코드가 다르면 false가 될 수 있음!
```

**규칙: `x.Equals(y)`가 true이면 `x.GetHashCode() == y.GetHashCode()`이어야 합니다.**
역은 성립하지 않아도 됩니다(해시 충돌 가능).

### Q5: 비공개 생성자를 가진 값 객체는 어떻게 테스트하나요?
**A**: `Create()` 메서드를 통해 테스트합니다. 테스트 전용으로 `CreateFromValidated()`를 활용할 수도 있습니다.

```csharp
// Create()를 통한 테스트
[Fact]
public void Create_ValidInput_Success()
{
    var result = Email.Create("user@example.com");
    result.ShouldBeSuccess();
}

// CreateFromValidated()를 통한 동등성 테스트
[Fact]
public void Equality_SameValue_AreEqual()
{
    // 검증 로직을 건너뛰고 바로 객체 생성
    var email1 = Email.CreateFromValidated("user@example.com");
    var email2 = Email.CreateFromValidated("user@example.com");

    email1.Should().Be(email2);
}
```

`CreateFromValidated()`는 테스트 코드에서 빠르게 값 객체를 생성할 때 유용합니다. 단, 유효하지 않은 값으로 테스트해서는 안 됩니다.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd TestingStrategies.Tests.Unit
dotnet test
```

### 테스트 구조
```
TestingStrategies.Tests.Unit/
├── CreationPatternTests.cs       # 생성 패턴 테스트
├── EqualityPatternTests.cs       # 동등성 패턴 테스트
├── ComparabilityPatternTests.cs  # 비교 가능성 패턴 테스트
└── FinTestExtensionsTests.cs     # Fin<T> 테스트 확장 검증
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| CreationPatternTests | 유효/무효 입력, 정규화, 경계값 |
| EqualityPatternTests | 동일 값 동등, 다른 값 비동등, 해시코드 |
| ComparabilityPatternTests | 정렬, 비교 연산자 |
| FinTestExtensionsTests | ShouldBeSuccess, ShouldBeFail 확장 |
