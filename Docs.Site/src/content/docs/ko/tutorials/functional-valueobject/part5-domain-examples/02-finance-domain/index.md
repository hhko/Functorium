---
title: "금융 도메인"
---
## 개요

이자율 5%와 0.05를 혼동하면 금액이 100배 달라집니다. "USD/KRW = 1350"이 1달러당 1350원인지 1원당 1350달러인지 모호하면 환전 오류가 발생합니다. 계좌번호가 로그에 평문으로 출력되면 보안 사고로 이어집니다. 금융 시스템에서 원시 타입은 이런 위험을 그대로 노출합니다.

이 장에서는 금융 도메인의 핵심 개념 4가지를 값 객체로 구현하여, 계산 오류와 보안 위험을 타입 시스템으로 방지합니다.

- **AccountNumber**: 은행 코드와 계좌번호를 파싱하고 마스킹하는 값 객체
- **InterestRate**: 이자율을 표현하며 단리/복리 계산 기능을 제공
- **ExchangeRate**: 통화 쌍과 환율을 관리하며 변환/역변환 기능 제공
- **TransactionType**: 거래 유형(입금/출금)을 표현하는 타입 안전 열거형

## 학습 목표

### **핵심 학습 목표**
- InterestRate에서 단리/복리 이자 계산을 값 객체 내부에 **캡슐화할 수** 있습니다.
- ExchangeRate에서 Invert() 메서드로 **역환율을 계산할 수** 있습니다.
- AccountNumber에서 계좌번호를 **마스킹하여 보안을 강화할 수** 있습니다.
- TransactionType에서 입금/출금 구분 속성을 가진 **타입 안전 열거형을 구현할 수** 있습니다.

### **실습을 통해 확인할 내용**
- AccountNumber의 은행 코드 파싱과 마스킹
- InterestRate의 단리/복리 계산
- ExchangeRate의 통화 변환과 역환율 계산
- TransactionType의 입금/출금 분류

## 왜 필요한가?

금융 시스템은 정확성과 보안이 특히 중요합니다. 원시 타입으로 금융 데이터를 다루면 여러 위험이 발생합니다.

이자율 계산에서 백분율(5%)과 소수(0.05)를 혼동하면 금액 오류가 발생하는데, InterestRate 값 객체는 Percentage와 Decimal 속성을 명확히 구분하여 제공합니다. ExchangeRate는 BaseCurrency와 QuoteCurrency를 명시적으로 관리하여 환율 방향 혼동을 원천 차단합니다. 계좌번호를 로그에 그대로 출력하는 보안 문제도, AccountNumber의 Masked 속성으로 안전한 표시를 지원하여 해결합니다.

## 핵심 개념

### AccountNumber (계좌번호)

AccountNumber는 은행 계좌번호를 검증하고 파싱합니다. 은행 코드 추출과 마스킹 기능을 제공합니다.

```csharp
public sealed class AccountNumber : SimpleValueObject<string>
{
    private static readonly Regex Format = new(@"^\d{3}-\d{10,14}$", RegexOptions.Compiled);

    private AccountNumber(string value) : base(value) { }

    public string FullNumber => Value;  // protected Value에 대한 public 접근자
    public string BankCode => Value[..3];            // "110"
    public string Number => Value[4..];              // "1234567890"
    public string Masked => $"{BankCode}-****{Number[^4..]}"; // "110-****7890"

    public static Fin<AccountNumber> Create(string? value) =>
        CreateFromValidation(Validate(value ?? "null"), v => new AccountNumber(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<AccountNumber>.NotEmpty(value)
            .ThenNormalize(v => v.Replace(" ", "").Replace("\u2212", "-"))
            .ThenMatches(Format,
                $"Invalid account number format. Expected: 'NNN-NNNNNNNNNN'. Current value: '{value}'");

    public static implicit operator string(AccountNumber account) => account.Value;
}
```

`ToString()`은 전체 계좌번호를 반환하지만, `Masked`는 중간 부분을 가려서 로그나 화면 표시에 사용할 수 있습니다. 민감 정보의 안전한 표시 패턴입니다.

