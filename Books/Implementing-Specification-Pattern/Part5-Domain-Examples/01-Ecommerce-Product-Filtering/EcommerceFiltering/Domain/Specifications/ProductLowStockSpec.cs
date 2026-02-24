using System.Linq.Expressions;
using EcommerceFiltering.Domain.ValueObjects;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain.Specifications;

public sealed class ProductLowStockSpec : ExpressionSpecification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold) => Threshold = threshold;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        int threshold = Threshold;
        return product => (int)product.Stock < threshold;
    }
}
