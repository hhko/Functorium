using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace Cqrs03Functional.Demo.Domain.ValueObjects;

/// <summary>
/// 재고 수량을 나타내는 값 객체
/// </summary>
public sealed class StockQuantity : ComparableSimpleValueObject<int>
{
    private StockQuantity(int value) : base(value) { }

    /// <summary>
    /// 재고 수량 생성
    /// </summary>
    public static Fin<StockQuantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new StockQuantity(v));

    /// <summary>
    /// 재고 수량 검증
    /// - 0 이상
    /// </summary>
    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<StockQuantity>.NonNegative(value);

    public static implicit operator int(StockQuantity quantity) => quantity.Value;
}
