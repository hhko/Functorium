namespace MyShop.Domain.AggregateRoots.Products.ValueObjects;

public sealed class ProductName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private ProductName(string value) : base(value) { }

    public static Fin<ProductName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ProductName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ProductName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());

    public static ProductName CreateFromValidated(string value) => new(value);
    public static implicit operator string(ProductName name) => name.Value;
}
