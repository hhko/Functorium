# 복합 값 객체
> `ValueObject`

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에 보는 정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 값 객체(Value Object) 패턴의 확장된 개념인 복합 값 객체(Composite Value Object)를 이해하고 실습하는 것을 목표로 합니다. 여러 개별 값 객체들을 조합하여 더 복잡하고 의미 있는 도메인 개념을 표현합니다.

## 학습 목표

### **핵심 학습 목표**
1. **복합 값 객체 이해**: 여러 값 객체를 조합하여 복잡한 도메인 개념을 표현하는 방법을 학습합니다.
2. **계층적 검증**: 여러 단계의 검증 로직을 LINQ Expression으로 구현하는 방법을 실습합니다.
3. **값 객체 컴포지션**: 작은 값 객체들을 조합하여 큰 개념을 만드는 방법을 체험합니다.
4. **복합 동등성**: 여러 구성 요소들의 동등성을 종합적으로 판단하는 방법을 학습합니다.

### **실습을 통해 확인할 내용**
- `ValueObject`의 계층적 상속 구조
- LINQ Expression을 활용한 복합 검증
- 여러 값 객체의 조합과 컴포지션
- 복합 동등성 비교의 구현

## 왜 필요한가?

이전 단계인 `04-ComparableValueObject-Primitive`에서는 여러 primitive 타입을 직접 조합하여 복합 데이터를 표현했습니다. 하지만 실제 애플리케이션에서는 더 복잡한 도메인 개념들이 등장합니다.

**첫 번째 문제는 도메인 복잡성의 증가입니다.** 이메일 주소처럼 로컬 부분과 도메인 부분이 각각 다른 규칙을 가지는 경우, 이를 하나의 단위로 다루어야 합니다. 이는 마치 객체지향에서 복합 객체를 다루는 것처럼 복잡한 도메인을 작은 단위로 분해하고 조합하는 과정입니다.

**두 번째 문제는 검증 로직의 복잡성입니다.** 이메일 주소의 경우 형식 검증, 로컬 부분 검증, 도메인 검증이 순차적으로 이루어져야 합니다. 이는 마치 파이프라인에서 데이터를 단계별로 처리하는 것처럼 복잡한 검증 체인을 구현해야 합니다.

**세 번째 문제는 재사용성과 모듈성입니다.** 이메일 로컬 부분이나 도메인은 다른 곳에서도 재사용될 수 있습니다. 이는 마치 LEGO 블록처럼 작은 단위의 값 객체들을 조합하여 더 큰 구조를 만드는 것입니다.

이러한 문제들을 해결하기 위해 복합 값 객체(Composite Value Object)를 도입했습니다. 복합 값 객체는 작은 값 객체들을 조합하여 더 복잡한 도메인 개념을 표현할 수 있게 합니다. 이는 마치 컴퓨터 과학에서 합성(Composition) 패턴을 사용하는 것처럼, 작은 구성 요소들로부터 복잡한 구조를 만드는 것입니다.

## 핵심 개념

이 프로젝트의 핵심은 여러 개별 값 객체들을 조합하여 복합적인 도메인 개념을 표현하는 것입니다. 크게 세 가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: 값 객체 컴포지션

복합 값 객체는 여러 개별 값 객체들을 조합하여 더 의미 있는 도메인 개념을 표현합니다. 이는 작은 LEGO 블록들을 조합하여 큰 구조를 만드는 것과 유사합니다.

**핵심 아이디어는 "작은 단위의 조합"입니다.** `EmailLocalPart`와 `EmailDomain`이라는 두 개의 작은 값 객체를 조합하여 `Email`이라는 더 큰 개념을 만듭니다.

예를 들어, 이메일 주소는 로컬 부분과 도메인 부분으로 구성됩니다. 각 부분은 독립적인 값 객체로 존재하지만, 함께 조합되어 이메일이라는 더 큰 개념을 형성합니다. 이는 마치 화학에서 원자들이 결합하여 분자를 형성하는 것과 유사합니다.

```csharp
// 개별 값 객체들
EmailLocalPart localPart = EmailLocalPart.Create("user");
EmailDomain domain = EmailDomain.Create("example.com");

// 복합 값 객체
Email email = Email.Create("user@example.com");
```

