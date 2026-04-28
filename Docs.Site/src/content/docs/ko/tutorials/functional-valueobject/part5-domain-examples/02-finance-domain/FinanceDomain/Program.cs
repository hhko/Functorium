using System.Text.RegularExpressions;
using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using Functorium.Domains.Errors;
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
/// ValidationRules 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class AccountNumber : SimpleValueObject<string>
{
    private static readonly Regex Format = new(@"^\d{3}-\d{10,14}$", RegexOptions.Compiled);

    private AccountNumber(string value) : base(value) { }

    public string FullNumber => Value;
    public string BankCode => Value[..3];
    public string Number => Value[4..];
    public string Masked => $"{BankCode}-****{Number[^4..]}";

    public static Fin<AccountNumber> Create(string? value) =>
        CreateFromValidation(Validate(value ?? "null"), v => new AccountNumber(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<AccountNumber>.NotEmpty(value)
            .ThenNormalize(v => v.Replace(" ", "").Replace("−", "-"))
            .ThenMatches(Format,
                $"Invalid account number format. Expected: 'NNN-NNNNNNNNNN'. Current value: '{value}'");

    public static implicit operator string(AccountNumber account) => account.Value;
}

/// <summary>
/// InterestRate 값 객체 (ComparableSimpleValueObject 기반)
/// ValidationRules 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class InterestRate : ComparableSimpleValueObject<decimal>
{
    private InterestRate(decimal value) : base(value) { }

    public decimal Percentage => Value;
    public decimal Decimal => Value / 100m;

    public static Fin<InterestRate> Create(decimal percentValue) =>
        CreateFromValidation(Validate(percentValue), v => new InterestRate(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<InterestRate>.NonNegative(value)
            .ThenAtMost(100m);

    public decimal CalculateSimpleInterest(decimal principal, int years) =>
        principal * Decimal * years;

    public decimal CalculateCompoundInterest(decimal principal, int years) =>
        principal * ((decimal)Math.Pow((double)(1 + Decimal), years) - 1);

    public static implicit operator decimal(InterestRate rate) => rate.Decimal;

    public override string ToString() => $"{Value:F2}%";
}

/// <summary>
/// ExchangeRate 값 객체 (ValueObject 기반)
/// ValidationRules 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class ExchangeRate : ValueObject
{
    public sealed record InvalidBaseCurrency : DomainErrorKind.Custom;
    public sealed record InvalidQuoteCurrency : DomainErrorKind.Custom;
    public sealed record SameCurrency : DomainErrorKind.Custom;

    public string BaseCurrency { get; }
    public string QuoteCurrency { get; }
    public decimal Rate { get; }

    private ExchangeRate(string baseCurrency, string quoteCurrency, decimal rate)
    {
        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
        Rate = rate;
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

    private static Validation<Error, string> ValidateCurrency(string value, DomainErrorKind errorType, string fieldName) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == 3
            ? value
            : DomainError.For<ExchangeRate>(
                errorType,
                value,
                $"Invalid {fieldName} code. Must be 3 characters. Current value: '{value}'");

    private static Validation<Error, Unit> ValidateDifferentCurrencies(string baseCurrency, string quoteCurrency) =>
        !baseCurrency.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)
            ? unit
            : DomainError.For<ExchangeRate, string, string>(
                new SameCurrency(),
                baseCurrency, quoteCurrency,
                $"Base and quote currency cannot be the same. Base: '{baseCurrency}', Quote: '{quoteCurrency}'");

    public decimal Convert(decimal amount) => amount * Rate;
    public decimal ConvertBack(decimal amount) => amount / Rate;
    public ExchangeRate Invert() => new(QuoteCurrency, BaseCurrency, 1m / Rate);
    public string Pair => $"{BaseCurrency}/{QuoteCurrency}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseCurrency;
        yield return QuoteCurrency;
        yield return Rate;
    }

    public override string ToString() => $"{Pair} = {Rate:F4}";
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
