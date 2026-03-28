using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using LanguageExt;
using LanguageExt.Common;

namespace OrderService.Domain.ValueObjects;

/// <summary>
/// 주문 수량을 나타내는 값 객체
/// </summary>
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    /// <summary>
    /// 주문 수량 생성
    /// </summary>
    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    /// <summary>
    /// 주문 수량 검증
    /// - 0보다 커야 함 (양수)
    /// </summary>
    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<Quantity>.Positive(value);

    public static implicit operator int(Quantity quantity) => quantity.Value;
}
