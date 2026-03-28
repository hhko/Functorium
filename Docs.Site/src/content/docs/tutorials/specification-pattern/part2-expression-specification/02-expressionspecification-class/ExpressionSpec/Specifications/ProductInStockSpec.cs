using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ExpressionSpec.Specifications;

public sealed class ProductInStockSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Stock > 0;
}
