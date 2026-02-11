namespace LayeredArch.Domain.SharedKernel.ValueObjects;

/// <summary>
/// 수량 값 객체 (0 이상)
/// </summary>
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<Quantity>.NonNegative(value);

    public static implicit operator int(Quantity quantity) => quantity.Value;

    public Quantity Add(int amount) => new(Value + amount);
    public Quantity Subtract(int amount) => new(Math.Max(0, Value - amount));
}
