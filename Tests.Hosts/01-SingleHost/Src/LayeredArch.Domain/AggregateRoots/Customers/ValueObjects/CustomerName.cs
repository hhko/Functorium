namespace LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

/// <summary>
/// 고객명 값 객체
/// </summary>
public sealed class CustomerName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private CustomerName(string value) : base(value) { }

    public static Fin<CustomerName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new CustomerName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<CustomerName>.NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());

    public static CustomerName CreateFromValidated(string value) => new(value);

    public static implicit operator string(CustomerName name) => name.Value;
}
