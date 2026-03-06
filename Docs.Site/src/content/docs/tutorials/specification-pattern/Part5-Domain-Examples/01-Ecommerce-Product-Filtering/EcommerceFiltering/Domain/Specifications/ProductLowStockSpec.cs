using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductLowStockSpec(Quantity threshold) : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        int value = threshold;
        return product => (int)product.Stock < value;
    }
}
