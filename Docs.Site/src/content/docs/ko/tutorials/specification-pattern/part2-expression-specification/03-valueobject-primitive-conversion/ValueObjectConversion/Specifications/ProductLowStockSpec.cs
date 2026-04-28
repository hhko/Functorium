using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ValueObjectConversion.Specifications;

public sealed class ProductLowStockSpec(Quantity threshold) : ExpressionSpecification<Product>
{
    public Quantity Threshold { get; } = threshold;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        int thresholdVal = Threshold;
        return product => (int)product.Stock <= thresholdVal;
    }
}
