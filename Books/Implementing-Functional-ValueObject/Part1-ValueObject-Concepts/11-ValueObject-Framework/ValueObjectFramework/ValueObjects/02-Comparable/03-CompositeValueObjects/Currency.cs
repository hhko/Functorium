using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 통화를 나타내는 기본 값 객체
/// SmartEnum 없이 기본 enum과 문자열 기반으로 구현하여 enum의 한계를 보여줌
/// ValueObject 규칙을 준수하여 구현
/// </summary>
public sealed class Currency : ComparableSimpleValueObject<string>
{
    /// <summary>
    /// 지원되는 통화 코드들
    /// </summary>
    public static readonly string[] SupportedCurrencies = 
    {
        "KRW", "USD", "EUR", "JPY", "CNY", "GBP", "AUD", "CAD", "CHF", "SGD"
    };

    /// <summary>
    /// 통화 코드와 한글 이름 매핑
    /// </summary>
    private static readonly Dictionary<string, string> CurrencyNames = new()
    {
        { "KRW", "한국 원화" },
        { "USD", "미국 달러" },
        { "EUR", "유로" },
        { "JPY", "일본 엔" },
        { "CNY", "중국 위안" },
        { "GBP", "영국 파운드" },
        { "AUD", "호주 달러" },
        { "CAD", "캐나다 달러" },
        { "CHF", "스위스 프랑" },
        { "SGD", "싱가포르 달러" }
    };

    /// <summary>
    /// 통화 코드와 기호 매핑
    /// </summary>
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        { "KRW", "₩" },
        { "USD", "$" },
        { "EUR", "€" },
        { "JPY", "¥" },
        { "CNY", "¥" },
        { "GBP", "£" },
        { "AUD", "A$" },
        { "CAD", "C$" },
        { "CHF", "CHF" },
        { "SGD", "S$" }
    };

    /// <summary>
    /// Currency 인스턴스를 생성하는 private 생성자
    /// </summary>
    /// <param name="currencyCode">통화 코드</param>
    private Currency(string currencyCode) 
        : base(currencyCode) 
    {
    }

    /// <summary>
    /// 통화 코드로부터 Currency 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    /// <param name="currencyCode">통화 코드</param>
    /// <returns>생성 결과</returns>
    public static Fin<Currency> Create(string currencyCode) =>
        CreateFromValidation(
            Validate(currencyCode),
            validCode => new Currency(validCode));

    /// <summary>
    /// 검증된 통화 코드로부터 Currency 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    /// <param name="currencyCode">검증된 통화 코드</param>
    /// <returns>생성된 Currency 인스턴스</returns>
    internal static Currency CreateFromValidated(string currencyCode) =>
        new Currency(currencyCode);

    /// <summary>
    /// 통화 코드 검증
    /// Bind 순차 검증 패턴 적용
    /// </summary>
    /// <param name="currencyCode">검증할 통화 코드</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    /// <summary>
    /// 빈 값 검증
    /// </summary>
    /// <param name="currencyCode">검증할 통화 코드</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? Error.New($"통화 코드는 비어있을 수 없습니다: {currencyCode}")
            : currencyCode;

    /// <summary>
    /// 형식 검증
    /// </summary>
    /// <param name="currencyCode">검증할 통화 코드</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? Error.New($"통화 코드는 3자리 영문자여야 합니다: {currencyCode}")
            : currencyCode.ToUpperInvariant();

    /// <summary>
    /// 지원 여부 검증
    /// </summary>
    /// <param name="currencyCode">검증할 통화 코드</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateSupported(string currencyCode) =>
        SupportedCurrencies.Contains(currencyCode)
            ? currencyCode
            : Error.New($"지원하지 않는 통화 코드입니다: {currencyCode}");

    /// <summary>
    /// 지원되는 모든 통화 목록을 반환
    /// </summary>
    /// <returns>지원되는 통화 목록</returns>
    public static IEnumerable<Currency> GetAllSupportedCurrencies() =>
        SupportedCurrencies.Select(code => new Currency(code));

    /// <summary>
    /// 통화의 상세 정보를 문자열로 반환
    /// </summary>
    /// <returns>통화 상세 정보</returns>
    public override string ToString() => 
        $"{Value} ({GetKoreanName()}) {GetSymbol()}";

    /// <summary>
    /// 통화 코드만 반환
    /// </summary>
    /// <returns>통화 코드</returns>
    public string GetCode() => Value;

    /// <summary>
    /// 통화의 한글 이름 반환
    /// </summary>
    /// <returns>한글 이름</returns>
    public string GetKoreanName() => 
        CurrencyNames.TryGetValue(Value, out var name) ? name : "알 수 없는 통화";

    /// <summary>
    /// 통화 기호 반환
    /// </summary>
    /// <returns>통화 기호</returns>
    public string GetSymbol() => 
        CurrencySymbols.TryGetValue(Value, out var symbol) ? symbol : "?";

    /// <summary>
    /// 통화 기호와 함께 금액을 포맷팅
    /// </summary>
    /// <param name="amount">금액</param>
    /// <returns>포맷팅된 금액 문자열</returns>
    public string FormatAmount(decimal amount) => 
        $"{GetSymbol()}{amount:N2}";

    /// <summary>
    /// 통화 기호와 함께 금액을 포맷팅 (소수점 없이)
    /// </summary>
    /// <param name="amount">금액</param>
    /// <returns>포맷팅된 금액 문자열</returns>
    public string FormatAmountWithoutDecimals(decimal amount) => 
        $"{GetSymbol()}{amount:N0}";

    // 비교 기능은 ComparableSimpleValueObject<string>에서 자동으로 제공됨:
    // - IComparable<Currency> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=, ==, !=)
    // - GetComparableEqualityComponents() 자동 구현
}
