using Functorium.Domains.ValueObjects;

namespace Cqrs02Pipeline.Demo.Domain.ValueObjects;

/// <summary>
/// 상품명을 나타내는 값 객체
/// </summary>
public sealed class ProductName : SimpleValueObject<string>
{
    private ProductName(string value) : base(value) { }

    /// <summary>
    /// 상품명 생성
    /// </summary>
    public static Fin<ProductName> Create(string value) =>
        CreateFromValidation(Validate(value), v => new ProductName(v));

    /// <summary>
    /// 상품명 검증
    /// - 빈 값 불가
    /// - 최대 100자
    /// </summary>
    public static Validation<Error, string> Validate(string value) =>
        Validate<ProductName>.NotEmpty(value)
            .ThenMaxLength(100);
}
