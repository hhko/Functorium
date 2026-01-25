using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace TwoWayMappingLayered.Domains.ValueObjects;

/// <summary>
/// 금액 Value Object
/// Two-Way Mapping: Domain Core에서 기술 의존성 없이 순수한 Value Object로 정의
///
/// 구현 패턴 (가이드 참조):
/// - ValueObject 상속 (복합 속성)
/// - Create: Validate(...).ToFin() 사용
/// - Validate: Apply 패턴으로 병렬 검증, 모든 오류 수집
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Money 생성 (Apply 패턴으로 병렬 검증)
    /// </summary>
    public static Fin<Money> Create(decimal amount, string currency) =>
        Validate(amount, currency).ToFin();

    /// <summary>
    /// Money 검증 (병렬 검증 - 모든 오류 수집)
    /// </summary>
    public static Validation<Error, Money> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => new Money(a, c))
            .As();

    /// <summary>
    /// Amount 검증
    /// </summary>
    private static Validation<Error, decimal> ValidateAmount(decimal amount) =>
        Validate<Money>.NonNegative(amount);

    /// <summary>
    /// Currency 검증 (ISO 4217 형식)
    /// </summary>
    private static Validation<Error, string> ValidateCurrency(string currency) =>
        Validate<Money>.NotEmpty(currency ?? "")
            .ThenExactLength(3)
            .ThenNormalize(v => v.ToUpperInvariant());

    /// <summary>
    /// 검증 없이 Money 생성 (복원 시 사용)
    /// 주의: 이미 검증된 값에만 사용해야 합니다.
    /// </summary>
    public static Money FromValues(decimal amount, string currency) =>
        new(amount, currency);

    /// <summary>
    /// 포맷된 금액 문자열
    /// Two-Way Mapping: 비즈니스 로직은 Domain에만 존재
    /// </summary>
    public string Formatted => $"{Amount:N2} {Currency}";

    /// <summary>
    /// 동등성 비교 컴포넌트
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
