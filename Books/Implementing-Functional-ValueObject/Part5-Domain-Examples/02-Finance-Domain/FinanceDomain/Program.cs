using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;
using Ardalis.SmartEnum;
using static LanguageExt.Prelude;

namespace FinanceDomain;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 금융 도메인 값 객체 ===\n");

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
        Console.WriteLine("1. AccountNumber (계좌번호)");
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
        Console.WriteLine("2. InterestRate (이자율)");
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
        Console.WriteLine("3. ExchangeRate (환율)");
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
        Console.WriteLine("4. TransactionType (거래 유형)");
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
// 값 객체 구현
// ========================================

public sealed class AccountNumber : IEquatable<AccountNumber>
{
    public string Value { get; }

    private AccountNumber(string value) => Value = value;

    public static Fin<AccountNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        var normalized = value.Replace(" ", "").Replace("−", "-");

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d{3}-\d{10,14}$"))
            return DomainErrors.InvalidFormat(normalized);

        return new AccountNumber(normalized);
    }

    public string BankCode => Value[..3];
    public string Number => Value[4..];
    public string Masked => $"{BankCode}-****{Number[^4..]}";

    public bool Equals(AccountNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is AccountNumber other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(AccountNumber account) => account.Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(AccountNumber)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "계좌번호가 비어있습니다.");
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(AccountNumber)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "계좌번호 형식이 올바르지 않습니다. (예: 110-1234567890)");
    }
}

public sealed class InterestRate : IComparable<InterestRate>, IEquatable<InterestRate>
{
    public decimal Value { get; }

    private InterestRate(decimal value) => Value = value;

    public static Fin<InterestRate> Create(decimal percentValue)
    {
        if (percentValue < 0)
            return DomainErrors.Negative(percentValue);
        if (percentValue > 100)
            return DomainErrors.ExceedsMaximum(percentValue);
        return new InterestRate(percentValue);
    }

    public decimal Percentage => Value;
    public decimal Decimal => Value / 100m;

    public decimal CalculateSimpleInterest(decimal principal, int years) =>
        principal * Decimal * years;

    public decimal CalculateCompoundInterest(decimal principal, int years) =>
        principal * ((decimal)Math.Pow((double)(1 + Decimal), years) - 1);

    public int CompareTo(InterestRate? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public bool Equals(InterestRate? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is InterestRate other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{Value:F2}%";

    public static implicit operator decimal(InterestRate rate) => rate.Decimal;

    internal static class DomainErrors
    {
        public static Error Negative(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(InterestRate)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: "이자율은 음수일 수 없습니다.");
        public static Error ExceedsMaximum(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(InterestRate)}.{nameof(ExceedsMaximum)}",
                errorCurrentValue: value,
                errorMessage: "이자율은 100%를 초과할 수 없습니다.");
    }
}

public sealed class ExchangeRate : IEquatable<ExchangeRate>
{
    public string BaseCurrency { get; }
    public string QuoteCurrency { get; }
    public decimal Rate { get; }

    private ExchangeRate(string baseCurrency, string quoteCurrency, decimal rate)
    {
        BaseCurrency = baseCurrency;
        QuoteCurrency = quoteCurrency;
        Rate = rate;
    }

    public static Fin<ExchangeRate> Create(string baseCurrency, string quoteCurrency, decimal rate)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency) || baseCurrency.Length != 3)
            return DomainErrors.InvalidBaseCurrency(baseCurrency ?? "null");
        if (string.IsNullOrWhiteSpace(quoteCurrency) || quoteCurrency.Length != 3)
            return DomainErrors.InvalidQuoteCurrency(quoteCurrency ?? "null");
        if (rate <= 0)
            return DomainErrors.InvalidRate(rate);
        if (baseCurrency.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase))
            return DomainErrors.SameCurrency(baseCurrency, quoteCurrency);

        return new ExchangeRate(
            baseCurrency.ToUpperInvariant(),
            quoteCurrency.ToUpperInvariant(),
            rate);
    }

    public decimal Convert(decimal amount) => amount * Rate;
    public decimal ConvertBack(decimal amount) => amount / Rate;
    public ExchangeRate Invert() => new(QuoteCurrency, BaseCurrency, 1m / Rate);

    public string Pair => $"{BaseCurrency}/{QuoteCurrency}";

    public bool Equals(ExchangeRate? other) =>
        other is not null &&
        BaseCurrency == other.BaseCurrency &&
        QuoteCurrency == other.QuoteCurrency &&
        Rate == other.Rate;

    public override bool Equals(object? obj) => obj is ExchangeRate other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(BaseCurrency, QuoteCurrency, Rate);
    public override string ToString() => $"{Pair} = {Rate:F4}";

    internal static class DomainErrors
    {
        public static Error InvalidBaseCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ExchangeRate)}.{nameof(InvalidBaseCurrency)}",
                errorCurrentValue: value,
                errorMessage: "기준 통화 코드가 올바르지 않습니다.");
        public static Error InvalidQuoteCurrency(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ExchangeRate)}.{nameof(InvalidQuoteCurrency)}",
                errorCurrentValue: value,
                errorMessage: "견적 통화 코드가 올바르지 않습니다.");
        public static Error InvalidRate(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ExchangeRate)}.{nameof(InvalidRate)}",
                errorCurrentValue: value,
                errorMessage: "환율은 0보다 커야 합니다.");
        public static Error SameCurrency(string baseCurrency, string quoteCurrency) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ExchangeRate)}.{nameof(SameCurrency)}",
                baseCurrency, quoteCurrency,
                errorMessage: "기준 통화와 견적 통화는 동일할 수 없습니다.");
    }
}

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
