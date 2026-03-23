namespace MyShop.Domain.AggregateRoots.Products.ValueObjects;

public sealed class Money : ComparableSimpleValueObject<decimal>
{
    public static readonly Money Zero = new(0m);

    private Money(decimal value) : base(value) { }

    public static Fin<Money> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Money(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Money>.Positive(value);

    public static Money CreateFromValidated(decimal value) => new(value);
    public static implicit operator decimal(Money money) => money.Value;
}
