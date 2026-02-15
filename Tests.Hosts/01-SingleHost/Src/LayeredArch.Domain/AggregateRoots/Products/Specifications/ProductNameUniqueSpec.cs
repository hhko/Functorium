using Functorium.Domains.Specifications;

namespace LayeredArch.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 상품명 중복 확인 Specification.
/// ExcludeId가 지정되면 해당 상품은 제외하고 검사합니다 (업데이트 시 자기 자신 제외).
/// </summary>
public sealed class ProductNameUniqueSpec : Specification<Product>
{
    public ProductName Name { get; }
    public ProductId? ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, ProductId? excludeId = null)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override bool IsSatisfiedBy(Product product) =>
        (string)product.Name == (string)Name &&
        (ExcludeId is null || product.Id != ExcludeId.Value);
}
