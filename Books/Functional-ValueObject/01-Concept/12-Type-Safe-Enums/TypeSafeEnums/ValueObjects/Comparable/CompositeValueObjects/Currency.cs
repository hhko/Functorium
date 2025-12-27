using Ardalis.SmartEnum;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace TypeSafeEnums.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 통화를 나타내는 SmartEnum 기반 값 객체
/// SmartEnum을 사용하여 타입 안전하고 강력한 통화 열거형 구현
/// ValueObject 규칙을 준수하여 구현
/// </summary>
public sealed class Currency 
    : SmartEnum<Currency, string>
    , IValueObject
{
    /// <summary>
    /// 한국 원화
    /// </summary>
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");

    /// <summary>
    /// 미국 달러
    /// </summary>
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");

    /// <summary>
    /// 유로
    /// </summary>
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "유로", "€");

    /// <summary>
    /// 일본 엔
    /// </summary>
    public static readonly Currency JPY = new(nameof(JPY), "JPY", "일본 엔", "¥");

    /// <summary>
    /// 중국 위안
    /// </summary>
    public static readonly Currency CNY = new(nameof(CNY), "CNY", "중국 위안", "¥");

    /// <summary>
    /// 영국 파운드
    /// </summary>
    public static readonly Currency GBP = new(nameof(GBP), "GBP", "영국 파운드", "£");

    /// <summary>
    /// 호주 달러
    /// </summary>
    public static readonly Currency AUD = new(nameof(AUD), "AUD", "호주 달러", "A$");

    /// <summary>
    /// 캐나다 달러
    /// </summary>
    public static readonly Currency CAD = new(nameof(CAD), "CAD", "캐나다 달러", "C$");

    /// <summary>
    /// 스위스 프랑
    /// </summary>
    public static readonly Currency CHF = new(nameof(CHF), "CHF", "스위스 프랑", "CHF");

    /// <summary>
    /// 싱가포르 달러
    /// </summary>
    public static readonly Currency SGD = new(nameof(SGD), "SGD", "싱가포르 달러", "S$");

    /// <summary>
    /// 통화의 한글 이름
    /// </summary>
    public string KoreanName { get; }

    /// <summary>
    /// 통화 기호
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Currency 인스턴스를 생성하는 private 생성자
    /// </summary>
    /// <param name="name">통화 이름</param>
    /// <param name="value">통화 코드 (ISO 4217)</param>
    /// <param name="koreanName">한글 이름</param>
    /// <param name="symbol">통화 기호</param>
    private Currency(string name, string value, string koreanName, string symbol) 
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }

    /// <summary>
    /// 통화 코드로부터 Currency 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    /// <param name="currencyCode">통화 코드</param>
    /// <returns>생성 결과</returns>
    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    /// <summary>
    /// 검증된 통화 코드로부터 Currency 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    /// <param name="currencyCode">검증된 통화 코드</param>
    /// <returns>생성된 Currency 인스턴스</returns>
    internal static Currency CreateFromValidated(string currencyCode) =>
        FromValue(currencyCode);

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
            ? DomainErrors.Empty(currencyCode)
            : currencyCode;

    /// <summary>
    /// 형식 검증
    /// </summary>
    /// <param name="currencyCode">검증할 통화 코드</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainErrors.NotThreeLetters(currencyCode)
            : currencyCode.ToUpperInvariant();

    /// <summary>
    /// 지원 여부 검증
    /// </summary>
    /// <param name="currencyCode">검증할 통화 코드</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateSupported(string currencyCode)
    {
        try
        {
            FromValue(currencyCode);
            return currencyCode;
        }
        catch (SmartEnumNotFoundException)
        {
            return DomainErrors.Unsupported(currencyCode);
        }
    }

    /// <summary>
    /// 지원되는 모든 통화 목록을 반환
    /// </summary>
    /// <returns>지원되는 통화 목록</returns>
    public static IEnumerable<Currency> GetAllSupportedCurrencies() => 
        List;

    /// <summary>
    /// 통화의 상세 정보를 문자열로 반환
    /// </summary>
    /// <returns>통화 상세 정보</returns>
    public override string ToString() => 
        $"{Value} ({KoreanName}) {Symbol}";

    /// <summary>
    /// 통화 코드만 반환
    /// </summary>
    /// <returns>통화 코드</returns>
    public string GetCode() => 
        Value;

    /// <summary>
    /// 통화 기호와 함께 금액을 포맷팅
    /// </summary>
    /// <param name="amount">금액</param>
    /// <returns>포맷팅된 금액 문자열</returns>
    public string FormatAmount(decimal amount) => 
        $"{Symbol}{amount:N2}";

    /// <summary>
    /// 통화 기호와 함께 금액을 포맷팅 (소수점 없이)
    /// </summary>
    /// <param name="amount">금액</param>
    /// <returns>포맷팅된 금액 문자열</returns>
    public string FormatAmountWithoutDecimals(decimal amount) => 
        $"{Symbol}{amount:N0}";

    /// <summary>
    /// DomainErrors 중첩 클래스
    /// ValueObject 규칙에 따른 구조화된 에러 처리
    /// </summary>
    internal static class DomainErrors
    {
        /// <summary>
        /// 빈 통화 코드 에러
        /// </summary>
        /// <param name="value">빈 통화 코드</param>
        /// <returns>에러</returns>
        public static Error Empty(string value) =>
            Error.New($"통화 코드는 비어있을 수 없습니다: {value}");

        /// <summary>
        /// 3자리 영문자가 아닌 통화 코드 에러
        /// </summary>
        /// <param name="value">잘못된 형식의 통화 코드</param>
        /// <returns>에러</returns>
        public static Error NotThreeLetters(string value) =>
            Error.New($"통화 코드는 3자리 영문자여야 합니다: {value}");

        /// <summary>
        /// 지원하지 않는 통화 코드 에러
        /// </summary>
        /// <param name="value">지원하지 않는 통화 코드</param>
        /// <returns>에러</returns>
        public static Error Unsupported(string value) =>
            Error.New($"지원하지 않는 통화 코드입니다: {value}");
    }
}