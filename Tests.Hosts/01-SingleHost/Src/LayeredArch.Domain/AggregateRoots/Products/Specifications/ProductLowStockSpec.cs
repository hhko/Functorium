using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace LayeredArch.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 재고 부족 Specification.
/// 재고가 Threshold 미만인 상품을 만족합니다.
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다.
/// </summary>
public sealed class ProductLowStockSpec : ExpressionSpecification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold)
    {
        Threshold = threshold;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        int threshold = Threshold;
        return product => (int)product.StockQuantity < threshold;
    }
}
