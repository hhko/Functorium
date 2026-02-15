using Functorium.Domains.Specifications;

namespace LayeredArch.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 가격 범위 Specification.
/// MinPrice 이상, MaxPrice 이하인 상품을 만족합니다.
/// public 프로퍼티는 EfCore adapter에서 pattern-match로 SQL 최적화에 사용됩니다.
/// </summary>
public sealed class ProductPriceRangeSpec : Specification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money minPrice, Money maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override bool IsSatisfiedBy(Product product) =>
        product.Price >= MinPrice && product.Price <= MaxPrice;
}