이러한 컴포지션은 코드의 모듈성과 재사용성을 크게 향상시킵니다. 작은 값 객체들은 다른 맥락에서도 재사용될 수 있습니다.

### 두 번째 개념: 계층적 검증 로직

복합 값 객체는 여러 단계의 검증 로직을 가지고 있습니다. 전체 검증, 부분 검증, 개별 검증이 계층적으로 이루어집니다.

**핵심 아이디어는 "단계적 검증 파이프라인"입니다.** 이메일 검증은 형식 검증 → 분할 검증 → 로컬 부분 검증 → 도메인 검증 순으로 진행됩니다.

예를 들어, 이메일 주소 검증은 다음과 같은 단계로 진행됩니다:
1. 기본 형식 검증 (`@` 포함 여부)
2. 주소 분할 검증 (로컬/도메인 분리)
3. 로컬 부분 검증 (길이, 형식)
4. 도메인 검증 (형식, 유효성)

이는 마치 공장에서 제품을 만드는 조립 라인처럼, 각 단계에서 품질 검사를 수행하는 것입니다.

```csharp
// 계층적 검증
public static Validation<Error, (EmailLocalPart, EmailDomain)> Validate(string email) =>
    from validEmail in ValidateEmailFormat(email)        // 1. 형식 검증
    from validParts in ValidateEmailParts(validEmail)     // 2. 분할 검증
    select validParts;                                     // 결과 조합
```

이러한 계층적 검증은 복잡한 비즈니스 규칙을 체계적으로 구현할 수 있게 합니다.

### 세 번째 개념: 복합 동등성

복합 값 객체의 동등성은 모든 구성 요소들의 동등성을 종합적으로 판단합니다. 모든 부분이 같아야 전체가 같다고 할 수 있습니다.

**핵심 아이디어는 "전체 = 부분들의 합"입니다.** 이메일 주소의 동등성은 로컬 부분과 도메인 부분이 모두 같을 때 성립합니다.

예를 들어, 두 이메일 주소가 동일하려면 로컬 부분과 도메인 부분이 모두 같아야 합니다. 이는 마치 수학에서 복합 수의 동등성을 판단하는 것처럼, 각 구성 요소의 동등성을 확인하는 것입니다.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return LocalPart;  // 로컬 부분 비교
    yield return Domain;     // 도메인 부분 비교
}
```

이러한 복합 동등성은 값 객체의 불변성과 일관성을 보장합니다. 모든 구성 요소가 같아야만 같은 객체로 취급됩니다.

## 실전 지침

### 예상 출력
```
=== 5. 비교 불가능한 복합 값 객체 - ValueObject ===
부모 클래스: ValueObject
예시: Email (이메일 주소) - EmailLocalPart + EmailDomain 조합

📋 특징:
   ✅ 복잡한 검증 로직을 가진 값 객체
   ✅ 동등성 비교만 제공
   ✅ 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
   ✅ EmailLocalPart + EmailDomain = Email

🔍 성공 케이스:
   ✅ Email: user@example.com
     - LocalPart: user
     - Domain: example.com

   ✅ Email: user@example.com
     - LocalPart: user
     - Domain: example.com

   ✅ Email: admin@test.org
     - LocalPart: admin
     - Domain: test.org

📊 동등성 비교:
   user@example.com == user@example.com = True
   user@example.com == admin@test.org = False

🔢 해시코드:
   user@example.com.GetHashCode() = -1711187277
   user@example.com.GetHashCode() = -1711187277
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   Email("invalid-email"): InvalidEmailFormat
   Email("@example.com"): EmptyOrOutOfRange
   Email("user@"): EmptyOrInvalidFormat

💡 복합 값 객체의 특징:
   - EmailLocalPart와 EmailDomain은 각각 독립적인 값 객체
   - Email은 이 두 값 객체를 조합하여 더 복잡한 도메인 개념 표현
   - 각 구성 요소는 자체적인 검증 로직을 가짐
   - 전체 Email은 구성 요소들의 조합으로 동등성 비교

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트
1. **계층적 값 객체 구조**: EmailLocalPart + EmailDomain → Email
2. **LINQ Expression 계층적 검증**: from-in 체인으로 복합 검증 구현
3. **GetEqualityComponents() 복합 구현**: 여러 구성 요소의 동등성 정의
4. **모듈성**: 작은 값 객체들의 재사용성 보장

