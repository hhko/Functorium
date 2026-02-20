namespace LayeredArch.Domain.SharedModels.ValueObjects;

/// <summary>
/// 금액 값 객체 (양수만 허용)
/// </summary>
public sealed class Money : ComparableSimpleValueObject<decimal>
{
    /// <summary>
    /// 합산 연산의 항등원 (Identity element for addition)
    /// </summary>
    public static readonly Money Zero = new(0m);

    private Money(decimal value) : base(value) { }

    public static Fin<Money> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Money(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Money>
            .Positive(value);

    public static Money CreateFromValidated(decimal value) => new(value);

    public static implicit operator decimal(Money money) => money.Value;

    public Money Add(Money other) => new(Value + other.Value);
    public Fin<Money> Subtract(Money other) => Create(Value - other.Value);
    public Money Multiply(decimal factor) => new(Value * factor);

    public static Money Sum(IEnumerable<Money> values) =>
        values.Aggregate(Zero, (acc, m) => acc.Add(m));
}
