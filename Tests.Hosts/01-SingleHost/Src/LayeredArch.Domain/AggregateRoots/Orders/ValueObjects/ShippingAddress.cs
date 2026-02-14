namespace LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;

/// <summary>
/// 배송지 주소 값 객체
/// </summary>
public sealed class ShippingAddress : SimpleValueObject<string>
{
    public const int MaxLength = 500;

    private ShippingAddress(string value) : base(value) { }

    public static Fin<ShippingAddress> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ShippingAddress(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ShippingAddress>.NotEmpty(value ?? "")
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());

    public static ShippingAddress CreateFromValidated(string value) => new(value);

    public static implicit operator string(ShippingAddress address) => address.Value;
}
