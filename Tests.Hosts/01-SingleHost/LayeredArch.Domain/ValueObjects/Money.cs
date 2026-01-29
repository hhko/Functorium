namespace LayeredArch.Domain.ValueObjects;

/// <summary>
/// 금액 값 객체 (양수만 허용)
/// </summary>
public sealed class Money : ComparableSimpleValueObject<decimal>
{
    private Money(decimal value) : base(value) { }

    public static Fin<Money> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Money(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Money>.Positive(value);

    public static implicit operator decimal(Money money) => money.Value;

    public Money Add(Money other) => new(Value + other.Value);
    public Money Subtract(Money other) => new(Value - other.Value);
    public Money Multiply(decimal factor) => new(Value * factor);
}
