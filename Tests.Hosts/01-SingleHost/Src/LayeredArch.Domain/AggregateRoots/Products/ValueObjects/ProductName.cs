namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품명 값 객체
/// </summary>
public sealed class ProductName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private ProductName(string value) : base(value) { }

    public static Fin<ProductName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ProductName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ProductName>
            .NotEmpty(value ?? "")
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());

    public static ProductName CreateFromValidated(string value) => new(value);

    public static implicit operator string(ProductName productName) => productName.Value;
}