## 프로젝트 설명

### 프로젝트 구조
```
05-ValueObject-Composite/
├── Program.cs                    # 메인 실행 파일
├── ValueObjectComposite.csproj  # 프로젝트 파일
├── ValueObjects/
│   ├── Email.cs                 # 복합 이메일 값 객체
│   ├── EmailLocalPart.cs        # 이메일 로컬 부분 값 객체
│   └── EmailDomain.cs           # 이메일 도메인 값 객체
└── README.md                    # 프로젝트 문서
```

### 핵심 코드

**EmailLocalPart.cs - 기본 값 객체**
```csharp
public sealed class EmailLocalPart : SimpleValueObject<string>
{
    private EmailLocalPart(string value) : base(value) { }

    public static Fin<EmailLocalPart> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new EmailLocalPart(validValue));

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 1 && value.Length <= 64
            ? value
            : DomainErrors.EmptyOrOutOfRange(value);
}
```

**Email.cs - 복합 값 객체**
```csharp
public sealed class Email : ValueObject
{
    public EmailLocalPart LocalPart { get; }
    public EmailDomain Domain { get; }

    private Email(EmailLocalPart localPart, EmailDomain domain)
    {
        LocalPart = localPart;
        Domain = domain;
    }

    // 계층적 검증
    public static Validation<Error, (EmailLocalPart, EmailDomain)> Validate(string email) =>
        from validEmail in ValidateEmailFormat(email)      // 1. 형식 검증
        from validParts in ValidateEmailParts(validEmail)   // 2. 분할 검증
        select validParts;                                   // 결과 조합

    // 복합 동등성
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LocalPart;
        yield return Domain;
    }
}
```

**Program.cs - 복합 값 객체 데모**
```csharp
// 복합 값 객체 생성
var email1 = Email.Create("user@example.com");
var email2 = Email.Create("user@example.com");

// 동등성 비교
var e1 = email1.Match(Succ: x => x, Fail: _ => default!);
var e2 = email2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {e1} == {e2} = {e1 == e2}");
```

## 한눈에 보는 정리

### 비교 표
| 구분 | ValueObject-Primitive | ValueObject-Composite |
|------|----------------------|---------------------|
| **구성 요소** | Primitive 타입 직접 사용 | 값 객체 컴포지션 |
| **검증 복잡성** | 단일 단계 검증 | 계층적 검증 |
| **재사용성** | 제한적 | 높음 (컴포넌트 재사용) |
| **모듈성** | 낮음 | 높음 |
| **유지보수성** | 보통 | 높음 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **높은 모듈성** | 구현 복잡도 증가 |
| **컴포넌트 재사용** | 계층 구조 복잡 |
| **유지보수성 향상** | 학습 곡선 존재 |
| **도메인 표현력** | 성능 오버헤드 |

## FAQ

### Q1: 복합 값 객체와 일반 클래스의 차이점은 무엇인가요?
**A**: 복합 값 객체는 구성 요소들의 불변성과 값 기반 동등성을 강제하는 반면, 일반 클래스는 이러한 제약이 없습니다. 복합 값 객체는 각 구성 요소가 값 객체여야 하며, 전체적으로 불변성을 유지합니다.

일반 클래스는 구성 요소를 자유롭게 변경할 수 있지만, 복합 값 객체는 생성 후에는 변경할 수 없습니다. 이는 마치 함수형 프로그래밍에서 불변 데이터 구조를 사용하는 것처럼 안전한 데이터 관리를 가능하게 합니다.

또한 복합 값 객체는 `GetEqualityComponents()`를 통해 동등성 비교 방식을 명시적으로 정의할 수 있습니다. 이는 일반 클래스에서 `Equals()`를 오버라이드하는 것보다 더 구조적이고 예측 가능한 방식입니다.

### Q2: 왜 계층적 검증을 사용하나요?
**A**: 계층적 검증은 복잡한 비즈니스 규칙을 단계별로 명확하게 구현할 수 있습니다. 각 단계에서 특정 측면의 유효성을 검증하므로 디버깅과 유지보수가 용이합니다.