### InterestRate (이자율)

InterestRate는 이자율을 백분율로 저장하고, 단리/복리 이자 계산 기능을 제공합니다.

```csharp
public sealed class InterestRate : ComparableSimpleValueObject<decimal>
{
    private InterestRate(decimal value) : base(value) { }

    public decimal Percentage => Value;          // 5.5 (%)
    public decimal Decimal => Value / 100m;      // 0.055

    public static Fin<InterestRate> Create(decimal percentValue) =>
        CreateFromValidation(Validate(percentValue), v => new InterestRate(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<InterestRate>.NonNegative(value)
            .ThenAtMost(100m);

    // 단리: 원금 x 이율 x 기간
    public decimal CalculateSimpleInterest(decimal principal, int years) =>
        principal * Decimal * years;

    // 복리: 원금 x ((1 + 이율)^기간 - 1)
    public decimal CalculateCompoundInterest(decimal principal, int years) =>
        principal * ((decimal)Math.Pow((double)(1 + Decimal), years) - 1);

    public static implicit operator decimal(InterestRate rate) => rate.Decimal;
}
```

이자 계산 공식이 값 객체 내부에 있으므로 어디서든 일관된 계산을 보장합니다. Percentage와 Decimal 속성을 분리하여 백분율/소수 혼동을 방지합니다.

### ExchangeRate (환율)

ExchangeRate는 통화 쌍(USD/KRW)과 환율을 관리합니다. 변환과 역환율 계산 기능을 제공합니다.

```csharp
public sealed class ExchangeRate : ValueObject
{
    public sealed record InvalidBaseCurrency : DomainErrorKind.Custom;
    public sealed record InvalidQuoteCurrency : DomainErrorKind.Custom;
    public sealed record SameCurrency : DomainErrorKind.Custom;

    public string BaseCurrency { get; }    // "USD"
    public string QuoteCurrency { get; }   // "KRW"
    public decimal Rate { get; }           // 1350.50

    private ExchangeRate(string baseCurrency, string quoteCurrency, decimal rate)
    {
        BaseCurrency = baseCurrency; QuoteCurrency = quoteCurrency; Rate = rate;
    }

    public static Fin<ExchangeRate> Create(string? baseCurrency, string? quoteCurrency, decimal rate) =>
        CreateFromValidation(
            Validate(baseCurrency ?? "null", quoteCurrency ?? "null", rate),
            v => new ExchangeRate(v.BaseCurrency.ToUpperInvariant(), v.QuoteCurrency.ToUpperInvariant(), v.Rate));

    public static Validation<Error, (string BaseCurrency, string QuoteCurrency, decimal Rate)> Validate(
        string baseCurrency, string quoteCurrency, decimal rate) =>
        (ValidateCurrency(baseCurrency, new InvalidBaseCurrency(), "basecurrency"),
         ValidateCurrency(quoteCurrency, new InvalidQuoteCurrency(), "quotecurrency"),
         ValidationRules<ExchangeRate>.Positive(rate))
            .Apply((b, q, r) => (BaseCurrency: b, QuoteCurrency: q, Rate: r))
            .Bind(v => ValidateDifferentCurrencies(v.BaseCurrency, v.QuoteCurrency)
                .Map(_ => (v.BaseCurrency, v.QuoteCurrency, v.Rate)));

    public decimal Convert(decimal amount) => amount * Rate;       // 100 USD -> 135,050 KRW
    public decimal ConvertBack(decimal amount) => amount / Rate;   // 135,050 KRW -> 100 USD
    public ExchangeRate Invert() => new(QuoteCurrency, BaseCurrency, 1m / Rate);
    public string Pair => $"{BaseCurrency}/{QuoteCurrency}";       // "USD/KRW"

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseCurrency; yield return QuoteCurrency; yield return Rate;
    }
}
```

