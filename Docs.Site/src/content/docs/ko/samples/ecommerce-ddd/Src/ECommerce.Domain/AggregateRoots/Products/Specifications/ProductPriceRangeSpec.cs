using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ECommerce.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 가격 범위 Specification.
/// MinPrice 이상, MaxPrice 이하인 상품을 만족합니다.
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다.
/// </summary>
public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money minPrice, Money maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal min = MinPrice;
        decimal max = MaxPrice;
        return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
    }
}
