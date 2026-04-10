using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductCategorySpec(Category category) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        string categoryStr = category;
        return product => (string)product.Category == categoryStr;
    }
}