`Convert()`는 기준 통화에서 견적 통화로, `ConvertBack()`은 반대 방향으로, `Invert()`는 역환율 객체를 반환합니다. 양방향 변환이 명시적으로 표현됩니다.

### TransactionType (거래 유형)

TransactionType은 SmartEnum을 사용하여 입금/출금을 구분합니다.

```csharp
public sealed class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Deposit = new("DEPOSIT", "입금", isCredit: true);
    public static readonly TransactionType Withdrawal = new("WITHDRAWAL", "출금", isCredit: false);
    public static readonly TransactionType Transfer = new("TRANSFER", "이체", isCredit: false);
    public static readonly TransactionType Interest = new("INTEREST", "이자", isCredit: true);
    public static readonly TransactionType Fee = new("FEE", "수수료", isCredit: false);

    public string DisplayName { get; }
    public bool IsCredit { get; }    // 입금 계열
    public bool IsDebit => !IsCredit;  // 출금 계열
}
```

`IsCredit` 속성으로 잔액 계산 시 더하기/빼기를 결정할 수 있습니다. 다음 코드는 이 분류 속성을 활용한 잔액 갱신 예시입니다.

```csharp
decimal UpdateBalance(decimal balance, TransactionType type, decimal amount) =>
    type.IsCredit ? balance + amount : balance - amount;
```

## 실전 지침

### 예상 출력
```
=== 금융 도메인 값 객체 ===

1. AccountNumber (계좌번호)
────────────────────────────────────────
   계좌번호: 110-1234567890
   은행 코드: 110
   마스킹: 110-****7890

2. InterestRate (이자율)
────────────────────────────────────────
   연이율: 5.50%
   원금: 1,000,000원
   기간: 3년
   단리 이자: 165,000원
   복리 이자: 174,241원

3. ExchangeRate (환율)
────────────────────────────────────────
   환율: USD/KRW = 1350.5000
   100 USD = 135,050 KRW
   역환율: KRW/USD = 0.0007

4. TransactionType (거래 유형)
────────────────────────────────────────
   모든 거래 유형:
      - DEPOSIT: 입금 (입금)
      - WITHDRAWAL: 출금 (출금)
      - TRANSFER: 이체 (출금)
      - INTEREST: 이자 (입금)
      - FEE: 수수료 (출금)
```

## 프로젝트 설명

### 프로젝트 구조
```
02-Finance-Domain/
├── FinanceDomain/
│   ├── Program.cs              # 메인 실행 파일 (4개 값 객체 구현)
│   └── FinanceDomain.csproj    # 프로젝트 파일
└── README.md                   # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Ardalis.SmartEnum" />
</ItemGroup>
```

### 값 객체별 프레임워크 타입

각 값 객체가 상속하는 프레임워크 타입과 주요 특징을 정리한 것입니다.

| 값 객체 | 프레임워크 타입 | 특징 |
|--------|---------------|------|
| AccountNumber | SimpleValueObject\<string\> | ValidationRules 체인, 파싱, 마스킹 |
| InterestRate | ComparableSimpleValueObject\<decimal\> | ValidationRules 체인, 단리/복리 계산 |
| ExchangeRate | ValueObject | 병렬 검증 + Bind, 통화 쌍 관리, 변환 |
| TransactionType | SmartEnum | 입금/출금 분류 |

## 한눈에 보는 정리

### 금융 값 객체 요약

각 값 객체의 속성, 검증 규칙, 도메인 연산을 한눈에 비교할 수 있습니다.

| 값 객체 | 주요 속성 | 검증 규칙 | 도메인 연산 |
|--------|----------|----------|------------|
| AccountNumber | Value | NNN-NNNNNNNNNN 형식 | BankCode, Masked |
| InterestRate | Value | 0~100% 범위 | 단리/복리 계산 |
| ExchangeRate | Base, Quote, Rate | 3자리 통화, 양수 환율 | Convert, Invert |
| TransactionType | Value, IsCredit | 정의된 유형만 | 없음 |

### 금융 도메인 패턴

금융 도메인에서 활용된 설계 패턴을 유형별로 분류하면 다음과 같습니다.