예를 들어, 이메일 검증에서 먼저 기본 형식을 확인하고, 그 다음에 각 부분의 유효성을 검증하는 방식입니다. 이는 마치 공정 관리에서 각 단계별 품질 검사를 수행하는 것처럼 체계적인 검증을 가능하게 합니다.

이러한 계층적 접근은 검증 로직의 재사용성과 모듈성을 높여줍니다. 각 검증 단계는 독립적으로 테스트하고 재사용할 수 있습니다.

### Q3: GetEqualityComponents()는 어떻게 복합 동등성을 구현하나요?
**A**: `GetEqualityComponents()`는 복합 값 객체의 모든 구성 요소를 순차적으로 반환합니다. 두 복합 값 객체가 동일하려면 반환된 모든 요소가 pairwise로 같아야 합니다.

예를 들어, 이메일의 경우 로컬 부분과 도메인 부분이 모두 같아야 동일한 이메일로 취급됩니다. 이는 마치 데이터베이스에서 복합 키의 동등성을 비교하는 것처럼, 여러 필드의 조합으로 동일성을 판단하는 것입니다.

이러한 방식은 복합 객체의 동등성을 정확하고 예측 가능하게 만듭니다. 개발자는 어떤 요소들이 동등성에 영향을 미치는지 명확히 알 수 있습니다.

### Q4: 복합 값 객체의 성능에 영향이 있나요?
**A**: 복합 값 객체는 일반적으로 성능에 미미한 영향을 미칩니다. `GetEqualityComponents()`는 LINQ를 사용하므로 약간의 할당 오버헤드가 있을 수 있지만, 대부분의 경우 무시할 만합니다.

실제 성능 bottleneck은 대개 동등성 비교의 빈도와 복합 객체의 크기에 있습니다. 값 객체는 일반적으로 자주 생성되지 않고, 동등성 비교도 빈번하지 않습니다. 이는 마치 마이크로 최적화가 큰 의미가 없는 것처럼, 복합 값 객체의 성능 오버헤드는 실제 애플리케이션에서 큰 문제가 되지 않습니다.

성능이 정말 중요한 경우라도 복합 값 객체의 안정성과 가독성 이점이 더 큽니다. 대부분의 애플리케이션에서 성능 차이는 무시할 수준입니다.

### Q5: 언제 복합 값 객체 대신 일반 클래스를 사용해야 하나요?
**A**: 구성 요소가 변경되어야 하거나, 참조 동등성이 필요한 경우 일반 클래스를 사용합니다. 복합 값 객체는 값 기반 동등성과 불변성이 필요한 경우에만 사용해야 합니다.

예를 들어, 사용자의 이메일 주소가 자주 변경되는 경우 일반 클래스를 사용하는 것이 좋습니다. 복합 값 객체는 이벤트, 설정, 구성과 같이 한 번 설정된 후 변경되지 않는 값에 적합합니다.

이는 마치 데이터베이스에서 정규화된 스키마를 사용할지, 비정규화된 스키마를 사용할지 결정하는 것과 유사합니다. 변경이 빈번한 데이터는 일반 클래스로, 변경이 드문 값은 복합 값 객체로 표현하는 것이 좋습니다.

### Q6: 작은 값 객체들의 재사용성은 어떻게 보장되나요?
**A**: 복합 값 객체는 컴포지션 패턴을 따르므로 각 구성 요소가 독립적입니다. `EmailLocalPart`나 `EmailDomain` 같은 작은 값 객체들은 다른 맥락에서도 재사용될 수 있습니다.

예를 들어, `EmailDomain`은 사용자 등록, 뉴스레터 구독 등 다양한 곳에서 재사용될 수 있습니다. 이는 마치 소프트웨어 공학에서 모듈화와 컴포넌트 재사용의 원칙을 따르는 것입니다.

이러한 재사용성은 코드 중복을 줄이고 일관성을 높여줍니다. 작은 값 객체들은 마치 표준 라이브러리의 유틸리티 클래스처럼 여러 곳에서 재사용될 수 있습니다.



