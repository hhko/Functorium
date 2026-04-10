using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ECommerce.Domain.AggregateRoots.Products.Specifications;

/// <summary>
/// 상품명 검색 Specification.
/// 지정된 이름과 일치하는 상품을 만족합니다.
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다.
/// </summary>
public sealed class ProductNameSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }

    public ProductNameSpec(ProductName name) => Name = name;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        return product => (string)product.Name == nameStr;
    }
}
