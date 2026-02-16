using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace LayeredArch.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 상품명 중복 확인 Specification.
/// ExcludeId가 지정되면 해당 상품은 제외하고 검사합니다 (업데이트 시 자기 자신 제외).
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다.
/// </summary>
public sealed class ProductNameUniqueSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }
    public ProductId? ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, ProductId? excludeId = null)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        string? excludeIdStr = ExcludeId?.ToString();
        return product => (string)product.Name == nameStr &&
                          (excludeIdStr == null || product.Id.ToString() != excludeIdStr);
    }
}