| 패턴 | 값 객체 | 설명 |
|------|--------|------|
| 민감 정보 마스킹 | AccountNumber | 일부 정보를 가려서 안전하게 표시 |
| 도메인 계산 캡슐화 | InterestRate | 이자 계산 공식을 값 객체 내부에 구현 |
| 양방향 변환 | ExchangeRate | 정방향/역방향 변환 메서드 제공 |
| 분류 속성 | TransactionType | IsCredit/IsDebit으로 동작 결정 |

## FAQ

### Q1: InterestRate에서 월복리나 일복리를 계산하려면?

복리 기간을 매개변수로 추가하거나 별도 메서드를 제공합니다.

```csharp
public decimal CalculateCompoundInterest(
    decimal principal,
    int years,
    CompoundingFrequency frequency = CompoundingFrequency.Annual)
{
    int n = frequency switch
    {
        CompoundingFrequency.Annual => 1,
        CompoundingFrequency.SemiAnnual => 2,
        CompoundingFrequency.Quarterly => 4,
        CompoundingFrequency.Monthly => 12,
        CompoundingFrequency.Daily => 365,
        _ => 1
    };

    return principal * ((decimal)Math.Pow((double)(1 + Decimal / n), n * years) - 1);
}
```

### Q2: ExchangeRate에서 여러 통화 간 체인 환전을 하려면?

별도의 ExchangeRateService를 만들어 환율 체인을 관리합니다.

```csharp
public class ExchangeRateService
{
    private readonly Dictionary<string, ExchangeRate> _rates;

    public decimal Convert(decimal amount, string from, string to)
    {
        if (_rates.TryGetValue($"{from}/{to}", out var directRate))
            return directRate.Convert(amount);

        // USD를 중간 통화로 사용하여 변환
        var toUsd = _rates[$"{from}/USD"];
        var fromUsd = _rates[$"USD/{to}"];
        return fromUsd.Convert(toUsd.Convert(amount));
    }
}
```

### Q3: AccountNumber에서 국가별 다른 형식을 지원하려면?

국가 코드를 매개변수로 받아 다른 검증 패턴을 적용합니다.

```csharp
public static Fin<AccountNumber> Create(string value, string countryCode = "KR")
{
    var pattern = countryCode switch
    {
        "KR" => @"^\d{3}-\d{10,14}$",
        "US" => @"^\d{9}-\d{12}$",  // 라우팅 번호 + 계좌번호
        "GB" => @"^\d{6}-\d{8}$",   // 정렬 코드 + 계좌번호
        _ => throw new ArgumentException("지원하지 않는 국가 코드")
    };

    // 검증 로직...
}
```

금융 도메인의 값 객체 구현을 살펴보았습니다. 다음 장에서는 이메일, 비밀번호, 전화번호 등 보안과 데이터 품질이 특히 중요한 사용자 관리 도메인의 값 객체를 구현합니다.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd FinanceDomain.Tests.Unit
dotnet test
```

### 테스트 구조
```
FinanceDomain.Tests.Unit/
├── AccountNumberTests.cs     # 계좌번호 형식 검증 테스트
├── InterestRateTests.cs      # 이자율 범위 검증 테스트
├── ExchangeRateTests.cs      # 환율 변환 테스트
└── TransactionTypeTests.cs   # 거래 유형 SmartEnum 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| AccountNumberTests | 형식 검증, 은행 코드/계좌번호 파싱 |
| InterestRateTests | 범위 검증, 소수점 변환, 비교 연산 |
| ExchangeRateTests | 환율 검증, 통화 변환 계산 |
| TransactionTypeTests | 입금/출금 분류, IsCredit/IsDebit |

---

금융 도메인의 값 객체를 구현했습니다. 다음 장에서는 사용자 관리 도메인에서 이메일, 비밀번호, 전화번호 등 개인정보 보호가 중요한 값 객체를 다룹니다.

→ [3장: 사용자 관리 도메인](../03-User-Management-Domain/)
