using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ValueObjectConversion.Specifications;

public sealed class ProductLowStockSpec : ExpressionSpecification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold) => Threshold = threshold;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        int threshold = Threshold;
        return product => (int)product.Stock <= threshold;
    }
}
