using Ardalis.SmartEnum;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 통화를 나타내는 SmartEnum 기반 값 객체
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Currency
    : SmartEnum<Currency, string>
    , IValueObject
{
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "유로", "€");
    public static readonly Currency JPY = new(nameof(JPY), "JPY", "일본 엔", "¥");
    public static readonly Currency CNY = new(nameof(CNY), "CNY", "중국 위안", "¥");
    public static readonly Currency GBP = new(nameof(GBP), "GBP", "영국 파운드", "£");
    public static readonly Currency AUD = new(nameof(AUD), "AUD", "호주 달러", "A$");
    public static readonly Currency CAD = new(nameof(CAD), "CAD", "캐나다 달러", "C$");
    public static readonly Currency CHF = new(nameof(CHF), "CHF", "스위스 프랑", "CHF");
    public static readonly Currency SGD = new(nameof(SGD), "SGD", "싱가포르 달러", "S$");

    public string KoreanName { get; }
    public string Symbol { get; }

    private Currency(string name, string value, string koreanName, string symbol)
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }

    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Currency CreateFromValidated(string currencyCode) =>
        FromValue(currencyCode);

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "",
                $"Currency code cannot be empty. Current value: '{currencyCode}'")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<Currency>(new DomainErrorType.WrongLength(3), currencyCode,
                $"Currency code must be exactly 3 letters. Current value: '{currencyCode}'")
            : currencyCode.ToUpperInvariant();

    private static Validation<Error, string> ValidateSupported(string currencyCode)
    {
        try
        {
            FromValue(currencyCode);
            return currencyCode;
        }
        catch (SmartEnumNotFoundException)
        {
            return DomainError.For<Currency>(new DomainErrorType.Custom("Unsupported"), currencyCode,
                $"Currency code is not supported. Current value: '{currencyCode}'");
        }
    }

    public static IEnumerable<Currency> GetAllSupportedCurrencies() =>
        List;

    public override string ToString() =>
        $"{Value} ({KoreanName}) {Symbol}";

    public string GetCode() => Value;

    public string FormatAmount(decimal amount) =>
        $"{Symbol}{amount:N2}";

    public string FormatAmountWithoutDecimals(decimal amount) =>
        $"{Symbol}{amount:N0}";
}
