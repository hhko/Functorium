using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace PropertyMapDemo.Specifications;

public sealed class ProductPriceRangeSpec(decimal min, decimal max) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Price >= min && p.Price <= max;
}
