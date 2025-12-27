using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace TypeSafeEnums.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 가격을 나타내는 복합 값 객체
/// MoneyAmount와 Currency를 조합한 ComparableValueObject 기반 구현
/// ValueObject 규칙을 준수하여 구현
/// </summary>
public sealed class Price : ComparableValueObject
{
    /// <summary>
    /// 금액
    /// </summary>
    public MoneyAmount Amount { get; }

    /// <summary>
    /// 통화
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Price 인스턴스를 생성하는 private 생성자
    /// </summary>
    /// <param name="amount">금액</param>
    /// <param name="currency">통화</param>
    private Price(MoneyAmount amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// 가격 값 객체 생성
    /// </summary>
    /// <param name="amount">금액</param>
    /// <param name="currency">통화 코드</param>
    /// <returns>성공 시 Price 값 객체, 실패 시 에러</returns>
    public static Fin<Price> Create(decimal amount, string currency) =>
        CreateFromValidation(
            Validate(amount, currency),
            validValues => new Price(validValues.Amount, validValues.Currency));

    /// <summary>
    /// 이미 검증된 가격으로 값 객체 생성
    /// </summary>
    /// <param name="validatedValues">검증된 가격 값들</param>
    /// <returns>Price 값 객체</returns>
    internal static Price CreateFromValidated((MoneyAmount Amount, Currency Currency) validatedValues) =>
        new Price(validatedValues.Amount, validatedValues.Currency);

    /// <summary>
    /// 가격 유효성 검증
    /// </summary>
    /// <param name="amount">금액</param>
    /// <param name="currency">통화 코드</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (MoneyAmount Amount, Currency Currency)> Validate(decimal amount, string currency) =>
        from validAmount in MoneyAmount.Validate(amount)
        from validCurrency in Currency.Validate(currency)
        select (Amount: MoneyAmount.CreateFromValidated(validAmount), 
                Currency: Currency.CreateFromValidated(validCurrency));

    /// <summary>
    /// 비교 가능한 구성 요소 반환
    /// 통화를 먼저 비교하고, 통화가 같으면 금액으로 비교
    /// </summary>
    /// <returns>비교 가능한 구성 요소</returns>
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Currency.Value;    // 통화를 먼저 비교
        yield return (decimal)Amount;   // 금액을 나중에 비교
    }

    /// <summary>
    /// 다른 가격과 비교 가능한지 확인
    /// </summary>
    /// <param name="other">비교할 가격</param>
    /// <returns>같은 통화이면 true, 다른 통화이면 false</returns>
    public bool CanCompareWith(Price other) => 
        Currency.Equals(other.Currency);

    /// <summary>
    /// 가격의 문자열 표현
    /// </summary>
    /// <returns>가격의 문자열 표현</returns>
    public override string ToString() => 
        $"{Currency} {Amount}";

    // 비교 기능은 ComparableValueObject에서 자동으로 제공됨:
    // - IComparable<Price> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=, ==, !=)
    // - GetComparableEqualityComponents() 자동 구현
}
