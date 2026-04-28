using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using DomainError = Functorium.Domains.Errors.DomainError;
using DomainErrorKind = Functorium.Domains.Errors.DomainErrorKind;

namespace TypeSafeEnums.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// SimpleValueObject&lt;string&gt; + HashMap 패턴으로 구현한 통화 값 객체
/// SmartEnum 패키지 없이 순수 LanguageExt만으로 열거형 패턴 구현
/// </summary>
public sealed class CurrencyHashMap : SimpleValueObject<string>, IValueObject
{
    public sealed record Unsupported : DomainErrorKind.Custom;

    public static readonly CurrencyHashMap KRW = new("KRW", "한국 원화", "₩");
    public static readonly CurrencyHashMap USD = new("USD", "미국 달러", "$");
    public static readonly CurrencyHashMap EUR = new("EUR", "유로", "€");
    public static readonly CurrencyHashMap JPY = new("JPY", "일본 엔", "¥");
    public static readonly CurrencyHashMap CNY = new("CNY", "중국 위안", "¥");
    public static readonly CurrencyHashMap GBP = new("GBP", "영국 파운드", "£");
    public static readonly CurrencyHashMap AUD = new("AUD", "호주 달러", "A$");
    public static readonly CurrencyHashMap CAD = new("CAD", "캐나다 달러", "C$");
    public static readonly CurrencyHashMap CHF = new("CHF", "스위스 프랑", "CHF");
    public static readonly CurrencyHashMap SGD = new("SGD", "싱가포르 달러", "S$");

    private static readonly HashMap<string, CurrencyHashMap> All = HashMap(
        ("KRW", KRW), ("USD", USD), ("EUR", EUR), ("JPY", JPY), ("CNY", CNY),
        ("GBP", GBP), ("AUD", AUD), ("CAD", CAD), ("CHF", CHF), ("SGD", SGD));

    /// <summary>
    /// 통화의 한글 이름
    /// </summary>
    public string KoreanName { get; }

    /// <summary>
    /// 통화 기호
    /// </summary>
    public string Symbol { get; }

    private CurrencyHashMap(string value, string koreanName, string symbol)
        : base(value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }

    public static Fin<CurrencyHashMap> Create(string currencyCode) =>
        Validate(currencyCode).ToFin();

    public static CurrencyHashMap CreateFromValidated(string currencyCode) =>
        All.Find(currencyCode)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid currency code for CreateFromValidated: '{currencyCode}'"));

    public static Validation<Error, CurrencyHashMap> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainError.For<CurrencyHashMap>(
                new DomainErrorKind.Empty(),
                currencyCode ?? string.Empty,
                $"통화 코드는 비어있을 수 없습니다. Current value: '{currencyCode}'")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<CurrencyHashMap>(
                new DomainErrorKind.WrongLength(3),
                currencyCode,
                $"통화 코드는 3자리 영문자여야 합니다. Current value: '{currencyCode}'")
            : currencyCode.ToUpperInvariant();

    private static Validation<Error, CurrencyHashMap> ValidateSupported(string currencyCode) =>
        All.Find(currencyCode)
            .ToValidation(DomainError.For<CurrencyHashMap>(
                new Unsupported(),
                currencyCode,
                $"지원하지 않는 통화 코드입니다. Current value: '{currencyCode}'"));

    public static IEnumerable<CurrencyHashMap> GetAllSupportedCurrencies() =>
        All.Values;

    public override string ToString() =>
        $"{Value} ({KoreanName}) {Symbol}";

    public string GetCode() => Value;

    public string FormatAmount(decimal amount) =>
        $"{Symbol}{amount:N2}";

    public string FormatAmountWithoutDecimals(decimal amount) =>
        $"{Symbol}{amount:N0}";
}
