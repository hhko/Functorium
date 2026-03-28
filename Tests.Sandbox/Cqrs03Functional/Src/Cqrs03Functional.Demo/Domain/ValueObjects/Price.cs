using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace Cqrs03Functional.Demo.Domain.ValueObjects;

/// <summary>
/// 가격을 나타내는 값 객체
/// </summary>
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    private Price(decimal value) : base(value) { }

    /// <summary>
    /// 가격 생성
    /// </summary>
    public static Fin<Price> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Price(v));

    /// <summary>
    /// 가격 검증
    /// - 양수만 허용
    /// </summary>
    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Price>.Positive(value);

    public static implicit operator decimal(Price price) => price.Value;
}
