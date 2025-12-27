using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ValueObjectFramework.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 금액을 나타내는 비교 가능한 값 객체
/// ComparableSimpleValueObject<decimal> 기반으로 구현
/// ValueObject 규칙을 준수하여 구현
/// </summary>
public sealed class MoneyAmount : ComparableSimpleValueObject<decimal>
{
    /// <summary>
    /// MoneyAmount 인스턴스를 생성하는 private 생성자
    /// </summary>
    /// <param name="value">금액 값</param>
    private MoneyAmount(decimal value) 
        : base(value) 
    {
    }

    /// <summary>
    /// 금액 값 객체 생성
    /// </summary>
    /// <param name="value">금액</param>
    /// <returns>성공 시 MoneyAmount 값 객체, 실패 시 에러</returns>
    public static Fin<MoneyAmount> Create(decimal value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new MoneyAmount(validValue));

    /// <summary>
    /// 이미 검증된 금액으로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 금액</param>
    /// <returns>MoneyAmount 값 객체</returns>
    internal static MoneyAmount CreateFromValidated(decimal validatedValue) =>
        new MoneyAmount(validatedValue);

    /// <summary>
    /// 금액 유효성 검증
    /// </summary>
    /// <param name="value">검증할 금액</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, decimal> Validate(decimal value) =>
        value >= 0 && value <= 999999.99m
            ? value
            : Error.New($"금액은 0 이상 999,999.99 이하여야 합니다: {value}");

    /// <summary>
    /// 금액의 문자열 표현
    /// </summary>
    /// <returns>금액의 문자열 표현</returns>
    public override string ToString() => 
        $"{Value:N2}";

    // 비교 기능은 ComparableSimpleValueObject<decimal>에서 자동으로 제공됨:
    // - IComparable<MoneyAmount> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=, ==, !=)
    // - GetComparableEqualityComponents() 자동 구현
}
