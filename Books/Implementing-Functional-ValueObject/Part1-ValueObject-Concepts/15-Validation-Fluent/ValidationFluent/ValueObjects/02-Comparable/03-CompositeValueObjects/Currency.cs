using Ardalis.SmartEnum;
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 통화를 나타내는 SmartEnum 기반 값 객체
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
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

    private static readonly System.Collections.Generic.HashSet<string> SupportedCodes =
        new(List.Select(c => c.Value), StringComparer.OrdinalIgnoreCase);

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
        Validate<Currency>.NotEmpty(currencyCode ?? "")
            .ThenExactLength(3)
            .ThenNormalize(v => v.ToUpperInvariant())
            .ThenMust(
                v => SupportedCodes.Contains(v),
                new DomainErrorType.Custom("Unsupported"),
                v => $"Currency '{v}' is not supported");

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
