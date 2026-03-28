using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace Cqrs04Endpoint.WebApi.Domain.ValueObjects;

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
        ValidationRules<ProductName>.NotEmpty(value)
            .ThenMaxLength(100);

    public static implicit operator string(ProductName name) => name.ToString();
}
