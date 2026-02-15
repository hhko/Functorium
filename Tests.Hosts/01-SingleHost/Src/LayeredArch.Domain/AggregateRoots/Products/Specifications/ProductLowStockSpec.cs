using Functorium.Domains.Specifications;

namespace LayeredArch.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 재고 부족 Specification.
/// 재고가 Threshold 미만인 상품을 만족합니다.
/// public 프로퍼티는 EfCore adapter에서 pattern-match로 SQL 최적화에 사용됩니다.
/// </summary>
public sealed class ProductLowStockSpec : Specification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold)
    {
        Threshold = threshold;
    }

    public override bool IsSatisfiedBy(Product product) =>
        product.StockQuantity < Threshold;
}
