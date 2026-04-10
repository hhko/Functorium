using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace ValueObjectConversion.Specifications;

public sealed class ProductPriceRangeSpec(Money min, Money max) : ExpressionSpecification<Product>
{
    public Money MinPrice { get; } = min;
    public Money MaxPrice { get; } = max;

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal minVal = MinPrice;
        decimal maxVal = MaxPrice;
        return product => (decimal)product.Price >= minVal && (decimal)product.Price <= maxVal;
    }
}
