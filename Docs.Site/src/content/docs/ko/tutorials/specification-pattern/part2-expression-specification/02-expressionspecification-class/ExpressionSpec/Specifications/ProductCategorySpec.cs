using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ExpressionSpec.Specifications;

public sealed class ProductCategorySpec(string category) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Category == category;
}
