using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductInStockSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => (int)product.Stock > 0;
    }
}
