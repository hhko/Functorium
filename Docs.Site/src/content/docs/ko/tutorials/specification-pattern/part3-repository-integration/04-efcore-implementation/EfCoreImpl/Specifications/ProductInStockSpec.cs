using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace EfCoreImpl.Specifications;

public sealed class ProductInStockSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Stock > 0;
}
