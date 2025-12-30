using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using Ardalis.SmartEnum;
using static LanguageExt.Prelude;

namespace FinanceDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 금융 도메인 값 객체 (Functorium 프레임워크 기반) ===\n");

        // 1. AccountNumber (계좌번호)
        DemonstrateAccountNumber();

        // 2. InterestRate (이자율)
        DemonstrateInterestRate();

        // 3. ExchangeRate (환율)
        DemonstrateExchangeRate();

        // 4. TransactionType (거래 유형)
        DemonstrateTransactionType();
    }

    static void DemonstrateAccountNumber()
    {
        Console.WriteLine("1. AccountNumber (계좌번호) - SimpleValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var account = AccountNumber.Create("110-1234567890");
        account.Match(
            Succ: a =>
            {
                Console.WriteLine($"   계좌번호: {a}");
                Console.WriteLine($"   은행 코드: {a.BankCode}");
                Console.WriteLine($"   마스킹: {a.Masked}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateInterestRate()
    {
        Console.WriteLine("2. InterestRate (이자율) - ComparableSimpleValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var rate = InterestRate.Create(5.5m);
        rate.Match(
            Succ: r =>
            {
                var principal = 1_000_000m;
                var years = 3;

                Console.WriteLine($"   연이율: {r}");
                Console.WriteLine($"   원금: {principal:N0}원");
                Console.WriteLine($"   기간: {years}년");
                Console.WriteLine($"   단리 이자: {r.CalculateSimpleInterest(principal, years):N0}원");
                Console.WriteLine($"   복리 이자: {r.CalculateCompoundInterest(principal, years):N0}원");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateExchangeRate()
    {
        Console.WriteLine("3. ExchangeRate (환율) - ValueObject");
        Console.WriteLine("─".PadRight(40, '─'));

        var rate = ExchangeRate.Create("USD", "KRW", 1350.50m);
        rate.Match(
            Succ: r =>
            {
                Console.WriteLine($"   환율: {r}");
                Console.WriteLine($"   100 USD = {r.Convert(100):N0} KRW");

                var inverse = r.Invert();
                Console.WriteLine($"   역환율: {inverse}");
            },
            Fail: e => Console.WriteLine($"   오류: {e.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateTransactionType()
    {
        Console.WriteLine("4. TransactionType (거래 유형) - SmartEnum");
        Console.WriteLine("─".PadRight(40, '─'));

        Console.WriteLine("   모든 거래 유형:");
        foreach (var type in TransactionType.List)
        {
            Console.WriteLine($"      - {type.Value}: {type.DisplayName} ({(type.IsCredit ? "입금" : "출금")})");
        }

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 구현 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// AccountNumber 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class AccountNumber : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private AccountNumber(string value) : base(value) { }

    /// <summary>
    /// 계좌번호에 대한 public 접근자 (전체 값)
    /// </summary>
    public string FullNumber => Value;

    // 파생 속성
    public string BankCode => Value[..3];
    public string Number => Value[4..];
    public string Masked => $"{BankCode}-****{Number[^4..]}";

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<AccountNumber> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new AccountNumber(validValue));

    // 5. Public Validate 메서드 - 순차 검증
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateFormat(value));

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 형식 검증
    private static Validation<Error, string> ValidateFormat(string value)
    {
        var normalized = value.Replace(" ", "").Replace("−", "-");
        return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d{3}-\d{10,14}$")
            ? normalized
            : DomainErrors.InvalidFormat(normalized);
    }

    public static implicit operator string(AccountNumber account) => account.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(AccountNumber)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Account number cannot be empty. Current value: '{value}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(AccountNumber)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid account number format. Expected format: 'NNN-NNNNNNNNNN' (e.g., 110-1234567890). Current value: '{value}'");
    }
}

/// <summary>
/// InterestRate 값 객체 (ComparableSimpleValueObject 기반)
/// </summary>
public sealed class InterestRate : ComparableSimpleValueObject<decimal>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private InterestRate(decimal value) : base(value) { }

    /// <summary>
    /// 이자율 값에 대한 public 접근자 (퍼센트)
    /// </summary>
    public decimal Percentage => Value;

    // 파생 속성
    public decimal Decimal => Value / 100m;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<InterestRate> Create(decimal percentValue) =>
        CreateFromValidation(
            Validate(percentValue),
            validValue => new InterestRate(validValue));

    // 5. Public Validate 메서드 - 순차 검증
    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotExceedsMaximum(value))
            .Map(_ => value);

    // 5.1 음수 검증
    private static Validation<Error, decimal> ValidateNotNegative(decimal value) =>
        value >= 0
            ? value
            : DomainErrors.Negative(value);

    // 5.2 최대값 검증
    private static Validation<Error, decimal> ValidateNotExceedsMaximum(decimal value) =>
        value <= 100
            ? value
            : DomainErrors.ExceedsMaximum(value);

    // 도메인 메서드
    public decimal CalculateSimpleInterest(decimal principal, int years) =>
        principal * Decimal * years;

    public decimal CalculateCompoundInterest(decimal principal, int years) =>
        principal * ((decimal)Math.Pow((double)(1 + Decimal), years) - 1);

    public static implicit operator decimal(InterestRate rate) => rate.Decimal;

    public override string ToString() => $"{Value:F2}%";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Negative(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(InterestRate)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: $"Interest rate cannot be negative. Current value: '{value}'");

        public static Error ExceedsMaximum(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(InterestRate)}.{nameof(ExceedsMaximum)}",
                errorCurrentValue: value,
                errorMessage: $"Interest rate cannot exceed 100%. Current value: '{value}'");
    }
}

/// <summary>
/// ExchangeRate 값 객체 (ValueObject 기반)
/// </summary>
public sealed class ExchangeRate : ValueObject
{
    // 1.1 속성 선언
    public string BaseCurrency { get; }
    public string QuoteCurrency { get; }
    public decimal Rate { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private ExchangeRate(string baseCurrency, string quoteCurrency, decimal rate)
    {
        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
        Rate = rate;
    }

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<ExchangeRate> Create(string? baseCurrency, string? quoteCurrency, decimal rate) =>
        CreateFromValidation(
            Validate(baseCurrency ?? "null", quoteCurrency ?? "null", rate),
            validValues => new ExchangeRate(
                validValues.BaseCurrency.ToUpperInvariant(),
                validValues.QuoteCurrency.ToUpperInvariant(),
                validValues.Rate));

    // 5. Public Validate 메서드 - 병렬 검증 후 순차 검증
    public static Validation<Error, (string BaseCurrency, string QuoteCurrency, decimal Rate)> Validate(
        string baseCurrency, string quoteCurrency, decimal rate) =>
        (ValidateBaseCurrency(baseCurrency), ValidateQuoteCurrency(quoteCurrency), ValidateRate(rate))
            .Apply((validBase, validQuote, validRate) => (validBase, validQuote, validRate))
            .As()
            .Bind(values => ValidateDifferentCurrencies(values.validBase, values.validQuote)
                .Map(_ => (values.validBase, values.validQuote, values.validRate)));

    // 5.1 기준 통화 검증
    private static Validation<Error, string> ValidateBaseCurrency(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == 3
            ? value
            : DomainErrors.InvalidBaseCurrency(value);

    // 5.2 견적 통화 검증
    private static Validation<Error, string> ValidateQuoteCurrency(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == 3
            ? value
            : DomainErrors.InvalidQuoteCurrency(value);

    // 5.3 환율 검증
    private static Validation<Error, decimal> ValidateRate(decimal value) =>
        value > 0
            ? value
            : DomainErrors.InvalidRate(value);

    // 5.4 통화 다름 검증
    private static Validation<Error, Unit> ValidateDifferentCurrencies(string baseCurrency, string quoteCurrency) =>
        !baseCurrency.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)
            ? unit
            : DomainErrors.SameCurrency(baseCurrency, quoteCurrency);

    // 도메인 메서드
    public decimal Convert(decimal amount) => amount * Rate;
    public decimal ConvertBack(decimal amount) => amount / Rate;
    public ExchangeRate Invert() => new(QuoteCurrency, BaseCurrency, 1m / Rate);

    public string Pair => $"{BaseCurrency}/{QuoteCurrency}";

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseCurrency;
        yield return QuoteCurrency;
        yield return Rate;
    }

    public override string ToString() => $"{Pair} = {Rate:F4}";

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error InvalidBaseCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ExchangeRate)}.{nameof(InvalidBaseCurrency)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid base currency code. Must be 3 characters. Current value: '{value}'");

        public static Error InvalidQuoteCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ExchangeRate)}.{nameof(InvalidQuoteCurrency)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid quote currency code. Must be 3 characters. Current value: '{value}'");

        public static Error InvalidRate(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ExchangeRate)}.{nameof(InvalidRate)}",
                errorCurrentValue: value,
                errorMessage: $"Exchange rate must be greater than zero. Current value: '{value}'");

        public static Error SameCurrency(string baseCurrency, string quoteCurrency) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(ExchangeRate)}.{nameof(SameCurrency)}",
                baseCurrency, quoteCurrency,
                errorMessage: $"Base currency and quote currency cannot be the same. Base: '{baseCurrency}', Quote: '{quoteCurrency}'");
    }
}

/// <summary>
/// TransactionType 값 객체 (SmartEnum 기반)
/// </summary>
public sealed class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Deposit = new("DEPOSIT", "입금", isCredit: true);
    public static readonly TransactionType Withdrawal = new("WITHDRAWAL", "출금", isCredit: false);
    public static readonly TransactionType Transfer = new("TRANSFER", "이체", isCredit: false);
    public static readonly TransactionType Interest = new("INTEREST", "이자", isCredit: true);
    public static readonly TransactionType Fee = new("FEE", "수수료", isCredit: false);

    public string DisplayName { get; }
    public bool IsCredit { get; }
    public bool IsDebit => !IsCredit;

    private TransactionType(string value, string displayName, bool isCredit)
        : base(displayName, value)
    {
        DisplayName = displayName;
        IsCredit = isCredit;
    }
}
