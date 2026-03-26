namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 설명 값 객체
/// </summary>
public sealed class ProductDescription : SimpleValueObject<string>
{
    public const int MaxLength = 1000;

    private ProductDescription(string value) : base(value) { }

    public static Fin<ProductDescription> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ProductDescription(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ProductDescription>
            .NotNull(value)
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static ProductDescription CreateFromValidated(string value) => new(value);

    public static implicit operator string(ProductDescription desc) => desc.Value;
}
