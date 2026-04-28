using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace EfCoreImpl.Specifications;

public sealed class ProductCategorySpec(string category) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Category == category;
}
