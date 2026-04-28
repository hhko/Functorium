using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ExpressionResolver.Specifications;

public sealed class ProductPriceRangeSpec(decimal min, decimal max) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Price >= min && product.Price <= max;
}
